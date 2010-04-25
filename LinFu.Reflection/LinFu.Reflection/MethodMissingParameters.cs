namespace LinFu.Reflection
{
    public class MethodMissingParameters
    {
        private readonly object[] _args;
        private readonly string _name;
        private readonly object _target;

        public MethodMissingParameters(string name, object target, object[] args)
        {
            _name = name;
            _target = target;
            _args = args;
        }

        public string MethodName
        {
            get { return _name; }
        }

        public object Target
        {
            get { return _target; }
        }

        public object[] Arguments
        {
            get { return _args; }
        }

        public bool Handled { get; set; }

        public object ReturnValue { get; set; }
    }
}