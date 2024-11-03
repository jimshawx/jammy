using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
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

		public static string Pack(byte[] mem)
		{
			using (var unzipped = new MemoryStream(mem))
			{
				using (var zipped = new MemoryStream())
				{
					using (var gz = new GZipStream(zipped, CompressionLevel.SmallestSize))
					{
						unzipped.CopyTo(gz);
					}
					return Convert.ToBase64String(zipped.ToArray());
				}
			}
		}

		public static byte[] Unpack(string mem)
		{
			using (var zipped = new MemoryStream(Convert.FromBase64String(mem)))
			{
				using (var unzipped = new MemoryStream())
				{
					using (var gz = new GZipStream(zipped, CompressionMode.Decompress))
					{
						gz.CopyTo(unzipped);
					}
					return unzipped.ToArray();
				}
			}
		}

		public static bool Is(JObject obj, string id)
		{
			return (obj.TryGetValue("id", StringComparison.InvariantCulture, out var k) && k.ToString() == id);
		}

		private class FieldProp
		{
			public FieldProp(FieldInfo field)
			{
				GetValue = field.GetValue;
				SetValue = field.SetValue;
				Name = field.Name;
				PropertyType = field.FieldType;
			}
			
			public FieldProp(PropertyInfo prop)
			{
				GetValue = prop.GetValue;
				SetValue = prop.SetValue;
				Name = prop.Name;
				PropertyType = prop.PropertyType;
			}

			public Func<object,object> GetValue { get; }
			public Action<object, object> SetValue { get; }
			public string Name { get; }
			public Type PropertyType { get; }
		}

		private static List<FieldProp> GetFieldsAndProperties(object obj)
		{
			var props = obj.GetType()
				.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(x => x.IsDefined(typeof(Persist), false));

			var fields = obj.GetType()
				.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(x => x.IsDefined(typeof(Persist), false));

			return props.Select(x=>new FieldProp(x))
						.Concat(fields.Select(x=>new FieldProp(x)))
						.ToList();
		}

		private static object GetDefaultValue(Type t)
		{
			if (t.IsValueType) return Activator.CreateInstance(t);
			return null;
		}

		public static JObject ToJObject(object obj, string id)
		{
			var jo = new JObject();
			jo.Add("id", id);
			var props = GetFieldsAndProperties(obj);
			foreach (var prop in props)
			{
				var x = prop.GetValue(obj);
				if (x is Array)	jo.Add(prop.Name, JToken.FromObject(x));
				else jo.Add(prop.Name, x.ToString());
			}
			return jo;
		}

		public static void FromJObject(object obj, JObject jo)
		{
			var props = GetFieldsAndProperties(obj);
			foreach (var prop in props)
			{
				if (prop.PropertyType.IsArray)
				{
					var array = (Array)prop.GetValue(obj);
					jo.GetValue(prop.Name)
						.Select(x=>Convert.ChangeType(x, prop.PropertyType.GetElementType()))
						.ToArray()
						.CopyTo(array, 0);
				}
				else
				{ 
					prop.SetValue(obj, Convert.ChangeType(jo.GetValue(prop.Name)??GetDefaultValue(prop.PropertyType), prop.PropertyType));
				}
			}
		}
	}
}
