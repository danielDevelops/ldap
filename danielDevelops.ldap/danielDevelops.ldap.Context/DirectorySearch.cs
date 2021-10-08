using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Novell.Directory.Ldap;

namespace danielDevelops.ldap
{
    internal sealed class DirectorySearch
    {
        private readonly Context context;
        public DirectorySearch(Context context)
        {
            this.context = context;
        }

        public LdapSearchResults ExecuteRawQuery(string query, 
            string[] propertiesToLoad, 
            SearchScope scope, 
            string overrideBaseOu = null)
        {
            context.InitContext();
            return context.Connection.Search(overrideBaseOu ?? context.BaseOu, (int)scope, query, propertiesToLoad, false);
        }


        internal IEnumerable<T> RunQuery<T>(string query,
            string[] propertiesToLoad,
            SearchScope scope,
            int resultLimit,
            IEnumerable<DirectoryPropertyForT> properties,
            string overrideBaseOu = null)
        {
            context.InitContext();
            var results = context.Connection.Search(overrideBaseOu ?? context.BaseOu, (int)scope, query, propertiesToLoad, false);
            var recs = new List<T>();
            while (results.hasMore())
            {
                var rec = results.next();
                recs.Add(ConvertToT<T>(rec,properties));
            }
            return recs;
            
        }

        private T ConvertToT<T>(LdapEntry entry, IEnumerable<DirectoryPropertyForT> properties)
        {
            if (entry == null)
                return default;
            var rec = Activator.CreateInstance(typeof(T));
            var directoryResult = new DirectoryResult(entry);
            foreach (var item in properties)
            {
                MethodInfo genericMethod;
                if (item.PropertyInfo.PropertyType.IsGenericType && 
                    item.PropertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    var method = typeof(DirectoryResult).GetMethod(nameof(DirectoryResult.GetPropertyAllValues));
                    genericMethod = method.MakeGenericMethod(item.PropertyInfo.PropertyType.GetGenericArguments()[0]);
                }
                else
                {
                    var method = typeof(DirectoryResult).GetMethod(nameof(DirectoryResult.GetPropertyValue));
                    genericMethod = method.MakeGenericMethod(item.PropertyInfo.PropertyType);
                }
                var val = genericMethod.Invoke(directoryResult, new object[] { item.FieldName.ToString() });
                item.PropertyInfo.SetValue(rec, val);

            }
            return (T)rec;
        }
    }

}
