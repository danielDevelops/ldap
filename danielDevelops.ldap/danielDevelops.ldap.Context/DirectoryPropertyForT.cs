using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace danielDevelops.ldap
{
    internal class DirectoryPropertyForT
    {
        public string FieldName { get; private set; }
        public PropertyInfo PropertyInfo { get; private set; }

        public static IEnumerable<DirectoryPropertyForT> GetPropertyAttributesForT(Type type)
        {
            foreach (var item in type.GetProperties())
            {
                var attr = GetPropertyAttributes(item);
                if (attr?.Ignore == true)
                    continue;
                yield return GetDirectoryPropertyForT(item, attr);
            }
        }

        public static LdapProperty GetPropertyAttributes(PropertyInfo property) 
            => property.GetCustomAttributes(typeof(LdapProperty), false)?.Cast<LdapProperty>()?.SingleOrDefault();

        public static DirectoryPropertyForT GetDirectoryPropertyForT(PropertyInfo property, LdapProperty ldapProperties)
            => new DirectoryPropertyForT
            {
                FieldName = ldapProperties?.FieldName ?? property.Name,
                PropertyInfo = property
            };
    }
}
