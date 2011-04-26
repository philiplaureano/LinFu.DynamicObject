using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinFu.Reflection
{
    public class MethodNotFoundException : Exception
    {
        public MethodNotFoundException(string message) : base(message)
        {
        }
    }
}
