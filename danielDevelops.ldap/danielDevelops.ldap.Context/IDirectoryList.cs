using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace danielDevelops.ldap
{
    public interface IDirectoryList<T> where T : class, new()
    {
        IEnumerable<T> ExecuteRawQuery(string query, SearchScope searchScope, int limit, string overrideBaseOu = null);
        IEnumerable<T> Get(Expression<Func<T, bool>> query, SearchScope searchScope, int limit);
        T FirstOrDefault(Expression<Func<T, bool>> query, SearchScope searchScope);
    }
}