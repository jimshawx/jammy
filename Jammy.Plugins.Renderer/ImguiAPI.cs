using ImGuiNET;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

public static class ImGuiAPI
{
	private static volatile object instance;
	private static readonly Lock @lock = new Lock();

	public static object Instance
	{
		get
		{
			if (instance == null)
			{
				lock (@lock)
				{
					if (instance == null)
						instance = BuildImGuiWrapper();
				}
			}
			return instance;
		}
	}

	private static object BuildImGuiWrapper()
	{
		var asmName = new AssemblyName("Jammy.Plugins.ImGui.Interface");
		var asm = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
		var module = asm.DefineDynamicModule("MainModule");

		// Mark as debuggable (disable optimizations)
		//var dbgCtor = typeof(DebuggableAttribute).GetConstructor(new[] { typeof(DebuggableAttribute.DebuggingModes) });
		//var dbgAttr = new CustomAttributeBuilder(
		//	dbgCtor!,
		//	new object[] {
		//		DebuggableAttribute.DebuggingModes.DisableOptimizations |
		//		DebuggableAttribute.DebuggingModes.Default
		//	});
		//asm.SetCustomAttribute(dbgAttr);

		var typeBuilder = module.DefineType(
			"ImGuiDynamicWrapper",
			TypeAttributes.Public | TypeAttributes.Class);

		var imguiType = typeof(ImGui);

		foreach (var method in imguiType.GetMethods(BindingFlags.Public | BindingFlags.Static)
											.OrderBy(x=>x.Name)
											.ThenBy(x=>x.GetParameters().Length))
		{
			// skip generic methods
			if (method.IsGenericMethodDefinition)
				continue;

			var parameters = method.GetParameters();

			// ignore generic parameter types
			if (parameters.Any(x=>x.ParameterType.IsGenericType))
				continue;

			var byRefParams = parameters.Where(p => p.ParameterType.IsByRef).ToArray();

			if (byRefParams.Length == 0)
			{
				EmitDirectWrapper(typeBuilder, method, parameters);
			}
			else if (byRefParams.Length == 1)
			{
				EmitSingleRefWrapper(typeBuilder, method, parameters, byRefParams[0]);
			}
			else
			{
				Trace.WriteLine("Skipping ImGui method with multiple ref/out parameters: " + method.Name);
				// Methods with multiple ref/out parameters are skipped
			}
		}

		var wrapperType = typeBuilder.CreateType()
						 ?? throw new InvalidOperationException("Failed to create wrapper type");

		return Activator.CreateInstance(wrapperType)
			   ?? throw new InvalidOperationException("Failed to instantiate wrapper");
	}

	private static void EmitDirectWrapper0(
		TypeBuilder typeBuilder,
		MethodInfo targetMethod,
		ParameterInfo[] parameters)
	{
		var paramTypes = parameters.Select(p => p.ParameterType).ToArray();

		var mb = typeBuilder.DefineMethod(
			targetMethod.Name,
			MethodAttributes.Public,
			targetMethod.ReturnType,
			paramTypes);

		for (int i = 0; i < parameters.Length; i++)
			mb.DefineParameter(i + 1, ParameterAttributes.None, parameters[i].Name);

		var il = mb.GetILGenerator();

		for (int i = 0; i < paramTypes.Length; i++)
			il.Emit(OpCodes.Ldarg_S, (byte)(i+1));

		il.Emit(OpCodes.Call, targetMethod);
		il.Emit(OpCodes.Ret);
	}
	private static void EmitDirectWrapper(
	TypeBuilder typeBuilder,
	MethodInfo targetMethod,
	ParameterInfo[] parameters)
	{
		// Replace string parameters with object in the wrapper signature
		var wrapperParamTypes = parameters
			.Select(p => p.ParameterType == typeof(string) ? typeof(object) : p.ParameterType)
			.ToArray();

		var mb = typeBuilder.DefineMethod(
			targetMethod.Name,
			MethodAttributes.Public,
			targetMethod.ReturnType,
			wrapperParamTypes);

		for (int i = 0; i < parameters.Length; i++)
			mb.DefineParameter(i + 1, ParameterAttributes.None, parameters[i].Name);

		var il = mb.GetILGenerator();

		for (int i = 0; i < parameters.Length; i++)
		{
			var originalType = parameters[i].ParameterType;

			// Load argument
			il.Emit(OpCodes.Ldarg_S, (byte)(i + 1));

			if (originalType == typeof(string))
			{
				// Convert object → string via ToString()
				var toString = typeof(object).GetMethod(nameof(ToString));
				il.Emit(OpCodes.Callvirt, toString);
			}
		}

		il.Emit(OpCodes.Call, targetMethod);
		il.Emit(OpCodes.Ret);
	}
	private static void EmitDirectWrapper2(
	TypeBuilder typeBuilder,
	MethodInfo targetMethod,
	ParameterInfo[] parameters)
	{
		// Replace string → object, int → double
		var wrapperParamTypes = parameters
			.Select(p =>
				p.ParameterType == typeof(string) ? typeof(object) :
				p.ParameterType == typeof(int) ? typeof(double) :
				p.ParameterType)
			.ToArray();

		var mb = typeBuilder.DefineMethod(
			targetMethod.Name,
			MethodAttributes.Public,
			targetMethod.ReturnType,
			wrapperParamTypes);

		for (int i = 0; i < parameters.Length; i++)
			mb.DefineParameter(i + 1, ParameterAttributes.None, parameters[i].Name);

		var il = mb.GetILGenerator();

		for (int i = 0; i < parameters.Length; i++)
		{
			var originalType = parameters[i].ParameterType;

			// Load wrapper argument
			il.Emit(OpCodes.Ldarg_S, (byte)(i + 1));

			if (originalType == typeof(string))
			{
				// object → string via ToString()
				var toString = typeof(object).GetMethod(nameof(ToString));
				il.Emit(OpCodes.Callvirt, toString);
			}
			else if (originalType == typeof(int))
			{
				// wrapper param is double → convert to int32
				il.Emit(OpCodes.Conv_I4);
			}
		}

		il.Emit(OpCodes.Call, targetMethod);
		il.Emit(OpCodes.Ret);
	}
	private static void EmitDirectWrapper3(
	TypeBuilder typeBuilder,
	MethodInfo targetMethod,
	ParameterInfo[] parameters)
	{
		// Replace string → object, int → double, enum → double
		var wrapperParamTypes = parameters
			.Select(p =>
				p.ParameterType == typeof(string) ? typeof(object) :
				p.ParameterType == typeof(int) ? typeof(double) :
				p.ParameterType.IsEnum ? typeof(double) :
				p.ParameterType)
			.ToArray();

		var mb = typeBuilder.DefineMethod(
			targetMethod.Name,
			MethodAttributes.Public,
			targetMethod.ReturnType,
			wrapperParamTypes);

		for (int i = 0; i < parameters.Length; i++)
			mb.DefineParameter(i + 1, ParameterAttributes.None, parameters[i].Name);

		var il = mb.GetILGenerator();

		for (int i = 0; i < parameters.Length; i++)
		{
			var originalType = parameters[i].ParameterType;

			// Load wrapper argument
			il.Emit(OpCodes.Ldarg_S, (byte)(i + 1));

			if (originalType == typeof(string))
			{
				// object → string via ToString()
				var toString = typeof(object).GetMethod(nameof(ToString));
				il.Emit(OpCodes.Callvirt, toString);
			}
			else if (originalType == typeof(int))
			{
				// wrapper param is double → convert to int32
				il.Emit(OpCodes.Conv_I4);
			}
			else if (originalType.IsEnum)
			{
				// wrapper param is double → convert to enum's underlying type
				var underlying = Enum.GetUnderlyingType(originalType);

				// Convert double → underlying integer
				if (underlying == typeof(int))
					il.Emit(OpCodes.Conv_I4);
				else if (underlying == typeof(uint))
					il.Emit(OpCodes.Conv_U4);
				else if (underlying == typeof(short))
					il.Emit(OpCodes.Conv_I2);
				else if (underlying == typeof(ushort))
					il.Emit(OpCodes.Conv_U2);
				else if (underlying == typeof(byte))
					il.Emit(OpCodes.Conv_U1);
				else if (underlying == typeof(sbyte))
					il.Emit(OpCodes.Conv_I1);
				else if (underlying == typeof(long))
					il.Emit(OpCodes.Conv_I8);
				else if (underlying == typeof(ulong))
					il.Emit(OpCodes.Conv_U8);
				else
					throw new NotSupportedException($"Unsupported enum underlying type: {underlying}");

				// Now treat the integer as the enum
				il.Emit(OpCodes.Conv_I4); // IL requires the correct stack type
			}
		}

		il.Emit(OpCodes.Call, targetMethod);
		il.Emit(OpCodes.Ret);
	}


	/// <summary>
	/// For methods with exactly one ref/out parameter:
	/// - Wrapper parameter at that position is 'object' (JS passes { value: ... }).
	/// - Wrapper:
	/// * reads box.value into a local
	/// * passes it by ref to the original method
	/// * writes updated value back to box.value
	/// * returns the original return value
	/// </summary>
	private static void EmitSingleRefWrapper(
		TypeBuilder typeBuilder,
		MethodInfo targetMethod,
		ParameterInfo[] parameters,
		ParameterInfo refParam)
	{
		int refIndex = Array.IndexOf(parameters, refParam);
		var refElementType = refParam.ParameterType.GetElementType()
							 ?? throw new InvalidOperationException("ByRef without element type");

		// Wrapper parameter types: same count, but ref param becomes object
		var wrapperParamTypes = parameters
			.Select((p, i) => i == refIndex ? typeof(object) : p.ParameterType)
			.ToArray();

		var mb = typeBuilder.DefineMethod(
			targetMethod.Name,
			MethodAttributes.Public,
			targetMethod.ReturnType,
			wrapperParamTypes);

		for (int i = 0; i < parameters.Length; i++)
			mb.DefineParameter(i + 1, ParameterAttributes.None, parameters[i].Name);

		var il = mb.GetILGenerator();

		// Locals:
		// 0: ref value local (T)
		// 1: original return value (if not void)
		var refLocal = il.DeclareLocal(refElementType);
		LocalBuilder? retLocal = null;
		if (targetMethod.ReturnType != typeof(void))
			retLocal = il.DeclareLocal(targetMethod.ReturnType);

		// Call helper: T GetRefValue<T>(object box)
		var getMethodGeneric = typeof(RefBoxHelper)
			.GetMethod(nameof(RefBoxHelper.GetRefValue), BindingFlags.Public | BindingFlags.Static)
			?? throw new InvalidOperationException("GetRefValue not found");

		var getMethod = getMethodGeneric.MakeGenericMethod(refElementType);

		// Load arg[refIndex] (object box)
		il.Emit(OpCodes.Ldarg_S, (byte)refIndex);

		// Call GetRefValue<T>(box)
		il.Emit(OpCodes.Call, getMethod);
		// Store into refLocal
		il.Emit(OpCodes.Stloc, refLocal);

		// Load arguments for original call
		for (int i = 0; i < parameters.Length; i++)
		{
			if (i == refIndex)
			{
				// Load address of refLocal for ref/out
				il.Emit(OpCodes.Ldloca_S, refLocal);
			}
			else
			{
				il.Emit(OpCodes.Ldarg_S, (byte)(i+1));
			}
		}

		// Call original ImGui method
		il.Emit(OpCodes.Call, targetMethod);

		// Store original return value if not void
		if (targetMethod.ReturnType != typeof(void) && retLocal != null)
			il.Emit(OpCodes.Stloc, retLocal);

		// Call helper: void SetRefValue<T>(object box, T value)
		var setMethodGeneric = typeof(RefBoxHelper)
			.GetMethod(nameof(RefBoxHelper.SetRefValue), BindingFlags.Public | BindingFlags.Static)
			?? throw new InvalidOperationException("SetRefValue not found");

		var setMethod = setMethodGeneric.MakeGenericMethod(refElementType);

		// Load arg[refIndex] (object box)
		if (refIndex <= byte.MaxValue)
			il.Emit(OpCodes.Ldarg_S, (byte)refIndex);
		else
			il.Emit(OpCodes.Ldarg, refIndex);

		// Load updated refLocal
		il.Emit(OpCodes.Ldloc, refLocal);

		// Call SetRefValue<T>(box, value)
		il.Emit(OpCodes.Call, setMethod);

		// Load and return original return value (or just return for void)
		if (targetMethod.ReturnType == typeof(void))
		{
			il.Emit(OpCodes.Ret);
		}
		else
		{
			il.Emit(OpCodes.Ldloc, retLocal!);
			il.Emit(OpCodes.Ret);
		}
	}
}

public static class RefBoxHelper
{
	/// <summary>
	/// Reads box.value as T. If box or property is missing, returns default(T).
	/// </summary>
	public static T GetRefValue<T>(object box)
	{
		if (box == null)
			return default!;

		var type = box.GetType();
		var prop = type.GetProperty("value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		if (prop == null || !prop.CanRead)
			return default!;

		var raw = prop.GetValue(box);
		if (raw == null)
			return default!;

		if (raw is T t)
			return t;

		return (T)Convert.ChangeType(raw, typeof(T));
	}

	/// <summary>
	/// Writes value into box.value if possible.
	/// </summary>
	public static void SetRefValue<T>(object box, T value)
	{
		if (box == null)
			return;

		var type = box.GetType();
		var prop = type.GetProperty("value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		if (prop == null || !prop.CanWrite)
			return;

		prop.SetValue(box, value);
	}
}