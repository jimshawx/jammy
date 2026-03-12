using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.WebAPI
{
	public static class Documentation
	{
		private static readonly Lazy<string> json = new(GetOpenAPIJson);

		public static string Json => json.Value;

		private static string GetOpenAPIJson()
		{
			var endpoints = Assembly.GetExecutingAssembly()
				.GetTypes()
				.SelectMany(t => t.GetMethods())
				.Select(m => new
				{
					Method = m,
					Attr = m.GetCustomAttribute<UrlActionAttribute>()
				})
				.Where(x => x.Attr != null)
				.ToList();

			var paths = new Dictionary<string, object>();

			foreach (var ep in endpoints)
			{
				var fullPath = $"/{ep.Attr.Ver}/{ep.Attr.Path}";

				if (!paths.ContainsKey(fullPath))
					paths[fullPath] = new Dictionary<string, object>();

				((Dictionary<string, object>)paths[fullPath])[ep.Attr.Action.ToLower()] =
					new
					{
						summary = ep.Attr.Summary,
						responses = new Dictionary<string, object>
						{
							["200"] = new { description = "Success" }
						}
					};
			}

			var openApiDoc = new
			{
				openapi = "3.0.1",
				info = new { title = "Jammy API", version = "1.0.0" },
				paths
			};

			return JsonSerializer.Serialize(openApiDoc, new JsonSerializerOptions { WriteIndented = true });
		}
	}
}
