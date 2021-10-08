using System.Collections.Generic;
using Novell.Directory.Ldap;

namespace danielDevelops.ldap
{
    public interface ILdapResult<T>
    {
        IEnumerable<T> AllValues { get; }
        T FirstOrDefault { get; }
        string PropertyName { get; }
        bool ResultFound { get; }
    }
}