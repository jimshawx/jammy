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
using System.Collections.Generic;
using System.Linq;

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
		private readonly ILabelDao labelDao;
		private readonly IHeaderDao headerDao;
		private readonly ICommentDao commentDao;
		private readonly IDatabaseDao databaseDao;
		private readonly IMemTypeDao memTypeDao;
		private readonly ILabeller labeller;

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

		public bool OutOfMemtypeRange(uint address)
		{
			return false;
		}

		public void SaveAnalysis()
		{
			var database = databaseDao.Search(new DatabaseSearch { Name = "default"}).SingleOrDefault();
			foreach (var comment in comments.Values)
				comment.DbId = database.Id;
			commentDao.Save(comments.Values.ToList());

			foreach (var header in headers.Values)
				header.DbId = database.Id;
			headerDao.Save(headers.Values.ToList());

			var labels = labeller.GetLabels().Values;
			foreach (var label in labels)
				label.DbId = database.Id;
			labelDao.Save(labels.ToList());

			uint address = 0;
			var currentType = MemType.Unknown;
			uint currentTypeStart = 0;
			var ranges = new List<MemTypeRange>();
			void CheckType(MemType item)
			{
				if (item != currentType)
				{
					if (currentType != MemType.Unknown)
					{
						// Save the current type block
						ranges.Add(new MemTypeRange
						{
							Address = currentTypeStart,
							Type = (RangeType)currentType,
							Size = address - currentTypeStart,
							DbId = database.Id
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
						switch (item)
						{
							case MemType.Code:
								address += 2;
								break;
							case MemType.Byte:
								address += 1;
								break;
							case MemType.Word:
								address += 2;
								break;
							case MemType.Long:
								address += 4;
								break;
							case MemType.Str:
								// Find the length of the string
								uint strLen = 0;
								while (block[(address & MemTypeCollection.MEMTYPE_MASK) + strLen] == MemType.Str)
									strLen++;
								// +1 for the null terminator
								address += strLen + 1;
								break;
							default:
								address += 1;
								break;
						}
					}
				}
				else
				{
					address += MemTypeCollection.MEMTYPE_BLOCKSIZE;
				}
			}
			var ids = memTypeDao.Search(new MemTypeSearch { DbId = database.Id });
			memTypeDao.Delete(ids);
			memTypeDao.Save(ranges);
		}

		public void LoadAnalysis()
		{
			var database = databaseDao.Search(new DatabaseSearch { Name = "default" }).SingleOrDefault();
			var comments = commentDao.Search(new CommentSearch { DbId = database.Id });
			foreach (var comment in comments)
				AddComment(comment);
			var headers = headerDao.Search(new HeaderSearch { DbId = database.Id });
			foreach (var header in headers)
				AddHeader(header);
			var labels = labelDao.Search(new LabelSearch { DbId = database.Id });
			foreach (var label in labels)
				labeller.AddLabel(label);
			var memTypes = memTypeDao.Search(new MemTypeSearch { DbId = database.Id });
			foreach (var range in memTypes)
			{
				uint address = range.Address;
				ulong endAddress = range.Address + range.Size;
				while (address < endAddress)
				{
					switch (range.Type)
					{
						case RangeType.Code:
							SetMemType(address, MemType.Code);
							address += 2;
							break;
						case RangeType.Byte:
							SetMemType(address, MemType.Byte);
							address += 1;
							break;
						case RangeType.Word:
							SetMemType(address, MemType.Word);
							address += 2;
							break;
						case RangeType.Long:
							SetMemType(address, MemType.Long);
							address += 4;
							break;
						case RangeType.Str:
							SetMemType(address, MemType.Str);
							address += 1;
							break;
						default:
							address += 1;
							break;
					}
				}
			}
		}

		public void DeleteAnalysis()
		{
			var database = databaseDao.Search(new DatabaseSearch { Name = "default" }).SingleOrDefault();
			var comments = commentDao.Search(new CommentSearch { DbId = database.Id });
			commentDao.Delete(comments);
			var headers = headerDao.Search(new HeaderSearch { DbId = database.Id });
			headerDao.Delete(headers);
			var labels = labelDao.Search(new LabelSearch { DbId = database.Id });
			labelDao.Delete(labels);
			var memTypes = memTypeDao.Search(new MemTypeSearch { DbId = database.Id });
			memTypeDao.Delete(memTypes);
		}

		public void ResetAnalysis()
		{
			comments.Clear();
			headers.Clear();
			for (int i = 0; i < memType.Length; i++)
				memType[i] = null;
			labeller.ResetLabels();
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