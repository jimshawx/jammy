using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
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

	public class DebugAsyncLogger : ILogger
	{
		private readonly string name;
		private readonly Channel<DbMessage> messageQueue;

		public DebugAsyncLogger(string name, Channel<DbMessage> messageQueue)
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

			messageQueue.Writer.TryWrite(new DbMessage
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
		private sealed class DebugAsyncLoggerInstance
		{
			public DebugAsyncConsoleLoggerReader Reader { get; private set; }
			public Channel<DbMessage> MessageQueue { get; private set; }

			private DebugAsyncLoggerInstance()
			{
				MessageQueue = Channel.CreateUnbounded<DbMessage>(new UnboundedChannelOptions { SingleReader = true });
				Reader = new DebugAsyncConsoleLoggerReader(MessageQueue);
			}

			private static DebugAsyncLoggerInstance instance;

			public static DebugAsyncLoggerInstance Instance
			{
				get { return instance ??= new DebugAsyncLoggerInstance(); }
			}
		}

		public ILogger CreateLogger(string name) { return new DebugAsyncLogger(name, DebugAsyncLoggerInstance.Instance.MessageQueue); }
		public void Dispose() { DebugAsyncLoggerInstance.Instance.Reader.Dispose(); }
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
		static extern bool AttachConsole(uint dwProcessId);
		[DllImport("kernel32.dll")]
		static extern bool FreeConsole();
		[DllImport("kernel32.dll")]
		static extern uint GetLastError();

		private const uint ERROR_ACCESS_DENIED = 5;
		private const uint ERROR_INVALID_HANDLE = 6;
		private const uint ERROR_INVALID_PARAMETER = 87;

		private readonly CancellationTokenSource cancellation;
		private readonly Task readerTask;

		public DebugAsyncConsoleLoggerReader(Channel<DbMessage> messageQueue)
		{
			cancellation = new CancellationTokenSource();
			readerTask = Task.Run(async () =>
			{
				bool consoleAllocated = true;
				if (!AllocConsole())
				{
					consoleAllocated = false;

					//ERROR_ACCESS_DENIED means we're already attached to a console
					if (GetLastError() != ERROR_ACCESS_DENIED)
					{
						Trace.WriteLine($"AllocConsole LastError {GetLastError()}");
						if (!AttachConsole(0xffffffff))
						{
							Trace.WriteLine($"AttachConsole LastError {GetLastError()}");
							Trace.WriteLine("Can't get a console for logging");
							return;
						}

						//attached to an existing console, need to call FreeConsole()
						consoleAllocated = true;
					}
				}

				var writer = Console.Out;
				var sb = new StringBuilder();

				try
				{
					while (await messageQueue.Reader.WaitToReadAsync(cancellation.Token))
					{
						sb.Clear();
						while (messageQueue.Reader.TryRead(out DbMessage rv))
						{
							sb.AppendLine($"{rv.Name}: {rv.LogLevel}: {rv.Message}");
						}

						writer.Write(sb.ToString());
					}
				}
				catch (OperationCanceledException)
				{
					/* exit condition */
				}
				finally
				{
					if (consoleAllocated)
						FreeConsole();
				}

			}, cancellation.Token);
		}

		public void Dispose()
		{
			cancellation.Cancel();
			readerTask.Wait(1000);
		}
	}
}
