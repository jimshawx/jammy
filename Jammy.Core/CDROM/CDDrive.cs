using Jammy.Core.Interface.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.CDROM
{
	public class CDDrive : ICDDrive
	{
		private readonly ILogger<CDDrive> logger;

		public CDDrive(ILogger<CDDrive> logger)
		{
			this.logger = logger;
		}

		public void InsertImage(ICDImage image)
		{
		}

		/*
		Commands:

			1 = STOP

			 Size: 1 byte
			 Returns status response

			2 = PAUSE

			 Size: 1 byte
			 Returns status response

			3 = UNPAUSE

			 Size: 1 byte
			 Returns status response
	
			4 = PLAY/READ

			 Size: 12 bytes
			 Response: 2 bytes

			5 = LED (2 bytes)

			 Size: 2 bytes. Bit 7 set in second byte = response wanted.
			 Response: no response or 2 bytes. Second byte non-zero: led is currently lit.

			6 = SUBCODE

			 Size: 1 byte
			 Response: 15 bytes

			7 = INFO

			 Size: 1 byte
			 Response: 20 bytes (status and firmware version)

			Common status response: 2 bytes
			Status second byte bit 7 = Error, bit 3 = Playing, bit 0 = Door closed.

			First byte of command is combined 4 bit counter and command code.
			Command response's first byte is same as command.
			Counter byte can be used to match command with response.
			Command and response bytes have checksum byte appended.
		 */

		private bool playing = false;
		private bool dooropen = false;

		private readonly List<byte[]> responses = new List<byte[]>();

		public List<byte[]> SendCommand(byte[] command)
		{
			responses.Clear();

			int i = 0;

			while (i < command.Length)
			{ 
				byte cmdByte = command[i];
				uint seq = (uint)(command[i] >> 4);
				uint cmd = (uint)(command[i] & 0xf);
				i++;

				switch (cmd)
				{
					case 1: logger.LogTrace($"{seq:X1} STOP"); responses.Add(standardResponse(cmdByte)); break;
					case 2: logger.LogTrace($"{seq:X1} PAUSE"); responses.Add(standardResponse(cmdByte)); break;
					case 3: logger.LogTrace($"{seq:X1} UNPAUSE"); responses.Add(standardResponse(cmdByte)); break;
					case 4:
						var sb = new StringBuilder($"{seq:X1} PLAY/READ ");
						for (int j = 0; j < 11; j++)
						{ 
							sb.Append($"{command[i]:X2} ");
							i++;
						}
						logger.LogTrace($"{sb.ToString()}");
						responses.Add(standardResponse(cmdByte));
						break;
					case 5:
						uint onOff = command[i]; i++;
						logger.LogTrace($"{seq:X1} LED {(onOff==0?"OFF":"ON")}");
						if ((onOff&0x80)!=0)
							responses.Add(standardResponse(cmdByte));
						break;
					case 6: logger.LogTrace($"{seq:X1} SUBCODE"); responses.Add(subcodeResponse(cmdByte)); break;
					case 7: logger.LogTrace($"{seq:X1} INFO"); responses.Add(infoResponse(cmdByte)); break;
					default:
						logger.LogTrace($"{seq:X1} ***UNKNOWN{cmd:X2}***");
						break;
				}
				uint chksum = command[i];
				logger.LogTrace($"CHK {chksum:X2}");
				i++;
			}
			return responses;
		}
		
		private const string FIRMWAREVERSION = "CHINON  O-658-2 24";

		private byte[] infoResponse(byte cmdByte)
		{
			var r = new byte[21];
			r[0] = cmdByte;
			r[1] = 1;//something to do with the CDDrive's door
			Array.Copy(Encoding.ASCII.GetBytes(FIRMWAREVERSION), 0, r, 2, FIRMWAREVERSION.Length);
			r[20] = checksum(r);
			return r;
		}

		private byte[] subcodeResponse(byte cmdByte)
		{
			var r = new byte[16];
			r[0] = cmdByte;
			//todo: what goes here?
			r[15] = checksum(r);
			return r;
		}

		private byte[] standardResponse(byte cmdByte)
		{
			var r = new byte[0];
			r[0] = cmdByte;
			if (playing) r[1] |= 1<<3;
			if (!dooropen) r[1] |= 1 << 0;
			r[2] = checksum(r);
			return r;
		}

		private byte checksum(byte[] r)
		{
			byte cs = 0xff;
			for (int i = 0; i < r.Length - 1; i++)
				cs -= r[i];
			return cs;
		}
	}
}
