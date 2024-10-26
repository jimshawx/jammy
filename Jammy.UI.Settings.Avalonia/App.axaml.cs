using Avalonia.Markup.Xaml;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Simple;

namespace Jammy.UI.Settings.Avalonia
{
	public class App : Application
	{
		public override void Initialize()
		{
			Styles.Add(new SimpleTheme());
		}

		public override void OnFrameworkInitializationCompleted()
		{
			if (ApplicationLifetime	is IClassicDesktopStyleApplicationLifetime desktop)
				desktop.MainWindow = new Settings();
			else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
				singleView.MainView = new Settings();
			base.OnFrameworkInitializationCompleted();
		}
	}
}
