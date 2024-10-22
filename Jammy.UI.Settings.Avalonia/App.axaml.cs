using Avalonia.Markup.Xaml;
using Avalonia;

namespace Jammy.UI.Settings.Avalonia
{
	public partial class App : Application
	{
		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
