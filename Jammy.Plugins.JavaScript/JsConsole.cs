using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.JavaScript
{
	public class JsConsole
	{
		private readonly ILogger logger;
		private readonly Dictionary<object, int> counts = new Dictionary<object, int>();
		private readonly Dictionary<object, Stopwatch> timers = new Dictionary<object, Stopwatch>();

		public JsConsole(ILogger logger)
		{
			this.logger = logger;
		}

		public void assert(object o, object m) { }
		//Log an error message to console if the first argument is false.

		public void clear() { }
		//Clear the console

		public void count(object l) { if (!counts.ContainsKey(l)) counts.Add(l,0); counts[l]++; }
		//Log the number of times this line has been called with the given label.

		public void countReset(object l) { counts[l] = 0;}
		//Resets the value of the counter with the given label.

		public void debug(object o) => logger.LogTrace(o?.ToString());
		//Outputs a message to the console with the debug log level.

		public void dir() { }
		//Displays an interactive listing of the properties of a specified JavaScript object. This listing lets you use disclosure triangles to examine the contents of child objects.

		public void dirxml(object o)
		{
			try { 
				var xml = new XmlSerializer(o.GetType());
				var sb = new StringBuilder();
				using var t = XmlWriter.Create(sb, new XmlWriterSettings {Indent  = true });
				xml.Serialize(t, o);
				logger.LogTrace(sb.ToString());
				Trace.WriteLine(sb.ToString());
			} catch { }

			try { 
				string json = JsonConvert.SerializeObject(o, Newtonsoft.Json.Formatting.Indented);
				logger.LogTrace(json);
				Trace.WriteLine(json);
			} catch { }
		}

		//Displays an XML/HTML Element representation of the specified object if possible or the JavaScript Object view if it is not possible.

		public void error(object o) => logger.LogTrace(o?.ToString());
		//Outputs a message to the console with the error log level.

		public void exception(object o) { error(o); } //Non-standard Deprecated
		//An alias for public void error().

		public void group() { }
		//Creates a new inline group, indenting all following output by another level.To move back out a level, call public void groupEnd().

		public void groupCollapsed() { }
		//Creates a new inline group, indenting all following output by another level.However, unlike public void group() this starts with the inline group collapsed requiring the use of a disclosure button to expand it. To move back out a level, call public void groupEnd().

		public void groupEnd() { }
		//Exits the current inline group.

		public void info(object o) => logger.LogTrace(o?.ToString());
		//Outputs a message to the console with the info log level.

		public void log(object o) => logger.LogTrace(o?.ToString());
		//Outputs a message to the public void 

		public void profile() { } //Non-standard
		//Starts the browser's built-in profiler (for example, the Firefox performance tool). You can specify an optional name for the profile.

		public void profileEnd() { } //Non-standard
		//Stops the profiler. You can see the resulting profile in the browser's performance tool (for example, the Firefox performance tool).

		public void table() { }
		//Displays tabular data as a table.

		public void time(object o) { timers[o] = new Stopwatch(); timers[o].Start(); }
		//Starts a timer with a name specified as an input parameter.Up to 10,000 simultaneous timers can run on a given page.

		public void timeEnd(object o) { if (!timers.ContainsKey(o)) { warn("Timer '"+o?.ToString() + "' does not exist"); return; } timers[o].Stop(); log(timers[o].ElapsedMilliseconds); }
		//Stops the specified timer and logs the elapsed time in milliseconds since it started.

		public void timeLog(object o) { if (!timers.ContainsKey(o)) { warn("Timer '" + o?.ToString() + "' does not exist"); return; } log(timers[o].ElapsedMilliseconds); }
		//Logs the value of the specified timer to the public void 

		public void timeStamp() { } //Non-standard
		//Adds a marker to the browser performance tool's timeline (Chrome or Firefox).

		public void trace() { }
		//Outputs a stack trace.

		public void warn(object o) => logger.LogTrace(o?.ToString());
		//Outputs a message to the console with the warning log level.
	}
}
