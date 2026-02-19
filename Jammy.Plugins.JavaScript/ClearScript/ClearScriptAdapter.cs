using Jammy.Plugins.JavaScript.APIWrapper;
using Microsoft.ClearScript;
using System;
using System.Linq;
using System.Reflection.Emit;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.JavaScript.ClearScript
{
	public sealed class ClearScriptAdapter : IJsEngineAdapter
	{
		public Delegate ConvertToDelegate(object jsFunc, Type delegateType)
		{
			if (jsFunc is not ScriptObject scriptFunc)
				throw new ArgumentException("Expected a JavaScript function");

			var closure = new JsCallbackClosure(args =>
			{
				return scriptFunc.Invoke(false, args);
			});

			return CreateDelegate(delegateType, closure);
		}

		private static Delegate CreateDelegate(Type delegateType, JsCallbackClosure closure)
		{
			var invoke = delegateType.GetMethod("Invoke")!;
			var paramInfos = invoke.GetParameters();
			var originalParamTypes = paramInfos.Select(p => p.ParameterType).ToArray();
			var returnType = invoke.ReturnType;

			// Prepend JsCallbackClosure as the first parameter so we can bind it as the target
			var paramTypes = new Type[originalParamTypes.Length + 1];
			paramTypes[0] = typeof(JsCallbackClosure);
			Array.Copy(originalParamTypes, 0, paramTypes, 1, originalParamTypes.Length);

			var dm = new DynamicMethod(
				name: "JsCallback_" + delegateType.Name,
				returnType: returnType,
				parameterTypes: paramTypes,
				m: typeof(ClearScriptAdapter).Module,
				skipVisibility: true);

			var il = dm.GetILGenerator();

			// object[] args = new object[paramCount]
			var argsLocal = il.DeclareLocal(typeof(object[]));
			il.Emit(OpCodes.Ldc_I4, originalParamTypes.Length);
			il.Emit(OpCodes.Newarr, typeof(object));
			il.Emit(OpCodes.Stloc, argsLocal);

			// Fill args[i] from ldarg (i + 1) because arg0 is the closure
			for (int i = 0; i < originalParamTypes.Length; i++)
			{
				il.Emit(OpCodes.Ldloc, argsLocal);
				il.Emit(OpCodes.Ldc_I4, i);
				il.Emit(OpCodes.Ldarg, i + 1);

				if (originalParamTypes[i].IsValueType)
					il.Emit(OpCodes.Box, originalParamTypes[i]);

				il.Emit(OpCodes.Stelem_Ref);
			}

			// Call closure.Invoke(args)
			il.Emit(OpCodes.Ldarg_0);          // closure instance
			il.Emit(OpCodes.Ldloc, argsLocal); // args[]
			il.Emit(OpCodes.Callvirt, typeof(JsCallbackClosure).GetMethod(nameof(JsCallbackClosure.Invoke))!);

			// Handle return
			if (returnType == typeof(void))
			{
				il.Emit(OpCodes.Pop);
			}
			else if (returnType.IsValueType)
			{
				il.Emit(OpCodes.Unbox_Any, returnType);
			}
			else
			{
				il.Emit(OpCodes.Castclass, returnType);
			}

			il.Emit(OpCodes.Ret);

			// Now this matches: first param is JsCallbackClosure, bound to 'closure'
			return dm.CreateDelegate(delegateType, closure);
		}
	}
}
