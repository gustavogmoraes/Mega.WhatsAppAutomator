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
		
		public bool UseHumanizer { get; set; }
		
		public int MinimumDelayAfterGreeting { get; set; }
		public int MaximumDelayAfterGreeting { get; set; }
		
		public int MinimumDelayAfterMessage { get; set; }
		public int MaximumDelayAfterMessage { get; set; }
		
		public int MinimumGreetingTypingDelay { get; set; }
		public int MaximumGreetingTypingDelay { get; set; }
		
		public int MinimumMessageTypingDelay { get; set; }
		
		public int MaximumMessageTypingDelay { get; set; }
		public int MinimumDelayAfterFarewell { get; set; }			
		public int MaximumDelayAfterFarewell { get; set; }
	}
}