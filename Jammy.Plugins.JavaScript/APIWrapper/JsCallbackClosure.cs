using System;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.JavaScript.APIWrapper
{
	internal sealed class JsCallbackClosure
	{
		private readonly Func<object[], object> _invoker;

		public JsCallbackClosure(Func<object[], object> invoker)
		{
			_invoker = invoker;
		}

		public object Invoke(object[] args)
		{
			return _invoker(args);
		}
	}
}
