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

		public SQLiteLogger(string name)
		{
			this.name = name;
			connection = new SQLiteConnection("Data Source=errorlog.db");
			connection.Open();
		}

		public IDisposable BeginScope<TState>(TState state)
		{
			return NullScope.Instance;
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return connection != null && logLevel != LogLevel.None;
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

			var cmd = new SQLiteCommand("insert into errorlog (message, name, loglevel) values (:message, :name, :loglevel)", connection);
			cmd.Parameters.AddWithValue("message", message);
			cmd.Parameters.AddWithValue("name", name);
			cmd.Parameters.AddWithValue("loglevel", logLevel);
			cmd.ExecuteScalar();
		}
	}

	[ProviderAlias("SQLite")]
	public class SQLiteLoggerProvider : ILoggerProvider
	{
		private SQLiteLoggerReader reader;
		
		public SQLiteLoggerProvider()
		{
			var connection = new SQLiteConnection("Data Source=errorlog.db");
			connection.Open();
			var cmd = new SQLiteCommand("drop table if exists errorlog; create table errorlog (id integer primary key autoincrement, message text not null, name text, loglevel int not null)", connection);
			cmd.ExecuteScalar();
			connection.Close();

			reader = new SQLiteLoggerReader();
		}

		public ILogger CreateLogger(string name) { return new SQLiteLogger(name); }
		public void Dispose() { }
	}

	public static class SQLiteExtensions
	{
		public static ILoggingBuilder AddSQLite(this ILoggingBuilder builder)
		{
			builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SQLiteLoggerProvider>());
			return builder;
		}
	}

	public interface ISQLiteLoggerReader { }

	public class SQLiteLoggerReader : ISQLiteLoggerReader, IDisposable
	{
		private readonly SQLiteConnection connection;
		private uint counter = 0;
		private readonly Thread thread;
		private bool quit;

		public SQLiteLoggerReader()
		{
			connection = new SQLiteConnection("Data Source=errorlog.db;Read Only=True");
			connection.Open();
			var cmd = new SQLiteCommand("select max(id) from errorlog", connection);
			var cnt = cmd.ExecuteScalar();
			if (cnt != DBNull.Value)
				counter = Convert.ToUInt32(cnt);

			thread = new Thread(Reader);
			thread.Start();
		}

		private class DbMessage
		{
			public string Message { get; set; }
			public string Name { get; set; }
			public LogLevel LogLevel { get; set; }
		}

		public void Reader()
		{
			var sb = new StringBuilder();
			while (!quit)
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
							LogLevel = (LogLevel)rv.GetInt32(3)
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

			quit = false;
		}

		public void Dispose()
		{
			quit = true;
			while (quit) Thread.Sleep(10);
			connection.Close();
		}
	}
}
