using System;
using System.Collections.Generic;
using System.Reflection;
using LinFu.Delegates;
using LinFu.DynamicProxy;
using LinFu.Finders;

namespace LinFu.Reflection
{
    public class DynamicObject : IMethodMissingCallback, IInterceptor
    {
        private readonly ProxyFactory _factory;
        private readonly IMethodFinder _finder = new MethodFinder();
        private readonly List<IMethodMissingCallback> _handlers = new List<IMethodMissingCallback>();
        private object _target;

        public DynamicObject()
        {
            _target = new object();
            _factory = new ProxyFactory();
        }

        public DynamicObject(object target)
        {
            Target = target;
            _factory = new ProxyFactory();
        }

        public static DynamicObject operator -(DynamicObject lhs, IMethodMissingCallback callback)
        {
            if (lhs._handlers.Contains(callback))
                lhs._handlers.Remove(callback);

            return lhs;
        }

        public static DynamicObject operator +(DynamicObject lhs, IMethodMissingCallback callback)
        {
            lhs._handlers.Add(callback);

            return lhs;
        }

        public IObjectMethods Methods
        {
            get { return new Binder(_target, _finder, this); }
        }

        public IObjectProperties Properties
        {
            get { return new Binder(_target, _finder, this); }
        }

        public object Target
        {
            get { return _target; }
            set { _target = value; }
        }

        public object Intercept(InvocationInfo info)
        {
            return Methods[info.TargetMethod.Name](info.Arguments);
        }

        public void MethodMissing(object source, MethodMissingParameters missingParameters)
        {
            bool handled = false;
            object result = null;
            try
            {
                result = Methods[missingParameters.MethodName](missingParameters.Arguments);
                handled = true;
            }
            catch (Exception ex)
            {
                handled = false;
            }
            missingParameters.Handled = handled;

            if (handled)
                missingParameters.ReturnValue = result;
        }

        public bool CanHandle(MethodInfo method)
        {
            var targetType = _target.GetType();
            var searchPool =
                targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var searchList = searchPool.AsFuzzyList();
            PredicateBuilder.AddPredicates(searchList, method);
            // Find a method that can handle this particular method signature
            var match = searchList.BestMatch(.66);

            if (match != null)
                return true;

            var found = false;
            foreach (var callback in _handlers)
            {
                if (!callback.CanHandle(method))
                    continue;

                found = true;
                break;
            }

            return found;
        }

        internal object ExecuteMethodMissing(string methodName, object[] args, ref bool handled)
        {
            if (_handlers.Count == 0)
                throw new MethodNotFoundException(
                    string.Format("Method '{0}' not found on type '{1}",
                                  methodName, Target.GetType().FullName));

            // Emulate Ruby's MethodMissing behavior
            // so that methods can be added to this 
            // class at runtime
            var missingParameters =
                new MethodMissingParameters(methodName, _target, args);

            // Recently added handlers take priority
            var handlers = new List<IMethodMissingCallback>(_handlers);
            handlers.Reverse();

            // Fire the event until a handler is found 
            foreach (IMethodMissingCallback callback in handlers)
            {
                callback.MethodMissing(this, missingParameters);
                if (missingParameters.Handled)
                    break;
            }


            if (missingParameters.Handled == false)
                throw new MethodNotFoundException(
                    string.Format("Method '{0}' not found on type '{1}",
                                  methodName, Target.GetType().FullName));

            handled = missingParameters.Handled;

            return missingParameters.ReturnValue;
        }

        public void AddMethod(string methodName, MulticastDelegate body)
        {
            _handlers.Add(new DelegateMixin(methodName, body));
        }

        public void AddMethod(string methodName, CustomDelegate body, Type returnType, params Type[] parameters)
        {
            MulticastDelegate stronglyTypedDelegate = DelegateFactory.DefineDelegate(body, returnType, parameters);
            if (stronglyTypedDelegate == null)
                return;

            AddMethod(methodName, stronglyTypedDelegate);
        }

        public void MixWith(object otherInstance)
        {
            if (otherInstance == null)
                throw new ArgumentNullException("otherInstance");

            if (otherInstance is DynamicObject)
            {
                var other = (DynamicObject)otherInstance;
                _handlers.Add(other);
                return;
            }

            Type targetType = otherInstance.GetType();

            MethodInfo[] methods =
                targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (MethodInfo method in methods)
            {
                if (method.IsGenericMethodDefinition)
                    continue;

                // Ignore anything declared on System.Object
                if (method.DeclaringType == typeof(object))
                    continue;
                if (method.DeclaringType == typeof(IMixinAware))
                    continue;

                Type delegateType =
                    DelegateFactory.DefineDelegateType("__AnonymousDelegate",
                                                       method.ReturnType, method.GetParameters());

                // Bind it to the new delegate
                IntPtr methodPointer = method.MethodHandle.GetFunctionPointer();
                var delegateInstance =
                    Activator.CreateInstance(delegateType, new[] { otherInstance, methodPointer }) as MulticastDelegate;

                // Mix the new delegate in with everything else
                AddMethod(method.Name, delegateInstance);
            }

            // Introduce the dynamic object with the mixin
            var mixin = otherInstance as IMixinAware;
            if (mixin == null)
                return;

            mixin.Self = this;
        }

        public bool LooksLike<T>()
            where T : class
        {
            return LooksLike(typeof(T));
        }

        public bool LooksLike(Type comparisonType)
        {
            if (_target == null)
                return false;

            Type targetType = _target.GetType();

            // Build the list of methods to compare against
            var methodPool = new List<MethodInfo>();

            // Add the methods native to the type
            methodPool.AddRange(targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance));
            var searchPool = methodPool.AsFuzzyList();
            
            MethodInfo[] comparisonTypeMethods = comparisonType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            bool result = true;
            foreach (MethodInfo method in comparisonTypeMethods)
            {
                // Search for a similar method
                searchPool.Reset();
                PredicateBuilder.AddPredicates(searchPool, method);

                var bestMatch = searchPool.BestMatch(.66);
                if (bestMatch == null)
                    continue;

                var compatibleMethod = bestMatch.Item;
                if (compatibleMethod != null)
                    continue;

                bool canHandleMethod = false;

                // If the search fails, we need to query for a replacement
                foreach (IMethodMissingCallback callback in _handlers)
                {
                    if (!callback.CanHandle(method))
                        continue;

                    canHandleMethod = true;
                    break;
                }

                if (canHandleMethod)
                    continue;

                result = false;
                break;
            }

            return result;
        }

        public T CreateDuck<T>(params Type[] baseInterfaces)
            where T : class
        {
            IInterceptor interceptor = new DuckType(this);
            var result = _factory.CreateProxy<T>(interceptor, baseInterfaces);
            return result;
        }

        public object CreateDuck(Type duckType, params Type[] baseInterfaces)
        {
            IInterceptor interceptor = new DuckType(this);
            object result = _factory.CreateProxy(duckType, interceptor, baseInterfaces);
            return result;
        }
    }
}