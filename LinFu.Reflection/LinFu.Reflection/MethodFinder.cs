using System;
using System.Collections.Generic;
using System.Reflection;

using LinFu.Finders;
namespace LinFu.Reflection
{
    public class MethodFinder : IMethodFinder
    {
        private readonly List<MethodInfo> _cachedResults = new List<MethodInfo>();

        private readonly Dictionary<Type, IEnumerable<MethodInfo>> _methodCache =
            new Dictionary<Type, IEnumerable<MethodInfo>>();

        public MethodInfo Find(string methodName, Type targetType, object[] args)
        {
            var builder = new PredicateBuilder();
            builder.MethodName = methodName;
            builder.MatchParameters = true;
            builder.MatchCovariantParameterTypes = true;

            // Find the method that has a compatible signature
            // and a matching method name
            var arguments = new List<object>();
            if (args != null && args.Length > 0)
                arguments.AddRange(args);

            builder.RuntimeArguments.AddRange(arguments);
            builder.MatchRuntimeArguments = true;

            var cachedSearchList = _cachedResults.AsFuzzyList();
            builder.AddPredicates(cachedSearchList);

            // Search for any previous matches
            var tolerance = .66;
            var bestMatch = cachedSearchList.BestMatch(tolerance);

            MethodInfo result = null;
            if (bestMatch == null)
            {
                // If there isn't a match, search the current type
                // for an existing match
                var methods = GetMethods(targetType);
                var methodSearchPool = methods.AsFuzzyList();
                builder.AddPredicates(methodSearchPool);

                bestMatch = methodSearchPool.BestMatch();
            }

            if (bestMatch != null)
                result = bestMatch.Item;

            return result;
        }

        private IEnumerable<MethodInfo> GetMethods(Type targetType)
        {
            if (_methodCache.ContainsKey(targetType))
                return _methodCache[targetType];

            _methodCache[targetType] =
                targetType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            return _methodCache[targetType];
        }
    }
}