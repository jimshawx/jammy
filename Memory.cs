using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Schema;
using Size = RunAmiga.Types.Size;

namespace RunAmiga
{
	public class Memory : IMemoryMappedDevice, IEmulate
	{
		private readonly byte[] memory;
		private const uint memoryMask = 0x00ffffff;

		private readonly Debugger debugger;
		private readonly string id;

		public Memory(Debugger debugger, string id)
		{
			this.debugger = debugger;
			this.id = id;
			this.memory = new byte[16 * 1024 * 1024];
		}

		public bool IsMapped(uint address)
		{
			return true;
		}

		public uint read32(uint address)
		{
			if ((address & 1) != 0)
				throw new MemoryAlignmentException(address);

			return ((uint)memory[address & memoryMask] << 24) +
				((uint)memory[(address + 1) & memoryMask] << 16) +
				((uint)memory[(address + 2) & memoryMask] << 8) +
				(uint)memory[(address + 3) & memoryMask];
		}

		public ushort read16(uint address)
		{
			if ((address & 1) != 0)
				throw new MemoryAlignmentException(address);

			return (ushort)(
				((ushort)memory[address & memoryMask] << 8) +
				(ushort)memory[(address + 1) & memoryMask]);
		}

		public byte read8(uint address)
		{
			return memory[address & memoryMask];
		}

		public void write32(uint address, uint value)
		{
			if ((address & 1) != 0)
				throw new MemoryAlignmentException(address);

			byte b0, b1, b2, b3;
			b0 = (byte)(value >> 24);
			b1 = (byte)(value >> 16);
			b2 = (byte)(value >> 8);
			b3 = (byte)(value);
			memory[address & memoryMask] = b0;
			memory[(address + 1) & memoryMask] = b1;
			memory[(address + 2) & memoryMask] = b2;
			memory[(address + 3) & memoryMask] = b3;
		}

		public void write16(uint address, ushort value)
		{
			if ((address & 1) != 0)
				throw new MemoryAlignmentException(address);

			byte b0, b1;
			b0 = (byte)(value >> 8);
			b1 = (byte)(value);
			memory[address & memoryMask] = b0;
			memory[(address + 1) & memoryMask] = b1;
		}

		public void write8(uint address, byte value)
		{
			memory[address & memoryMask] = value;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			if (size == Size.Byte) return read8(address);
			if (size == Size.Word) return read16(address);
			if (size == Size.Long) return read32(address);
			throw new UnknownInstructionSizeException(insaddr, 0);
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			//if ((address >= 0xc014cd && address <= 0xC014ff && address != 0xc014f4) || (address == 0xc014f4 && (insaddr == 0xfc1798 || insaddr == 0)))
			//{
			//	if (size == Size.Long)
			//	{
			//		Write(insaddr, address + 2, (ushort)value, Size.Word);
			//		Write(insaddr, address,   (ushort)(value>>16), Size.Word);
			//		return;
			//	}
			//	Logger.WriteLine($"[LOG{id}] a:{address:X8} v:{value:X8} pc:{insaddr:X8} s:{size}");
			//}

			if (size == Size.Byte) { write8(address, (byte)value); return; }
			if (size == Size.Word) { write16(address, (ushort)value); return; }
			if (size == Size.Long) { write32(address, value); return; }
			throw new UnknownInstructionSizeException(insaddr, 0);
		}

		public byte[] GetMemoryArray()
		{
			return memory;
		}

		public void Clear()
		{
			Array.Clear(memory, 0, memory.Length);
		}

		public byte Read8(uint address)
		{
			if (address >= 0x1000000)
			{
				//Logger.WriteLine($"Memory Read Byte from {address:X8}");
				return 0;
			}
			return memory[address];
		}

		public ushort Read16(uint address)
		{
			if (address >= 0xfffffe)
			{
				Logger.WriteLine($"Memory Read Word from ${address:X8}");
				return 0;
			}
			if ((address & 1) != 0)
			{
				Logger.WriteLine($"Memory Read Unaligned Word from ${address:X8}");
				return 0;
			}
			return (ushort)(((ushort)memory[address] << 8) +
							(ushort)memory[(address + 1)]);
		}

		public uint Read32(uint address)
		{
			if (address >= 0xfffffc)
			{
				Logger.WriteLine($"Memory Read Int from ${address:X8}");
				return 0;
			}
			if ((address & 1) != 0)
			{
				Logger.WriteLine($"Memory Read Unaligned Int from ${address:X8}");
				return 0;
			}
			return ((uint)memory[address] << 24) +
					((uint)memory[(address + 1)] << 16) +
					((uint)memory[(address + 2)] << 8) +
					(uint)memory[(address + 3)];
		}

		public void Emulate(ulong ns)
		{

		}

		public void BulkWrite(int dst, byte[] src, int length)
		{
			Array.Copy(src, 0, memory, dst, length);
		}

		[DllImport("gdi32.dll")]
		static extern uint FloodFill(IntPtr hdc, int x, int y, int color);

		[DllImport("gdi32.dll")]
		static extern uint SetDCBrushColor(IntPtr hdc, int color);

		[DllImport("gdi32.dll")]
		static extern int GetStockObject(IntPtr hdc, int color);

		[DllImport("gdi32.dll")]
		static extern uint SelectObject(IntPtr hdc, int obj);

		public void Reset()
		{
			Array.Clear(memory, 0, memory.Length);

			byte[] rom = File.ReadAllBytes("../../../../kick12.rom");
			Debug.Assert(rom.Length == 256 * 1024);

			BulkWrite(0xfc0000, rom, 256 * 1024);
			BulkWrite(0, rom, 256 * 1024);

			//byte[] rom = File.ReadAllBytes("../../../../kick13.rom");
			//Debug.Assert(rom.Length == 256 * 1024);

			//BulkWrite(0xfc0000, rom, 256 * 1024);
			//BulkWrite(0, rom, 256 * 1024);

			//byte[] rom = File.ReadAllBytes("../../../../kick31.rom");
			//Debug.Assert(rom.Length == 512 * 1024);

			//BulkWrite(0xf80000, rom, 512 * 1024);
			//BulkWrite(0, rom, 512 * 1024);

			//KSLogo(rom);
		}

		private void KSLogo(byte[]rom)
		{
			for (int i = 0xfc0000; i < 0xfc0000 + 256 * 1024 * 1024 - kslogo.Length; i++)
			{
				if (kslogo.SequenceEqual(rom.Skip(i - 0xfc0000).Take(kslogo.Length)))
				{
					Logger.WriteLine($"Found the kickstart logo at {i:X8}");
					break;
				}
			}

			int k = 0;
			byte b0, b1;
			int mode = 0; //0 unknown, 1 polyline start, 2 polyline, 3 fill
			const int ox = 70, oy = 40;
			var form = new Form{ClientSize = new System.Drawing.Size(320,200)};
			form.Show();
			var g = form.CreateGraphics();
			int dx = 0, dy = 0;
			Pen p = new Pen(Color.Blue);//.FromArgb(0xff0000ff));
			var hdc = g.GetHdc();
			int dcbrush = GetStockObject(hdc, 18);
			SelectObject(hdc, dcbrush);
			SetDCBrushColor(hdc, 0x0000ff);
			g.ReleaseHdc();
			int miny = 200;
			for (;;)
			{
				b0 = kslogo[k++];
				b1 = kslogo[k++];
				if (b0 == 0xff && b1 == 0xff) break;
				if (b0 == 0xfe)
				{
					Logger.WriteLine($"fill colour {b1}");
					mode = 3;
				}
				else if (b0 == 0xff)
				{
					Logger.WriteLine($"colour {b1}");
					mode = 1;
				}
				else
				{
					if (mode == 0) Logger.WriteLine("unknown mode");
					else if (mode == 1)
					{
						Logger.WriteLine($"move {ox+b0},{oy+b1}");
						dx = ox + b0;
						dy = oy + b1;
						
						mode = 2;
					}
					else if (mode == 2)
					{
						Logger.WriteLine($"draw {ox + b0},{oy + b1} // {ox + b0 - dx},{oy + b1 - dy}");
						int nx = ox + b0, ny = oy + b1;

						g.DrawLine(p, dx, dy, nx, ny);
						if (ny == dy)
						{
							if (ny < miny) miny = ny;
						}

						dx = nx;
						dy = ny;
					}
					else if (mode == 3)
					{
						Logger.WriteLine($"fill {ox + b0},{oy + b1}");
						hdc = g.GetHdc();
						FloodFill(hdc, ox + b0, oy + b1, 0xff0000);
						g.ReleaseHdc();
						mode = 0;
					}
				}
			}

			Application.DoEvents();
		}

		//ks logo
		private byte [] kslogo =
		{
			0xFF, 0x01, 0x23, 0x0B, 0x3A, 0x0B, 0x3A, 0x21, 0x71, 0x21, 0x71, 0x0B, 0x7D, 0x0B, 0x88, 0x16, 0x88, 0x5E, 0x7F, 0x5E, 0x7F, 0x38, 0x40, 0x38,
			0x3E, 0x36, 0x35, 0x36, 0x34, 0x38, 0x2D, 0x38, 0x2D, 0x41, 0x23, 0x48, 0x23, 0x0B, 0xFE, 0x02, 0x25, 0x45, 0xFF, 0x01, 0x21, 0x48, 0x21, 0x0A,
			0x7E, 0x0A, 0x8A, 0x16, 0x8A, 0x5F, 0x56, 0x5F, 0x56, 0x64, 0x52, 0x6C, 0x4E, 0x71, 0x4A, 0x74, 0x44, 0x7D, 0x3C, 0x81, 0x3C, 0x8C, 0x0A, 0x8C,
			0x0A, 0x6D, 0x09, 0x6D, 0x09, 0x51, 0x0D, 0x4B, 0x14, 0x45, 0x15, 0x41, 0x19, 0x3A, 0x1E, 0x37, 0x21, 0x36, 0x21, 0x36, 0x1E, 0x38, 0x1A, 0x3A,
			0x16, 0x41, 0x15, 0x45, 0x0E, 0x4B, 0x0A, 0x51, 0x0A, 0x6C, 0x0B, 0x6D, 0x0B, 0x8B, 0x28, 0x8B, 0x28, 0x76, 0x30, 0x76, 0x34, 0x72, 0x34, 0x5F,
			0x32, 0x5C, 0x32, 0x52, 0x41, 0x45, 0x41, 0x39, 0x3E, 0x37, 0x3B, 0x37, 0x3E, 0x3A, 0x3E, 0x41, 0x3D, 0x42, 0x36, 0x42, 0x33, 0x3F, 0x2A, 0x46,
			0x1E, 0x4C, 0x12, 0x55, 0x12, 0x54, 0x1E, 0x4B, 0x1A, 0x4A, 0x17, 0x47, 0x1A, 0x49, 0x1E, 0x4A, 0x21, 0x48, 0xFF, 0x01, 0x32, 0x3D, 0x34, 0x36,
			0x3C, 0x37, 0x3D, 0x3A, 0x3D, 0x41, 0x36, 0x41, 0x32, 0x3D, 0xFF, 0x01, 0x33, 0x5C, 0x33, 0x52, 0x42, 0x45, 0x42, 0x39, 0x7D, 0x39, 0x7D, 0x5E,
			0x34, 0x5E, 0x33, 0x5A, 0xFF, 0x01, 0x3C, 0x0B, 0x6F, 0x0B, 0x6F, 0x20, 0x3C, 0x20, 0x3C, 0x0B, 0xFF, 0x01, 0x60, 0x0E, 0x6B, 0x0E, 0x6B, 0x1C,
			0x60, 0x1C, 0x60, 0x0E, 0xFE, 0x03, 0x3E, 0x1F, 0xFF, 0x01, 0x62, 0x0F, 0x69, 0x0F, 0x69, 0x1B, 0x62, 0x1B, 0x62, 0x0F, 0xFE, 0x02, 0x63, 0x1A,
			0xFF, 0x01, 0x2F, 0x39, 0x32, 0x39, 0x32, 0x3B, 0x2F, 0x3F, 0x2F, 0x39, 0xFF, 0x01, 0x29, 0x8B, 0x29, 0x77, 0x30, 0x77, 0x35, 0x72, 0x35, 0x69,
			0x39, 0x6B, 0x41, 0x6B, 0x41, 0x6D, 0x45, 0x72, 0x49, 0x72, 0x49, 0x74, 0x43, 0x7D, 0x3B, 0x80, 0x3B, 0x8B, 0x29, 0x8B, 0xFF, 0x01, 0x35, 0x5F,
			0x35, 0x64, 0x3A, 0x61, 0x35, 0x5F, 0xFF, 0x01, 0x39, 0x62, 0x35, 0x64, 0x35, 0x5F, 0x4A, 0x5F, 0x40, 0x69, 0x3F, 0x69, 0x41, 0x67, 0x3C, 0x62,
			0x39, 0x62, 0xFF, 0x01, 0x4E, 0x5F, 0x55, 0x5F, 0x55, 0x64, 0x51, 0x6C, 0x4E, 0x70, 0x49, 0x71, 0x46, 0x71, 0x43, 0x6D, 0x43, 0x6A, 0x4E, 0x5F,
			0xFF, 0x01, 0x44, 0x6A, 0x44, 0x6D, 0x46, 0x70, 0x48, 0x70, 0x4C, 0x6F, 0x4D, 0x6C, 0x49, 0x69, 0x44, 0x6A, 0xFF, 0x01, 0x36, 0x68, 0x3E, 0x6A,
			0x40, 0x67, 0x3C, 0x63, 0x39, 0x63, 0x36, 0x65, 0x36, 0x68, 0xFF, 0x01, 0x7E, 0x0B, 0x89, 0x16, 0x89, 0x5E, 0xFE, 0x01, 0x22, 0x0B, 0xFE, 0x01,
			0x3B, 0x0B, 0xFE, 0x01, 0x61, 0x0F, 0xFE, 0x01, 0x6A, 0x1B, 0xFE, 0x01, 0x70, 0x0F, 0xFE, 0x01, 0x7E, 0x5E, 0xFE, 0x01, 0x4B, 0x60, 0xFE, 0x01,
			0x2E, 0x39, 0xFF, 0xFF
		};
		//https://retrocomputing.stackexchange.com/questions/13897/why-was-the-kickstart-1-x-insert-floppy-graphic-so-bad

		// The code uses the SetAPen, Move, Draw, Flood, and BltTemplate calls (and some others) from graphics.library to do all this. The screen resolution is set to 320x200 (2 bitplanes; 4 colors) and the code centers the vector image by drawing it at an offset.

		//412 bytes (KS 1.2 FE8E1C->FE8FB8, routine @ fe8cfa)

		//Rendering algorithm:

		//Read two bytes at a time.
		//If both bytes are FF, end the program.
		//If the first byte is FF and the second byte is not, start drawing a polyline with the color index given in the second byte.
		//  Treat any subsequent two bytes as x, y coordinates belonging to that polyline except if the first byte is FF (see rules 2 and 3) or FE(see rule 4), which is where you stop drawing the line.
		//If the first byte is FE, flood fill an area using the color index given in the second byte, starting from the point whose coordinates are given in the next two bytes.
		//The palette is:

		//    0: #fff 
		//    1: #000
		//    2: #77c
		//    3: #bbb
		//The offsets used for drawing the image centered are X = 70, Y= 40.

	}
}
