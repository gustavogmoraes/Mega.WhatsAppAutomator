using System.Collections.Generic;

namespace Mega.WhatsAppAutomator.Domain.Objects
{
	public class HumanizerConfiguration
	{
		public IList<string> GreetingsPool { get; set; }

		public IList<string> CumplimentsPool { get; set; }

		public IList<string> ClientPresentationsPool { get; set; }

		public IList<string> FarewellsPool { get; set; }

		public IList<string> CollaboratorsContacts { get; set; }

		public bool InsaneMode { get; set; }
	}
}