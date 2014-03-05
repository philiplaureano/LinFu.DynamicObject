using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using LinFu.Delegates;
using NMock2;
using NUnit.Framework;
using Is = NMock2.Is;

namespace LinFu.Reflection.Tests
{    
    [TestFixture]
    public class DynamicObjectTests : BaseFixture
    {
        [Test]
        public void ShouldBeAbleToUseCLRDynamicObjectsAsExpandoObjects()
        {
            CustomDelegate body = args => 42;

            var dynamicObject = new LinFu.Reflection.DynamicObject(new object());
            dynamicObject.AddMethod("GetFoo", body, typeof(int));

            dynamic expando = dynamicObject.AsExpandoObject();

            int result = expando.GetFoo();
            Assert.AreEqual(42, result);
        }

        [Test]
        public void ShouldCallTargetMethod()
        {
            var methodName = "TargetMethod";
            var mockTarget = new MockClass();

            var dynamic = new DynamicObject(mockTarget);
            dynamic.Methods[methodName]();
            Assert.AreEqual(1, mockTarget.CallCount, "The target method was not called!");
        }

        [Test]
        public void ShouldCallTargetProperty()
        {
            var propertyName = "TargetProperty";
            var mockTarget = new MockClass();

            var dynamic = new DynamicObject(mockTarget);

            // Call the getter and the setter
            dynamic.Properties[propertyName] = 0;
            var value = dynamic.Properties[propertyName];

            Assert.AreEqual(2, mockTarget.CallCount, "The target property was not called!");
        }

        [Test]
        public void ShouldBeAbleToAddNewMethod()
        {
            IntegerOperation addBody = delegate(int a, int b) { return a + b; };
            var dynamic = new DynamicObject(new object());
            dynamic.AddMethod("Add", addBody);

            var result = (int)dynamic.Methods["Add"](1, 1);
            Assert.AreEqual(2, result);
        }

        [Test]
        public void ShouldBeAbleToMixAnotherClassInstance()
        {
            var test = new MockClass();
            var dynamic = new DynamicObject(new object());

            var methodName = "TargetMethod";
            dynamic.MixWith(test);
            dynamic.Methods[methodName]();
            Assert.AreEqual(1, test.CallCount);
        }
        [Test]
        public void ShouldAssignSelfToMixinAwareInstance()
        {
            var test = mock.NewMock<IMixinAware>();
            var dynamic = new DynamicObject(new object());
            Expect.Once.On(test).SetProperty("Self").To(dynamic);

            dynamic.MixWith(test);
        }

        [Test]
        public void ShouldAllowDuckTyping()
        {
            var test = new MockClass();
            var dynamic = new DynamicObject(new object());

            var duck = dynamic.CreateDuck<ITest>();

            // Combine the MockClass implementation with the current
            // object instance
            dynamic.MixWith(test);
            duck.TargetMethod();
            duck.TargetMethod<int>();
            Assert.AreEqual(2, test.CallCount);
        }

        [Test]
        public void ShouldBeAbleToCombineMultipleDynamicObjects()
        {
            var firstInstance = new FirstClass();
            var secondInstance = new SecondClass();
            var first = new DynamicObject(firstInstance);
            var second = new DynamicObject(secondInstance);

            var combined = first + second;
            combined.Methods["TestMethod1"]();
            combined.Methods["TestMethod2"]();

            Assert.AreEqual(1, firstInstance.CallCount);
            Assert.AreEqual(1, secondInstance.CallCount);
        }
        [Test]
        public void ShouldBeAbleToTellWhetherOrNotSomethingLooksLikeADuck()
        {
            var dynamic = new DynamicObject(new RubberDucky());
            Assert.IsTrue(dynamic.LooksLike(typeof(IDuck)));
            Assert.IsTrue(dynamic.LooksLike<IDuck>());
        }

        [Test]
        public void ShouldBeAbleToAddMethodsUsingRuntimeAnonymousDelegates()
        {
            CustomDelegate addBody = delegate(object[] args)
                                         {
                                             var a = (int)args[0];
                                             var b = (int)args[1];
                                             return a + b;
                                         };

            var dynamic = new DynamicObject(new object());
            var returnType = typeof(int);
            Type[] parameterTypes = { typeof(int), typeof(int) };
            dynamic.AddMethod("Add", addBody, returnType, parameterTypes);

            var result = (int)dynamic.Methods["Add"](1, 1);
            Assert.AreEqual(2, result);
        }

        [Test]
        public void CanCreateADynamicAdder()
        {
            CustomDelegate addBody = delegate(object[] args)
            {
                var a = (int)args[0];
                var b = (int)args[1];
                return a + b;
            };

            // Map LinFu's DynamicObject to an ICanAdd interface
            var linfuDynamicObject = new DynamicObject(new object());
            var returnType = typeof(int);
            var parameterTypes = new[] { typeof(int), typeof(int) };
            linfuDynamicObject.AddMethod("Add", addBody, returnType, parameterTypes);

            // If it looks like a duck...
            Assert.IsTrue(linfuDynamicObject.LooksLike<ICanAdd>());

            // ...then it must be a duck, right?
            var somethingThatCanAdd = new SomethingThatAdds(linfuDynamicObject.CreateDuck<ICanAdd>());
            somethingThatCanAdd.FirstNumber = 10;
            somethingThatCanAdd.SecondNumber = 20;
            Assert.AreEqual(somethingThatCanAdd.AddNumbers(), 30);
        }       
    }
}
