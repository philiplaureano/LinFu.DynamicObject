using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinFu.Reflection.Tests
{
    public static class SampleExtensionMethods
    {
        private static int _counter = 0;
        public static void ResetCallCounter()
        {
            _counter = 0;
        }

        public static int IncrementCounter(this RubberDucky target)
        {
            return ++_counter;
        }
    }
}
