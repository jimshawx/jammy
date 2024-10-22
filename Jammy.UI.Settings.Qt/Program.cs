using Qml.Net;
using Qml.Net.Runtimes;

namespace Jammy.UI.Settings.Qt
{
	class Program
	{
		static int Main(string[] args)
		{
			RuntimeManager.DiscoverOrDownloadSuitableQtRuntime();
			using (var app = new QGuiApplication(args))
			using (var engine = new QQmlApplicationEngine())
			{
				engine.Load("Screen01.ui.qml");
				return app.Exec();
			}
		}
	}
}

