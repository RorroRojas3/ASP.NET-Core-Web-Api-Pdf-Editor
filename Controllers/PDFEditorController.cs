using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using pdf_editor_api.Service;

namespace pdf_editor_api.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}")]
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
                                    ILogger<PDFEditorController> logger)
        {
            _pdfEditorService = pdfEditorService;
            _logger = logger;
        }

        /// <summary>
        ///     Converts Image(s) to a single PDF file
        /// </summary>
        /// <returns>Returns PDF file with converted images</returns>
        [HttpPost]
        [Route("Image/Pdf")]
        public async Task<IActionResult> ImagesToPDF(IFormFileCollection files)
        {
            try
            {
                _logger.LogInformation("ImagesToPdf started");

                var formFiles = HttpContext.Request?.Form?.Files;

                if (formFiles == null || formFiles.Count <= 0)
                {
                    return StatusCode(StatusCodes.Status406NotAcceptable, "No files on request");
                }

                _logger.LogInformation("ImagesToPdf ConverToImages called");
                var pdf = await _pdfEditorService.ConvertImagesToPDF(formFiles);

                if(pdf == null)
                {
                    _logger.LogInformation("File(s) received are not images");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "File(s) received are not images");
                }

                _logger.LogInformation("Images converted to PDF file");
                return new FileStreamResult(pdf, "application/pdf");
            }
            catch(Exception ex)
            {
                _logger.LogError($"ImagesToPdf - Error: {ex.Message}");
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
        [Route("Pdf/Image/{imageFormat}")]
        public async Task<IActionResult> PdfToImages(IFormFile formFile, string imageFormat)
        {         
            try
            {
                _logger.LogInformation("PdfToImages started");

                IFormFile file = HttpContext.Request?.Form?.Files.FirstOrDefault();

                if (string.IsNullOrEmpty(imageFormat) || file == null)
                {
                    _logger.LogInformation("PdfToImages - not all parameters given");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "Missing imageFormat parameter OR PDF file");
                }

                var zipFile = await _pdfEditorService.PDFToImages(file, imageFormat);

                if (zipFile == null)
                {
                    _logger.LogInformation("Files given are not acceptable");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "ImageFormat not acceptable");
                }

                _logger.LogInformation("Pdf converted to images succesfully");
                return new FileContentResult(zipFile, "application/zip") { FileDownloadName = "PDFToImages.zip" };
            }
            catch(Exception ex)
            {
                _logger.LogError($"PdfToImages - Error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        ///     Removes pages from PDF
        /// </summary>
        /// <param name="file"></param>
        /// <param name="pages"></param>
        /// <returns>PDF with removed pages</returns>
        [HttpPost]
        [Route("Pdf/RemovePages/{pages}")]
        public async Task<IActionResult> RemovePages(IFormFile file, string pages)
        {
            try
            {
                _logger.LogInformation("RemovePages started");

                IFormFile formFile = HttpContext.Request?.Form?.Files.FirstOrDefault();

                if (pages.Length == 0 || formFile == null)
                {
                    _logger.LogInformation("RemovePages - not all parameters given");
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
                _logger.LogInformation("RemovePages - Pages removed succesfully");
                return new FileStreamResult(pdfStream, "application/pdf");
            }
            catch(Exception ex)
            {
                _logger.LogError($"RemovePages - Error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        /// <summary>
        ///     Merged PDF
        /// </summary>
        /// <returns>Merge PDF file</returns>
        [HttpPost]
        [Route("Pdf/Merge")]
        public async Task<IActionResult> MergePDF(IFormFileCollection files)
        {
            try
            {
                _logger.LogInformation("MergePdf started");

                IFormFileCollection formFiles = HttpContext.Request?.Form?.Files;

                if (formFiles == null || formFiles.Count <= 0)
                {
                    _logger.LogInformation("MergePdf - not all parameters given");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "No files sent");
                }

                Stream pdf = await _pdfEditorService.MergePDF(formFiles);
                _logger.LogInformation("MergePdf - Pdfs succesfully merged");
                return new FileStreamResult(pdf, "application/pdf");
            }
            catch(Exception ex)
            {
                _logger.LogError($"MergePdf - Error : {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        ///     Splits PDF by fixed range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Pdf/Split/Range/Fix/{range}")]
        public async Task<IActionResult> SplitPdfByRange(string range, IFormFile file)
        {
            try
            {
                _logger.LogInformation("SplitPdfByRange started");

                IFormFile formFile = HttpContext.Request?.Form?.Files.FirstOrDefault();

                if (formFile == null || string.IsNullOrEmpty(range))
                {
                    _logger.LogInformation("SplitPdf - FixRange - not all parameters given");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "No file OR range sent");
                }

                var zipFile = await _pdfEditorService.SplitPDFByRange(formFile, range);

                if (zipFile == null)
                {
                    _logger.LogInformation("SplitPdf - FixRange - Information not acceptable");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "Infomation not acceptable");
                }

                _logger.LogInformation("SplitPdf - FixRange - Succesfully splitted Pdf");
                return new FileContentResult(zipFile, "application/zip") { FileDownloadName = "PDFSplitByRange.zip" };
            }
            catch(Exception ex)
            {
                _logger.LogError($"SplitPdf - FixRange - Error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        
        /// <summary>
        ///     Splits Pdf into custom range sent
        /// </summary>
        /// <param name="range"></param>
        /// <returns>New Pdf with custom range</returns>
        [HttpPost]
        [Route("Pdf/Split/Range/Custom/{range}")]
        public async Task<IActionResult> SplitPdfByCustomRange(IFormFile file, string range)
        {
            try
            {
                _logger.LogInformation("SplitPdf/CustomerRange started");

                IFormFile formFile = HttpContext.Request?.Form?.Files.FirstOrDefault();

                if (formFile == null || string.IsNullOrEmpty(range))
                {
                    _logger.LogInformation("SplitPdf - CustomRange - Not all parameters given");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "No file OR range sent");
                }

                Stream pdf = await _pdfEditorService.SplitPDFByCustomRange(formFile, range);

                if (pdf == null)
                {
                    _logger.LogInformation("SplitPdf - CustomRange - Ranges not acceptable");
                    return StatusCode(StatusCodes.Status406NotAcceptable, "Infomation not acceptable");
                }

                _logger.LogInformation("SplitPdf - CustomRange - Succesfully splitted Pdf");
                return new FileStreamResult(pdf, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError($"SplitPdf - CustomRange - Error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

    }
}
