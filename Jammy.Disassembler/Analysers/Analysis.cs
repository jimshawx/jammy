using Jammy.Core.Types;
using Jammy.Interface;
using Jammy.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Disassembler.Analysers
{
	public class Analysis : IAnalysis
	{
		private readonly Dictionary<uint, Comment> comments = new Dictionary<uint, Comment>();
		private readonly Dictionary<uint, Header> headers = new Dictionary<uint, Header>();
		private readonly Dictionary<string, LVOCollection> lvos = new Dictionary<string, LVOCollection>();
		private readonly MemType[][] memType = new MemType[MemTypeCollection.MEMTYPE_NUM_BLOCKS][];

		private readonly EmulationSettings settings;

		public Analysis(IOptions<EmulationSettings> settings, ILogger<Analyser> logger)
		{
			this.settings = settings.Value;
		}

		public void ClearSomeAnalysis()
		{
			for (int i = 0; i < memType.Length; i++)
				memType[i] = null;
			headers.Clear();
			comments.Clear();
		}

		public MemTypeCollection GetMemTypes()
		{
			return new MemTypeCollection(memType);
		}

		public Dictionary<uint, Header> GetHeaders()
		{
			return headers;
		}

		public Dictionary<uint, Comment> GetComments()
		{
			return comments;
		}

		public Dictionary<string, LVOCollection> GetLVOs()
		{
			return lvos;
		}

		public LVOCollection GetLVOs(string library)
		{
			return lvos.GetValueOrDefault(library, new LVOCollection(LVOType.Empty));
		}

		private bool IgnoreComment(Comment comment)
		{
			return false;
		}

		public void AddComment(Comment comment)
		{
			if (!IgnoreComment(comment))
				comments[comment.Address] = comment;
		}

		public void AddComment(uint address, string s)
		{
			comments[address] = new Comment { Address = address, Text = s };
		}

		public void AddHeader(uint address, string hdr)
		{
			if (!headers.ContainsKey(address))
				headers[address] = new Header { Address = address };

			headers[address].TextLines.Add(hdr);
		}

		public void AddHeader(uint address, List<string> hdr)
		{
			if (!headers.ContainsKey(address))
				headers[address] = new Header { Address = address };

			headers[address].TextLines.AddRange(hdr);
		}

		public void ReplaceHeader(uint address, string hdr)
		{
			if (!headers.ContainsKey(address))
				headers[address] = new Header { Address = address };

			headers[address].TextLines.Clear();
			headers[address].TextLines.Add(hdr);
		}

		public void ReplaceHeader(uint address, List<string> hdr)
		{
			if (!headers.ContainsKey(address))
				headers[address] = new Header { Address = address };

			headers[address].TextLines.Clear();
			headers[address].TextLines.AddRange(hdr);
		}

		private MemType[] Ensure(uint address)
		{
			uint block = address >> MemTypeCollection.MEMTYPE_SHIFT;
			if (memType[block] == null)
				memType[block] = new MemType[MemTypeCollection.MEMTYPE_BLOCKSIZE];
			return memType[block];
		}

		public void SetMemType(uint address, MemType type)
		{
			var block = Ensure(address);
			block[address&MemTypeCollection.MEMTYPE_MASK] = type;
		}

		public void AddLVO(string currentLib, LVO lvo)
		{
			lvos[currentLib].LVOs.Add(lvo);
		}

		public void SetLVO(string currentLib, LVOCollection lvoCollection)
		{
			lvos[currentLib] = lvoCollection;
		}

		public bool OutOfMemtypeRange(uint address)
		{
			return false;
		}
	}
}