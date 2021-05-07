using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jammy.Core.Interface.Interfaces;
using Jammy.Interface;
using Jammy.Types.AmigaTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jammy.Disassembler.TypeMapper
{
	public class BaseMapper
	{
		private readonly IDebugMemoryMapper memory;
		private readonly IAmigaTypesMapper mapper;
		private readonly ILogger logger;
		
		private uint baseAddress;
		private readonly HashSet<long> lookup = new HashSet<long>();
		private StringBuilder sb;

		public BaseMapper(IDebugMemoryMapper memory)
		{
			this.memory = memory;
			mapper = new AmigaTypesMapper(memory);
			this.logger = ServiceProviderFactory.ServiceProvider.GetRequiredService<ILoggerProvider>().CreateLogger("BaseMapper");
		}

		private uint MapObject(Type type, object obj, uint addr, int depth)
		{
			if (lookup.Contains(addr+type.GetHashCode()))
			{
				return 0;
			}
			lookup.Add(addr+type.GetHashCode());

			uint startAddr = addr;
			var properties = type.GetProperties().Where(x=>x.CanWrite).OrderBy(x => x.MetadataToken).ToList();

			if (!properties.Any())
				throw new ApplicationException();

			uint lastAddr = addr;
			foreach (var prop in properties)
			{
				//if (depth == 0)
					sb.Append($"{addr:X8} {addr-baseAddress:X4} {addr - baseAddress,5} {prop.Name,-25} {prop.PropertyType}\n");

				if (prop.Name == "ln_Pred")
				{
					addr += 4;
					continue;
				}

				object rv = null;
				var propType = prop.PropertyType;
				try
				{
					if (typeof(IWrappedPtr).IsAssignableFrom(propType))
					{
						if (propType == typeof(TaskPtr))
						{
							var tp = new TaskPtr();
							tp.Address = memory.UnsafeRead32(addr); addr += 4;
							if (tp.Address != 0 && tp.Address < 0x1000000)
							{
								tp.Task = new Task();
								MapObject(typeof(Task), tp.Task, tp.Address, depth+1);
							}
							else
							{
								tp = null;
							}
							rv = tp;
						}
						else if (propType == typeof(NodePtr))
						{
							var tp = new NodePtr();
							tp.Address = memory.UnsafeRead32(addr); addr += 4;
							if (tp.Address != 0 && tp.Address < 0x1000000)
							{
								tp.Node = new Node();
								MapObject(typeof(Node), tp.Node, tp.Address, depth+1);
							}
							else
							{
								tp = null;
							}
							rv = tp;
						}
						else if (propType == typeof(MinNodePtr))
						{
							var tp = new MinNodePtr();
							tp.Address = memory.UnsafeRead32(addr); addr += 4;
							if (tp.Address != 0 && tp.Address < 0x1000000)
							{
								tp.MinNode = new MinNode();
								MapObject(typeof(MinNode), tp.MinNode, tp.Address, depth+1);
							}
							else
							{
								tp = null;
							}
							rv = tp;
						}
						else if (propType.GetInterfaces().Any(x=>x.GenericTypeArguments.Length > 0))
						{
							var genericT = propType.GetInterfaces().Single(x => x.GenericTypeArguments.Length > 0).GenericTypeArguments[0];

							dynamic tp = Activator.CreateInstance(propType);
							tp.Address = memory.UnsafeRead32(addr); addr += 4;
							if (tp.Address != 0 && tp.Address < 0x1000000)
							{
								tp.Wrapped = (dynamic)Convert.ChangeType(Activator.CreateInstance(genericT), genericT);
								MapObject(genericT, tp.Wrapped, tp.Address, depth + 1);
							}
							else
							{
								tp = null;
							}
							rv = tp;
						}
						else
						{
							throw new NotImplementedException();
						}
					}
					else if (propType == typeof(String))
					{
						rv = MapString(addr);
						addr += 4;
					}
					else if (propType.BaseType == typeof(Array))
					{
						var array = (Array)prop.GetValue(obj);
						var arrayType = array.GetType().GetElementType();

						if (arrayType.BaseType == typeof(object))
						{ 
							for (int i = 0; i < array.Length; i++)
							{
								array.SetValue(Activator.CreateInstance(arrayType), i);
								addr += MapObject(arrayType, array.GetValue(i), addr, depth+1);
							}
						}
						else
						{
							for (int i = 0; i < array.Length; i++)
							{
								object s = mapper.MapSimple(arrayType, addr);
								array.SetValue(s, i);
								addr += mapper.GetSize(s);
								
							}
						}
						rv = array;
					}
					else if (propType == typeof(List))
					{
						var list = new List();
						rv = list;
						uint size = MapObject(propType, rv, addr, depth + 1);
						//it's an empty list
						if (list.lh_TailPred == null || list.lh_TailPred.Address == addr)
							list.lh_Head = list.lh_Tail = list.lh_TailPred = null;
						addr += size;
					}
					else if (propType.BaseType == typeof(object))
					{
						rv = Activator.CreateInstance(propType);
						addr += MapObject(propType, rv, addr, depth+1);
					}
					else
					{
						rv = mapper.MapSimple(propType, addr);
						addr += mapper.GetSize(rv);
					}
					
					prop.SetValue(obj, rv);
				}
				catch (NullReferenceException ex)
				{
					logger.LogTrace($"Problem Mapping {prop.Name} was null\n{ex}");
				}
				catch (Exception ex)
				{
					if (rv != null)
						logger.LogTrace($"Problem Mapping {prop.Name} {prop.PropertyType} != {rv.GetType()}\n{ex}");
					else
						logger.LogTrace($"Problem Mapping {prop.Name} {prop.PropertyType}\n{ex}");
				}
			}
			return addr - startAddr;
		}

		private string MapString(uint addr)
		{
			uint strPtr = memory.UnsafeRead32(addr);

			if (strPtr == 0)
				return "(null)";

			var str = new StringBuilder();
			for (; ; )
			{
				byte c = memory.UnsafeRead8(strPtr);
				if (c == 0)
					return str.ToString();

				str.Append(Convert.ToChar(c));
				strPtr++;
			}
		}

		public string FromAddress(object amigaObj, uint address)
		{
			baseAddress = address;

			lookup.Clear();

			sb = new StringBuilder();

			//maps the memory at "address" into "amigaObj"
			MapObject(amigaObj.GetType(), amigaObj, address, 0);

			//Walk the object writing out property names, offsets and values
			return ObjectWalk.Walk(amigaObj) + "\n" + sb.ToString();
		}
	}

	public class ObjectMapper
	{
		public static string MapObject(object tp, uint address)
		{
			var memory = ServiceProviderFactory.ServiceProvider.GetRequiredService<IDebugMemoryMapper>();
			return new BaseMapper(memory).FromAddress(tp, address);
		}

		public static string MapObject(object tp, byte[] b, uint address)
		{
			var memory = new ByteArrayDebugMemoryMapper(b);
			return new BaseMapper(memory).FromAddress(tp, address);
		}
	}
}
