using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Reflection;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Persistence
{
	[AttributeUsage(AttributeTargets.Property|AttributeTargets.Field)]
	public class Persist : Attribute
	{
	}

	public interface IPersistenceManager
	{
		void Save(string filename);
		void Load(string filename);
	}

	public class PersistenceManager : IPersistenceManager
	{
		private readonly IEnumerable<IStatePersister> persisters;
		private readonly EmulationSettings settings;
		private readonly ILogger<PersistenceManager> logger;

		public PersistenceManager(IEnumerable<IStatePersister> persisters, IOptions<EmulationSettings> settings, ILogger<PersistenceManager> logger)
		{
			this.persisters = persisters;
			this.settings = settings.Value;
			this.logger = logger;
		}

		public void Save(string filename)
		{
			var jo = new JArray();
				jo.Add(JObject.FromObject(settings));

			foreach (var p in persisters)
				p.Save(jo);

			using (var f = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				using (var x = new StreamWriter(f))
					x.Write(jo.ToString());
			}
		}

		public void Load(string filename)
		{
			var objects = JArray.Parse(File.ReadAllText(filename));
			foreach (var o in objects)
				foreach (var p in persisters)
					p.Load((JObject)o);
			logger.LogTrace("Snapshot restored!");
		}

		public static bool Is(JObject obj, string id)
		{
			return (obj.TryGetValue("id", StringComparison.InvariantCulture, out var k) && k.ToString() == id);
		}

		public static JObject ToJObject(object obj, string id)
		{
			var jo = new JObject();
			jo.Add("id", id);
			var props = obj.GetType().GetProperties().Where(x=>x.IsDefined(typeof(Persist), false));
			foreach (var prop in props)
			{
				var x = prop.GetValue(obj);
				if (x is Array)	jo.Add(JToken.FromObject(x));
				else jo.Add(prop.Name, (string)x);
			}
			return jo;
		}

		private static object GetDefaultValue(Type t)
		{
			if (t.IsValueType) return Activator.CreateInstance(t);
			return null;
		}

		public static void FromJObject(object obj, JObject jo)
		{
			var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic|BindingFlags.Instance).Where(x => x.IsDefined(typeof(Persist), false));

			foreach (var prop in props)
			{
				if (prop.PropertyType.IsArray)
				{
					var array = (Array)prop.GetValue(obj);
					jo.GetValue(prop.Name).Select(x=>Convert.ChangeType(x, prop.PropertyType.GetElementType())).ToArray().CopyTo(array, 0);
				}
				else
				{ 
					prop.SetValue(obj, Convert.ChangeType(jo.GetValue(prop.Name)??GetDefaultValue(prop.PropertyType), prop.PropertyType));
				}
			}

			var fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.IsDefined(typeof(Persist), false));
			foreach (var prop in fields)
			{
				if (prop.FieldType.IsArray)
				{
					var array = (Array)prop.GetValue(obj);
					jo.GetValue(prop.Name).Select(x => Convert.ChangeType(x, prop.FieldType.GetElementType())).ToArray().CopyTo(array, 0);
				}
				else
				{
					prop.SetValue(obj, Convert.ChangeType(jo.GetValue(prop.Name)??GetDefaultValue(prop.FieldType), prop.FieldType));
				}
			}
		}
	}
}
