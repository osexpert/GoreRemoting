using System.Reflection;
using System.Reflection.Emit;
using Castle.DynamicProxy;
using stakx.DynamicProxy;

namespace GoreRemoting.RemoteDelegates;

/// <summary>
/// Proxy for intercepting calls on a specified delegate type. 
/// </summary>
public sealed class DelegateProxy : AsyncInterceptor
{
	private Func<MethodInfo, object?[], object?> _callInterceptionHandler;
	private readonly Func<MethodInfo, object?[], Task<object?>> _callInterceptionAsyncHandler;

	static readonly MethodInfo _interceptMethod = typeof(DelegateProxy ).GetMethod(
			name: nameof(DelegateProxy.DelegateTarget),
		bindingAttr: BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance);

	/// <summary>
	/// Creates a new instance of the DelegateProxy class.
	/// </summary>
	/// <param name="delegateType">Delegate type to be proxied</param>
	/// <param name="callInterceptionHandler">Function to be called when intercepting calls on the delegate</param>
	internal DelegateProxy(Type delegateType, 
		Func<MethodInfo, object?[], object?> callInterceptionHandler,
		Func<MethodInfo, object?[], Task<object?>> callInterceptionAsyncHandler)
	{
		_callInterceptionHandler =
			callInterceptionHandler ??
				throw new ArgumentNullException(nameof(callInterceptionHandler));

		_callInterceptionAsyncHandler =
			callInterceptionAsyncHandler ??
				throw new ArgumentNullException(nameof(callInterceptionAsyncHandler));

		ProxiedDelegate =
			CreateProxiedDelegate(
				delegateType: delegateType,
				interceptMethod: _interceptMethod,
				interceptor: this);
	}

	protected override void Intercept(IInvocation invocation)
	{
		var res = _callInterceptionHandler(invocation.Method, invocation.Arguments);
		invocation.ReturnValue = res;
	}

	protected override async ValueTask InterceptAsync(IAsyncInvocation invocation)
	{
		var res = await _callInterceptionAsyncHandler(invocation.Method, invocation.Arguments.ToArray()).ConfigureAwait(false);
		invocation.Result = res;
	}

	/// <summary>
	/// Gets the proxied delegate.
	/// </summary>
	public Delegate ProxiedDelegate { get; private set; }

	/// <summary>
	/// Method called by delegate proxy, when the proxies delegate is called.
	/// </summary>
	/// <param name="args">Arguments passed to the proxied delegate by caller</param>
	/// <returns>Return value provided by call interception handler</returns>
	private object? DelegateTarget(params object?[] args)
	{
		var sin = new Invocation(ProxiedDelegate.Method, args);
		((IInterceptor)this).Intercept(sin);
		return sin.ReturnValue;
	}

	class Invocation(MethodInfo method, object?[] args) : IInvocation
	{
		readonly MethodInfo _method = method;
		readonly object?[] _args = args;

		public object?[] Arguments => _args;
		public MethodInfo Method => _method;
		public object? ReturnValue { get; set; }
		public IInvocationProceedInfo CaptureProceedInfo() => null!;

		public Type[] GenericArguments => throw new NotImplementedException();
		public object InvocationTarget => throw new NotImplementedException();
		public MethodInfo MethodInvocationTarget => throw new NotImplementedException();
		public object Proxy => throw new NotImplementedException();
		public Type TargetType => throw new NotImplementedException();
		public object GetArgumentValue(int index) => throw new NotImplementedException();
		public MethodInfo GetConcreteMethod() => throw new NotImplementedException();
		public MethodInfo GetConcreteMethodInvocationTarget() => throw new NotImplementedException();
		public void Proceed() => throw new NotImplementedException();
		public void SetArgumentValue(int index, object? value) => throw new NotImplementedException();
	}

	/// <summary>
	/// Creates a delegate for intercepting calls on a specified delegate type. 
	/// </summary>
	/// <param name="delegateType">The delegate type to proxy</param>
	/// <param name="interceptMethod">Method to call when intercepting calls on the proxied delegate</param>
	/// <param name="interceptor">Object on which the intercept method is called</param>
	/// <returns>Proxied delegate</returns>
	/// <exception cref="ArgumentNullException">Thrown if any argument is null</exception>
	/// <exception cref="NotSupportedException">Thrown if delegate type has no 'Invoke' method</exception>
	/// <exception cref="ArgumentException">Thrown if argument 'delegateType' is not a delegate</exception>
	private Delegate CreateProxiedDelegate(Type delegateType, MethodInfo interceptMethod, object interceptor)
	{
		if (delegateType == null)
			throw new ArgumentNullException(nameof(delegateType));

		if (interceptMethod == null)
			throw new ArgumentNullException(nameof(interceptMethod));

		if (interceptor == null)
			throw new ArgumentNullException(nameof(interceptor));

		if (!typeof(Delegate).IsAssignableFrom(delegateType))
			throw new ArgumentException("Specified type must be a delegate type.", nameof(delegateType));

		var interceptorObjectType = interceptor.GetType();
		var invokeMethod = delegateType.GetMethod("Invoke");

		if (invokeMethod == null)
			throw new NotSupportedException("Provided delegate type has no 'Invoke' method.");

		var parameterTypeList =
			invokeMethod
				.GetParameters()
				.Select(p => p.ParameterType)
				.ToList();

		var parameterCount = parameterTypeList.Count;
		parameterTypeList.Insert(0, interceptorObjectType);
		var parameterTypes = parameterTypeList.ToArray();

		// Create dynamic method for delegate call interception
		var delegateProxyMethod =
			new DynamicMethod(
				name: interceptMethod.Name,
				returnType: invokeMethod.ReturnType,
				parameterTypes: parameterTypes,
				owner: interceptorObjectType);

		// Create method body, declare local variable of type object[]
		var ilGenerator = delegateProxyMethod.GetILGenerator();
		var argumentsArray = ilGenerator.DeclareLocal(typeof(object[]));

		// var args = new object[paramCount];
		ilGenerator.Emit(OpCodes.Nop);
		ilGenerator.Emit(OpCodes.Ldc_I4, parameterCount);
		ilGenerator.Emit(OpCodes.Newarr, typeof(object));
		ilGenerator.Emit(OpCodes.Stloc, argumentsArray);

		var index = 1;

		// Load method arguments one by one
		foreach (var paramType in parameterTypes.Skip(1))
		{
			// Load object[] array reference
			ilGenerator.Emit(OpCodes.Ldloc, argumentsArray);
			ilGenerator.Emit(OpCodes.Ldc_I4, index - 1); // Array index
			ilGenerator.Emit(OpCodes.Ldarg, index++); // Method parameter index

			// Box parameter value, if it is a value type
			if (typeof(ValueType).IsAssignableFrom(paramType))
				ilGenerator.Emit(OpCodes.Box, paramType);

			// Store reference
			ilGenerator.Emit(OpCodes.Stelem_Ref);
		}

		// Call intercept method and pass parameters as arguments array
		ilGenerator.Emit(OpCodes.Ldarg_0);
		ilGenerator.Emit(OpCodes.Ldloc, argumentsArray); // object[] args
		ilGenerator.Emit(OpCodes.Call, interceptMethod);

		// Discard return value, if return type of proxied delegate is void
		if (invokeMethod.ReturnType == typeof(void))
			ilGenerator.Emit(OpCodes.Pop);
		else if (typeof(ValueType).IsAssignableFrom(invokeMethod.ReturnType)) // Unbox return value, if it is a value type
			ilGenerator.Emit(OpCodes.Unbox_Any, invokeMethod.ReturnType);

		// Return the return value
		ilGenerator.Emit(OpCodes.Ret);

		// Bake dynamic method and create a delegate of it
		var result =
			delegateProxyMethod.CreateDelegate(delegateType, interceptor);

		// Return as proxied delegate type
		return result;
	}

	/// <summary>
	/// Frees managed resources.
	/// </summary>
	public void Dispose()
	{
		ProxiedDelegate = null;
		_callInterceptionHandler = null;
	}
}
