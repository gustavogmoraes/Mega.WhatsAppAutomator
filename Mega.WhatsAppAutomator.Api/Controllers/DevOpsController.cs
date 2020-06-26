using System;
using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Infrastructure;
using Mega.WhatsAppAutomator.Infrastructure.DevOps;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Mega.WhatsAppAutomator.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevOpsController : Controller
    {
        [HttpGet("[action]")]
        public ActionResult GetLastTakenQrCode() => 
            File(FileManagement.GetLastTakenQrCode(), "image/jpeg", "QrCode.jpg");

        [HttpGet("[action]")]
        public async Task<ActionResult> RestartAutomation()
        {
            await AutomationStartup.Start();

            return Ok();
        }
    }
}
