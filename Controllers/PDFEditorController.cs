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

        /// <summary>
        ///     Converts Image(s) to a single PDF file
        /// </summary>
        /// <returns>Returns PDF file with converted images</returns>
        [HttpPost]
        [Route("ImagesToPdf")]
        public async Task<IActionResult> ImagesToPDF()
        {
            var formFiles = HttpContext.Request.Form.Files;
            
            if (formFiles.Count <= 0)
            {
                return StatusCode(StatusCodes.Status406NotAcceptable, "No files on request");
            }

            try
            {
                var pdf = await _pdfEditorService.ConvertImagesToPDF(formFiles);

                if(pdf == null)
                {
                    return StatusCode(StatusCodes.Status406NotAcceptable, "File(s) received are not images");
                }

                return new FileStreamResult(pdf, "application/pdf");
            }
            catch(Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error on converting Images to PDF");
            }
        }
    }
}
