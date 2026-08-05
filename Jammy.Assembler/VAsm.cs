using Jammy.Extensions.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Assembler
{
	public class VAsm : IAssembler
	{
		private readonly ILogger<VAsm> logger;

		public VAsm(ILogger<VAsm> logger)
		{
			this.logger = logger;
		}

		public Assembly AssembleFile(string filename)
		{
			return Assemble(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, filename), Encoding.UTF8));
		}

		public Assembly Assemble(string s)
		{
			var r = new Assembly();

			string p = Path.ChangeExtension(Path.GetTempFileName(), "asm");
			try
			{ 
				using var f = File.OpenWrite(p);
				f.Write(Encoding.UTF8.GetBytes(s));
				f.Close();
			}
			catch (Exception ex)
			{
				r.Errors.Add(new AssemblyMessage("Can't create tmp file for input"));
				r.Errors.Add(new AssemblyMessage(ex.ToString()));
			}

			if (r.HasErrors()) return r;

			string q = Path.ChangeExtension(Path.GetTempFileName(), "bin");

			AssemblePayload(p, q, r);
			if (r.HasErrors()) return r;

			try
			{
				using var f = File.OpenRead(q);
				byte[] b = new byte[f.Length+(f.Length&1)];
				f.ReadExactly(b, 0, (int)f.Length);
				f.Close();

				r.Program = b.AsUWord().ToArray();
			}
			catch (Exception ex)
			{
				r.Errors.Add(new AssemblyMessage("Can't create tmp file for output"));
				r.Errors.Add(new AssemblyMessage(ex.ToString()));
			}

			return r;
		}

		private static bool AssemblePayload(string sourceFile, string outputFile, Assembly r)
		{
			//vasmm68k_mot -m68000 -Fbin -o {outputFile} [sourceFile]

			string vasmExecutable = "vasmm68k_mot_Win64/vasmm68k_mot.exe";

			string arguments = $"-m68000 -Fbin -o \"{outputFile}\" \"{sourceFile}\"";

			var startInfo = new ProcessStartInfo
			{
				FileName = vasmExecutable,
				Arguments = arguments,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = Environment.CurrentDirectory
			};

			using (Process process = new Process { StartInfo = startInfo })
			{
				try
				{
					process.Start();

					string output = process.StandardOutput.ReadToEnd();
					string errors = process.StandardError.ReadToEnd();

					process.WaitForExit();

					if (process.ExitCode == 0)
					{
						r.Messages.Add(new AssemblyMessage("Assembly successful"));
						r.Messages.Add(new AssemblyMessage(output));
					}
					else
					{
						r.Errors.Add(new AssemblyMessage($"Assembly failed with exit code {process.ExitCode}"));
						r.Errors.Add(new AssemblyMessage(errors));
					}
				}
				catch (Exception ex)
				{
					r.Errors.Add(new AssemblyMessage($"Failed to launch vasm"));
					r.Errors.Add(new AssemblyMessage(ex.ToString()));
				}
			}
			return r.HasErrors();
		}
	}
}
