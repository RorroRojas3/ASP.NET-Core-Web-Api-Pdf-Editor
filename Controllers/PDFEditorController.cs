using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using pdf_editor_api.Responses;
using pdf_editor_api.Service;
using PdfSharp.Pdf;

namespace pdf_editor_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PDFEditorController : Controller
    {
        private readonly PDFEditorService _pdfEditorService;

        public PDFEditorController(PDFEditorService pdfEditorService)
        {
            _pdfEditorService = pdfEditorService;
        }

        [HttpPost]
        [Route("ImagesToPdf")]
        public async Task<IActionResult> ImagesToPDF()
        {
            var formFiles = HttpContext.Request.Form.Files;
            if (formFiles.Count <= 0)
            {
                return null; //new StandardResponse<PdfDocument>(HttpStatusCode.NotAcceptable, "No input file", null);
            }


            try
            {
                var pdf = await _pdfEditorService.ConvertImagesToPDF(formFiles);
                return new FileStreamResult(pdf, "application/pdf");
            }
            catch(Exception ex)
            {
                return null; // new StandardResponse<PdfDocument>(HttpStatusCode.InternalServerError, ex.Message, null);
            }
        }
    }
}
