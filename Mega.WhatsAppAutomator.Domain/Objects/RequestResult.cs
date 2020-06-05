using System;
using System.Collections.Generic;

namespace Mega.WhatsAppAutomator.Domain.Objects
{
    public class RequestResult
    {
        public string Message { get; set; }
        
        public IList<Error> Errors { get; set; }
    }
}
