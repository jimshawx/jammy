using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Extensions.Extensions
{
	public static class Paths
	{
		public static List<string> GetCandidatePaths(string path, string root)
		{
			var candidates = new List<string>();

			//original path
			candidates.Add(path);

			//original path with normalised-to-forward slash directory separators
			path = path.Replace('\\', '/');
			candidates.Add(path);

			//if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{ 
				// c:/blah -> /mnt/c/blah
				if (Regex.IsMatch(path, "^[a-zA-Z]:"))
				{
					string drive = path.Substring(0, 1);
					candidates.Add($"/mnt/{drive}" + path.Substring(2));
					candidates.Add($"/mnt/{drive.ToUpper()}" + path.Substring(2));
					candidates.Add($"/mnt/{drive.ToLower()}" + path.Substring(2));
				}
			}

			//if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{ 
				// /mnt/c/blah -> c:/blah
				if (Regex.IsMatch(path, "^/mnt/[a-zA-Z]/"))
				{
					string drive = path.Substring(5, 1);
					candidates.Add($"{drive}:" + path.Substring(6));
				}
			}

			// remove any leading path up to and including the specified root folder
			int idx = path.LastIndexOf($"{Path.DirectorySeparatorChar}{root}{Path.DirectorySeparatorChar}");
			if (idx >= 0)
				candidates.Add(path.Substring(idx + 1));

			//filename with root folder
			candidates.Add(root + $"/{Path.GetFileName(path)}");

			return candidates.Distinct().ToList();;
		}

		public static string GetCandidatePath(string path, string root)
		{
			var candidates = GetCandidatePaths(path, root);
			foreach (var candidate in candidates)
			{
				if (File.Exists(candidate))
					return candidate;
			}
			return null;
		}
	}
}
