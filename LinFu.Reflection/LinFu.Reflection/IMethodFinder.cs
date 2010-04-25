using System;
using System.Reflection;

namespace LinFu.Reflection
{
    public interface IMethodFinder
    {
        MethodInfo Find(string methodName, Type targetType, object[] arguments);
    }
}