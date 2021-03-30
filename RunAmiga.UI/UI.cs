using System.Threading;

namespace RunAmiga.UI
{
	public sealed class UI
	{
		private static readonly SemaphoreSlim uiSemaphore = new SemaphoreSlim(1);

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
		public static bool IsDirty { set; get; }
	}
}

