using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using danielDevelops.ldap.QueryFilterParser;

namespace danielDevelops.ldap
{
    internal static class QueryExtensions
    {
        public static string CreateQuery<T>(Expression<Func<T, bool>> query) where T : class, new()
        {
            var queryCompiler = new CompilerCore();
            return queryCompiler.ExpressionToString(query.Body, query.Parameters);
        }
    }
}

