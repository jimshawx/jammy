using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Newtonsoft.Json.Linq;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Floppy
{
	public class Drive
	{
		[Persist]
		public bool motor;
		[Persist]
		public uint track;
		[Persist]
		public uint side;

		[Persist]
		public int indexCounter;
		[Persist]
		public int stateCounter;
		[Persist]
		public DiskDrives.DriveState state;

		public PRB DSKSEL;

		public bool attached;
		[Persist]
		public bool diskinserted;

		public IDisk disk;
		[Persist]
		public bool writeProtected;
		[Persist]
		public bool ready;

		public void Reset()
		{
			state = DiskDrives.DriveState.Idle;
			stateCounter = 10;
			indexCounter = 0;

			motor = false;
			track = 0;
			side = 0;
			writeProtected = true;
		}

		public void Save(JArray jobj, string id)
		{
			var jo = PersistenceManager.ToJObject(this, id);
			jobj.Add(jo);
		}

		public void Load(JObject obj)
		{
			PersistenceManager.FromJObject(this, obj);
		}
	}
}