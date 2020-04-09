using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using Microsoft.AspNetCore.Http;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace pdf_editor_api.Service
{
    /// <summary>
    /// 
    /// </summary>
    public class PDFEditorService
    {
        private static string[] _imagesExtensions = { ".jpg", ".png", ".gif", ".tiff", ".bpm" };
        private readonly ILogger _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public PDFEditorService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        ///     Converts Image(s) to PDF document
        /// </summary>
        /// <param name="formFiles"></param>
        /// <returns>PDF document stream</returns>
        public async Task<MemoryStream> ConvertImagesToPDF(IFormFileCollection formFiles)
        {
            _logger.Information("ConverImagesToPdf started");
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            PdfDocument pdfDocument = new PdfDocument();
            foreach (var file in formFiles)
            {
                if (!IsFileAnImage(file))
                {
                    _logger.Information("File was not an Image");
                    return null;
                }

                MemoryStream memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                PdfPage page = pdfDocument.Pages.Add();
                XGraphics xGraphics = XGraphics.FromPdfPage(page);
                XImage xImage = XImage.FromStream(memoryStream);
                xGraphics.DrawImage(xImage, 0, 0, 612, 792);
            }

            MemoryStream resultMemoryStream = new MemoryStream();
            pdfDocument.Save(resultMemoryStream);
            return resultMemoryStream;
        }

        /// <summary>
        ///     Converts PDF to Images and compresses them into a ZIP file
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="imageFormat"></param>
        /// <returns>Array of bytes of ZIP file</returns>
        public async Task<byte[]> PDFToImages(IFormFile formFile, string imageFormat)
        {
            _logger.Information("PdfToImages started");

            // Gets DLL of GhostScritp
            string binPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string gsDllPath = Path.Combine(binPath, Environment.Is64BitProcess ? "gsdll64.dll" : "gsdll32.dll");

            // Set DPI (User will eventually chose)
            int xDPI = 300;
            int yDPI = 300;

            // Get Image format
            ImageFormat imageFormatExtension = GetImageFormat(imageFormat);

            if (imageFormatExtension == null)
            {
                _logger.Information("Image format not supported");
                return null;
            }

            // PDF to desired image(s)
            GhostscriptVersionInfo gsVersion = new GhostscriptVersionInfo(gsDllPath);
            GhostscriptRasterizer rasterizer = new GhostscriptRasterizer();

            // Get stream of file
            MemoryStream memoryStream = new MemoryStream();
            await formFile.CopyToAsync(memoryStream);
            rasterizer.Open(memoryStream, gsVersion, false);

            // Converts PDF pages into images and compresses them into a ZIP file
            var archiveStream = new MemoryStream();
            var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true);
            for (var i = 1; i <= rasterizer.PageCount; i++)
            {
                MemoryStream imageStream = new MemoryStream();
                Image img = rasterizer.GetPage(xDPI, yDPI, i);
                img.Save(imageStream, imageFormatExtension);
                var zipArchiveEntry = archive.CreateEntry($"Page-{i}.{imageFormatExtension.ToString().ToLower()}", CompressionLevel.Optimal);
                var zipStream = zipArchiveEntry.Open();
                var imageBytes = imageStream.ToArray();
                await zipStream.WriteAsync(imageBytes, 0, imageBytes.Length);
                zipStream.Close();
            }

            // Closes GhostScript Rasterizer and closes Stream for archive
            _logger.Information("PdfToImage - Zip file created");
            rasterizer.Close();
            archive.Dispose();

            return archiveStream.ToArray();
        }

        /// <summary>
        ///     Removes pages from PDF
        /// </summary>
        /// <returns>Stream with wanted PDF content</returns>
        public async Task<Stream> RemovePagesFromPDF(IFormFile file, List<int> pages)
        {
            _logger.Information("RemovePagesFromPdf started");
            // Open PDF file
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            PdfDocument pdfDocument = PdfReader.Open(file.OpenReadStream(), PdfDocumentOpenMode.Import);
            PdfDocument newPdfDocument = new PdfDocument();

            // Add wanted pages from PDF into new PDF document
            for(int i = 0; i < pdfDocument.PageCount; i++)
            {
                if (!pages.Contains(i + 1))
                {
                    newPdfDocument.AddPage(pdfDocument.Pages[i]);
                }
            }

            // Create and return stream of new PDF created
            MemoryStream newPdfStream = new MemoryStream();
            newPdfDocument.Save(newPdfStream, false);
            _logger.Information("RemovePagesFromPdg - Pdf stream created");
            return newPdfStream;
        }

        /// <summary>
        ///     Goes through each PDF and extracts all pages from it
        ///     and puts it into a single PDF document
        /// </summary>
        /// <param name="formFiles"></param>
        /// <returns>Merge PDF stream</returns>
        public async Task<Stream> MergePDF(IFormFileCollection formFiles)
        {
            _logger.Information("MergePdf started");
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            PdfDocument pdfDocument = new PdfDocument();
            MemoryStream pdfStream = new MemoryStream();

            foreach(var file in formFiles)
            {
                PdfDocument tempPdf = PdfReader.Open(file.OpenReadStream(), PdfDocumentOpenMode.Import);
                for(var i = 0; i < tempPdf.PageCount; i++)
                {
                    pdfDocument.AddPage(tempPdf.Pages[i]);
                }
            }

            pdfDocument.Save(pdfStream, false);
            _logger.Information("MergePdf - Pdf stream created");
            return pdfStream;
        }

        /// <summary>
        ///     Splits PDF in fixed ranges.
        ///     If there are remaining pages based on the fixed split
        ///     they will be stored in last PDF file on ZIP
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="range"></param>
        /// <returns>Byte array with ZIP file containing PDFs</returns>
        public async Task<byte[]> SplitPDFByRange(IFormFile formFile, string range)
        {
            _logger.Information("SplitPdfByRange started");
            // Register encoding and open PDF file
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            PdfDocument pdfDocument = PdfReader.Open(formFile.OpenReadStream(), PdfDocumentOpenMode.Import);

            // Converts PDF pages into images and compresses them into a ZIP file
            var archiveStream = new MemoryStream();
            var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true);

            // Checks if input is a valid integer
            int totalRange;
            bool rangeParsed = int.TryParse(range, out totalRange);
            if (!rangeParsed)
            {
                _logger.Information("SplitPdfByRange - Invalid range");
                return null;
            }

            // Splits the PDF by range
            int j = 0;
            for (var i = 0; i <= pdfDocument.PageCount / totalRange; i++)
            {
                PdfDocument newPdf = new PdfDocument();
                MemoryStream tempPdfStream = new MemoryStream();
                for(var k = 0; k < totalRange; k++)
                {
                    if (j < pdfDocument.PageCount)
                    {
                        newPdf.AddPage(pdfDocument.Pages[j]);
                    }
                    else
                    {
                        break;
                    }
                    j++;
                }

                if (j == pdfDocument.PageCount)
                {
                    break;
                }
                
                // Creates ZIP file and stores PDF splitted into new PDF files
                var zipArchiveEntry = archive.CreateEntry($"PDF-Range-{i}.pdf", CompressionLevel.Optimal);
                var zipStream = zipArchiveEntry.Open();
                newPdf.Save(tempPdfStream);
                byte[] newPdfBytes = tempPdfStream.ToArray();
                await zipStream.WriteAsync(newPdfBytes, 0, newPdfBytes.Length);
                zipStream.Close();
            }

            archive.Dispose();
            _logger.Information("SplitPdfByRange - Zip file created");
            return archiveStream.ToArray();
        }

        /// <summary>
        ///     Splits PDF and creates new PDF with selected range
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="range"></param>
        /// <returns>Stream of selected pages of Pdf</returns>
        public async Task<Stream> SplitPDFByCustomRange(IFormFile formFile, string range)
        {
            _logger.Information("SplitPdfByCustomRange started");

            // Register encoding and open PDF file
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            PdfDocument pdfDocument = PdfReader.Open(formFile.OpenReadStream(), PdfDocumentOpenMode.Import);

            // Get starting and ending page
            string[] rangeSplitted = range.Split("-");

            // Checks if inputs are valid integers
            int startPage;
            int lastPage;
            bool isStartPage = int.TryParse(rangeSplitted[0], out startPage);
            bool isLastPage = int.TryParse(rangeSplitted[1], out lastPage);
            if (!isStartPage || !isLastPage)
            {
                _logger.Information("SplitPdfByCustomRange - Start/Last page not valid");
                return null;
            }

            // Create new PDF document
            PdfDocument newPdf = new PdfDocument();
            MemoryStream newPdfStream = new MemoryStream();
            startPage -= 1;
            lastPage -= 1;
            for (var i = startPage; i <= lastPage; i++)
            {
                newPdf.AddPage(pdfDocument.Pages[i]);
            }

            newPdf.Save(newPdfStream, false);
            _logger.Information("SplitPdfByCustomRange - Pdf stream created");
            return newPdfStream;
        }

        /// <summary>
        ///     Determines if file is an image
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private bool IsFileAnImage(IFormFile file)
        {
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            return _imagesExtensions.Contains(fileExtension) ? true : false;
        }

        private ImageFormat GetImageFormat(string imageFormat)
        {
            if (imageFormat.Equals("jpg", StringComparison.OrdinalIgnoreCase) || imageFormat.Equals("jpeg", StringComparison.OrdinalIgnoreCase))
            {
                return ImageFormat.Jpeg;
            }
            else if (imageFormat.Equals("png", StringComparison.OrdinalIgnoreCase))
            {
                return ImageFormat.Png;
            }
            else if (imageFormat.Equals("tiff", StringComparison.OrdinalIgnoreCase))
            {
                return ImageFormat.Tiff;
            }
            else if (imageFormat.Equals("gif", StringComparison.OrdinalIgnoreCase))
            {
                return ImageFormat.Gif;
            }
            else if (imageFormat.Equals("bmp", StringComparison.OrdinalIgnoreCase))
            {
                return ImageFormat.Bmp;
            }
            else
            {
                return null;
            }
        }
    }
}
