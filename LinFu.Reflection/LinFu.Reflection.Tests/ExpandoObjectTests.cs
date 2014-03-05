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
            var expectedValue = 12345;
            CustomDelegate setterBody = args =>
                                            {
                                                var value = args[0];
                                                Assert.AreEqual(value, expectedValue);
                                                return null;
                                            };

            var target = new DynamicObject();
            target.AddMethod("set_TargetProperty", setterBody, typeof(void), typeof(int));

            dynamic expando = new ExpandoObject(target);
            expando.TargetProperty = expectedValue;
        }

        [Test]
        public void ShouldBeAbleToCallCovariantPropertySetterOnExpandoObject()
        {
            var expectedValue = 12345;
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

        [Test]
        public void ShouldBeAbleToAddNewMethodByAssigningADelegateToAPropertySetter()
        {
            var target = new DynamicObject();
            dynamic expando = new ExpandoObject(target);

            Func<int, int, int> addition = (a, b) => a + b;
            expando.Add = addition;

            var result = expando.Add(2, 2);
            Assert.AreEqual(4, result);
        }

        [Test]
        public void ShouldOnlyExecuteReplacementMethodAfterReplacingExpandoMethod()
        {
            var target = new DynamicObject();
            dynamic expando = new ExpandoObject(target);

            Func<int, int, int> addition = (a, b) => a + b;
            Func<int, int, int> subtraction = (a, b) => a - b;

            expando.Operation = addition;
            expando.Operation = subtraction;
            int result = expando.Operation(2, 2);
            Assert.AreEqual(0, result);            
        }

        [Test]
        public void ShouldBeAbleToImplicitlyCastAnExpandoObjectToADuckTypeIfExpandoLooksLikeDuckType()
        {
            var mock = new MockClass();
            var target = new DynamicObject(mock);
            dynamic expando = new ExpandoObject(target);

            ITest test = expando;
            test.TargetMethod();

            Assert.AreEqual(1, mock.CallCount);
        }
    }
}
