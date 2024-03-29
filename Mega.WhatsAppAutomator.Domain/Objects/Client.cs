using System;
using System.Collections;
using System.Collections.Generic;

namespace Mega.WhatsAppAutomator.Domain.Objects
{
    public class Client
    {
        public string Id { get; set; }
        /// <summary>
        /// Client name.
        /// </summary>
        /// <value>The client name.</value>
        public string Nome { get; set; }

        /// <summary>
        /// Client unique token used for requests (Guid format).
        /// </summary>
        /// <value>The token.</value>
        public string Token { get; set; }

        public SendMessageConfiguration SendMessageConfiguration { get; set; }
        
        public bool SituationOk { get; set; }

        //public RestartConfiguration RestartConfiguration { get; set; }
    }
}
