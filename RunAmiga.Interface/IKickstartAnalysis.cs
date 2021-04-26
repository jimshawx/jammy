using System.Collections.Generic;
using RunAmiga.Types.Kickstart;

namespace RunAmiga.Interface
{
	public interface IKickstartAnalysis
	{
		List<Resident> GetRomTags();
		KickstartVersion GetVersion();
		void ShowRomTags();
		uint GetChecksum();
	}
}