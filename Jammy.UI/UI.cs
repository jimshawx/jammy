using System.Threading;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.UI
{
	public sealed class UI
	{
		private static readonly SemaphoreSlim uiSemaphore = new SemaphoreSlim(1);
		private static readonly AutoResetEvent uiRefreshWaitHandle = new AutoResetEvent(false);

		private static void Lock()
		{
			uiSemaphore.Wait();
		}

		private static void Unlock()
		{
			uiSemaphore.Release();
		}

		public static bool PowerLight { set; get; }
		public static bool DiskLight { set; get; }
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

