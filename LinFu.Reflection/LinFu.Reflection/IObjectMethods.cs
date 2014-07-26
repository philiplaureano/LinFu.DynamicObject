using System;
using LinFu.Delegates;

namespace LinFu.Reflection
{
    public delegate object CustomDelegate(params object[] args);
    public interface IObjectMethods
    {
        CustomDelegate this[string methodName] { get; }
    }
}