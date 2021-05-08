using System;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Interface
{
	public interface IAmigaTypesMapper
	{
		object MapSimple(Type type, uint addr);
		uint GetSize(object s);
	}
}