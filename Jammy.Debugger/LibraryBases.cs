using Jammy.Core.Interface.Interfaces;
using Jammy.Interface;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Debugger
{
	public interface ILibraryBaseCollection
	{
		bool HasLibrary(string libraryName, uint address);
		void AddLibrary(string libraryName, uint address);
		Dictionary<string, uint> GetAllLibraryBases();
	}

	public class LibraryBaseCollection : ILibraryBaseCollection
	{
		private readonly Dictionary<string, uint> libraryBaseAddresses = new Dictionary<string, uint>();
		private readonly ILVOInterceptorCollection lvoInterceptorCollection;

		public LibraryBaseCollection(ILVOInterceptorCollection lvoInterceptorCollection)
		{
			this.lvoInterceptorCollection = lvoInterceptorCollection;
		}

		public bool HasLibrary(string libraryName, uint address)
		{
			return libraryBaseAddresses.TryGetValue(libraryName, out uint addr) && addr == address;
		}

		public void AddLibrary(string libraryName, uint address)
		{
			libraryBaseAddresses[libraryName] = address;
			lvoInterceptorCollection.UpdateActiveLVOInterceptors(libraryBaseAddresses);
		}

		public Dictionary<string, uint> GetAllLibraryBases()
		{
			return libraryBaseAddresses;
		}
	}

	public interface ILibraryBases
	{
		void SetLibraryBaseAddress(string libraryName, uint address);
		Dictionary<string, uint> GetAllLibraryBases();
	}

	public class LibraryBases : ILibraryBases
	{
		private readonly IAnalyser analyser;
		private readonly ILibraryBaseCollection libraryBaseCollection;
		private readonly ILogger<LibraryBases> logger;

		public LibraryBases(IDebugMemoryMapper memory, IAnalyser analyser,
			ILibraryBaseCollection libraryBaseCollection, ILogger<LibraryBases> logger)
		{
			this.analyser = analyser;
			this.libraryBaseCollection = libraryBaseCollection;
			this.logger = logger;
		}

		public void SetLibraryBaseAddress(string libraryName, uint address)
		{
			if (address == 0) return;
			if (string.IsNullOrWhiteSpace(libraryName)) return;

			//not a new library
			if (libraryBaseCollection.HasLibrary(libraryName, address))
				return;

			logger.LogTrace($"Setting {libraryName} base to {address:X8}");

			libraryBaseCollection.AddLibrary(libraryName, address);
			analyser.AnalyseLibraryBase(libraryName, address);
		}

		public Dictionary<string,uint> GetAllLibraryBases()
		{
			var v = libraryBaseCollection.GetAllLibraryBases();
			foreach (var w in v)
				analyser.AnalyseLibraryBase(w.Key, w.Value);
			return v;
		}
	}
}
