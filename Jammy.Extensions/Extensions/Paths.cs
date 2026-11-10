using System.IO;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Extensions.Extensions
{
	public static class Paths
	{
		// normalise a path to use the correct directory separator character for the current platform
		public static string NormalisePath(string path)
		{
			return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
		}

		// normalise a path to use the correct directory separator character for the current platform
		// and remove any leading path up to and including the specified root folder
		public static string NormalisePathRoot(string path, string root)
		{
			path = NormalisePath(path);
			int idx = path.LastIndexOf($"{Path.DirectorySeparatorChar}{root}{Path.DirectorySeparatorChar}");
			if (idx >= 0)
				path = path.Substring(idx + 1);
			return path;
		}
	}
}
