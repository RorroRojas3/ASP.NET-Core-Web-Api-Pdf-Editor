﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
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
            catch(Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        ///     Converts PDF to Images and compresses them into a ZIP file
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="imageFormat"></param>
        /// <returns>ZIP File</returns>
        [HttpPost]
        [Route("PDFToImages/{imageFormat}")]
        public async Task<IActionResult> PDFToImages(IFormFile formFile, string imageFormat)
        {

            IFormFile file = HttpContext.Request.Form.Files.FirstOrDefault();

            if (string.IsNullOrEmpty(imageFormat) || file == null)
            {
                return StatusCode(StatusCodes.Status406NotAcceptable, "Missing imageFormat parameter OR PDF file");
            }

            try
            {
                var zipFile = await _pdfEditorService.PDFToImages(file, imageFormat);

                if (zipFile == null)
                {
                    return StatusCode(StatusCodes.Status406NotAcceptable, "ImageFormat not acceptable");
                }

                return new FileContentResult(zipFile, "application/zip") { FileDownloadName = "PDFToImages.zip" };
            }
            catch(Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        ///     Removes pages from PDF
        /// </summary>
        /// <param name="pages"></param>
        /// <returns>PDF with removed pages</returns>
        [HttpPost]
        [Route("RemovePages")]
        public async Task<IActionResult> RemovePages([FromForm] string pages)
        {
            IFormFile formFile = HttpContext.Request.Form.Files.FirstOrDefault();

            if (pages.Length == 0 || formFile == null)
            {
                return StatusCode(StatusCodes.Status406NotAcceptable, "File not provided OR Pages parameter not in body");
            }

            try
            {
                // Parse string for integers
                var splitPages = pages.Split(",");
                List<int> parsedPage = new List<int>();

                // Parse string into integers
                int currentPage;
                foreach(var page in splitPages)
                {
                    bool isInt = int.TryParse(page, out currentPage);
                    if (isInt)
                    {
                        parsedPage.Add(int.Parse(page));
                    }   
                }

                // Get the PDF stream with wanted PDF pages
                Stream pdfStream = await _pdfEditorService.RemovePagesFromPDF(formFile, parsedPage);
                return new FileStreamResult(pdfStream, "application/pdf");
            }
            catch(Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        /// <summary>
        ///     Merged PDF
        /// </summary>
        /// <returns>Merge PDF file</returns>
        [HttpPost]
        [Route("MergePDF")]
        public async Task<IActionResult> MergePDF()
        {
            IFormFileCollection formFiles = HttpContext.Request.Form.Files;

            if (formFiles.Count <= 0)
            {
                return StatusCode(StatusCodes.Status406NotAcceptable, "No files sent");
            }

            try
            {
                Stream pdf = await _pdfEditorService.MergePDF(formFiles);
                return new FileStreamResult(pdf, "application/pdf");
            }
            catch(Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        [Route("SplitPDF/ByRange/{range}")]
        public async Task<IActionResult> SplitPDFByRange(string range)
        {
            IFormFile formFile = HttpContext.Request.Form.Files.FirstOrDefault();

            if (formFile == null || string.IsNullOrEmpty(range))
            {
                return StatusCode(StatusCodes.Status406NotAcceptable, "No file OR range sent"); 
            }

            try
            {
                var zipFile = await _pdfEditorService.SplitPDFByRange(formFile, range);

                if (zipFile == null)
                {
                    return StatusCode(StatusCodes.Status406NotAcceptable, "ImageFormat not acceptable");
                }

                return new FileContentResult(zipFile, "application/zip") { FileDownloadName = "PDFSplitByRange.zip" };
            }
            catch(Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

    }
}
