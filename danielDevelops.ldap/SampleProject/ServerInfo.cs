using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using danielDevelops.ldap;

namespace SampleProject
{
    public class ServerInfo : IServerInfo
    {
        public string ServerAddress => "";
        public int Port => 636;
        public string Username => "";
        public string Password => "";
        public string BaseOU => "";
        public bool IsSecuredConnection => true;
        public bool IsMSAD => false;
    }
}
