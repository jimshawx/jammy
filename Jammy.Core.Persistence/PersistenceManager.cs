using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Persistence
{
	public interface IPersistenceManager
	{
		void Save(string filename);
		void Load(string filename);
	}

	public class PersistenceManager : IPersistenceManager
	{
		private readonly IEnumerable<IStatePersister> persisters;
		private readonly EmulationSettings settings;
		private readonly ILogger<PersistenceManager> logger;

		public PersistenceManager(IEnumerable<IStatePersister> persisters, IOptions<EmulationSettings> settings, ILogger<PersistenceManager> logger)
		{
			this.persisters = persisters;
			this.settings = settings.Value;
			this.logger = logger;
		}

		public void Save(string filename)
		{
			var jo = new JArray();
				jo.Add(JObject.FromObject(settings));

			foreach (var p in persisters)
				p.Save(jo);

			using (var f = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				using (var x = new StreamWriter(f))
					x.Write(jo.ToString());
			}
		}

		public void Load(string filename)
		{
			var objects = JArray.Parse(File.ReadAllText(filename));
			foreach (var o in objects)
				foreach (var p in persisters)
					p.Load((JObject)o);
			logger.LogTrace("Snapshot restored!");
		}

		public static bool Is(JObject obj, string id)
		{
			return (obj.TryGetValue("id", StringComparison.InvariantCulture, out var k) && k.ToString() == id);
		}
	}
}
