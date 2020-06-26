using System.Linq;
using Mega.WhatsAppAutomator.Api.ApiUtils;
using Mega.WhatsAppAutomator.Api.Objects;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mega.WhatsAppAutomator.Api.Filters
{
    /// <summary>
    /// Exception handler filter class.
    /// </summary>
    public class ExceptionHandlerFilter : IExceptionFilter
    {
        /// <summary>
        /// Gets called on exception.
        /// </summary>
        /// <param name="context">Context.</param>
        public void OnException(ExceptionContext context)
        {
            // StoreError(new UntreatedError
            // {
            //     Route = context.ActionDescriptor.RouteValues.ToDictionary(x => x.Key, x => x.Value),
            //     Exception = context.Exception,
            //     ClientSession = context.HttpContext.GetClientSession()
            // });
            //
            // context.Result = new InternalServerErrorObjectResult(new RequestResult
            // {
            //     Message = "Ocorreu um erro desconhecido na API!\nO desenvolvedor já foi contatado com os logs e informações e já está trabalhando para resolver.\nTente refazer a requisição."
            // });
        }

        private void StoreError(UntreatedError error)
        {
            Logger.StoreError(error);
        }
    }
}