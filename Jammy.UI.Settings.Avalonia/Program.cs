using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace Jammy.UI.Settings.Avalonia
{
	public class Program
	{
		[STAThread]
		static int Main(string[] args)
		{
			BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

			var window = new Jammy.UI.Settings.Avalonia.Settings();
			window.Show();

			Thread.Sleep(4000);
			return 0;
		}

		public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>() // `App` is child of `Application`
			.UsePlatformDetect()
			//.LogToTrace()
			.UseReactiveUI();
	}


}
