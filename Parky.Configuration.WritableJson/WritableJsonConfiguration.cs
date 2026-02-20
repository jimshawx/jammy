using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/*
	Copyright 2025 James Shaw. All Rights Reserved.
*/

namespace Parky.Configuration.WritableJson
{
	public static class WritableJsonConfigurationExtensions
	{
		public static IConfigurationBuilder AddWritableJsonFile(this IConfigurationBuilder builder, IFileProvider provider, string path, bool optional, bool reloadOnChange)
		{
			return builder.Add(new WritableJsonConfigurationSource
			{
				Path = path,
				Optional = optional,
				ReloadOnChange = reloadOnChange
			});
		}

		public static IConfigurationBuilder AddWritableJsonFile(this IConfigurationBuilder builder, string path, bool optional)
		{
			return AddWritableJsonFile(builder, provider: null, path: path, optional: optional, reloadOnChange: false);
		}
	}

	public class WritableJsonConfigurationSource : JsonConfigurationSource
	{
		public override IConfigurationProvider Build(IConfigurationBuilder builder)
		{
			EnsureDefaults(builder);
			return new WritableJsonConfigurationProvider(this);
		}
	}

	public class WritableJsonConfigurationProvider : JsonConfigurationProvider
	{
		public WritableJsonConfigurationProvider(JsonConfigurationSource source) : base(source) { }

		public override void Set(string key, string value)
		{
			base.Set(key, value);

			key = key.Replace(':', '.');

			var fileInfo = Source.FileProvider.GetFileInfo(Source.Path);

			JToken tokens;
			using (var read = fileInfo.CreateReadStream())
			using (var file = new StreamReader(read))
			using (var reader = new JsonTextReader(file))
			{
				tokens = JToken.ReadFrom(reader);
				var setting = tokens.SelectToken(key);
				if (setting != null)
				{
					((JValue)setting).Value = value;
				}
				else
				{
					var bits = key.Split('.');
					JObject jo = new JObject(), root = jo;
					foreach (var seg in bits[..^1])
						jo[seg] = jo = new JObject();
					jo.Add(bits[^1], value);
					((JObject)tokens).Merge(root);
				}
			}

			using (var file = File.CreateText(fileInfo.PhysicalPath))
			using (var jw = new JsonTextWriter(file))
			{
				jw.Formatting = Formatting.Indented;
				tokens.WriteTo(jw);
			}
		}
	}
}
