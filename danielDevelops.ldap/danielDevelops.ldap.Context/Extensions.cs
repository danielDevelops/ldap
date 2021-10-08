using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Novell.Directory.Ldap;

namespace danielDevelops.ldap
{
    public static class Extensions
    {
        static readonly Regex ValidAttributeRegex = new Regex(@"^[a-zA-Z0-9-\._\s',]{2,}$");
        public static bool IsValidLdapAttribute(this string value)
            => ValidAttributeRegex.IsMatch(value);

        public static ILdapResult<T> ExtractProperty<T>(this LdapEntry searchResults, string property)
            => new LdapResult<T>(searchResults, property);
    }
}
