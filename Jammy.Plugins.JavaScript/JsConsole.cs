using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
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
		private int tab = 0;

		public JsConsole(ILogger logger)
		{
			this.logger = logger;
		}

		private void Log(string level, params object[] o)
		{
			if (level != "") level += " ";
			logger.LogTrace($"{"".PadLeft(tab * 2)}{level}{string.Join(' ', o)}");
		}

		private bool IsFalsy(object o)
		{
			if (o == null) return true;
			if (o is bool b) return !b;
			if (o is string s) return string.IsNullOrEmpty(s);
			if (o is double d) return d == 0 || double.IsNaN(d);
			if (o is float f) return f == 0 || float.IsNaN(f);
			if (o is Array arr) return arr.Length == 0;
			if (o is ExpandoObject ex) return !ex.Any();
			return false;
		}

		public void assert(object o, params object[] m) { if (IsFalsy(o)) error(m); }
		//Log an error message to console if the first argument is false.

		public void clear() { }
		//Clear the console

		public void count(object l) { counts.TryAdd(l, 0); counts[l]++; }
		//Log the number of times this line has been called with the given label.

		public void countReset(object l) { if (!counts.ContainsKey(l)) { warn("Count for '" + l?.ToString() + "' does not exist"); return; }; counts[l] = 0;}
		//Resets the value of the counter with the given label.

		public void debug(params object[] o) => Log("DEBUG", o);
		//Outputs a message to the console with the debug log level.

		public void dir(object o) { dirxml(o); }
		//Displays an interactive listing of the properties of a specified JavaScript object.
		//This listing lets you use disclosure triangles to examine the contents of child objects.

		public void dirxml(object o)
		{
			try { 
				var xml = new XmlSerializer(o.GetType());
				var sb = new StringBuilder();
				using var t = XmlWriter.Create(sb, new XmlWriterSettings {Indent  = true });
				xml.Serialize(t, o);
				logger.LogTrace(Environment.NewLine + sb.ToString());
				Trace.WriteLine(sb.ToString());
			} catch { }

			try { 
				string json = JsonConvert.SerializeObject(o, Newtonsoft.Json.Formatting.Indented);
				logger.LogTrace(Environment.NewLine + json);
				Trace.WriteLine(json);
			} catch { }
		}
		//Displays an XML/HTML Element representation of the specified object if possible or the JavaScript Object view if it is not possible.

		public void error(params object[] o) => Log("ERROR", o);
		//Outputs a message to the console with the error log level.

		public void exception(params object[] o) { error(o); } //Non-standard Deprecated
		//An alias for public void error().

		public void group(params object[] o) { if (o.Length >= 1) log(o[0]); tab++; }
		//Creates a new inline group, indenting all following output by another level.
		//To move back out a level, call public void groupEnd().

		public void groupCollapsed(params object[] o) { group(o); }
		//Creates a new inline group, indenting all following output by another level.
		//However, unlike public void group() this starts with the inline group collapsed requiring the use of a disclosure button to expand it.
		//To move back out a level, call public void groupEnd().

		public void groupEnd() { tab--; if (tab<0)tab=0; }
		//Exits the current inline group.

		public void info(params object[] o) => Log("INFO", o);
		//Outputs a message to the console with the info log level.

		public void log(params object[] o) => Log("", o);
		//Outputs a message to the public void 

		public void profile() { } //Non-standard
		//Starts the browser's built-in profiler (for example, the Firefox performance tool).
		//You can specify an optional name for the profile.

		public void profileEnd() { } //Non-standard
		//Stops the profiler.
		//You can see the resulting profile in the browser's performance tool (for example, the Firefox performance tool).

		public void table() { }
		//Displays tabular data as a table.

		public void time(object o) { timers[o] = new Stopwatch(); timers[o].Start(); }
		//Starts a timer with a name specified as an input parameter.Up to 10,000 simultaneous timers can run on a given page.

		public void timeEnd(object o) { if (!timers.TryGetValue(o, out Stopwatch value)) { warn("Timer '"+o?.ToString() + "' does not exist"); return; } value.Stop(); log($"{o} {value.ElapsedMilliseconds}ms - timer ended"); timers.Remove(o); }
		//Stops the specified timer and logs the elapsed time in milliseconds since it started.

		public void timeLog(object o) { if (!timers.TryGetValue(o, out Stopwatch value)) { warn("Timer '" + o?.ToString() + "' does not exist"); return; } log($"{o}: {value.ElapsedMilliseconds}ms"); }
		//Logs the value of the specified timer to the public void 

		public void timeStamp() { } //Non-standard
		//Adds a marker to the browser performance tool's timeline (Chrome or Firefox).

		public void trace() { }
		//Outputs a stack trace.

		public void warn(params object[] o) => Log("WARN",  o);
		//Outputs a message to the console with the warning log level.
	}
}
