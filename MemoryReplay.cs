using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RunAmiga.Types;

namespace RunAmiga
{
	public class MemoryReplay : IMemoryMappedDevice, IEmulate
	{
		private class Operation
		{
			public uint PC;
			public ReadWrite RW;
			public uint Address;
			public uint Value;
			public Size Size;
		}

		private enum ReadWrite
		{
			Read,
			Write
		}

		private enum Mode
		{
			Record,
			Playback
		}

		private Queue<Operation> operations = new Queue<Operation>();
		private Queue<Operation> used = new Queue<Operation>();
		private Mode mode = Mode.Record;

		public void Emulate(ulong ns)
		{
			//Trace.WriteLine("Clear");
			mode = Mode.Record;
			//if (operations.Any())
			//{
			//	Trace.WriteLine($"Unused ops {operations.Count}");
			//	foreach (var o in operations)
			//		DMP2(o);
			//}

			operations.Clear();
			used.Clear();
		}

		public void Reset()
		{
			//Trace.WriteLine("Reset");
			mode = Mode.Record;
			operations.Clear();
			used.Clear();
		}

		public bool IsMapped(uint address)
		{
			return mode == Mode.Playback;
		}

		public void SetRecording()
		{
			mode = Mode.Record;
		}

		public void SetPlayback()
		{
			mode = Mode.Playback;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			var op = DMP(operations.Dequeue());
			if (op.Size != size) throw new ApplicationException("read size");
			if (op.Address != address) throw new ApplicationException("read address");
			if (op.RW != ReadWrite.Read) throw new ApplicationException("read rw");
			return op.Value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			var op = DMP(operations.Peek());

			if (size == Size.Word && op.Size == Size.Long
			    && address == op.Address + 2 && op.RW == ReadWrite.Write)
			{
				//split a recorded Long into two Word
				if ((op.Value & 0xffff) != value) throw new ApplicationException("write value word");
				op.Value = op.Value >> 16;
				op.Size = Size.Word;
				return;
			}
			else if (size == Size.Long && op.Size == Size.Word && address == op.Address && op.RW == ReadWrite.Write)
			{
				//join two Word into Long
				op = operations.Dequeue();
				uint v = op.Value;
				op = operations.Dequeue();
				op.Size = Size.Long;
				op.Value |= v << 16;
			}
			else
			{
				op = operations.Dequeue();
				used.Enqueue(op);
			}

			//if (op.Size != size) throw new ApplicationException("write size");
			//if (op.Address != address) throw new ApplicationException("write address");
			//if (op.Value != value) throw new ApplicationException("write value");
			//if (op.RW != ReadWrite.Write) throw new ApplicationException("write rw");
		}

		public void LogRead(uint insaddr, uint address, Size size, uint value)
		{
			operations.Enqueue(DMP(new Operation
			{
				PC = insaddr,
				Value = value,
				Address = address,
				Size = size,
				RW = ReadWrite.Read
			}));
		}

		public void LogWrite(uint insaddr, uint address, Size size, uint value)
		{
			operations.Enqueue(DMP(new Operation
			{
				PC = insaddr,
				Value = value,
				Address = address,
				Size = size,
				RW = ReadWrite.Write
			}));
		}

		private Operation DMP(Operation op)
		{
			//Trace.WriteLine($"{mode} {op.Address:X6} {op.Value:X8} {op.RW} {op.Size}");
			return op;
		}
		private Operation DMP2(Operation op)
		{
			Trace.WriteLine($"{mode} {op.Address:X6} {op.Value:X8} {op.RW} {op.Size}");
			return op;
		}
	}
}