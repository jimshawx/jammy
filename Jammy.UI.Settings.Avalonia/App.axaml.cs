using Avalonia.Markup.Xaml;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Jammy.UI.Settings.Avalonia
{
	public partial class App : Application
	{
		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
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
