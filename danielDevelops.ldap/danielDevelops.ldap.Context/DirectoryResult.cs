using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novell.Directory.Ldap;

namespace danielDevelops.ldap
{
    public class DirectoryResult
    {
        readonly LdapEntry result;
        public DirectoryResult(LdapEntry searchResult)
        {
            result = searchResult;
        }
        public T GetPropertyValue<T>(string property)
            => result.ExtractProperty<T>(property).FirstOrDefault;
        
        public IEnumerable<T> GetPropertyAllValues<T>(string property)
            => result.ExtractProperty<T>(property).AllValues;
    }
}
