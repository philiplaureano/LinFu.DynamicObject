using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinFu.Reflection.Tests
{
    public class SomethingThatAdds
    {
        private ICanAdd _adder;
        public SomethingThatAdds(ICanAdd adder)
        {
            _adder = adder;
        }

        public int FirstNumber { get; set; }
        public int SecondNumber { get; set; }
        public int AddNumbers()
        {
            return _adder.Add(FirstNumber, SecondNumber);
        }
    }
}
