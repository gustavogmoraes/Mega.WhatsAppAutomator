using System;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure;
using Mega.WhatsAppAutomator.Infrastructure.Enums;
using Mega.WhatsAppAutomator.Infrastructure.Objects;
using Microsoft.AspNetCore.Mvc;

namespace Mega.WhatsAppAutomator.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : Controller
    {
        [HttpPost("[action]")]
        public ActionResult SendMessage([FromBody]Message message)
        {
            AutomationQueue.AddTask(new WhatsAppWebTask
            {
                TaskData = message
            });

            return Ok(new { Message = "Message queue with success" });
        }
    }
}
