using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

			message = $"{ logLevel }: {message}";

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
		public void Dispose() { }
	}

	public static class DebugAsyncExtensions
	{
		public static ILoggingBuilder AddDebugAsync(this ILoggingBuilder builder)
		{
			builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DebugAsyncLoggerProvider>());
			return builder;
		}
	}

	public class DebugAsyncLoggerReader : IDisposable
	{
		private readonly Thread thread;
		private bool quit;

		public DebugAsyncLoggerReader()
		{
			thread = new Thread(Reader);
			thread.Start();
		}

		public void Reader()
		{
			var messageQueue = DebugAsyncLoggerProvider.MessageQueue;

			int backoff = 1;

			var sb = new StringBuilder();
			while (!quit)
			{
				if (!messageQueue.IsEmpty)
				{
					sb.Clear();
					while (messageQueue.TryDequeue(out DebugAsyncLoggerProvider.DbMessage rv))
					{
						sb.AppendLine(rv.Message);
					}
					Trace.Write(sb.ToString());

					backoff = 1;
				}
				else
				{
					backoff += backoff;
					if (backoff > 500) backoff = 500;
					Thread.Sleep(backoff);
				}
			}

			quit = false;
		}

		public void Dispose()
		{
			quit = true;
			while (quit) Thread.Sleep(10);
		}
	}

	public class DebugAsyncConsoleLoggerReader : IDisposable
	{
		[DllImport("kernel32.dll")]
		static extern bool AllocConsole();

		private readonly Thread thread;
		private bool quit;
		TextWriter writer;

		public DebugAsyncConsoleLoggerReader()
		{
			AllocConsole();

			writer = Console.Out;

			thread = new Thread(Reader);
			thread.Start();
		}

		public void Reader()
		{
			var messageQueue = DebugAsyncLoggerProvider.MessageQueue;

			int backoff = 1;

			var sb = new StringBuilder();
			while (!quit)
			{
				if (!messageQueue.IsEmpty)
				{
					sb.Clear();
					while (messageQueue.TryDequeue(out DebugAsyncLoggerProvider.DbMessage rv))
					{
						sb.AppendLine(rv.Message);
					}
					//Trace.Write(sb.ToString());
					writer.Write(sb.ToString());

					backoff = 1;
				}
				else
				{
					backoff += backoff;
					if (backoff > 500) backoff = 500;
					Thread.Sleep(backoff);
				}
			}

			quit = false;
		}

		public void Dispose()
		{
			quit = true;
			while (quit) Thread.Sleep(10);
		}
	}
}
