using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace pdf_editor_api.Responses
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StandardResponse<T> 
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpStatusCode"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        public StandardResponse(HttpStatusCode httpStatusCode, string message, List<T> data)
        {
            this.HttpStatusCode = httpStatusCode;
            this.Message = message;
            this.Data = data;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<T> Data { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IActionResult Result
        {
            get
            {
                ObjectResult objectResult = new ObjectResult(new { HttpStatusCode, Message, Data });
                objectResult.StatusCode = (int)HttpStatusCode;
                return objectResult;
            }
        }

    }
}
