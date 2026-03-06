using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Parky.Logging
{
	internal class NullScope : IDisposable
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

	public class LogFileLogger : ILogger
	{
		private readonly string name;
		private readonly ConcurrentQueue<DbMessage> messageQueue;

		public LogFileLogger(string name, ConcurrentQueue<DbMessage> messageQueue)
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

	[ProviderAlias("LogFile")]
	public class LogFileLoggerProvider : ILoggerProvider
	{
		private sealed class LogFileLoggerInstance
		{
			public LogFileLoggerReader Reader { get; private set; }
			public ConcurrentQueue<DbMessage> MessageQueue { get; private set; }

			private LogFileLoggerInstance()
			{
				MessageQueue = new ConcurrentQueue<DbMessage>();
				Reader = new LogFileLoggerReader(MessageQueue);
			}

			private static LogFileLoggerInstance instance;

			public static LogFileLoggerInstance Instance
			{
				get { return instance ??= new LogFileLoggerInstance(); }
			}
		}

		public ILogger CreateLogger(string name) { return new LogFileLogger(name, LogFileLoggerInstance.Instance.MessageQueue); }
		public void Dispose() { LogFileLoggerInstance.Instance.Reader.Dispose(); }
	}

	public static class LogFileExtensions
	{
		public static ILoggingBuilder AddLogFile(this ILoggingBuilder builder)
		{
			builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, LogFileLoggerProvider>());
			return builder;
		}
	}

	public class LogFileLoggerReader : IDisposable
	{
		private readonly CancellationTokenSource cancellation;
		private readonly Task readerTask;

		public LogFileLoggerReader(ConcurrentQueue<DbMessage> messageQueue)
		{
			cancellation = new CancellationTokenSource();
			readerTask = new Task(() =>
			{
				try
				{ 
					File.CreateText("jammylog.txt").Close();
				}
				catch (Exception ex)
				{
					Trace.WriteLine($"Failed to create file jammylog.txt: {ex}");
					return;
				}

				int backoff = 1;
				var sb = new StringBuilder();

				while (!cancellation.IsCancellationRequested)
				{
					if (!messageQueue.IsEmpty)
					{
						sb.Clear();
						while (messageQueue.TryDequeue(out DbMessage rv))
						{
							sb.AppendLine($"{rv.Name}: {rv.LogLevel}: {rv.Message}");
						}

						File.AppendAllText("jammylog.txt", sb.ToString());

						backoff = 1;
					}
					else
					{
						backoff += backoff;
						if (backoff > 500) backoff = 500;
						Task.Delay(backoff, cancellation.Token).Wait();
					}
				}

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
