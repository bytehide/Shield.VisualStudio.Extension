using System.Collections.Generic;

namespace Shield.Client.Models.API.Protections {
	public class ProtectionParamsDto {
		public string PrepareKey { get; set; }
		public List<ProtectionDto> SelectedProtections { get; set; }
	}
}
