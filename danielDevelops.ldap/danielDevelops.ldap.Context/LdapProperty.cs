using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace danielDevelops.ldap
{
    [AttributeUsage(AttributeTargets.Property)]
    public class LdapProperty : Attribute
    {
        public string FieldName;
        public bool Ignore;
        public LdapProperty()
        {

        }
        public LdapProperty(string fieldName)
        {
            FieldName = fieldName;
        }
    }
}
