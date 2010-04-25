using System.Reflection;

namespace LinFu.Reflection
{
    public interface IMethodMissingCallback
    {
        void MethodMissing(object source, MethodMissingParameters missingParameters);

        bool CanHandle(MethodInfo method);
    }
}