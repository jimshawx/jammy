using Microsoft.ClearScript;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Jammy.Plugins.JavaScript.ClearScript
{
	public static class WrapperFactory
	{
		public static object CreateWrapper(object target)
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

			// private static readonly MethodInfo[] _methods;
			var methodsField = typeBuilder.DefineField("_methods", typeof(MethodInfo[]),
				FieldAttributes.Private | FieldAttributes.Static);

			// .ctor(DebuggerApi target)
			EmitCtor(typeBuilder, targetField);

			// static .cctor: cache MethodInfo[]
			EmitTypeInitializer(typeBuilder, targetType, methodsField);

			// For each public instance method, emit wrapper
			var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
									.Where(m => !m.IsSpecialName)
									.ToArray();

			for (int i = 0; i < methods.Length; i++)
			{
				EmitWrapperMethod(typeBuilder, targetField, methodsField, methods[i], i);
			}

			var wrapperType = typeBuilder.CreateType()
							  ?? throw new InvalidOperationException("Failed to create wrapper type.");

			return Activator.CreateInstance(wrapperType, target)
				   ?? throw new InvalidOperationException("Failed to create wrapper instance.");
		}

		private static void EmitCtor(TypeBuilder typeBuilder, FieldInfo targetField)
		{
			var ctor = typeBuilder.DefineConstructor(
				MethodAttributes.Public,
				CallingConventions.Standard,
				new[] { targetField.FieldType });

			var il = ctor.GetILGenerator();

			// base .ctor()
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);

			// this._target = arg1;
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Stfld, targetField);

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

			// call WrapperRuntime.Invoke(target, methodInfo, args)
			il.Emit(OpCodes.Call, typeof(WrapperRuntime).GetMethod(nameof(WrapperRuntime.Invoke))!);

			il.Emit(OpCodes.Ret);
		}
	}

	public static class WrapperRuntime
	{
		public static object Invoke(
			object target,
			MethodInfo method,
			object[] jsArgs)
		{
			var parameters = method.GetParameters();
			var finalArgs = new object[parameters.Length];

			for (int i = 0; i < parameters.Length; i++)
			{
				var p = parameters[i];

				if (i < jsArgs.Length && jsArgs[i] != null && jsArgs[i] != Undefined.Value)
				{
					finalArgs[i] = ConvertArg(jsArgs[i], p.ParameterType);
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

		private static object ConvertArg(object value, Type targetType)
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

			// Delegates / callbacks
			if (typeof(Delegate).IsAssignableFrom(targetType))
				return ConvertCallback(value, targetType);

			// Fallback: try ChangeType
			return Convert.ChangeType(value, targetType);
		}

		private static object ConvertCallback(object jsFunc, Type delegateType)
		{
			if (jsFunc == null)
				return null;

			// ClearScript: JS function → ScriptObject
			if (jsFunc is ScriptObject scriptObj)
				return BuildDelegateFromScriptObject(scriptObj, delegateType);

			// Jint or already a delegate
			if (jsFunc is Delegate d && delegateType.IsInstanceOfType(d))
				return d;

			throw new NotSupportedException($"Cannot convert callback of type {jsFunc.GetType()} to {delegateType}");
		}

		private static object BuildDelegateFromScriptObject(ScriptObject scriptObj, Type delegateType)
		{
			var invoke = delegateType.GetMethod("Invoke");
			var paramInfos = invoke.GetParameters();

			// Only handle Func<T,bool> / Func<T> / Action<T> style here
			return Delegate.CreateDelegate(
				delegateType,
				new ScriptCallbackThunk(scriptObj, paramInfos),
				typeof(ScriptCallbackThunk).GetMethod(nameof(ScriptCallbackThunk.Invoke))!
			);
		}

		private sealed class ScriptCallbackThunk
		{
			private readonly ScriptObject _func;
			private readonly ParameterInfo[] _parameters;

			public ScriptCallbackThunk(ScriptObject func, ParameterInfo[] parameters)
			{
				_func = func;
				_parameters = parameters;
			}

			public object? Invoke(object? arg)
			{
				// Single-arg callbacks (e.g. Func<Breakpoint,bool>)
				if (_parameters.Length == 1)
					return _func.Invoke(false, arg);

				// No-arg callbacks
				if (_parameters.Length == 0)
					return _func.Invoke(false);

				// Extend here for multi-arg callbacks if you need them
				throw new NotSupportedException("Only single-arg callbacks are supported in this thunk.");
			}
		}
	}
}
