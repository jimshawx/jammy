using System;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.WebAPI
{
	public class UrlPathAttribute : Attribute
	{
		public UrlPathAttribute(string path)
		{
			Path = path;
		}

		public string Path { get; }
	}

	public class UrlActionAttribute : Attribute
	{
		public UrlActionAttribute(string action, string path, string summary = null, uint version = 1)
		{
			Action = action;
			Path = path;
			Summary = summary;
			Ver = version;
		}
		public string Action { get; }
		public string Path { get; }
		public string Summary { get; }
		public uint Ver { get; }
	}
}
