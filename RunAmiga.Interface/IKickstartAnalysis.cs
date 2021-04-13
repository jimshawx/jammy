using System.Collections.Generic;
using RunAmiga.Types.Kickstart;

namespace RunAmiga.Interface
{
	public interface IKickstartAnalysis
	{
		List<Resident> GetRomTags();
		void ShowRomTags();
	}
}