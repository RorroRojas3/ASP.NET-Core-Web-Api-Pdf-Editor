using Microsoft.AspNetCore.Http;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mime;
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
        ///     Determines if file is an image
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private bool IsFileAnImage(IFormFile file)
        {
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            return _imagesExtensions.Contains(fileExtension) ? true : false;
        }
    }
}
