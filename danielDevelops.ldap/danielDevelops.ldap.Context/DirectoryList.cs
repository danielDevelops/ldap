using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Novell.Directory.Ldap;

namespace danielDevelops.ldap
{
    internal class DirectoryList<T> : IDirectoryList<T> where T : class, new()
    {
        private readonly DirectorySearch search;
        private readonly IEnumerable<DirectoryPropertyForT> attributesOfT;

        public DirectoryList(DirectorySearch search)
        {
            if (search is null)
            {
                throw new ArgumentNullException(nameof(search));
            }
            this.search = search;
            attributesOfT = DirectoryPropertyForT.GetPropertyAttributesForT(typeof(T));
        }

        public IEnumerable<T> ExecuteRawQuery(string query, SearchScope searchScope, int limit, string overrideBaseOu = null)
        {
            var propsToLoad = attributesOfT.Select(t => t.FieldName).ToArray();
            return search.RunQuery<T>(query, propsToLoad, searchScope, limit, attributesOfT, overrideBaseOu);
        }
            
        public IEnumerable<T> Get(Expression<Func<T, bool>> query, SearchScope searchScope, int limit)
            => ExecuteRawQuery(QueryExtensions.CreateQuery(query), searchScope, limit);

        public T FirstOrDefault(Expression<Func<T, bool>> query, SearchScope searchScope)
            => Get(query, searchScope, 1).FirstOrDefault();
    }
}
