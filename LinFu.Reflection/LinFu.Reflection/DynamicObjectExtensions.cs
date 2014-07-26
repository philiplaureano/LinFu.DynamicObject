using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using CLRDynamicObject = System.Dynamic.DynamicObject;
namespace LinFu.Reflection
{
    public static class DynamicObjectExtensions
    {
        public static CLRDynamicObject AsExpandoObject(this DynamicObject target)
        {
            return new ExpandoObject(target);
        }
        
        public static DynamicObject AddExtensionClass(this DynamicObject dynamicObject, Type extensionClassType)
        {
            dynamicObject += new ExtensionMethodsFrom(extensionClassType);
            return dynamicObject;
        }
    }
}
