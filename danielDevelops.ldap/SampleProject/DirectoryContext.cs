using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using danielDevelops.ldap;

namespace SampleProject
{
    public class DirectoryContext : Context
    {
        public DirectoryContext(IServerInfo serverInfo) 
            : base(serverInfo)
        {
        }

        public IDirectoryList<Model> Models { get; set; }
    }
}
