using Jammy.AmigaTypes;
using System;
using System.Linq;
using System.Text;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Disassembler.TypeMapper
{
	public class ObjectWalk
	{
		private static void Align(ref int offset)
		{
			//pack 2
			offset++;
			offset &= 0x7ffffffe;
			//pack 4
			//offset+=3;
			//offset &= 0x7ffffffc;
		}

		private static int DumpObj(object obj, StringBuilder sb, int depth, int offset)
		{
			const string space = " ";
			const string space2 = "  ";

			//if (depth > 5) return;
			if (obj == null)
			{
				for (int j = 0; j < depth; j++)
					sb.Append(space);
				sb.Append($"(null)");
				return offset;
			}

			var properties = obj.GetType().GetProperties().OrderBy(x => x.MetadataToken).ToList();

			if (!properties.Any())
			{
				sb.Append($"{space2}{offset,4} {obj:X8} {obj}");
				sb.Append("\n");

				var objType = obj.GetType();

				if (objType == typeof(SByte) || objType == typeof(Byte))
					offset = 1;
				else if (objType == typeof(Int16) || objType == typeof(UInt16))
					offset = 2;
				else if (objType == typeof(Int32) || objType == typeof(UInt32))
					offset = 4;
				else
					sb.Append($"*** unknown type with no properties {objType.Name} ***");

				return offset;
			}

			foreach (var p in properties)
			{
				for (int j = 0; j < depth; j++)
					sb.Append(space);

				sb.Append($"{offset,4} {p.Name} ");

				if (p.PropertyType == typeof(string))
				{
					sb.Remove(sb.Length - 1, 1);
					sb.Append($"{space2}{offset,4} {p.GetValue(obj)}");
					offset += 4;
				}
				else if (p.PropertyType.BaseType == typeof(Array))
				{
					Align(ref offset);

					sb.Append("\n");
					var array = (Array)p.GetValue(obj);
					if (array == null)
					{
						//it didn't work, because the array on 'obj' hasn't been initialised
						//does it have an AmigaArraySize attribute?
						var sizeAttr = p.GetCustomAttributes(typeof(AmigaArraySize), false).SingleOrDefault();
						if (sizeAttr != null)
						{
							p.SetValue(obj, Activator.CreateInstance(p.PropertyType, ((AmigaArraySize)sizeAttr).Size));
							array = (Array)p.GetValue(obj);
						}
						else
						{
							//empty array (or should the following code support null arrays?)
							array = Array.CreateInstance(p.PropertyType.GetElementType(), 0);
						}
					}
					for (int i = 0; i < array.Length; i++)
					{
						for (int j = 0; j < depth; j++)
							sb.Append(space);
						sb.Append($"{offset,4} [{i}]");
						object v = array.GetValue(i);
						if (v == null) v = Activator.CreateInstance(p.PropertyType.GetElementType());
						sb.Append("\n");
						offset += DumpObj(v, sb, depth + 1, 0);
					}
					sb.Remove(sb.Length - 1, 1);
				}
				else if (p.PropertyType.BaseType == typeof(object))
				{
					Align(ref offset);

					object v = p.GetValue(obj);
					if (v == null) v = Activator.CreateInstance(p.PropertyType);
					sb.Append("\n");
					offset += DumpObj(v, sb, depth + 1, 0);
				}
				else if (p.PropertyType.BaseType == typeof(Enum))
				{
					Align(ref offset);

					sb.Append($"{space2}\t{offset,4} {p.GetValue(obj)} {Convert.ToInt32(p.GetValue(obj))}");
					offset += 1;//enums are all bytes
				}
				else if (typeof(IWrappedPtr).IsAssignableFrom(p.PropertyType))
				{
					Align(ref offset);

					dynamic v = p.GetValue(obj);

					//check for generic IWrapper<T>
					if (v != null && p.PropertyType.GetInterfaces().Any(x => x.GenericTypeArguments.Length > 0))
					{ 
						sb.Append("\n");
						DumpObj(v.Wrapped, sb, depth + 1, 0);
					}
					offset += 4;
				}
				else
				{
					if (p.PropertyType == typeof(SByte) || p.PropertyType == typeof(Byte))
					{ sb.Append($"{space2}\t{offset,4} {p.GetValue(obj):X2} {p.GetValue(obj)}"); offset += 1; }
					else if (p.PropertyType == typeof(Int16) || p.PropertyType == typeof(UInt16))
					{ Align(ref offset); sb.Append($"{space2}\t{offset,4} {p.GetValue(obj):X4} {p.GetValue(obj)}"); offset += 2; }
					else
					{ Align(ref offset); sb.Append($"{space2}\t{offset,4} {p.GetValue(obj):X8} {p.GetValue(obj)}"); offset += 4; }
				}
				sb.Append("\n");
			}
			return offset;
		}

		public static string Walk(object o)
		{
			var sb = new StringBuilder();
			int size = DumpObj(o, sb, 0, 0);
			return $"sizeof({o.GetType().Name})={size}" + sb.ToString();
		}

		//public override string ToString()
		//{
		//	return JsonConvert.SerializeObject(this);
		//}

		//public class IntNumberConverter : JsonConverter
		//{
		//	public override bool CanRead => false;

		//	public override bool CanWrite => true;

		//	public override bool CanConvert(Type objectType)
		//	{
		//		return objectType == typeof(UInt32) ||
		//			objectType == typeof(Int32);
		//	}

		//	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		//	{
		//		throw new NotImplementedException();
		//	}

		//	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		//	{
		//		writer.WriteValue($"0x{value:X8} {value}");
		//	}
		//}
		//public class WordNumberConverter : JsonConverter
		//{
		//	public override bool CanRead => false;

		//	public override bool CanWrite => true;

		//	public override bool CanConvert(Type objectType)
		//	{
		//		return 
		//			objectType == typeof(UInt16) ||
		//			objectType == typeof(Int16);
		//	}

		//	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		//	{
		//		throw new NotImplementedException();
		//	}

		//	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		//	{
		//		writer.WriteValue($"0x{value:X4} {value}");
		//	}
		//}
		//public class ByteNumberConverter : JsonConverter
		//{
		//	public override bool CanRead => false;

		//	public override bool CanWrite => true;

		//	public override bool CanConvert(Type objectType)
		//	{
		//		return 
		//			objectType == typeof(Byte) ||
		//			objectType == typeof(SByte);
		//	}

		//	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		//	{
		//		throw new NotImplementedException();
		//	}

		//	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		//	{
		//		writer.WriteValue($"0x{value:X2} {value}");
		//	}
		//}
		//public override string ToString()
		//{
		//	var serializer = new JsonSerializer { Formatting = Formatting.Indented };
		//	serializer.Converters.Add(new ByteNumberConverter());
		//	serializer.Converters.Add(new WordNumberConverter());
		//	serializer.Converters.Add(new IntNumberConverter());
		//	using (var sw = new StringWriter())
		//	{
		//		using (var writer = new JsonTextWriter(sw))
		//		{
		//			serializer.Serialize(writer, this);
		//		}
		//		sw.Flush();
		//		return sw.ToString();
		//	}
		//}
	}
}
