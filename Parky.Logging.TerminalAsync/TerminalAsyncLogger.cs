using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
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

	public class TerminalAsyncLogger : ILogger
	{
		private readonly string name;
		private readonly ConcurrentQueue<DbMessage> messageQueue;

		public TerminalAsyncLogger(string name, ConcurrentQueue<DbMessage> messageQueue)
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

			ArgumentNullException.ThrowIfNull(formatter);

			string message = formatter(state, exception);

			if (string.IsNullOrEmpty(message))
				return;

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

	[ProviderAlias("TerminalAsync")]
	public class TerminalLoggerProvider : ILoggerProvider
	{
		private sealed class TerminalLoggerInstance
		{
			public TerminalLoggerReader Reader { get; private set; }
			public ConcurrentQueue<DbMessage> MessageQueue { get; private set; }

			private TerminalLoggerInstance()
			{
				MessageQueue = new ConcurrentQueue<DbMessage>();
				Reader = new TerminalLoggerReader(MessageQueue);
			}

			private static TerminalLoggerInstance instance;

			public static TerminalLoggerInstance Instance
			{
				get { return instance ??= new TerminalLoggerInstance(); }
			}
		}

		public ILogger CreateLogger(string name) { return new TerminalAsyncLogger(name, TerminalLoggerInstance.Instance.MessageQueue); }
		public void Dispose() { TerminalLoggerInstance.Instance.Reader.Dispose(); }
	}

	public static class TerminalExtensions
	{
		public static ILoggingBuilder AddTerminalAsync(this ILoggingBuilder builder)
		{
			builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, TerminalLoggerProvider>());
			return builder;
		}
	}

	public class TerminalLoggerReader : IDisposable
	{
		private readonly CancellationTokenSource cancellation;
		private readonly Task readerTask;
		private readonly Process xterm;
		private readonly Stream logfile;

		public TerminalLoggerReader(ConcurrentQueue<DbMessage> messageQueue)
		{
			string fileName = "jammy-"+Path.ChangeExtension(Path.GetRandomFileName(), "log");

			string[] tmpdirs = ["/tmp", "$TMPDIR", @"%TEMP%"];
			string tmpFile = null;

			foreach (var tmpdir in tmpdirs)
			{ 
				try
				{
					tmpFile = Environment.ExpandEnvironmentVariables(Path.Combine(tmpdir, fileName));
					logfile = new FileStream(tmpFile, FileMode.Create, FileAccess.Write, FileShare.Read);
					break;
				}
				catch { /* ignore and try next tmp location */ }
			}

			if (logfile == null)
			{ 
				Trace.WriteLine("Can't open a tmp log file, there won't be any logging");
				return;
			}

			var writer = new StreamWriter(logfile) { AutoFlush = true };

			int parentPid = Process.GetCurrentProcess().Id;

			string tailCmd = $"tail --pid={parentPid} -f {tmpFile}";

			(string cmd, string arg)[] terminals = [
				("konsole", $"--separate --geometry 120x32 -e bash -c \"{tailCmd}\""),
				("xfce4-terminal", $"--geometry=120x32 -x bash -c \"{tailCmd}\""), // xfce4 uses -x
				("lxterminal", $"--geometry=120x32 -e bash -c \"{tailCmd}\""),
				("gnome-terminal", $"--geometry=120x32 -- bash -c \"{tailCmd}\""), // gnome uses --
				("xterm", $"-bg black -fg white -geometry 120x32 -fn fixed -e bash -c \"{tailCmd}\"")
			];
			xterm = null;

			foreach (var terminal in terminals)
			{
				var psi = new ProcessStartInfo
				{
					FileName = terminal.cmd,
					Arguments = terminal.arg,
					UseShellExecute = false
				}; 
				
				try
				{ 
					xterm = Process.Start(psi);
					if (xterm == null)
					{
						Trace.WriteLine($"Failed trying to run {psi.Arguments}");
						continue;
					}
					if (xterm.WaitForExit(100))
					{
						Trace.WriteLine($"Exited trying to run {psi.Arguments}");
						continue;
					}
					Trace.WriteLine($"Running {terminal.cmd} {terminal.arg} terminal");
					break;
				}
				catch
				{
					/* Ignore and try next terminal */
					Trace.WriteLine($"Crashed trying to run {psi.Arguments}");
				}
			}
			if (xterm == null)
				Trace.WriteLine($"Can't find a terminal, try 'tail -f {tmpFile}'");

			cancellation = new CancellationTokenSource();
			readerTask = new Task(() =>
			{
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

			}, cancellation.Token, TaskCreationOptions.LongRunning);
			readerTask.Start();
		}

		public void Dispose()
		{
			if (cancellation != null) cancellation.Cancel();
			if (readerTask != null) readerTask.Wait(1000);
			if (logfile != null) logfile.Dispose();
			if (xterm != null) xterm.Dispose();
		}
	}
}

