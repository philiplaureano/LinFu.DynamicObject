using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinFu.Reflection
{
    public static class FunctorExtensions
    {
        public static Func<T, bool> And<T>(this Func<T, bool> left, Func<T, bool> right)
        {
            return item => left(item) && right(item);
        }

        public static Func<T, bool> Or<T>(this Func<T, bool> left, Func<T, bool> right)
        {
            return item => left(item) || right(item);
        }

        public static Func<T, bool> Negate<T>(this Func<T, bool> func)
        {
            return item => !func(item);
        }
    }
}
