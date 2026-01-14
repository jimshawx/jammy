using System.Collections.Generic;
using Jammy.Types;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Interface
{
	public interface IAnalysis
	{
		MemTypeCollection GetMemTypes();
		Dictionary<uint, Header> GetHeaders();
		Dictionary<uint, Comment> GetComments();
		Dictionary<string, LVOCollection> GetLVOs();
		LVOCollection GetLVOs(string library);
		void AddComment(Comment comment);
		void AddComment(uint address, string s);
		void AddHeader(uint address, string hdr);
		void AddHeader(uint address, List<string> hdr);
		void ReplaceHeader(uint address, string hdr);
		void ReplaceHeader(uint address, List<string> hdr);
		void SetMemType(uint address, MemType type);
		void AddLVO(string currentLib, LVO lvo);
		void SetLVO(string currentLib, LVOCollection lvoCollection);
		void AugmentLVO(string lvoName, List<string> parms, List<string> regs);
		bool OutOfMemtypeRange(uint address);
		void ClearSomeAnalysis();
		void SaveAnalysis();
		void LoadAnalysis();
		void DeleteAnalysis();
		void ResetAnalysis();
	}
}