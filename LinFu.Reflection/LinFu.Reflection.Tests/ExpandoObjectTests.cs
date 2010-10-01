using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinFu.Delegates;
using NUnit.Framework;
using CLRDynamicObject = System.Dynamic.DynamicObject;
namespace LinFu.Reflection.Tests
{
    [TestFixture]
    public class ExpandoObjectTests
    {
        [Test]
        public void ShouldBeAbleToCallPropertySetterOnExpandoObject()
        {
            int expectedValue = 12345;
            CustomDelegate setterBody = args =>
                                            {
                                                var value = args[0];
                                                Assert.AreEqual(value, expectedValue);
                                                return null;
                                            };

            var target = new DynamicObject();
            target.AddMethod("set_TargetProperty", setterBody, typeof (void), typeof (int));
            
            dynamic expando = new ExpandoObject(target);
            expando.TargetProperty = expectedValue;
        }

        [Test]
        public void ShouldBeAbleToCallCovariantPropertySetterOnExpandoObject()
        {
            int expectedValue = 12345;
            CustomDelegate setterBody = args =>
            {
                var value = args[0];
                Assert.AreEqual(value, expectedValue);
                return null;
            };

            var target = new DynamicObject();
            target.AddMethod("set_TargetProperty", setterBody, typeof(void), typeof(object));

            dynamic expando = new ExpandoObject(target);
            expando.TargetProperty = expectedValue;
        }
    }
}
