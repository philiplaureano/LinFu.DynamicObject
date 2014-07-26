using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace LinFu.Reflection.Tests
{
    [TestFixture]
    public abstract class BaseFixture
    {
        [SetUp]
        public void Init()
        {
            OnInit();
        }
        [TearDown]
        public void Term()
        {
            OnTerm();
        }

        protected virtual void OnInit() { }
        protected virtual void OnTerm() { }
    }
}
