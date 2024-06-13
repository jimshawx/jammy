using System.Collections.Generic;
using Jammy.Types.Kickstart;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Interface
{
	public interface IKickstartAnalysis
	{
		List<Resident> GetRomTags();
		KickstartVersion GetVersion();
		void ShowRomTags();
		uint GetChecksum();
		uint GetCRC();
	}
}