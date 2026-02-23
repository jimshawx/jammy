using System.Threading;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.UI
{
	public sealed class UI
	{
		private static readonly AutoResetEvent uiRefreshWaitHandle = new AutoResetEvent(false);

		public static bool IsDirty
		{
			set
			{
				if (value)
					uiRefreshWaitHandle.Set();
			}

			get
			{
				uiRefreshWaitHandle.WaitOne();
				return true;
			}
		}
	}
}

