using System.Collections.Generic;
using System.Text;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Mega.WhatsAppAutomator.Infrastructure.Persistence;

namespace Mega.WhatsAppAutomator.Api.Filters
{
    public class SessionFilter : IActionFilter
    {
        public const string AdminToken = "adminrjdta";

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if(context.HttpContext.Request.Headers.ContainsKey("ClientToken") &&
               new[]{ AdminToken }.ToList().Contains(context.HttpContext.Request.Headers["ClientToken"]))
            {
                //Proceed
            }

            context.Result = new ForbidResult();
            context.HttpContext.Response.WriteAsync("Forbidden", Encoding.UTF8);
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
