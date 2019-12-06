using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using Microsoft.AspNetCore.Http;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
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
    public class PDFEditorService
    {
        private static string[] _imagesExtensions = { ".jpg", ".png", ".gif", ".tiff", ".bpm" };

        /// <summary>
        ///     Converts Image(s) to PDF document
        /// </summary>
        /// <param name="formFiles"></param>
        /// <returns>PDF document stream</returns>
        public async Task<MemoryStream> ConvertImagesToPDF(IFormFileCollection formFiles)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            PdfDocument pdfDocument = new PdfDocument();
            foreach (var file in formFiles)
            {
                if (!IsFileAnImage(file))
                {
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
