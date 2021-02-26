using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace RunAmiga.Logger.SQLite
{
	public class NullScope : IDisposable
	{
		public static NullScope Instance { get; } = new NullScope();
		private NullScope() { }
		public void Dispose() { }
	}

	public class SQLiteLogger : ILogger
	{
		private readonly string name;
		private readonly SQLiteConnection connection;

		public SQLiteLogger(string name, SQLiteConnection connection)
		{
			this.name = name;
			this.connection = connection;
		}

		/// <inheritdoc />
		public IDisposable BeginScope<TState>(TState state)
		{
			return NullScope.Instance;
		}

		/// <inheritdoc />
		public bool IsEnabled(LogLevel logLevel)
		{
			// If the filter is null, everything is enabled
			// unless the debugger is not attached
			return connection != null && logLevel != LogLevel.None;
		}

		/// <inheritdoc />
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			if (formatter == null)
			{
				throw new ArgumentNullException(nameof(formatter));
			}

			string message = formatter(state, exception);

			if (string.IsNullOrEmpty(message))
			{
				return;
			}

			message = $"{ logLevel }: {message}";

			if (exception != null)
			{
				message += Environment.NewLine + Environment.NewLine + exception;
			}

			var cmd = new SQLiteCommand("insert into errorlog (message, name, loglevel) values (:message,:name, :loglevel)", connection);
			cmd.Parameters.AddWithValue("message", message);
			cmd.Parameters.AddWithValue("name", name);
			cmd.Parameters.AddWithValue("loglevel", logLevel);
			cmd.ExecuteScalar();
		}
	}

	[ProviderAlias("SQLite")]
	public class SQLiteLoggerProvider : ILoggerProvider
	{
		private static SQLiteConnection connection;
		private SQLiteLoggerReader reader;
		private Thread thread;

		public ILogger CreateLogger(string name)
		{
			if (connection == null)
			{
				connection = new SQLiteConnection("Data Source=errorlog.db");
				connection.Open();
				var cmd = new SQLiteCommand("drop table if exists errorlog; create table errorlog (id integer primary key autoincrement, message text not null, name text, loglevel text)", connection);
				cmd.ExecuteScalar();

				reader = new SQLiteLoggerReader(connection);
				thread = new Thread(reader.Reader);
				thread.Start();
			}

			return new SQLiteLogger(name, connection);
		}

		public void Dispose()
		{
			if (connection != null)
			{
				connection.Close();
				connection = null;
			}
		}
	}

	public class SQLiteLoggerReader
	{
		private readonly SQLiteConnection connection;
		private uint counter = 0;

		public SQLiteLoggerReader(SQLiteConnection connection)
		{
			this.connection = connection;
		}

		private class DbMessage
		{
			public string Message { get; set; }
			public string Name { get; set; }
			public string LogLevel { get; set; }
		}

		public void Reader()
		{
			var sb = new StringBuilder();
			for (;;)
			{
				var cmd = new SQLiteCommand("select * from errorlog where id > :counter order by id asc", connection);
				cmd.Parameters.AddWithValue("counter", counter);
				var rv = cmd.ExecuteReader();
				var messages = new List<DbMessage>();

				if (rv.HasRows)
				{
					while (rv.Read())
					{
						counter = (uint)rv.GetInt32(0);
						messages.Add(new DbMessage
						{
							Message = rv.GetString(1),
							Name = rv.GetString(2),
							LogLevel = rv.GetString(3)
						});
					}

					sb.Clear();
					foreach (var msg in messages)
						sb.AppendLine(msg.Message);
					Trace.Write(sb.ToString());
				}
				else
				{
					Thread.Sleep(500);
				}
			}
		}
	}

	public static class SQLiteExtensions
	{
		public static ILoggingBuilder AddSQLite(this ILoggingBuilder builder)
		{
			builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SQLiteLoggerProvider>());
			return builder;
		}
	}
}
