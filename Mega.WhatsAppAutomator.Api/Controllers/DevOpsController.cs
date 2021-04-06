using System;
using System.Collections.Generic;
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
        public ActionResult RestartAutomation()
        {
            Task.Run(AutomationStartup.Start);

            return Ok();
        }
        
        [HttpGet("[action]")]
        public ActionResult StartSmsAutomator()
        {
            Task.Run(AutomationStartup.StartSms);

            return Ok();
        }
        
        [HttpGet("[action]")]
        public ActionResult Exit()
        {
            Task.Run(AutomationStartup.ExitApplication);
            
            return Ok();
        }
    }
}
