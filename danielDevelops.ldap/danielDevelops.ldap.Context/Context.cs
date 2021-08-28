using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novell.Directory.Ldap;

namespace danielDevelops.ldap
{
    public abstract class Context : IDisposable
    {
        private readonly IServerInfo serverInfo;
        internal LdapConnection Connection { get; private set; }
        internal string BaseOu { get; }
        public Context(IServerInfo serverInfo)
        {
            this.serverInfo = serverInfo;
            if (string.IsNullOrWhiteSpace(serverInfo.Username)
                || string.IsNullOrWhiteSpace(serverInfo.Password)
                || string.IsNullOrWhiteSpace(serverInfo.ServerAddress)
                || serverInfo.Port == 0
                || string.IsNullOrWhiteSpace(serverInfo.BaseOU))
            {
                throw new NullReferenceException("There is a missing value in the server info object.");
            }
            BaseOu = serverInfo.BaseOU;
            InitModels();
        }

        private void InitModels()
        {
            var lists = GetType()
                .GetProperties()
                .Where(t => 
                    t.PropertyType.IsGenericType 
                    && t.PropertyType.GetGenericTypeDefinition() == typeof(IDirectoryList<>)
                ).ToList();
            foreach (var item in lists)
            {
                var type = item.PropertyType.GetGenericArguments()[0];
                var listType = typeof(DirectoryList<>);
                var rec = listType.MakeGenericType(new Type[] { type });
                var search = new DirectorySearch(this);
                item.SetValue(this, Activator.CreateInstance(rec, search));
            }
        }

        internal void InitContext()
        {
            if (Connection != null && Connection.Connected)
            {
                return;
            }
            Connection = new()
            {
                SecureSocketLayer = serverInfo.IsSecuredConnection
            };
            Connection.Connect(serverInfo.ServerAddress, serverInfo.Port);
            Connection.Bind(serverInfo.Username, serverInfo.Password);
        }

        public void Dispose()
        {
            Connection.Disconnect();
            Connection.Dispose();
        }
    }
}
