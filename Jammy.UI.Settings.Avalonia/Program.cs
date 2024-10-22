using Avalonia;
using Avalonia.Logging;
using Avalonia.ReactiveUI;

namespace Jammy.UI.Settings.Avalonia
{
	public class Program
	{
		[STAThread]
		static int Main(string[] args)
		{
			BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
			return 0;
		}

		public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>() // `App` is child of `Application`
			.UsePlatformDetect()
			.LogToTrace(LogEventLevel.Verbose)
			.UseReactiveUI();
	}


}
