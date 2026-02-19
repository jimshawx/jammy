using Jammy.Plugins.JavaScript.APIWrapper;
using Jint;
using Jint.Native;
using Jint.Native.Function;
using System;
using System.Linq;
using System.Reflection.Emit;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.JavaScript.Jint
{
	public sealed class JintAdapter : IJsEngineAdapter
	{
		private readonly Engine engine;

		public JintAdapter(Engine engine)
		{
			this.engine = engine;
		}

		public Delegate ConvertToDelegate(object jsFunc, Type delegateType)
		{
			//
			// CASE 1: Real JS function (FunctionInstance)
			//
			if (jsFunc is JsValue jsValue && jsValue.IsObject())
			{
				var obj = jsValue.AsObject();

				if (obj is Function fn)
				{
					return WrapJsFunction(fn, delegateType);
				}

				// If it's an object but not a Function, fall through
			}

			//
			// CASE 2: Jint's internal delegate wrapper for JS functions
			//         Func<JsValue, JsValue[], JsValue>
			//
			if (jsFunc is Delegate del &&
				del.Method.GetParameters().Length == 2 &&
				del.Method.GetParameters()[0].ParameterType == typeof(JsValue) &&
				del.Method.GetParameters()[1].ParameterType == typeof(JsValue[]) &&
				del.Method.ReturnType == typeof(JsValue))
			{
				// Wrap this delegate as if it were a JS function
				Func<object[], object> invoker = args =>
				{
					var jsArgs = args.Select(a => JsValue.FromObject(engine, a)).ToArray();
					var result = (JsValue)del.DynamicInvoke(JsValue.Null, jsArgs)!;
					return result.ToObject();
				};

				var closure = new JsCallbackClosure(invoker);
				return CreateDelegate(delegateType, closure);
			}

			//
			// CASE 3: Already a .NET delegate of correct type
			//
			if (jsFunc is Delegate d2 && delegateType.IsInstanceOfType(d2))
				return d2;

			//
			// CASE 4: Everything else is unsupported
			//
			throw new ArgumentException($"Expected a JavaScript function, got {jsFunc.GetType()}");
		}

		private Delegate WrapJsFunction(Function fn, Type delegateType)
		{
			Func<object[], object> invoker = args =>
			{
				var jsArgs = args.Select(a => JsValue.FromObject(engine, a)).ToArray();
				var result = fn.Call(JsValue.Null, jsArgs);
				return result.ToObject();
			};

			var closure = new JsCallbackClosure(invoker);
			return CreateDelegate(delegateType, closure);
		}

		private static Delegate CreateDelegate(Type delegateType, JsCallbackClosure closure)
		{
			var invoke = delegateType.GetMethod("Invoke")!;
			var paramInfos = invoke.GetParameters();
			var originalParamTypes = paramInfos.Select(p => p.ParameterType).ToArray();
			var returnType = invoke.ReturnType;

			// Prepend JsCallbackClosure as the first parameter
			var paramTypes = new Type[originalParamTypes.Length + 1];
			paramTypes[0] = typeof(JsCallbackClosure);
			Array.Copy(originalParamTypes, 0, paramTypes, 1, originalParamTypes.Length);

			var dm = new DynamicMethod(
				name: "JsCallback_" + delegateType.Name,
				returnType: returnType,
				parameterTypes: paramTypes,
				m: typeof(JintAdapter).Module,
				skipVisibility: true);

			var il = dm.GetILGenerator();

			// object[] args = new object[paramCount]
			var argsLocal = il.DeclareLocal(typeof(object[]));
			il.Emit(OpCodes.Ldc_I4, originalParamTypes.Length);
			il.Emit(OpCodes.Newarr, typeof(object));
			il.Emit(OpCodes.Stloc, argsLocal);

			// Fill args[i] from ldarg(i+1)
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

			return dm.CreateDelegate(delegateType, closure);
		}
	}
}
