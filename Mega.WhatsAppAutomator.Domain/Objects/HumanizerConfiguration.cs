using System.Collections.Generic;

namespace Mega.WhatsAppAutomator.Domain.Objects
{
	public class HumanizerConfiguration
	{
		public bool InsaneMode { get; set; }
		
		public bool ScrambleMessageWithWhitespaces { get; set; }
		
		public bool UseHumanizer { get; set; }
		
		public MessagePhase Greeting { get; set; }
		
		public MessagePhase Message { get; set; }
		
		public FarewellPhase Farewell { get; set; }
	}
}