using Jammy.Core.Types;
using Jammy.Database.CommentDao;
using Jammy.Database.DatabaseDao;
using Jammy.Database.HeaderDao;
using Jammy.Database.LabelDao;
using Jammy.Database.MemTypeDao;
using Jammy.Interface;
using Jammy.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
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
		private readonly ILabelDao labelDao;
		private readonly IHeaderDao headerDao;
		private readonly ICommentDao commentDao;
		private readonly IDatabaseDao databaseDao;
		private readonly IMemTypeDao memTypeDao;
		private readonly ILabeller labeller;
		private readonly ILogger<Analyser> logger;

		public Analysis(
			ILabelDao labelDao, IHeaderDao headerDao, ICommentDao commentDao, IDatabaseDao databaseDao,
			IMemTypeDao memTypeDao, ILabeller labeller,
			IOptions<EmulationSettings> settings, ILogger<Analyser> logger)
		{
			this.settings = settings.Value;
			this.labelDao = labelDao;
			this.headerDao = headerDao;
			this.commentDao = commentDao;
			this.databaseDao = databaseDao;
			this.memTypeDao = memTypeDao;
			this.labeller = labeller;
			this.logger = logger;
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

		public void AddHeader(Header header)
		{
			headers[header.Address] = header;
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

		private LVO FindLVO(string name)
		{
			foreach (var lib in lvos)
			{
				foreach (var lvo in lib.Value.LVOs)
				{
					if (lvo.Name == name)
						return lvo;
				}
			}
			return null;
		}

		public void AugmentLVO(string lvoName, List<string> parms, List<string> regs)
		{
			var lvo = FindLVO(lvoName);
			if (lvo != null)
				lvo.parms.AddRange(parms.Zip(regs, (p, r) => new LVO.LVOParm { Name = p, Reg = r }));
			else
				logger.LogTrace($"Could not find LVO to augment: {lvoName}");
		}

		public bool OutOfMemtypeRange(uint address)
		{
			return false;
		}

		public void SaveAnalysis()
		{
			logger.LogTrace("Saving Analysis...");

			var database = databaseDao.Search(new DatabaseSearch { Name = "default"}).SingleOrDefault();

			logger.LogTrace("...Comments");
			foreach (var comment in comments.Values)
				comment.DbId = database.Id;
			commentDao.Save(comments.Values.ToList());

			logger.LogTrace("...Headers");
			foreach (var header in headers.Values)
				header.DbId = database.Id;
			headerDao.Save(headers.Values.ToList());

			logger.LogTrace("...Labels");
			var labels = labeller.GetLabels().Values;
			foreach (var label in labels)
				label.DbId = database.Id;
			labelDao.Save(labels.ToList());

			logger.LogTrace("...MemTypes");
			var ranges = PackMemTypes().Select(x =>
				new MemTypeRange
				{
					DbId = database.Id,
					Address = x.Address,
					Size = x.Size,
					Type = (RangeType)x.Type
				}).ToList();
			var ids = memTypeDao.Search(new MemTypeSearch { DbId = database.Id });
			memTypeDao.Delete(ids);
			memTypeDao.Save(ranges);
			logger.LogTrace("Complete");
		}

		public void LoadAnalysis()
		{
			logger.LogTrace("Loading Analysis...");
			
			var database = databaseDao.Search(new DatabaseSearch { Name = "default" }).SingleOrDefault();
			
			logger.LogTrace("...Comments");
			var comments = commentDao.Search(new CommentSearch { DbId = database.Id });
			foreach (var comment in comments)
				AddComment(comment);
			
			logger.LogTrace("...Headers");
			var headers = headerDao.Search(new HeaderSearch { DbId = database.Id });
			foreach (var header in headers)
				AddHeader(header);
			
			logger.LogTrace("...Labels");
			var labels = labelDao.Search(new LabelSearch { DbId = database.Id });
			foreach (var label in labels)
				labeller.AddLabel(label);
			
			logger.LogTrace("...MemTypes");
			var memTypes = memTypeDao.Search(new MemTypeSearch { DbId = database.Id });
			var ranges = memTypes.Select(x => 
				new MemRange
				{
					Address = x.Address,
					Size = x.Size,
					Type = (MemType)x.Type
				}).ToList();
			UnpackMemTypes(ranges);
			logger.LogTrace("Complete");
		}

		public void DeleteAnalysis()
		{
			logger.LogTrace("Deleting Analysis...");
			
			var database = databaseDao.Search(new DatabaseSearch { Name = "default" }).SingleOrDefault();
			
			logger.LogTrace("...Comments");
			var comments = commentDao.Search(new CommentSearch { DbId = database.Id });
			commentDao.Delete(comments);
			
			logger.LogTrace("...Headers");
			var headers = headerDao.Search(new HeaderSearch { DbId = database.Id });
			headerDao.Delete(headers);
			
			logger.LogTrace("...Labels");
			var labels = labelDao.Search(new LabelSearch { DbId = database.Id });
			labelDao.Delete(labels);

			logger.LogTrace("...MemTypes");
			var memTypes = memTypeDao.Search(new MemTypeSearch { DbId = database.Id });
			memTypeDao.Delete(memTypes);

			logger.LogTrace("Complete");
		}

		public void ResetAnalysis()
		{
			logger.LogTrace("Reset Analysis");
			comments.Clear();
			headers.Clear();
			for (int i = 0; i < memType.Length; i++)
				memType[i] = null;
			labeller.ResetLabels();
		}

		private class MemRange
		{
			public uint Address { get; set; }
			public ulong Size { get; set; }
			public MemType Type { get; set; }
		}

		private List<MemRange> PackMemTypes()
		{
			//DumpMemTypes("Before");
			
			uint address = 0;
			var currentType = MemType.Unknown;
			uint currentTypeStart = 0;
			var ranges = new List<MemRange>();

			void CheckType(MemType item)
			{
				if (item != currentType)
				{
					if (currentType != MemType.Unknown)
					{
						ranges.Add(new MemRange
						{
							Address = currentTypeStart,
							Type = currentType,
							Size = address - currentTypeStart,
						});
					}
					currentType = item;
					currentTypeStart = address;
				}
			}

			foreach (var block in memType)
			{
				if (block != null)
				{
					foreach (var item in block)
					{
						CheckType(item);
						address++;
					}
				}
				else
				{
					CheckType(MemType.Unknown);
					address += MemTypeCollection.MEMTYPE_BLOCKSIZE;
				}
			}
			return ranges;
		}

		private void UnpackMemTypes(List<MemRange> ranges)
		{
			foreach (var range in ranges)
			{
				uint address = range.Address;
				ulong endAddress = range.Address + range.Size;
				while (address < endAddress)
				{
					SetMemType(address, range.Type);
					address++;
				}
			}
			
			//DumpMemTypes("After");
		}

		private void DumpMemTypes(string label) 
		{
			using FileStream f = File.OpenWrite($"memtypes-{DateTime.Now:yyyyMMdd-HHmmss.fff}.txt");
			using StreamWriter writer = new StreamWriter(f);

			writer.WriteLine($"MemTypes Dump {label}:");
			uint address = 0;
			foreach (var block in memType)
			{
				if (block != null)
				{
					foreach (var item in block)
					{
						if (item != MemType.Unknown)
							writer.WriteLine($"{address:X8} {item}");
						address++;
					}
				}
				else
				{
					address += MemTypeCollection.MEMTYPE_BLOCKSIZE;
				}
			}
		}

				/*
		delete from comment where dbid = (select id from database where name = 'default');
		delete from label where dbid = (select id from database where name = 'default');
		delete from headerline where headerid in (select id from header where dbid = (select id from database where name = 'default'));
		delete from header where dbid = (select id from database where name = 'default');
		delete from memtype where dbid = (select id from database where name = 'default');
				*/
	}
}