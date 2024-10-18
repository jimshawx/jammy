using Jammy.Core.Interface.Interfaces;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.IO.Linux
{
   public class Keyboard : IKeyboard
    {
        public void Emulate()
        {

        }
        public void Reset()
        {

        }
        public byte ReadKey()
        {
            return 0;
        }
        public void SetCIA(ICIAAOdd ciaa)
        {

        }
        public void WriteCRA(uint insaddr, byte value)
        {

        }
    }
}