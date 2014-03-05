﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace LinFu.Delegates
{
    internal class CustomDelegateInvoker : IInvoker
    {
        private CustomDelegate _targetDelegate;
        private object[] _suppliedArguments;
        public CustomDelegateInvoker(CustomDelegate targetDelegate, object[] suppliedArguments)
        {
            _targetDelegate = targetDelegate;
            _suppliedArguments = suppliedArguments;
        }
        #region IInvoker Members

        public object Invoke(object target, MethodBase targetMethod, 
            IEnumerable<object> curriedArguments, 
            IEnumerable<object> invokeArguments)
        {
            IList<object> curriedList = new List<object>(curriedArguments);
            IList<object> invokeArgList = new List<object>(invokeArguments);

            var largerList = curriedList.Count > invokeArgList.Count ?
                curriedList : invokeArgList;

            IList<object> combinedArguments = new List<object>();
            foreach (var arg in largerList)
            {
                combinedArguments.Add(arg);
            }

            AssignArguments(curriedArguments, combinedArguments);
            AssignArguments(invokeArguments, combinedArguments);

            var args = new object[combinedArguments.Count];
            combinedArguments.CopyTo(args, 0);

            return _targetDelegate(args);
        }

        #endregion
        private static void AssignArguments(IEnumerable<object> sourceList, IList<object> targetList)
        {
            var source = new Queue<object>(sourceList);
            for (var i = 0; i < targetList.Count; i++)
            {
                if (source.Count == 0)
                    break;

                if (targetList[i] != Args.Lambda)
                    continue;

                var argument = source.Dequeue();

                if (argument is IDeferredArgument)
                {
                    var deferred = (IDeferredArgument)argument;
                    argument = deferred.Evaluate();
                }

                targetList[i] = argument;
            }
        }

    }
}
