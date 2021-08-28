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
        public IEnumerable<T> RunQuery<T>(string query,
            string[] propertiesToLoad,
            SearchScope scope,
            int resultLimit,
            IEnumerable<DirectoryPropertyForT> properties)
        {
            context.InitContext();
            var searchOptions = new SearchOptions(context.BaseOu, (int)scope, query, propertiesToLoad);
            return context.Connection.SearchUsingSimplePaging(t => 
                ConvertToT<T>(new DirectoryResult(t), 
                properties), 
                searchOptions, 
                resultLimit);
        }

        private T ConvertToT<T>(DirectoryResult entry, IEnumerable<DirectoryPropertyForT> properties)
        {
            var rec = Activator.CreateInstance(typeof(T));
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
                var val = genericMethod.Invoke(entry, new object[] { item.FieldName });
                item.PropertyInfo.SetValue(rec, val);

            }
            return (T)rec;
        }
    }

}
