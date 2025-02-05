using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;
using Newtonsoft.Json.Linq;

namespace Jammy.Core.Custom.Audio
{
	public class NullAudio : IAudio
	{
		public uint DebugChipsetRead(uint address, Size size)
		{
			return 0;
		}

		public void Emulate()
		{
			
		}

		public void Load(JObject obj)
		{
		}

		public ushort Read(uint insaddr, uint address)
		{
			return 0;
		}

		public void Reset()
		{
			
		}

		public void Save(JArray obj)
		{
			
		}

		public void Write(uint insaddr, uint address, ushort value)
		{
			
		}

		public void WriteDMACON(ushort v)
		{
			
		}

		public void WriteINTENA(ushort v)
		{
			
		}

		public void WriteINTREQ(ushort v)
		{
			
		}
	}
}
