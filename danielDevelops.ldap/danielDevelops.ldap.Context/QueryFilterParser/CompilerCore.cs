using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using danielDevelops.ldap.QueryFilterParser.Models;

namespace danielDevelops.ldap.QueryFilterParser
{
    public class CompilerCore
    {
        private Strings Strings { get; }
        private BooleanAlgebra BooleanAlgebra { get; }

        public CompilerCore(
            Strings strings = null,
            BooleanAlgebra booleanAlgebra = null
        )
        {
            Strings = strings ?? new Strings(this);
            BooleanAlgebra = booleanAlgebra ?? new BooleanAlgebra(this);
        }
        public string ExpressionToString(Expression expr, IReadOnlyCollection<ParameterExpression> p)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.MemberAccess:
                    return _MemberToString(expr as MemberExpression, p);
                case ExpressionType.Constant:
                    return _ConstExprToString(expr, p);
                case ExpressionType.AndAlso:
                    return BooleanAlgebra.AndOrExprToString(expr, p, "&");
                case ExpressionType.OrElse:
                    return BooleanAlgebra.AndOrExprToString(expr, p, "|");
                case ExpressionType.Not:
                    return BooleanAlgebra.NotExprToString(expr, p);
                case ExpressionType.Equal:
                    return _MaybeStringCompareToExpr(expr, p, "=")
                           ?? _ComparisonExprToString(expr, p, "=");
                case ExpressionType.GreaterThanOrEqual:
                    return _MaybeStringCompareToExpr(expr, p, ">=")
                           ?? _ComparisonExprToString(expr, p, ">=");
                case ExpressionType.LessThanOrEqual:
                    return _MaybeStringCompareToExpr(expr, p, "<=")
                           ?? _ComparisonExprToString(expr, p, "<=");
                case ExpressionType.Call:
                    return _CallExprToString(expr, p);

                // These are not implemented in RFC 1960, so translate via negation.
                case ExpressionType.GreaterThan:
                    return _Negate(_MaybeStringCompareToExpr(expr, p, "<=", ">")
                            ?? _ComparisonExprToString(expr, p, /* not */ "<="));
                case ExpressionType.LessThan:
                    return _Negate(_MaybeStringCompareToExpr(expr, p, ">=", "<")
                           ?? _ComparisonExprToString(expr, p, /* not */ ">="));
                case ExpressionType.NotEqual:
                    return _Negate(_MaybeStringCompareToExpr(expr, p, /* not */ "=")
                           ?? _ComparisonExprToString(expr, p, /* not */ "="));

                case ExpressionType.Conditional: /* ternary */
                    return _TernaryToBooleanAlgebra(expr, p);

                // Low-priority/do not implement:
                default:
                    throw new NotImplementedException(
                        $"Linq-to-LDAP not implemented for {expr.NodeType}. \n"
                        + "Use local variables to remove algebraic expressions and method calls, \n"
                        + "and reduce Linq expression complexity to boolean algebra and string ops.\n"
                        + "Please see the Linq2Ldap project site.");
            }
        }

        private string _TernaryToBooleanAlgebra(Expression expr, IReadOnlyCollection<ParameterExpression> p)
        {
            var tern = expr as ConditionalExpression;
            var test = ExpressionToString(tern.Test, p);
            var whenTrue = ExpressionToString(tern.IfTrue, p);
            var whenFalse = ExpressionToString(tern.IfFalse, p);
            return $"(|(&{test}{whenTrue})(&(!{test}){whenFalse}))";
        }

        internal string _CallExprToString(
            Expression expr, IReadOnlyCollection<ParameterExpression> p)
        {
            var e = expr as MethodCallExpression;
            var name = e.Method.Name;
            var type = e.Method.DeclaringType;
            var fullname = $"{type}.{name}";
            var validSubExprs = new List<ExpressionType>()
                { ExpressionType.Constant, ExpressionType.MemberAccess};

            switch (name)
            {
                case "Any":
                    return Strings.AnyExtensionOpToString(e, p);
                case "Contains":
                    return Strings.OpToString(e, p, validSubExprs, "({0}=*{1}*)");
                case "StartsWith":
                    return Strings.OpToString(e, p, validSubExprs, "({0}={1}*)");
                case "EndsWith":
                    return Strings.OpToString(e, p, validSubExprs, "({0}=*{1})");
                case "Has":
                    return $"({EvalExpr(e.Arguments.First(), p)}=*)";
                case "Matches":
                    return Strings.ExtensionOpToString(e, p, "=");
                case "Approx":
                    return Strings.ExtensionOpToString(e, p, "~=");
                case "get_Item":
                    return __PDictIndexesToString(e, p);
                default:
                    throw new NotImplementedException(
                        $"Linq-to-LDAP method calls only implemented for substring comparisons" +
                        $" (.Contains, .StartsWith, .EndsWith). Was: {fullname}.");
            }
        }

        internal string __PDictIndexesToString(
            MethodCallExpression expr,
            IReadOnlyCollection<ParameterExpression> p)
        {
            var conv = expr
                .Arguments
                .Select(a => __PDictIndexToObject(a, p))
                .ToList();
            try
            {
                var attr = (string)conv.SingleOrDefault(c => c is string);
                var rule = (Rule)conv.SingleOrDefault(c => c is Rule);
                var isDn = (bool?)conv.SingleOrDefault(c => c is bool);
                string formatted = attr != null ? attr : "";
                if (isDn == null && rule == null)
                    return formatted;

                formatted = formatted + (isDn != null && isDn.Value ? ":dn" : "");
                formatted = formatted + (rule != null ? $":{rule.RuleCode}" : "");
                return formatted + ":";
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Extended match rule format not understood. Please report to author.");
            }
        }

        internal object __PDictIndexToObject(
            Expression indexExpr,
            IReadOnlyCollection<ParameterExpression> p)
        {
            switch (indexExpr)
            {
                case ConstantExpression e when e.Type == typeof(string):
                    return EscapeString(e.Value as string);
                case ConstantExpression e when e.Type == typeof(Rule):
                    return e.Value as Rule;
                case ConstantExpression e when e.Type == typeof(bool):
                    return e.Value;
                case MemberExpression _:
                    var raw = RawEvalExpr(indexExpr, p);
                    if (raw is Rule r)
                    {
                        return r;
                    }

                    return EscapeString(raw.ToString());
            }

            throw new NotImplementedException(
                $"LDAP attribute reference must be a constant. Was: {indexExpr.NodeType} / {indexExpr?.Type}");
        }

        public string EvalExpr(
            Expression expr, IReadOnlyCollection<ParameterExpression> p)
            => Convert(RawEvalExpr(expr, p));

        private string Convert(object value)
        {
            switch (value)
            {
                case Guid guid:
                    return string.Join("", guid.ToByteArray().Select(b => $"\\{b:x2}"));
                case byte[] byteArray:
                    return string.Join("", byteArray.Select(b => $"\\{b:x2}"));
                default:
                    return EscapeString(value.ToString());
            }
        }

        public static string EscapeString(string value)
        {
            var replacer = new Regex(@"([*\)\(\\])");
            var result = replacer.Replace(value.ToString(), @"\$1");
            var left = result.TrimStart(' ');
            var right = result.TrimEnd(' ');
            var middle = result.Trim(' ');
            left = string.Concat(Enumerable.Repeat(@"\ ", result.Length - left.Length));
            if (middle.Length == 0)
            {
                return left;
            }
            right = string.Concat(Enumerable.Repeat(@"\ ", result.Length - right.Length));
            return left + middle + right;
        }

        public object RawEvalExpr(
            Expression expr, IReadOnlyCollection<ParameterExpression> p)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.Constant:
                    return _ConstExprToString(expr, p);
                case ExpressionType.MemberAccess:
                    var member = Expression.Convert(expr, typeof(object));
                    var getterLambda = Expression.Lambda<Func<object>>(member);
                    var getter = getterLambda.Compile();
                    return getter();
                default:
                    throw new NotImplementedException(
                        $"Linq-to-LDAP value access not implemented for type {expr.NodeType}.");
            }
        }
        
        internal string _Negate(string exprStr) => $"(!{exprStr})";

        internal string _ComparisonExprToString(
            Expression expr, IReadOnlyCollection<ParameterExpression> p, string op)
        {
            var e = expr as BinaryExpression;
            if (e.Left.NodeType == ExpressionType.Constant
                && e.Right.NodeType == ExpressionType.Constant) {
                throw new NotImplementedException("Constant comparisons not allowed in LDAP filter. One side must be member reference.");
            }
            var trueLeft = e.Left;
            var trueRight = e.Right;

            var left = ExpressionToString(trueLeft, p);
            try
            {
                var right = EvalExpr(trueRight, p);
                return $"({left}{op}{right})";
            } catch (NotImplementedException ex)
            {
                throw new InvalidOperationException($"Right side of LDAP comparison must be a constant.");
            }
        }

        internal string _MaybeStringCompareToExpr(
            Expression expr, IReadOnlyCollection<ParameterExpression> p, string op, string origOp = null)
        {
            origOp = origOp ?? op;
            var e = expr as BinaryExpression;
            MethodCallExpression[] mces;
            if ((mces = Strings.IsStringCompare(e.Left, e.Right)).Any())
            {
                return Strings.StringCompareToExpr(mces, e, p, op, origOp);
            }

            return null;
        }

        internal Expression __IsParamModelAccess(Expression e, IReadOnlyCollection<ParameterExpression> p)
        {
            if (e is MemberExpression me && me.Expression == p.FirstOrDefault())
            {
                return me;
            }
            else if (e is UnaryExpression ue
                     && ue.NodeType == ExpressionType.Convert
                     && ue.Operand is MemberExpression ume
                     && ume.Expression == p.FirstOrDefault())
            {
                return ume;
            } else if (e is MethodCallExpression mce && mce.Method.Name == "get_Item")
            {
                return mce;
            }

            return null;
        }

        internal string _ConstExprToString(
            Expression expr, IReadOnlyCollection<ParameterExpression> p)
        {
            var e = expr as ConstantExpression;
            if (e.Type == typeof(Boolean)) {
                // The following strings are LDAP filter's canonical true and false, respectively.
                return (e.Value is bool b && b) ? "(&)" : "(|)";
            }

            if (e.Type != typeof(string)
                && e.Type != typeof(char)
                && e.Type != typeof(int))
            {
                throw new NotImplementedException(
                    $"Type {e.Type} not implemented in {nameof(_ConstExprToString)}.");
            }

            return e.Value.ToString();
        }

        internal string _MemberToString(
            Expression e, IReadOnlyCollection<ParameterExpression> p)
        {
            if (e is MemberExpression me && me.Expression == p.FirstOrDefault())
            {
                var attr = me.Member.GetCustomAttribute<LdapProperty>();
                return EscapeString(attr != null ? attr.FieldName : me.Member.Name);
            } 
            else if (e.Type == typeof(Boolean)) 
            {
                return RawEvalExpr(e, p) is bool b && b ? "(&)" : "(|)";
            } 
            else if (e is MethodCallExpression mce && mce.Arguments.FirstOrDefault()?.Type == typeof(string)) 
            {
                return __PDictIndexesToString(mce, p);
            }

            // We could eval it, but may be out of scope?
            throw new NotImplementedException($"Unable to evaluate member expression: {e.Type}.");
        }
    }
}
