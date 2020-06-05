using System;
using System.Collections.Generic;
using Mega.WhatsAppAutomator.Domain.Objects.Base;

namespace Mega.WhatsAppAutomator.Domain.Objects
{
    public class UntreatedError : Entity
    {
        /// <summary>
        /// Stores the route.
        /// </summary>
        /// <value>The route.</value>
        public Dictionary<string, string> Route { get; set; }

        /// <summary>
        /// Stores the exception.
        /// </summary>
        /// <value>The exception.</value>
        public Exception Exception { get; set; }

        /// <summary>
        /// Api session for log and error tracing.
        /// </summary>
        /// <value></value>
        public ClientSession ClientSession { get; set; }
    }
}