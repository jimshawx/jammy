using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Disassembler.AmigaTypes;

namespace RunAmiga.Disassembler.TypeMapper
{
	public class ExecBaseMapper : BaseMapper<ExecBase>
	{
		private readonly IMemory memory;

		public ExecBaseMapper(IMemory memory) : base(memory)
		{
			this.memory = memory;
		}

		public string FromAddress()
		{
			uint execAddress = memory.Read32(4);
			if (execAddress == 0xc00276)
				return FromAddress(execAddress);

			return string.Empty;
		}
	}

	public class timerequestMapper : BaseMapper<timerequest>
	{
		public timerequestMapper(IMemory memory) : base(memory)
		{
		}
	}

	public class BaseMapper<T> where T : ObjectWalk, new() 
	{
		private readonly IMemory memory;
		private readonly ILogger logger;
		
		private uint baseAddress;
		private readonly HashSet<long> lookup = new HashSet<long>();
		private StringBuilder sb;

		public BaseMapper(IMemory memory)
		{
			this.memory = memory;
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
			var properties = type.GetProperties().OrderBy(x => x.MetadataToken).ToList();

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
							tp.Address = memory.Read32(addr); addr += 4;
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
							tp.Address = memory.Read32(addr); addr += 4;
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
							tp.Address = memory.Read32(addr); addr += 4;
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
							tp.Address = memory.Read32(addr); addr += 4;
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
								object s = AmigaTypesMapper.MapSimple(memory, arrayType, addr);
								array.SetValue(s, i);
								addr += AmigaTypesMapper.GetSize(s);
								
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
						rv = AmigaTypesMapper.MapSimple(memory, propType, addr);
						addr += AmigaTypesMapper.GetSize(rv);
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

		public string FromAddress(uint address)
		{
			baseAddress = address;

			lookup.Clear();

			sb = new StringBuilder();

			var amigaObj = new T();
			MapObject(typeof(T), amigaObj, address, 0);

			return amigaObj.ToString() + "\n"+ sb.ToString();
		}

		public string MapString(uint addr)
		{
			uint str = memory.Read32(addr);

			if (str == 0)
				return "(null)";

			var sb = new StringBuilder();
			for (; ; )
			{
				byte c = memory.Read8(str);
				if (c == 0)
					return sb.ToString();

				sb.Append(Convert.ToChar(c));
				str++;
			}
		}
	}
}
