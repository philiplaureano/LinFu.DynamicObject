using System;
using System.Collections.Generic;
using System.Reflection;
using LinFu.Finders;

namespace LinFu.Reflection
{
    internal class DelegateMixin : IMethodMissingCallback
    {
        private readonly string _methodName;
        private readonly MulticastDelegate _target;

        public DelegateMixin(string methodname, MulticastDelegate targetDelegate)
        {
            _methodName = methodname;
            _target = targetDelegate;
        }

        public void MethodMissing(object source,
                                  MethodMissingParameters missingParameters)
        {
            // The current method name must match the given method name
            if (_methodName != missingParameters.MethodName)
                return;

            var builder = new PredicateBuilder();
            if (missingParameters.Arguments != null)
                builder.RuntimeArguments.AddRange(missingParameters.Arguments);

            builder.MatchRuntimeArguments = true;

            // Match the criteria against the target delegate
            var items = new List<MethodInfo>(new[] { _target.Method });
            var searchItems = items.AsFuzzyList();

            // Determine if the signature is compatible
            builder.AddPredicates(searchItems);
            var match = searchItems.BestMatch();
            if (match == null)
                return;

            // If the signature is compatible, then execute the method
            var targetMethod = _target.Method;

            object result = null;
            try
            {
                result = targetMethod.Invoke(_target.Target, missingParameters.Arguments);
                missingParameters.Handled = true;
            }
            catch (TargetInvocationException ex)
            {
                missingParameters.Handled = false;
                throw ex.InnerException;
            }


            missingParameters.ReturnValue = result;
        }

        public bool CanHandle(MethodInfo method)
        {
            var targetMethod = _target.Method;
            return targetMethod.HasCompatibleSignature(targetMethod);
        }
    }
}