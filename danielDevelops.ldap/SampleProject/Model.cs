using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using danielDevelops.ldap;

namespace SampleProject
{
    public class Model
    {
        [LdapProperty("cn")]
        public string Username { get; set; }

        [LdapProperty("givenname")]
        public string FirstName { get; set; }

        [LdapProperty("preferredname")]
        public string PreferredName { get; set; }
        [LdapProperty("sn")]
        public string LastName { get; set; }

        [LdapProperty("groupmembership")]
        public IEnumerable<string> Groups { get; set; }
    }
}
