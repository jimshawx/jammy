using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace RunAmiga.Logger.DebugAsync
{
	public class NullScope : IDisposable
	{
		public static NullScope Instance { get; } = new NullScope();
		private NullScope() { }
		public void Dispose() { }
	}

	public class DebugAsyncLogger : ILogger
	{
		private readonly string name;
		private ConcurrentQueue<DebugAsyncLoggerProvider.DbMessage> messageQueue;

		public DebugAsyncLogger(string name)
		{
			this.name = name;
			this.messageQueue = DebugAsyncLoggerProvider.MessageQueue;
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

			messageQueue.Enqueue(new DebugAsyncLoggerProvider.DbMessage
			{
				LogLevel = logLevel,
				Message = message,
				Name = name
			});
		}
	}

	[ProviderAlias("DebugAsync")]
	public class DebugAsyncLoggerProvider : ILoggerProvider
	{
		internal class DbMessage
		{
			public string Message { get; set; }
			public string Name { get; set; }
			public LogLevel LogLevel { get; set; }
		}

		private DebugAsyncConsoleLoggerReader reader;
		internal static ConcurrentQueue<DbMessage> MessageQueue = new ConcurrentQueue<DbMessage>();

		public DebugAsyncLoggerProvider()
		{
			reader = new DebugAsyncConsoleLoggerReader();
		}

		public ILogger CreateLogger(string name) { return new DebugAsyncLogger(name); }
		public void Dispose() { reader.Dispose(); }
	}

	public static class DebugAsyncExtensions
	{
		public static ILoggingBuilder AddDebugAsync(this ILoggingBuilder builder)
		{
			builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DebugAsyncLoggerProvider>());
			return builder;
		}
	}

	public class DebugAsyncConsoleLoggerReader : IDisposable
	{
		[DllImport("kernel32.dll")]
		static extern bool AllocConsole();
		[DllImport("kernel32.dll")]
		static extern bool FreeConsole();

		private readonly CancellationTokenSource cancellation;
		private readonly Task readerTask;

		public DebugAsyncConsoleLoggerReader()
		{
			cancellation = new CancellationTokenSource();
			readerTask = new Task(() =>
			{
				AllocConsole();
				var writer = Console.Out;

				var messageQueue = DebugAsyncLoggerProvider.MessageQueue;
				int backoff = 1;
				var sb = new StringBuilder();

				while (!cancellation.IsCancellationRequested)
				{
					if (!messageQueue.IsEmpty)
					{
						sb.Clear();
						while (messageQueue.TryDequeue(out DebugAsyncLoggerProvider.DbMessage rv))
						{
							sb.AppendLine($"{rv.Name}: {rv.LogLevel}: {rv.Message}");
						}

						writer.Write(sb.ToString());

						backoff = 1;
					}
					else
					{
						backoff += backoff;
						if (backoff > 500) backoff = 500;
						Task.Delay(backoff, cancellation.Token).Wait();
					}
				}

				FreeConsole();

			}, cancellation.Token);

			readerTask.Start();
		}

		public void Dispose()
		{
			cancellation.Cancel();
			readerTask.Wait(1000);
		}
	}
}
