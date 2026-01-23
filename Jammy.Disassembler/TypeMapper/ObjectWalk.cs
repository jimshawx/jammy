using Jammy.AmigaTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Disassembler.TypeMapper
{
	public enum WalkEntryType
	{
		Unknown,
		Integer,
		String,
		Array,
		Object,
		Enum,
		Pointer,
		Integer2,
		Unhandled,
		ArrayElement,
		Root
	}

	public class WalkEntry
	{
		public WalkEntryType Type { get; set; }
		public int Offset { get; set; }
		public int Size { get; set; }
		public string Name { get; set; }
		public string Value1 { get; set; }
		public string Value2 { get; set; }
		public List<WalkEntry> Children { get; } = new List<WalkEntry>();
	}

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

		private static int DumpObj(object obj, int offset, WalkEntry parent)
		{
			if (obj == null)
				return offset;

			var properties = obj.GetType().GetProperties().OrderBy(x => x.MetadataToken).ToList();

			if (!properties.Any())
			{
				var p = new WalkEntry();
				parent.Children.Add(p);

				p.Type = WalkEntryType.Integer2;
				p.Name = $".";
				p.Offset = offset;
				p.Value1 = $"{obj:X8}";
				p.Value2 = $"{obj}";

				var objType = obj.GetType();

				if (objType == typeof(SByte) || objType == typeof(Byte))
				{ p.Size =1;	offset = 1; }
				else if (objType == typeof(Int16) || objType == typeof(UInt16))
				{ p.Size = 2;	offset = 2; }
				else if (objType == typeof(Int32) || objType == typeof(UInt32))
				{p.Size = 4;	offset = 4; }
				else
					p.Type = WalkEntryType.Unhandled;

				return offset;
			}

			foreach (var p in properties)
			{
				var tree = new WalkEntry();
				parent.Children.Add(tree);

				tree.Offset = offset;
				tree.Name = p.Name;

				if (p.PropertyType == typeof(string))
				{
					tree.Type = WalkEntryType.String;
					tree.Value1 = $"{p.GetValue(obj)}";
					tree.Value2 = null;
					tree.Size = 4;
					offset += 4;
				}
				else if (p.PropertyType.BaseType == typeof(Array))
				{
					Align(ref offset);
					tree.Type = WalkEntryType.Array;

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
						var t = new WalkEntry();
						tree.Children.Add(t);
						t.Offset = offset;
						t.Name = $"[{i}]";
						t.Type = WalkEntryType.ArrayElement;
						object v = array.GetValue(i);
						if (v == null) v = Activator.CreateInstance(p.PropertyType.GetElementType());
						int size = DumpObj(v, 0, t);
						t.Size = size;
						offset += size;
					}
					//sb.Remove(sb.Length - 1, 1);
				}
				else if (p.PropertyType.BaseType == typeof(object))
				{
					Align(ref offset);
					tree.Type = WalkEntryType.Object;

					object v = p.GetValue(obj);
					if (v == null) v = Activator.CreateInstance(p.PropertyType);
					int size = DumpObj(v, 0, tree);
					tree.Size = size;
					offset += size;
				}
				else if (p.PropertyType.BaseType == typeof(Enum))
				{
					Align(ref offset);
					tree.Type = WalkEntryType.Enum;
					tree.Offset = offset;
					tree.Value1 = $"{p.GetValue(obj)}";
					tree.Value2 = $"{Convert.ToInt32(p.GetValue(obj))}";
					tree.Size = 1;
					offset += 1;//enums are all bytes
				}
				else if (typeof(IWrappedPtr).IsAssignableFrom(p.PropertyType))
				{
					Align(ref offset);
					tree.Type = WalkEntryType.Pointer;
					tree.Offset = offset;

					dynamic v = p.GetValue(obj);

					//check for generic IWrapper<T>
					if (v != null && p.PropertyType.GetInterfaces().Any(x => x.GenericTypeArguments.Length > 0))
					{ 
						DumpObj(v.Wrapped, 0, tree);
					}
					offset += 4;
					tree.Size = 4;
				}
				else
				{
					tree.Type = WalkEntryType.Integer;
					if (p.PropertyType == typeof(SByte) || p.PropertyType == typeof(Byte))
					{
						tree.Value1 = $"{p.GetValue(obj):X2}";
						tree.Value2 = $"{p.GetValue(obj)}";
						tree.Size = 1;
						offset += 1;
					}
					else if (p.PropertyType == typeof(Int16) || p.PropertyType == typeof(UInt16))
					{
						Align(ref offset);
						tree.Offset = offset;
						tree.Value1 = $"{p.GetValue(obj):X4}";
						tree.Value2 = $"{p.GetValue(obj)}";
						tree.Size = 2;
						offset += 2;
					}
					else if (p.PropertyType == typeof(Int32) || p.PropertyType == typeof(UInt32))
					{
						Align(ref offset);
						tree.Offset = offset;
						tree.Value1 = $"{p.GetValue(obj):X8}";
						tree.Value2 = $"{p.GetValue(obj)}";
						tree.Size = 4;
						offset += 4; 
					}
					else
					{
						tree.Type = WalkEntryType.Unhandled;
					}
				}
			}
			return offset;
		}

		public static string Walk(object o)
		{
			var sb = new StringBuilder();
			var we = new WalkEntry();
			int size = DumpObj(o, 0, we);
			we.Size = size;
			we.Type = WalkEntryType.Root;
			we.Name = o.GetType().Name;
			Dump(sb,we,0, "");
			return $"sizeof({o.GetType().Name})={size}\n" + sb.ToString();
		}

		private static void Dump(StringBuilder sb, WalkEntry we, int depth, string trace)
		{
			for (int i = 0; i < depth; i++)
				sb.Append(" ");

			if (we.Name != null)
			{ 
				if (we.Name.StartsWith('['))
					trace = $"{trace}{we.Name}";
				else if (trace == "")
					trace = we.Name;
				else
					trace = $"{trace}.{we.Name}";
			}

			sb.Append($"o:{we.Offset,5} s:{we.Size} {we.Type} {we.Name} {we.Value1} {we.Value2} {trace}\n");
			foreach (var c in we.Children)
				Dump(sb, c, depth+1, trace);
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
