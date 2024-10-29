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
		private readonly IOptions<EmulationSettings> settings;
		private readonly ILogger<PersistenceManager> logger;

		public PersistenceManager(IEnumerable<IStatePersister> persisters, IOptions<EmulationSettings> settings, ILogger<PersistenceManager> logger)
		{
			this.persisters = persisters;
			this.settings = settings;
			this.logger = logger;
		}

		public void Save(string filename)
		{
			var jo = new JArray();
			using (var f = File.OpenWrite(filename))
			{
				using (var x = new StreamWriter(f))
				{
					foreach (var p in persisters)
						p.Save(jo);
					x.Write(jo.ToString());
				}
				f.Close();
			}
		}

		public void Load(string filename)
		{
			var objects = JArray.Parse(File.ReadAllText(filename));
			foreach (var o in objects)
				foreach (var p in persisters)
					p.Load((JObject)o);
		}
	}
}
