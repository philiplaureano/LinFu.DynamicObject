using System;
using System.Collections.Generic;
using System.Reflection;
using LinFu.Finders;
using LinFu.Finders.Interfaces;

namespace LinFu.Reflection
{
    internal class ExtensionMethodsFrom : IMethodMissingCallback
    {
        private readonly IEnumerable<MethodInfo> _extensionMethods;

        public ExtensionMethodsFrom(Type extensionClassType)
        {
            _extensionMethods = extensionClassType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        }

        public void MethodMissing(object source, MethodMissingParameters missingParameters)
        {
            var dynamicObject = source as DynamicObject;
            if (dynamicObject == null)
                return;

            var arguments = (missingParameters.Arguments ?? new object[0]);
            var target = dynamicObject.Target;

            var candidateMethods = _extensionMethods.AsFuzzyList();

            // Match the method name
            candidateMethods.AddCriteria(m => m.Name == missingParameters.MethodName, CriteriaType.Critical);

            // Match the number of method parameters (including the 'this' parameter)
            var expectedArgumentLength = arguments.Length + 1;
            candidateMethods.AddCriteria(m => m.GetParameters().Length == expectedArgumentLength, CriteriaType.Critical);

            Func<MethodBase, int, Type, bool> matchesParameterType = (method, index, type) =>
            {
                var parameters = method.GetParameters();
                var parameter = index < parameters.Length ? parameters[index] : null;
                return parameter != null && type.IsAssignableFrom(parameter.ParameterType);
            };

            var targetType = target.GetType();

            // Generic extension methods are not supported
            candidateMethods.AddCriteria(m => !m.IsGenericMethodDefinition, CriteriaType.Critical);

            // Match the first parameter type (the 'this' argument)
            candidateMethods.AddCriteria(m => matchesParameterType(m, 0, targetType), CriteriaType.Critical);

            // Match the remaining arguments
            var parameterIndex = 1;
            foreach (var arg in arguments)
            {
                if (arg == null)
                    continue;

                var argumentType = arg.GetType();
                var index = parameterIndex;
                candidateMethods.AddCriteria(m => matchesParameterType(m, index, argumentType), CriteriaType.Critical);
                parameterIndex++;
            }

            var bestMatch = candidateMethods.BestMatch();
            if (bestMatch == null)
                return;

            var matchingMethod = bestMatch.Item;

            // Combine the arguments with the 'this' target
            var extensionMethodArguments = new List<object> { target };
            extensionMethodArguments.AddRange(arguments);

            var returnValue = matchingMethod.Invoke(null, extensionMethodArguments.ToArray());
            missingParameters.ReturnValue = returnValue;
            missingParameters.Handled = true;
        }

        public bool CanHandle(MethodInfo method)
        {
            return false;
        }
    }
}