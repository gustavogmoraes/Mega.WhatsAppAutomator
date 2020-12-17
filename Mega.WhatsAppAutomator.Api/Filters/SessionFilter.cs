using System.Collections.Generic;
using System.Text;
using System;
using System.Linq;
using Mega.WhatsAppAutomator.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Mega.WhatsAppAutomator.Infrastructure.Persistence;

namespace Mega.WhatsAppAutomator.Api.Filters
{
    public class SessionFilter : IActionFilter
    {
        public async void OnActionExecuting(ActionExecutingContext context)
        {
            if(DoesRequestContainsClientTokenHeader(context) &&
               ValidTokenList.Contains(GetClientTokenFromHeader(context)))
            {
                //Proceed
                return;
            }

            context.Result = new ForbidResult();
            await context.HttpContext.Response.WriteAsync("Forbidden", Encoding.UTF8);
        }

        public void OnActionExecuted(ActionExecutedContext context) { }

        private IList<string> ValidTokenList =>  GetValidTokensFromDatabase().Append(AdminToken).ToList() ;

        private IList<string> GetValidTokensFromDatabase() => QueryBridge.GetValidClientTokens();

        private const string AdminToken = "adminrjdta";
        
        private string GetClientTokenFromHeader(ActionContext context)
        {
            return context.HttpContext.Request.Headers["ClientToken"];
        }
        
        private bool DoesRequestContainsClientTokenHeader(ActionContext context)
        {
            return context.HttpContext.Request.Headers.ContainsKey("ClientToken");
        }
    }
}
