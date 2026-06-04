using Jammy.Database.Types;
using Jammy.Types;
using System;
using System.Collections.Generic;
using Label = Jammy.Types.Label;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Datebase.Interface
{
	public interface IDao<T, U> where T : IBaseObject where U : ISearch
	{
		List<T> Search(U search);
		void Save(T item);
		void Save(List<T> items);
		bool SaveOrUpdate(T item);
		T Get(Guid id);
		void Delete(T item);
		void Delete(List<T> items);
	}

	public interface ICommentDao : IDbDao<Comment, CommentSearch>
	{
	}

	public interface IDbDao<T, U> : IDao<T, U> where T : IBaseDbObject where U : IDbSearch
	{
	}

	public interface IDatabaseDao : IDao<Database.Types.Database, DatabaseSearch>
	{
	}

	public interface IHeaderDao : IDbDao<Header, HeaderSearch>
	{
	}

	public interface ILabelDao : IDbDao<Label, LabelSearch>
	{
	}

	public interface IMemTypeDao : IDbDao<MemTypeRange, MemTypeSearch>
	{
	}
}
