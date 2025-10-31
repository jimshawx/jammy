using Jammy.Core.Interface.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

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

		private readonly string[] cmdNames =
		{
			"*** UNUSED 0 ***",
			"STOP",
			"PAUSE",
			"UNPAUSE",
			"PLAY/READ",
			"LED",
			"SUBCODE",
			"INFO",
			"*** UNUSED 8 ***",
			"*** UNUSED 9 ***",
			"*** UNUSED A ***",
			"*** UNUSED B ***",
			"*** UNUSED C ***",
			"*** UNUSED D ***",
			"*** UNUSED E ***",
			"*** UNUSED F ***",
		};

		//15 00 EA 27 D8

		public void SendCommand(byte[] command)
		{
			int i = 0;

			while (i < command.Length)
			{ 
				uint seq = (uint)(command[i] >> 4);
				uint cmd = (uint)(command[i] & 0xf);
				i++;

				switch (cmd)
				{
					case 1: logger.LogTrace($"{seq:X1} STOP"); break;
					case 2: logger.LogTrace($"{seq:X1} PAUSE"); break;
					case 3: logger.LogTrace($"{seq:X1} UNPAUSE"); break;
					case 4:
						var sb = new StringBuilder($"{seq:X1} PLAY/READ ");
						for (int j = 0; j < 11; j++)
						{ 
							sb.Append($"{command[i]:X2} ");
							i++;
						}
						logger.LogTrace($"{sb.ToString()}");
						break;
					case 5:
						uint onOff = command[i]; i++;
						logger.LogTrace($"{seq:X1} LED {(onOff==0?"OFF":"ON")}");
						break;
					case 6: logger.LogTrace($"{seq:X1} SUBCODE"); break;
					case 7: logger.LogTrace($"{seq:X1} INFO"); break;
					default:
						logger.LogTrace($"{seq:X1} ***UNKNOWN{cmd:X2}***");
						break;
				}
				uint chksum = command[i];
				logger.LogTrace($"CHK {chksum:X2}");
				i++;
			}
		}
	}
}
