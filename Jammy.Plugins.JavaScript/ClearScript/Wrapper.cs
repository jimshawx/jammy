using Jint;
using Jint.Native;
using Jint.Native.Function;
using Microsoft.ClearScript;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Jammy.Plugins.JavaScript.ClearScript
{
	public interface IJsEngineAdapter
	{
		Delegate ConvertToDelegate(object jsFunc, Type delegateType);
	}

	public static class WrapperFactory
	{
		public static object CreateWrapper(object target, IJsEngineAdapter adapter)
		{
			var targetType = target.GetType();

			var asmName = new AssemblyName(targetType.Name + "WrapperAssembly");
			var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
			var moduleBuilder = asmBuilder.DefineDynamicModule("Main");
			var typeBuilder = moduleBuilder.DefineType(
				targetType.Name + "Wrapper",
				TypeAttributes.Public | TypeAttributes.Class);

			// private readonly DebuggerApi _target;
			var targetField = typeBuilder.DefineField("_target", targetType, FieldAttributes.Private);

			// private readonly IJsEngineAdapter _adapter;
			var adapterField = typeBuilder.DefineField("_adapter", typeof(IJsEngineAdapter), FieldAttributes.Private);

			// private static readonly MethodInfo[] _methods;
			var methodsField = typeBuilder.DefineField("_methods", typeof(MethodInfo[]),
				FieldAttributes.Private | FieldAttributes.Static);

			// .ctor(DebuggerApi target, IJsEngineAdapter adapter)
			EmitCtor(typeBuilder, targetField, adapterField);

			// static .cctor: cache MethodInfo[]
			EmitTypeInitializer(typeBuilder, targetType, methodsField);

			// For each public instance method, emit wrapper
			var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
									.Where(m => !m.IsSpecialName)
									.ToArray();

			for (int i = 0; i < methods.Length; i++)
			{
				EmitWrapperMethod(typeBuilder, targetField, adapterField, methodsField, methods[i], i);
			}

			var wrapperType = typeBuilder.CreateType()
							  ?? throw new InvalidOperationException("Failed to create wrapper type.");

			return Activator.CreateInstance(wrapperType, target, adapter)
				   ?? throw new InvalidOperationException("Failed to create wrapper instance.");
		}

		private static void EmitCtor(TypeBuilder typeBuilder, FieldInfo targetField, FieldInfo adapterField)
		{
			var ctor = typeBuilder.DefineConstructor(
				MethodAttributes.Public,
				CallingConventions.Standard,
				new[] { targetField.FieldType, adapterField.FieldType });

			var il = ctor.GetILGenerator();

			// base .ctor()
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);

			// this._target = arg1;
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Stfld, targetField);

			// this._adapter = arg2;
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Stfld, adapterField);

			il.Emit(OpCodes.Ret);
		}

		private static void EmitTypeInitializer(TypeBuilder typeBuilder, Type targetType, FieldInfo methodsField)
		{
			var cctor = typeBuilder.DefineConstructor(
				MethodAttributes.Static | MethodAttributes.Private,
				CallingConventions.Standard,
				Type.EmptyTypes);

			var il = cctor.GetILGenerator();

			var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
									.Where(m => !m.IsSpecialName)
									.ToArray();

			// _methods = new MethodInfo[n];
			il.Emit(OpCodes.Ldc_I4, methods.Length);
			il.Emit(OpCodes.Newarr, typeof(MethodInfo));
			il.Emit(OpCodes.Stsfld, methodsField);

			for (int i = 0; i < methods.Length; i++)
			{
				il.Emit(OpCodes.Ldsfld, methodsField);
				il.Emit(OpCodes.Ldc_I4, i);
				il.Emit(OpCodes.Ldtoken, methods[i]);
				il.Emit(OpCodes.Call, typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle),
					new[] { typeof(RuntimeMethodHandle) })!);
				il.Emit(OpCodes.Castclass, typeof(MethodInfo));
				il.Emit(OpCodes.Stelem_Ref);
			}

			il.Emit(OpCodes.Ret);
		}

		private static void EmitWrapperMethod(
			TypeBuilder typeBuilder,
			FieldInfo targetField,
			FieldInfo adapterField,
			FieldInfo methodsField,
			MethodInfo targetMethod,
			int methodIndex)
		{
			// public object MethodName(params object[] args)
			var mb = typeBuilder.DefineMethod(
				targetMethod.Name,
				MethodAttributes.Public,
				typeof(object),
				new[] { typeof(object[]) });

			var param = mb.DefineParameter(1, ParameterAttributes.None, "args");
			param.SetCustomAttribute(new CustomAttributeBuilder(
				typeof(ParamArrayAttribute).GetConstructor(Type.EmptyTypes)!,
				Array.Empty<object>()));

			var il = mb.GetILGenerator();

			// load target: this._target
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, targetField);

			// load MethodInfo: _methods[methodIndex]
			il.Emit(OpCodes.Ldsfld, methodsField);
			il.Emit(OpCodes.Ldc_I4, methodIndex);
			il.Emit(OpCodes.Ldelem_Ref);

			// load args
			il.Emit(OpCodes.Ldarg_1);

			// load adapter: this._adapter
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, adapterField);

			// call WrapperRuntime.Invoke(target, methodInfo, args, adapter)
			il.Emit(OpCodes.Call, typeof(WrapperRuntime).GetMethod(nameof(WrapperRuntime.Invoke))!);

			il.Emit(OpCodes.Ret);
		}
	}

	public static class WrapperRuntime
	{
		public static object Invoke(
			object target,
			MethodInfo method,
			object[] jsArgs,
			IJsEngineAdapter adapter)
		{
			var parameters = method.GetParameters();
			var finalArgs = new object[parameters.Length];

			for (int i = 0; i < parameters.Length; i++)
			{
				var p = parameters[i];

				if (i < jsArgs.Length && jsArgs[i] != null && jsArgs[i] != Undefined.Value)
				{
					finalArgs[i] = ConvertArg(jsArgs[i], p.ParameterType, adapter);
				}
				else if (p.HasDefaultValue)
				{
					finalArgs[i] = p.DefaultValue;
				}
				else
				{
					throw new ArgumentException($"Missing required parameter '{p.Name}' for {method.Name}");
				}
			}

			var result = method.Invoke(target, finalArgs);

			return method.ReturnType == typeof(void) ? null : result;
		}

		private static object ConvertArg(object value, Type targetType, IJsEngineAdapter adapter)
		{
			if (value == null || value == Undefined.Value)
				return null;

			// Already assignable?
			if (targetType.IsInstanceOfType(value))
				return value;

			// Enums
			if (targetType.IsEnum)
				return Enum.ToObject(targetType, Convert.ToInt32(value));

			// Numeric basics
			if (targetType == typeof(uint))
				return Convert.ToUInt32(value);

			if (targetType == typeof(int))
				return Convert.ToInt32(value);

			if (targetType == typeof(long))
				return Convert.ToInt64(value);

			if (targetType == typeof(ulong))
				return Convert.ToUInt64(value);

			// Delegates / callbacks → defer to engine adapter
			if (typeof(Delegate).IsAssignableFrom(targetType))
				return adapter.ConvertToDelegate(value, targetType);

			// Fallback: try ChangeType
			return Convert.ChangeType(value, targetType);
		}
	}

	internal sealed class JsCallbackClosure
	{
		private readonly Func<object?[], object?> _invoker;

		public JsCallbackClosure(Func<object?[], object?> invoker)
		{
			_invoker = invoker;
		}

		public object? Invoke(object?[] args)
		{
			return _invoker(args);
		}
	}

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

	public sealed class JintAdapter : IJsEngineAdapter
	{
		private readonly Engine _engine;

		public JintAdapter(Engine engine)
		{
			_engine = engine;
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
				Func<object?[], object?> invoker = args =>
				{
					var jsArgs = args.Select(a => JsValue.FromObject(_engine, a)).ToArray();
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
			Func<object?[], object?> invoker = args =>
			{
				var jsArgs = args.Select(a => JsValue.FromObject(_engine, a)).ToArray();
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
