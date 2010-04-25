using System;
using System.Collections.Generic;
using System.Reflection;
using LinFu.Common;

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

        #region IMethodMissingCallback Members

        public void MethodMissing(object source,
                                  MethodMissingParameters missingParameters)
        {
            var builder = new PredicateBuilder();

            // The current method name must match the given method name
            if (_methodName != missingParameters.MethodName)
                return;

            if (missingParameters.Arguments != null)
                builder.RuntimeArguments.AddRange(missingParameters.Arguments);

            builder.MatchRuntimeArguments = true;

            Predicate<MethodInfo> finderPredicate = builder.CreatePredicate();
            var finder = new FuzzyFinder<MethodInfo>();
            finder.Tolerance = .66;

            // Match the criteria against the target delegate
            var searchList = new List<MethodInfo>(new[] {_target.Method});

            // Determine if the signature is compatible
            MethodInfo match = finder.Find(finderPredicate, searchList);
            if (match == null)
                return;

            // If the signature is compatible, then execute the method
            MethodInfo targetMethod = _target.Method;

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
            Predicate<MethodInfo> predicate = PredicateBuilder.CreatePredicate(method);
            var finder = new FuzzyFinder<MethodInfo>();
            finder.Tolerance = .66;

            var searchPool = new[] {_target.Method};
            MethodInfo match = finder.Find(predicate, searchPool);

            return match != null;
        }

        #endregion
    }
}