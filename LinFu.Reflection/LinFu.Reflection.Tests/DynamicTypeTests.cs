using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinFu.Reflection.Extensions;
using NUnit.Framework;

namespace LinFu.Reflection.Tests
{
    [TestFixture]
    public class DynamicTypeTests
    {
        [Test]
        public void ShouldBeAbleToShareTheSameDynamicType()
        {
            var typeSpec = new TypeSpec { Name = "Person" };

            // Add an age property 
            typeSpec.AddProperty("Age", typeof(int));

            // Attach the DynamicType named 'Person' to a bunch of dynamic objects
            var personType = new DynamicType(typeSpec);
            var first = new DynamicObject();
            var second = new DynamicObject();

            first += personType;
            second += personType;

            // Use both objects as persons
            var firstPerson = first.CreateDuck<IPerson>();
            var secondPerson = second.CreateDuck<IPerson>();

            firstPerson.Age = 18;
            secondPerson.Age = 21;

            Assert.AreEqual(18, firstPerson.Age);
            Assert.AreEqual(21, secondPerson.Age);

            // Change the type so that it supports the INameable interface
            typeSpec.AddProperty("Name", typeof(string));
            var firstNameable = first.CreateDuck<INameable>();
            var secondNameable = second.CreateDuck<INameable>();

            firstNameable.Name = "Foo";
            secondNameable.Name = "Bar";

            Assert.AreEqual("Foo", firstNameable.Name);
            Assert.AreEqual("Bar", secondNameable.Name);
        }
    }
}
