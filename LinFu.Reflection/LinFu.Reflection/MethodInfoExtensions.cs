using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using LinFu.Finders;

namespace LinFu.Reflection
{
    public static class MethodInfoExtensions
    {
        public static bool HasCompatibleSignature(this MethodInfo method, MethodInfo targetMethod)
        {
            var items = new[] { targetMethod };
            var searchPool = items.AsFuzzyList();
            PredicateBuilder.AddPredicates(searchPool, method);
            var match = searchPool.BestMatch(.66);

            return match != null;
        }
    }
}
