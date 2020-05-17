using System;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infraestructure;
using Mega.WhatsAppAutomator.Infraestructure.Objects;
using Microsoft.AspNetCore.Mvc;

namespace Mega.WhatsAppAutomator.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : Controller
    {
        [HttpPost("SendMessage")]
        public ActionResult SendMessage([FromBody]Message message)
        {
            AutomationQueue.AddTask(new WhatsAppWebTask()
            {
                KindOfTask = "SendMessage",
                TaskData = message
            });

            return Ok();
        }
    }
}
