using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace RunAmiga.Logger.DebugAsyncRTF
{
	public class NullScope : IDisposable
	{
		public static NullScope Instance { get; } = new NullScope();
		private NullScope() { }
		public void Dispose() { }
	}

	public class DbMessage
	{
		public string Message { get; set; }
		public string Name { get; set; }
		public LogLevel LogLevel { get; set; }
	}

	public class DebugAsyncRTFLogger : ILogger
	{
		private readonly string name;
		private readonly ConcurrentQueue<DbMessage> messageQueue;

		public DebugAsyncRTFLogger(string name, ConcurrentQueue<DbMessage> messageQueue)
		{
			this.name = name;
			this.messageQueue = messageQueue;
		}

		public IDisposable BeginScope<TState>(TState state)
		{
			return NullScope.Instance;
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return logLevel != LogLevel.None;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			if (!IsEnabled(logLevel))
				return;

			if (formatter == null)
				throw new ArgumentNullException(nameof(formatter));

			string message = formatter(state, exception);

			if (string.IsNullOrEmpty(message))
				return;

			//message = $"{ logLevel }: {message}";

			if (exception != null)
				message += Environment.NewLine + Environment.NewLine + exception;

			messageQueue.Enqueue(new DbMessage
			{
				LogLevel = logLevel,
				Message = message,
				Name = name
			});
		}
	}

	[ProviderAlias("DebugAsyncRTF")]
	public class DebugAsyncRTFLoggerProvider : ILoggerProvider
	{
		private sealed class DebugAsyncLoggerInstance
		{
			public DebugAsyncRTFLoggerReader Reader { get; private set; }
			public ConcurrentQueue<DbMessage> MessageQueue { get; private set; }

			private DebugAsyncLoggerInstance()
			{
				MessageQueue = new ConcurrentQueue<DbMessage>();
				Reader = new DebugAsyncRTFLoggerReader(MessageQueue);
			}

			private static DebugAsyncLoggerInstance instance;

			public static DebugAsyncLoggerInstance Instance
			{
				get { return instance ??= new DebugAsyncLoggerInstance(); }
			}
		}

		public ILogger CreateLogger(string name) { return new DebugAsyncRTFLogger(name, DebugAsyncLoggerInstance.Instance.MessageQueue); }
		public void Dispose() { DebugAsyncLoggerInstance.Instance.Reader.Dispose(); }
	}

	public static class DebugAsyncRTFExtensions
	{
		public static ILoggingBuilder AddDebugAsyncRTF(this ILoggingBuilder builder)
		{
			builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DebugAsyncRTFLoggerProvider>());
			return builder;
		}
	}

	public class DebugAsyncRTFLoggerReader : IDisposable
	{
		private readonly CancellationTokenSource cancellation;
		private readonly Task readerTask;

		public DebugAsyncRTFLoggerReader(ConcurrentQueue<DbMessage> messageQueue)
		{
			Form window = null;
			RichTextBox debugTxt = null;
			var ss = new SemaphoreSlim(1);
			ss.Wait();
			var t = new Thread(() =>
			{
				debugTxt = new RichTextBox
				{
					ClientSize = new Size(800, 600), 
					Multiline = true,
					BorderStyle = BorderStyle.None,
					BackColor = Color.Black,
					ForeColor = Color.LightGray,
					Font = new Font(new FontFamily("Consolas"), 8),
					ReadOnly = true,
					Anchor = AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top
				};
				window = new Form { Name = "Debug", Text = "Debug", ClientSize = debugTxt.Size };

				if (window.Handle == IntPtr.Zero)
					throw new ApplicationException();
				window.Controls.Add(debugTxt);

				ss.Release();
				window.Show();

				Application.Run(window);
			});

			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			ss.Wait();

			cancellation = new CancellationTokenSource();
			readerTask = new Task(() =>
			{
				int backoff = 1;
				var sb = new StringBuilder();
				const int maxTextLength = 1000000;

				while (!cancellation.IsCancellationRequested)
				{
					if (!messageQueue.IsEmpty)
					{
						sb.Clear();
						while (messageQueue.TryDequeue(out DbMessage rv))
						{
							sb.AppendLine($"{rv.Name}: {rv.LogLevel}: {rv.Message}");
						}

						window.Invoke((Action)delegate
						{
							debugTxt.AppendText(sb.ToString());
							if (debugTxt.TextLength > maxTextLength * 2)
							{
								debugTxt.Text = debugTxt.Text.Substring(debugTxt.TextLength - maxTextLength, maxTextLength);
							}
							debugTxt.ScrollToCaret();
						});

						backoff = 1;
					}
					else
					{
						backoff += backoff;
						if (backoff > 500) backoff = 500;
						Task.Delay(backoff, cancellation.Token).Wait();
					}
				}

				window.Close();

			}, cancellation.Token, TaskCreationOptions.LongRunning);

			readerTask.Start();
		}

		public void Dispose()
		{
			cancellation.Cancel();
			readerTask.Wait(1000);
		}
	}
}
