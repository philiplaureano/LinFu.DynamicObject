using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using CLRDynamicObject = System.Dynamic.DynamicObject;

namespace LinFu.Reflection
{
    public class ExpandoObject : CLRDynamicObject
    {
        private readonly DynamicObject _target;

        public ExpandoObject(DynamicObject target)
        {
            _target = target;
        }

        public void AddMethod<T>(string methodName, Action<T> methodBody)
        {
            MulticastDelegate targetDelegate = methodBody;
            AddMethod(methodName, targetDelegate);
        }

        public void AddMethod(string methodName, Action methodBody)
        {
            MulticastDelegate targetDelegate = methodBody;
            AddMethod(methodName, targetDelegate);
        }

        public void AddMethod(string methodName, MulticastDelegate methodBody)
        {
            _target.AddMethod(methodName, methodBody);
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (!_target.LooksLike(binder.ReturnType))
                return base.TryConvert(binder, out result);

            result = _target.CreateDuck(binder.ReturnType);
            return result != null;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var propertyName = binder.Name;
            var methodName = string.Format("set_{0}", propertyName);
            try
            {
                _target.Methods[methodName](new []{value});
            }
            catch (MethodNotFoundException ex)
            {
                var isDelegate = value is MulticastDelegate;
                if (!isDelegate)
                    throw;

                var body = (MulticastDelegate)value;
                _target.AddMethod(propertyName, body);
            }

            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var methodName = binder.Name;

            result = _target.Methods[methodName](args);

            return true;
        }
    }
}
