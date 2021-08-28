using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novell.Directory.Ldap;

namespace danielDevelops.ldap
{
    public interface IServerInfo
    {
        string ServerAddress { get; }
        int Port { get; }
        string Username { get; }
        string Password { get; }
        string BaseOU { get; }
        bool IsSecuredConnection { get; }
        bool IsMSAD { get; }
    }
}
