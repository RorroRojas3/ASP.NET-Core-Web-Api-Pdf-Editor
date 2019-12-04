using Microsoft.AspNetCore.Http;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdf_editor_api.Service
{
    public class PDFEditorService
    {
        public async Task<MemoryStream> ConvertImagesToPDF(IFormFileCollection formFiles)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            PdfDocument pdfDocument = new PdfDocument();
            foreach (var file in formFiles)
            {
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

        private Stream GetStream(IFormFile file)
        {
            MemoryStream memoryStream = new MemoryStream();
            file.CopyToAsync(memoryStream).GetAwaiter().GetResult();
            return memoryStream;
        }
    }
}
