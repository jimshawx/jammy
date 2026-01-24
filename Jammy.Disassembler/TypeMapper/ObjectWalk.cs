using DbUp.ScriptProviders;
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
		public uint Offset { get; set; }
		public uint BaseOffset { get; set; }
		public uint Size { get; set; }
		public Type ObjType { get; set; }
		public string Name { get; set; }
		public string FullName { get; set; }
		public string Value1 { get; set; }
		public string Value2 { get; set; }
		public List<WalkEntry> Children { get; } = new List<WalkEntry>();
	}

	public class LibOffset
	{
		public string Name { get; set; }
		public uint Offset { get; set; }
		public uint Size { get; set; }
	}

	public class ObjectWalk
	{
		private static void Align(ref uint offset)
		{
			//pack 2
			offset++;
			offset &= 0xfffffffe;
			//pack 4
			//offset+=3;
			//offset &= 0xfffffffc;
		}

		private static uint DumpObj(object obj, uint offset, WalkEntry parent)
		{
			if (obj == null)
				return offset;

			var properties = obj.GetType().GetProperties().OrderBy(x => x.MetadataToken).ToList();

			if (!properties.Any())
			{
				WalkEntry p;
				if (parent.Type == WalkEntryType.ArrayElement)
				{
					p = parent;
				}
				else
				{ 
					p = new WalkEntry();
					parent.Children.Add(p);
					p.Type = WalkEntryType.Integer2;
					p.Name = $"x";
				}

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
				tree.ObjType = p.PropertyType;

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
					uint off = 0;
					for (int i = 0; i < array.Length; i++)
					{
						var t = new WalkEntry();
						tree.Children.Add(t);
						t.Offset = off;
						t.Name = $"[{i}]";
						t.Type = WalkEntryType.ArrayElement;
						t.ObjType = p.PropertyType.GetElementType();
						object v = array.GetValue(i);
						if (v == null) v = Activator.CreateInstance(p.PropertyType.GetElementType());
						uint size = DumpObj(v, 0, t);
						t.Size = size;
						//bit of a hack to deal with arrays of primitive types
						if (t.Children.Count == 0)
							t.Offset = off;
						offset += size;
						off += size;
					}
				}
				else if (p.PropertyType.BaseType == typeof(object))
				{
					Align(ref offset);
					tree.Type = WalkEntryType.Object;

					object v = p.GetValue(obj);
					if (v == null) v = Activator.CreateInstance(p.PropertyType);
					uint size = DumpObj(v, 0, tree);
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
					if (v != null)
					{
						tree.Value1 = $"{v.Address:X8}";
						tree.Value2 = v.Address;
					}

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

		private static WalkEntry WalkObject(object o)
		{
			var root = new WalkEntry();
			uint size = DumpObj(o, 0, root);
			root.Size = size;
			root.Type = WalkEntryType.Root;
			root.Name = o.GetType().Name;
			root.ObjType = o.GetType();
			
			//complete the offsets (doesn't make sense with objects with non-null pointers)
			WalkOffsets(root, 0);
			//complete the names
			WalkNames(root,"");

			return root;
		}

		public static string Walk(object o)
		{ 
			var root = WalkObject(o);

			var sb = new StringBuilder();
			sb.Append('\n');
			Dump(sb,root,0);

			var rv = new List<LibOffset>();
			DumpLeaves(rv, root);

			var sb2 = new StringBuilder();
			sb2.Append('\n');
			foreach (var r in rv)
				sb2.Append($"{r.Offset,4} {r.Size,4} {r.Name}\n");

			return sb.ToString() + sb2.ToString();
		}

		public static List<LibOffset> GetLibraryOffsets(object o)
		{
			var root = WalkObject(o);
			var rv = new List<LibOffset>();
			DumpLeaves(rv, root);
			return rv;
		}


		private static void Dump(StringBuilder sb, WalkEntry we, int depth)
		{
			//for (int i = 0; i < depth; i++)
			//	sb.Append(" ");
			string pad = new(' ', depth);
			string pad0 = new(' ', (depth>1)?2:0);

			sb.Append($"{pad0}{we.Offset,4} {we.Size,3} {we.BaseOffset,4} {we.Type,-10} {we.ObjType.Name,-30} {pad} {we.Name,-20} {we.Value1,-10} {we.Value2,-10} {we.FullName}\n");
			foreach (var c in we.Children)
				Dump(sb, c, depth+1);
		}

		private static void DumpLeaves(List<LibOffset> offs, WalkEntry we)
		{
			if (we.Children.Count == 0)
			{
				offs.Add(new LibOffset
				{
					Name = we.FullName,
					Size = we.Size,
					Offset = we.BaseOffset
				});
			}

			foreach (var c in we.Children)
				DumpLeaves(offs, c);
		}

		private static void WalkOffsets(WalkEntry we, uint baseOffset)
		{
			we.BaseOffset = we.Offset + baseOffset;
			foreach (var c in we.Children)
				WalkOffsets(c, we.BaseOffset);
		}

		private static void WalkNames(WalkEntry we, string trace)
		{
			if (we.Name != null)
			{
				if (we.Name.StartsWith('['))
					trace = $"{trace}{we.Name}";
				else if (trace == "")
					trace = we.Name;
				else
					trace = $"{trace}.{we.Name}";
			}

			we.FullName = trace;

			foreach (var c in we.Children)
				WalkNames(c, trace);
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
