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
       
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {            
            var methodName = binder.Name;

            result = _target.Methods[methodName](args);

            return true;
        }
    }
}
