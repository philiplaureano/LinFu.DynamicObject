using LinFu.DynamicProxy;

namespace LinFu.Reflection
{
    internal class DuckType : Interceptor
    {
        private readonly DynamicObject _target;

        public DuckType(DynamicObject target)
        {
            _target = target;
        }

        public override object Intercept(InvocationInfo info)
        {
            var methodName = info.TargetMethod.Name;
            return _target.Methods[methodName](info.Arguments);
        }
    }
}