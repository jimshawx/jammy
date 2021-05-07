using System.Collections.Generic;
using Jammy.Types.Kickstart;

namespace Jammy.Interface
{
	public interface IKickstartAnalysis
	{
		List<Resident> GetRomTags();
		KickstartVersion GetVersion();
		void ShowRomTags();
		uint GetChecksum();
	}
}