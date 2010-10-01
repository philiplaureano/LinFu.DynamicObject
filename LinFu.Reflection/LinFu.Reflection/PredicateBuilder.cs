using System;
using System.Collections.Generic;
using System.Reflection;
using LinFu.Finders;
using LinFu.Finders.Interfaces;

namespace LinFu.Reflection
{
    public class PredicateBuilder
    {
        private readonly List<Type> _argumentTypes = new List<Type>();
        private readonly List<object> _arguments = new List<object>();
        private readonly List<ParameterInfo> _parameterTypes = new List<ParameterInfo>();
        private readonly List<Type> _typeArguments = new List<Type>();
        private bool? _isProtected;
        private bool? _isPublic;
        private bool _matchCovariantParameterTypes = true;
        private bool _matchCovariantReturnType;
        private bool _matchParameters = true;
        private bool _matchRuntimeArguments;
        private string _methodName;
        private Type _returnType;

        public List<Type> ArgumentTypes
        {
            get { return _argumentTypes; }
        }

        public List<object> RuntimeArguments
        {
            get { return _arguments; }
        }

        public string MethodName
        {
            get { return _methodName; }
            set { _methodName = value; }
        }

        public Type ReturnType
        {
            get { return _returnType; }
            set { _returnType = value; }
        }

        public List<ParameterInfo> ParameterTypes
        {
            get { return _parameterTypes; }
        }

        public List<Type> TypeArguments
        {
            get { return _typeArguments; }
        }

        public bool? IsPublic
        {
            get { return _isPublic; }
            set { _isPublic = value; }
        }

        public bool? IsProtected
        {
            get { return _isProtected; }
            set { _isProtected = value; }
        }

        public bool MatchCovariantReturnType
        {
            get { return _matchCovariantReturnType; }
            set { _matchCovariantReturnType = value; }
        }

        public bool MatchParameters
        {
            get { return _matchParameters; }
            set { _matchParameters = value; }
        }

        public bool MatchParameterTypes { get; set; }

        public bool MatchCovariantParameterTypes
        {
            get { return _matchCovariantParameterTypes; }
            set { _matchCovariantParameterTypes = value; }
        }

        public bool MatchRuntimeArguments
        {
            get { return _matchRuntimeArguments; }
            set { _matchRuntimeArguments = value; }
        }

        public static void AddPredicates(IList<IFuzzyItem<MethodInfo>> list, MethodInfo method)
        {
            var builder = new PredicateBuilder { MatchParameters = true, MethodName = method.Name, MatchRuntimeArguments = true };

            //if (args != null && args.Length > 0)
            //    builder.RuntimeArguments.Add(args);

            foreach (var param in method.GetParameters())
            {
                builder.ParameterTypes.Add(param);
            }

            // Match any type arguments
            if (method.IsGenericMethod)
            {
                foreach (var current in method.GetGenericArguments())
                {
                    builder.TypeArguments.Add(current);
                }
            }

            builder.ReturnType = method.ReturnType;
            builder.AddPredicates(list);
        }

        public void AddPredicates(IList<IFuzzyItem<MethodInfo>> methods)
        {
            ShouldMatchMethodName(methods);
            ShouldMatchParameterTypes(methods);
            ShouldMatchReturnType(methods);
            ShouldMatchParameters(methods);
            ShouldMatchGenericTypeParameters(methods);
            ShouldMatchPublicMethods(methods);
            ShouldMatchProtectedMethods(methods);
            ShouldMatchRuntimeArguments(methods);
            ShouldMatchParameterTypes(methods);
        }

        private void ShouldMatchParameterTypes(IList<IFuzzyItem<MethodInfo>> methods)
        {
            if (!MatchParameterTypes || _argumentTypes.Count <= 0)
                return;

            int position = 0;
            foreach (Type currentType in _argumentTypes)
            {
                var predicate = MakeParameterPredicate(position, currentType, false);
                methods.AddCriteria(predicate);
                position++;
            }
        }

        private void ShouldMatchRuntimeArguments(IList<IFuzzyItem<MethodInfo>> methods)
        {
            if (!MatchRuntimeArguments)
                return;

            if (_arguments.Count > 0 && MatchRuntimeArguments)
            {
                int position = 0;

                // Match the individual parameter types
                var typeMap = new Dictionary<int, Type>();
                foreach (object argument in _arguments)
                {
                    if (argument != null)
                    {
                        var argumentType = argument.GetType();
                        var matchCovariantParameterTypes = _matchCovariantParameterTypes;
                        AddParameterPredicate(position, argumentType, matchCovariantParameterTypes, methods);
                        typeMap[position] = argumentType;
                    }
                    position++;
                }

                // Match the exact method signature if possible
                var compositePredicates = new List<Func<MethodInfo, bool>>();
                foreach (var currentPosition in typeMap.Keys)
                {
                    var currentType = typeMap[currentPosition];
                    var predicate = MakeParameterPredicate(currentPosition, currentType, true);
                    compositePredicates.Add(predicate);
                }

                Func<MethodInfo, bool> hasExactMethodSignature =
                    method =>
                    {
                        if (compositePredicates.Count == 0)
                            return true;

                        var result = false;
                        foreach (var predicate in compositePredicates)
                        {
                            result = predicate(method);
                            if (!result)
                                break;
                        }

                        return result;
                    };

                methods.AddCriteria(hasExactMethodSignature, CriteriaType.Standard);
            }

            Func<MethodInfo, bool> shouldMatchParameterCount = currentMethod =>
                                                                  {
                                                                      var currentParameters =
                                                                          currentMethod.GetParameters();

                                                                      // Match the parameter count
                                                                      var parameterCount = currentParameters.Length;
                                                                      return parameterCount == _arguments.Count;
                                                                  };

            methods.AddCriteria(shouldMatchParameterCount);

        }

        private void AddParameterPredicate(int position, Type argumentType, bool matchCovariantParameterTypes, IList<IFuzzyItem<MethodInfo>> methods)
        {
            // Add a separate criteria for checking for covariant parameter types
            var hasMatchingParameterType = MakeParameterPredicate(position, argumentType, false);
            var hasCovariantParameterType = MakeParameterPredicate(position, argumentType, true);


            //methods.AddCriteria(hasMatchingParameterType, criteriaType);
            //methods.AddCriteria(hasCovariantParameterType, CriteriaType.Critical);
            if (!matchCovariantParameterTypes)
            {
                Func<MethodInfo, bool> hasExactParameterType =
                    method => hasCovariantParameterType(method) && hasMatchingParameterType(method);

                methods.AddCriteria(hasExactParameterType);
                return;
            }

            methods.AddCriteria(hasMatchingParameterType, CriteriaType.Standard);
            methods.AddCriteria(hasCovariantParameterType, CriteriaType.Critical);

        }

        private static Func<MethodInfo, bool> MakeParameterPredicate(int position, Type parameterType, bool covariant)
        {
            Func<MethodInfo, bool> result = method => MatchesParameterType(method, parameterType, position,
                                                                           covariant);

            return result;
        }

        private static bool MatchesParameterType(MethodInfo method, Type parameterType, int position, bool covariant)
        {
            var parameters = method.GetParameters();

            var checkResult = false;
            try
            {
                var currentParameterType = parameters[position].ParameterType;
                checkResult = covariant
                                  ? currentParameterType.IsAssignableFrom(
                                      parameterType)
                                  : currentParameterType ==
                                    parameterType;
            }
            catch
            {
                // Ignore any errors that occur
            }

            return checkResult;
        }

        private void ShouldMatchProtectedMethods(IList<IFuzzyItem<MethodInfo>> methods)
        {
            if (_isProtected == null)
                return;

            Func<MethodInfo, bool> isProtectedMethod = method => method.IsFamily == _isProtected;
            methods.AddCriteria(isProtectedMethod);
        }

        private void ShouldMatchPublicMethods(IList<IFuzzyItem<MethodInfo>> methods)
        {
            if (_isPublic == null)
                return;
            Func<MethodInfo, bool> isPublic = method => method.IsPublic == _isPublic;
            methods.AddCriteria(isPublic);
        }

        private void ShouldMatchGenericTypeParameters(IList<IFuzzyItem<MethodInfo>> methods)
        {
            if (_typeArguments.Count > 0)
            {
                var position = 0;
                foreach (Type currentType in _typeArguments)
                {
                    var parameterType = currentType;
                    var currentPosition = position++;
                    Func<MethodInfo, bool> matchesTypeParameter = method => MatchesGenericParameterType(currentPosition, method, parameterType,
                        (leftType, rightType) => leftType == rightType);

                    methods.AddCriteria(matchesTypeParameter);
                }
            }

            if (_typeArguments.Count != 0)
                return;

            Func<MethodInfo, bool> hasNoTypeArguments = delegate(MethodInfo currentMethod)
                                                            {
                                                                var currentParameterTypes = currentMethod.GetGenericArguments();

                                                                // Match the Type parameter
                                                                var parameterCount = currentParameterTypes.Length;
                                                                return parameterCount == 0;
                                                            };

            methods.AddCriteria(hasNoTypeArguments);
        }

        private bool MatchesGenericParameterType(int currentPosition, MethodInfo method, Type parameterType, Func<Type, Type, bool> areParameterTypesEqual)
        {
            if (!method.IsGenericMethod)
                return false;

            var typeArgs = method.GetGenericArguments();
            var isMatch = false;

            try
            {
                Type currentPropertyType = typeArgs[currentPosition];
                isMatch = areParameterTypesEqual(currentPropertyType, parameterType);
            }
            catch
            {
                // Ignore the error 
            }

            return isMatch;
        }

        private void ShouldMatchParameters(IList<IFuzzyItem<MethodInfo>> methods)
        {
            if (_matchParameters && _parameterTypes.Count > 0)
            {
                ParameterInfo[] currentParameters = _parameterTypes.ToArray();

                // Match the parameter count
                int parameterCount = currentParameters.Length;

                Func<MethodInfo, bool> hasParameterCount = method =>
                                                                {
                                                                    var parameters = method.GetParameters();
                                                                    var count = parameters.Length;

                                                                    return parameterCount == count;
                                                                };

                var covariant = _matchCovariantParameterTypes;

                // Match the parameter types
                ShouldMatchParameterTypes(currentParameters, covariant, methods);
                methods.AddCriteria(hasParameterCount);
            }


            // Check for zero parameters
            if (_matchParameters && _parameterTypes.Count == 0 && !_matchRuntimeArguments)
            {
                Func<MethodInfo, bool> hasZeroParameters = delegate(MethodInfo currentMethod)
                              {
                                  var currentParameters = currentMethod.GetParameters();

                                  // Match the parameter count
                                  var parameterCount = currentParameters.Length;
                                  return parameterCount == 0;
                              };

                methods.AddCriteria(hasZeroParameters);
            }
        }

        private void ShouldMatchParameterTypes(IEnumerable<ParameterInfo> currentParameters, bool covariant, IList<IFuzzyItem<MethodInfo>> methods)
        {
            foreach (ParameterInfo param in currentParameters)
            {
                int position = param.Position;
                Type parameterType = param.ParameterType;
                Func<MethodInfo, bool> hasParameter = MakeParameterPredicate(position, parameterType,
                                                                            covariant);
                methods.AddCriteria(hasParameter);
            }
        }

        private void ShouldMatchReturnType(IList<IFuzzyItem<MethodInfo>> methods)
        {
            if (_returnType == null)
                return;

            Func<MethodInfo, bool> hasSpecificReturnType = method => method.ReturnType == _returnType;
            methods.AddCriteria(hasSpecificReturnType);

            if (!_matchCovariantReturnType)
                return;

            Func<MethodInfo, bool> hasCovariantReturnType = method => method.ReturnType.IsAssignableFrom(_returnType);
            methods.AddCriteria(hasCovariantReturnType);
        }

        private void ShouldMatchMethodName(IList<IFuzzyItem<MethodInfo>> methods)
        {
            if (string.IsNullOrEmpty(_methodName))
                return;

            Func<MethodInfo, bool> shouldMatchMethodName =
                method => method.Name == _methodName;

            // Results that match the method name will get a higher
            // score
            methods.AddCriteria(shouldMatchMethodName, CriteriaType.Standard, 2);
        }


        public void SetParameterTypes(ParameterInfo[] parameterInfo)
        {
            if (parameterInfo != null && parameterInfo.Length > 0)
                _parameterTypes.AddRange(parameterInfo);
        }
    }
}