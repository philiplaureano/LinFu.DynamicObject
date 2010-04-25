using LinFu.Delegates;

namespace LinFu.Reflection
{
    public interface IObjectMethods
    {
        CustomDelegate this[string methodName] { get; }
    }
}