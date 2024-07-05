using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Parky.Logging
{
	internal class NullScope : IDisposable
	{
		public static NullScope Instance { get; } = new NullScope();
		private NullScope() { }
		public void Dispose() { }
	}

	public class OutputDebugStringLogger : ILogger
	{
		[DllImport("kernel32.dll")]
		static extern void OutputDebugString(string s);

		private readonly string name;

		public OutputDebugStringLogger(string name)
		{
			this.name = name;
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

			OutputDebugString($"{name}: {logLevel}: {message}\n");
		}
	}

	[ProviderAlias("OutputDebugString")]
	public class OutputDebugStringLoggerProvider : ILoggerProvider
	{
		private sealed class OutputDebugStringLoggerInstance
		{
			private OutputDebugStringLoggerInstance() { }

			private static OutputDebugStringLoggerInstance instance;

			public static OutputDebugStringLoggerInstance Instance
			{
				get { return instance ??= new OutputDebugStringLoggerInstance(); }
			}
		}

		public ILogger CreateLogger(string name) { return new OutputDebugStringLogger(name); }
		public void Dispose() { }
	}

	public static class OutputDebugStringExtensions
	{
		public static ILoggingBuilder AddOutputDebugString(this ILoggingBuilder builder)
		{
			builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, OutputDebugStringLoggerProvider>());
			return builder;
		}
	}
}
