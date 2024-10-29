using Newtonsoft.Json.Linq;

namespace Jammy.Core.Interface.Interfaces
{
	public interface IStatePersister
	{
		void Save(JArray obj);
		void Load(JObject obj);
	}
}
