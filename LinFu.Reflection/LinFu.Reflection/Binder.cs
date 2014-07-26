using System;
using System.Reflection;
using LinFu.Delegates;

namespace LinFu.Reflection
{
    internal class Binder : IObjectMethods, IObjectProperties
    {
        private readonly DynamicObject _dynamicObject;
        private readonly IMethodFinder _finder;
        private readonly object _target;

        public Binder(object target, IMethodFinder finder, DynamicObject dynamicObject)
        {
            _target = target;
            _finder = finder;
            _dynamicObject = dynamicObject;
        }

        public CustomDelegate this[string methodName]
        {
            get
            {
                CustomDelegate result = args => Bind(methodName, args);
                return result;
            }
        }

        private object Bind(string methodName, object[] args)
        {
            if (_target == null)
                throw new NullReferenceException("No target instance found!");

            var bestMatch =
                _finder.Find(methodName, _target.GetType(), args);

            object returnValue = null;

            if (bestMatch == null)
            {
                var handled = false;
                returnValue = _dynamicObject.ExecuteMethodMissing(methodName, args,
                                                                  ref handled);
                if (handled)
                    return returnValue;

                throw new MethodNotFoundException(string.Format("Method '{0}' not found", methodName));
            }

            try
            {
                returnValue = bestMatch.Invoke(_target, args);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }

            return returnValue;
        }

        object IObjectProperties.this[string propertyName]
        {
            get
            {
                var methodName = string.Format("get_{0}", propertyName);
                IObjectMethods methods = this;
                return methods[methodName](new object[0]);
            }
            set
            {
                var methodName = string.Format("set_{0}", propertyName);
                IObjectMethods methods = this;
                methods[methodName](new[] { value });
            }
        }
    }
}