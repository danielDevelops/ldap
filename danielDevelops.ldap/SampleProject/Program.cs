using System;
using System.Collections.Generic;
using System.Linq;

namespace SampleProject
{
    class Program
    {
        static void Main(string[] args)
        {
            using var context = new DirectoryContext(new ServerInfo());
            var things = context.Models.ExecuteRawQuery("cn=asdf", 
                danielDevelops.ldap.SearchScope.Base, 
                1);
            var data = context.Models.Get(t => t.FirstName == "Daniel" && t.LastName == "Martin", 
                danielDevelops.ldap.SearchScope.Base, 
                50);
        }
    }
}
