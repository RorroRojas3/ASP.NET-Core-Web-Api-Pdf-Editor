using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace pdf_editor_api.Responses
{
    public class StandardResponse<T> 
    {
        public StandardResponse(HttpStatusCode httpStatusCode, string message, List<T> data)
        {
            this.HttpStatusCode = httpStatusCode;
            this.Message = message;
            this.Data = data;
        }
        

        public HttpStatusCode HttpStatusCode { get; set; }
        public string Message { get; set; }
        public List<T> Data { get; set; }

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
