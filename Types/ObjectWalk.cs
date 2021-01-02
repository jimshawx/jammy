using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RunAmiga.Types
{
	public class ObjectWalk
	{
		private void DumpObj(object obj, StringBuilder sb, int depth)
		{
			const string space = " ";
			const string space2 = "  ";

			//if (depth > 5) return;
			if (obj == null)
			{
				for (int j = 0; j < depth; j++)
					sb.Append(space);
				sb.Append($"(null)");
				return;
			}

			var properties = obj.GetType().GetProperties().OrderBy(x => x.MetadataToken).ToList();

			if (!properties.Any())
			{
				sb.Append($"{space2}{obj:X8} {obj}");
				sb.Append("\n");
				return;
			}

			foreach (var p in properties)
			{
				for (int j = 0; j < depth; j++)
					sb.Append(space);

				sb.Append($"{p.Name} ");

				if (p.PropertyType == typeof(string))
				{
					sb.Remove(sb.Length - 1, 1);
					sb.Append($"{space2}{p.GetValue(obj)}");
				}
				else if (p.PropertyType.BaseType == typeof(Array))
				{
					sb.Append("\n");
					var array = (Array)p.GetValue(obj);
					for (int i = 0; i < array.Length; i++)
					{
						for (int j = 0; j < depth; j++)
							sb.Append(space);
						sb.Append($"[{i}]");
						object v = array.GetValue(i);
						if (v != null)
						{
							sb.Append("\n");
							DumpObj(v, sb, depth + 1);
						}
						else
						{
							sb.Append("(null)\n");
						}
					}
					sb.Remove(sb.Length - 1, 1);
				}
				else if (p.PropertyType.BaseType == typeof(object))
				{
					object v = p.GetValue(obj);
					if (v != null)
					{
						sb.Append("\n");
						DumpObj(v, sb, depth + 1);
					}
					else
					{
						sb.Append("(null)");
					}
				}
				else if (p.PropertyType.BaseType == typeof(Enum))
				{
					sb.Append($"{space2}{p.GetValue(obj)}");
				}
				else
				{
					sb.Append($"{space2}{p.GetValue(obj):X8} {p.GetValue(obj)}");
				}
				sb.Append("\n");
			}
		}

		//public override string ToString()
		//{
		//	var sb = new StringBuilder();
		//	DumpObj(this, sb, 0);
		//	return sb.ToString();
		//}

		//public override string ToString()
		//{
		//	return JsonConvert.SerializeObject(this);
		//}

		public class IntNumberConverter : JsonConverter
		{
			public override bool CanRead => false;

			public override bool CanWrite => true;

			public override bool CanConvert(Type objectType)
			{
				return objectType == typeof(UInt32) ||
					objectType == typeof(Int32);
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				throw new NotImplementedException();
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				writer.WriteValue($"0x{value:X8} {value}");
			}
		}
		public class WordNumberConverter : JsonConverter
		{
			public override bool CanRead => false;

			public override bool CanWrite => true;

			public override bool CanConvert(Type objectType)
			{
				return 
					objectType == typeof(UInt16) ||
					objectType == typeof(Int16);
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				throw new NotImplementedException();
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				writer.WriteValue($"0x{value:X4} {value}");
			}
		}
		public class ByteNumberConverter : JsonConverter
		{
			public override bool CanRead => false;

			public override bool CanWrite => true;

			public override bool CanConvert(Type objectType)
			{
				return 
					objectType == typeof(Byte) ||
					objectType == typeof(SByte);
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				throw new NotImplementedException();
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				writer.WriteValue($"0x{value:X2} {value}");
			}
		}
		public override string ToString()
		{
			var serializer = new JsonSerializer { Formatting = Formatting.Indented };
			serializer.Converters.Add(new ByteNumberConverter());
			serializer.Converters.Add(new WordNumberConverter());
			serializer.Converters.Add(new IntNumberConverter());
			using (var sw = new StringWriter())
			{
				using (var writer = new JsonTextWriter(sw))
				{
					serializer.Serialize(writer, this);
				}
				sw.Flush();
				return sw.ToString();
			}
		}
	}
}
