using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using pdf_editor_api.Service;
using Serilog;

namespace pdf_editor_api.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/Pdf")]
    [ApiVersion("1")]
    public class PDFEditorController : ControllerBase
    {
        /// <summary>
        ///     Private variables 
        /// </summary>
        private readonly PDFEditorService _pdfEditorService;
        private readonly ILogger _logger;

        /// <summary>
        ///     Constructor for PdfEditorController with DI
        /// </summary>
        /// <param name="pdfEditorService"></param>
        /// <param name="logger"></param>
        public PDFEditorController(PDFEditorService pdfEditorService,
                                    ILogger logger)
        {
            _pdfEditorService = pdfEditorService;
            _logger = logger;
        }

        /// <summary>
        ///     Converts Image(s) to a single PDF file
        /// </summary>
        /// <returns>Returns PDF file with converted images</returns>
        [HttpPost]
        [Route("ImagesToPdf")]
        public async Task<IActionResult> ImagesToPDF(IFormFileCollection files)
        {
            try
            {
                _logger.Information("ImagesToPdf started");

                var formFiles = HttpContext.Request?.Form?.Files;

                if (formFiles == null || formFiles.Count <= 0)
                {
                    return StatusCode(StatusCodes.Status406NotAcceptable, "No files on request");
                }

                _logger.Information("ImagesToPdf ConverToImages called");
                var pdf = await _pdfEditorService.ConvertImagesToPDF(formFiles);

                if(pdf == null)
                {
                    _logger.Information("File(s) received are not images");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "File(s) received are not images");
                }

                _logger.Information("Images converted to PDF file");
                return new FileStreamResult(pdf, "application/pdf");
            }
            catch(Exception ex)
            {
                _logger.Error($"ImagesToPdf - Error: {ex.Message}");
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
        [Route("PdfToImages/{imageFormat}")]
        public async Task<IActionResult> PdfToImages(IFormFile formFile, string imageFormat)
        {         
            try
            {
                _logger.Information("PdfToImages started");

                IFormFile file = HttpContext.Request?.Form?.Files.FirstOrDefault();

                if (string.IsNullOrEmpty(imageFormat) || file == null)
                {
                    _logger.Information("PdfToImages - not all parameters given");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "Missing imageFormat parameter OR PDF file");
                }

                var zipFile = await _pdfEditorService.PDFToImages(file, imageFormat);

                if (zipFile == null)
                {
                    _logger.Information("Files given are not acceptable");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "ImageFormat not acceptable");
                }

                _logger.Information("Pdf converted to images succesfully");
                return new FileContentResult(zipFile, "application/zip") { FileDownloadName = "PDFToImages.zip" };
            }
            catch(Exception ex)
            {
                _logger.Error($"PdfToImages - Error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        ///     Removes pages from PDF
        /// </summary>
        /// <param name="pages"></param>
        /// <returns>PDF with removed pages</returns>
        [HttpPost]
        [Route("RemovePages/{pages}")]
        public async Task<IActionResult> RemovePages(IFormFile file, string pages)
        {
            try
            {
                _logger.Information("RemovePages started");

                IFormFile formFile = HttpContext.Request?.Form?.Files.FirstOrDefault();

                if (pages.Length == 0 || formFile == null)
                {
                    _logger.Information("RemovePages - not all parameters given");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "File not provided OR Pages parameter not in body");
                }

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
                _logger.Information("RemovePages - Pages removed succesfully");
                return new FileStreamResult(pdfStream, "application/pdf");
            }
            catch(Exception ex)
            {
                _logger.Error($"RemovePages - Error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        /// <summary>
        ///     Merged PDF
        /// </summary>
        /// <returns>Merge PDF file</returns>
        [HttpPost]
        [Route("Merge")]
        public async Task<IActionResult> MergePDF(IFormFileCollection files)
        {
            try
            {
                _logger.Information("MergePdf started");

                IFormFileCollection formFiles = HttpContext.Request?.Form?.Files;

                if (formFiles == null || formFiles.Count <= 0)
                {
                    _logger.Information("MergePdf - not all parameters given");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "No files sent");
                }

                Stream pdf = await _pdfEditorService.MergePDF(formFiles);
                _logger.Information("MergePdf - Pdfs succesfully merged");
                return new FileStreamResult(pdf, "application/pdf");
            }
            catch(Exception ex)
            {
                _logger.Error($"MergePdf - Error : {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        ///     Splits PDF by fixed range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Split/FixRange/{range}")]
        public async Task<IActionResult> SplitPdfByRange(string range, IFormFile file)
        {
            try
            {
                _logger.Information("SplitPdfByRange started");

                IFormFile formFile = HttpContext.Request?.Form?.Files.FirstOrDefault();

                if (formFile == null || string.IsNullOrEmpty(range))
                {
                    _logger.Information("SplitPdf - FixRange - not all parameters given");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "No file OR range sent");
                }

                var zipFile = await _pdfEditorService.SplitPDFByRange(formFile, range);

                if (zipFile == null)
                {
                    _logger.Information("SplitPdf - FixRange - Information not acceptable");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "Infomation not acceptable");
                }

                _logger.Information("SplitPdf - FixRange - Succesfully splitted Pdf");
                return new FileContentResult(zipFile, "application/zip") { FileDownloadName = "PDFSplitByRange.zip" };
            }
            catch(Exception ex)
            {
                _logger.Error($"SplitPdf - FixRange - Error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        
        /// <summary>
        ///     Splits Pdf into custom range sent
        /// </summary>
        /// <param name="range"></param>
        /// <returns>New Pdf with custom range</returns>
        [HttpPost]
        [Route("Split/CustomRange/{range}")]
        public async Task<IActionResult> SplitPdfByCustomRange(IFormFile file, string range)
        {
            try
            {
                _logger.Information("SplitPdf/CustomerRange started");

                IFormFile formFile = HttpContext.Request?.Form?.Files.FirstOrDefault();

                if (formFile == null || string.IsNullOrEmpty(range))
                {
                    _logger.Information("SplitPdf - CustomRange - Not all parameters given");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "No file OR range sent");
                }

                Stream pdf = await _pdfEditorService.SplitPDFByCustomRange(formFile, range);

                if (pdf == null)
                {
                    _logger.Information("SplitPdf - CustomRange - Ranges not acceptable");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "Infomation not acceptable");
                }

                _logger.Information("SplitPdf - CustomRange - Succesfully splitted Pdf");
                return new FileStreamResult(pdf, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.Error($"SplitPdf - CustomRange - Error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

    }
}
