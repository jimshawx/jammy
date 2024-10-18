using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.IO.Linux
{
    public class Mouse : IMouse
    {

        private readonly IEmulationWindow emulationWindow;
		private readonly ILogger logger;

		private const uint PRAMASK = 0b1100_0000;

		private uint pra;
		private uint joy0dat;
		private uint joy1dat;
		private uint potgo;
		private uint pot0dat;
		private uint pot1dat;
		private uint joytest;

		public Mouse(IEmulationWindow emulationWindow, ILogger<Mouse> logger)
		{
			this.emulationWindow = emulationWindow;
			this.logger = logger;
		}
        
        public void Emulate()
        {

        }
        public void Reset() { }


        public ushort Read(uint insaddr, uint address)
        {
            uint value = 0;

            switch (address)
            {
                case ChipRegs.JOY0DAT: value = joy0dat; break;
                case ChipRegs.JOY1DAT: value = joy1dat; break;
                case ChipRegs.POTGOR: value = potgo; break;
                case ChipRegs.POT0DAT: value = pot0dat; break;
                case ChipRegs.POT1DAT: value = pot1dat; break;
            }
            return (ushort)value;
        }

        public void Write(uint insaddr, uint address, ushort value)
        {
            switch (address)
            {
                case ChipRegs.POTGO: potgo = value; break;
                case ChipRegs.JOYTEST:
                    joytest = value;
                    joy0dat = joytest;
                    joy1dat = joytest;
                    break;
            }
        }

        public byte ReadPRA(uint insaddr)
        {
            return (byte)(pra & PRAMASK);
        }

        public void WritePRA(uint insaddr, byte value)
        {
            pra = value;
        }

        public uint DebugChipsetRead(uint address, Size size)
        {
            uint value = 0;

            switch (address)
            {
                case ChipRegs.JOY0DAT: value = joy0dat; break;
                case ChipRegs.JOY1DAT: value = joy1dat; break;
                case ChipRegs.POTGO: value = potgo; break;
                case ChipRegs.POTGOR: value = potgo; break;
                case ChipRegs.POT0DAT: value = pot0dat; break;
                case ChipRegs.POT1DAT: value = pot1dat; break;
                case ChipRegs.JOYTEST: value = joytest; break;
            }
            return (ushort)value;
        }
    }
}
