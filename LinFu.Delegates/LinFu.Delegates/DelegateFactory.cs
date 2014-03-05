using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using LinFu.DynamicProxy;

namespace LinFu.Delegates
{
    public static class DelegateFactory
    {
        private static readonly ProxyFactory _factory = new ProxyFactory();
        private static readonly Dictionary<DelegateInfo, Type>
            _typeCache = new Dictionary<DelegateInfo, Type>();
        public static Type DefineDelegateType(string typeName, 
            Type returnType, ParameterInfo[] parameters)
        {
            var parameterTypes = new List<Type>();
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    parameterTypes.Add(param.ParameterType);
                }
            }
            return DefineDelegateType(typeName, returnType, parameterTypes.ToArray());
        }
        public static Type DefineDelegateType(string typeName, Type returnType, params Type[] parameterTypes)
        {
            var info = new DelegateInfo(returnType, parameterTypes);

            if (_typeCache.ContainsKey(info))
                return _typeCache[info];

            var currentDomain = AppDomain.CurrentDomain;
            var assemblyName = string.Format("{0}Assembly", typeName);
            var moduleName = string.Format("{0}Module", typeName);

            var name = new AssemblyName(assemblyName);
            var access = AssemblyBuilderAccess.RunAndSave;
            var assemblyBuilder = currentDomain.DefineDynamicAssembly(name, access);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName, string.Format("{0}.mod", moduleName), true);
            var typeAttributes = TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.AnsiClass | TypeAttributes.AutoClass;
            var typeBuilder = moduleBuilder.DefineType(typeName, typeAttributes, typeof(MulticastDelegate));

            // Delegate constructor
            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.RTSpecialName |
                MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.SpecialName,
                CallingConventions.Standard,
                new[] { typeof(object), typeof(System.IntPtr) });

            constructorBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            // Define the Invoke method with a signature that matches the parameter types
            var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
            var methodBuilder = typeBuilder.DefineMethod("Invoke", methodAttributes, returnType, parameterTypes);
            methodBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            #region Define the Begin/EndInvoke methods for async callback support
            methodBuilder = typeBuilder.DefineMethod("BeginInvoke", methodAttributes, typeof(System.IAsyncResult),
                new[] { typeof(int), typeof(AsyncCallback), typeof(object) });
            methodBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            methodBuilder = typeBuilder.DefineMethod("EndInvoke", methodAttributes, typeof(void),
                new[] { typeof(IAsyncResult) });
            methodBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
            #endregion

            var result = typeBuilder.CreateType();

            // Cache the result
            if (result != null)
                _typeCache[info] = result;

            return result;
        }

        public static MulticastDelegate DefineDelegate(Type delegateType, CustomDelegate methodBody)
        {
            var invokeMethod = delegateType.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance);
            
            var returnType = invokeMethod.ReturnType;
            var parameterTypes = new List<Type>();
            foreach(var param in invokeMethod.GetParameters())
            {
                parameterTypes.Add(param.ParameterType);
            }
            return DefineDelegate(delegateType, methodBody, returnType, parameterTypes.ToArray());
        }
        public static MulticastDelegate DefineDelegate(CustomDelegate methodBody, Type returnType, params Type[] parameterTypes)
        {
            var delegateType = DelegateFactory.DefineDelegateType("___Anonymous", returnType, parameterTypes);
            return DefineDelegate(delegateType, methodBody, returnType, parameterTypes);
        }
        public static MulticastDelegate DefineDelegate(Type delegateType, CustomDelegate methodBody, Type returnType, Type[] parameterTypes)
        {
            // Generate an interface that matches the given signature and return type
            var interfaceType = InterfaceBuilder.DefineInterfaceMethod(returnType, parameterTypes);

            // Proxy the interface type 
            var redirector = new CustomDelegateRedirector(methodBody);
            var interfaceInstance = _factory.CreateProxy(typeof (object), redirector, interfaceType);

            // Map the call from the custom delegate to the target
            // delegate type
            var targetMethod = interfaceType.GetMethods()[0];
            var result = BindTo(delegateType, targetMethod, interfaceInstance);


            return result;
        }

        private static MulticastDelegate BindTo(Type delegateType, 
            MethodInfo targetMethod, object interfaceInstance)
        {
            var methodPointer = targetMethod.MethodHandle.GetFunctionPointer();

            // Attach the newly implemented interface to the target delegate

            // Generate the custom delegate type 
            MulticastDelegate result = null;

            try
            {
                result = (MulticastDelegate)Activator.CreateInstance(delegateType, new[] { interfaceInstance, methodPointer });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
            return result;
        }

        public static MulticastDelegate DefineDelegate(object instance, MethodInfo targetMethod)
        {
            var delegateType = DefineDelegateType("Anonymous", targetMethod.ReturnType, targetMethod.GetParameters());

            return BindTo(delegateType, targetMethod, instance);
        }
    }
}
