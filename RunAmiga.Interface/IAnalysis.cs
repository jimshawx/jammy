using System.Collections.Generic;
using RunAmiga.Types;

namespace RunAmiga.Interface
{
	public interface IAnalysis
	{
		MemType[] GetMemTypes();
		Dictionary<uint, Header> GetHeaders();
		Dictionary<uint, Comment> GetComments();
		Dictionary<string, LVOCollection> GetLVOs();
		void AddComment(Comment comment);
		void AddComment(uint address, string s);
		void AddHeader(uint address, string hdr);
		void AddHeader(uint address, List<string> hdr);
		void ReplaceHeader(uint address, string hdr);
		void ReplaceHeader(uint address, List<string> hdr);
		void SetMemType(uint address, MemType type);
		void AddLVO(string currentLib, LVO lvo);
		void SetLVO(string currentLib, LVOCollection lvoCollection);
		bool OutOfMemtypeRange(uint address);
	}

}