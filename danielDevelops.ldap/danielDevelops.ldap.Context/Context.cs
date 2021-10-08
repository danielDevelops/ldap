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
        public string BaseOu { get; }
        public string Domain => $"{BaseOu.ToLower().Replace(',', '.').Replace("dc=", String.Empty)}";

        protected TimeSpan? MaxPasswordAge
        {
            get => serverInfo.MaxPasswordAge;
            set => serverInfo.MaxPasswordAge = value;
        }

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

        public LdapSearchResults ExecuteRawQuery(string query, 
            string[] propertiesToLoad, 
            SearchScope searchScope, 
            string overrideBaseOu = null)
            => new DirectorySearch(this).ExecuteRawQuery(query, propertiesToLoad, searchScope, overrideBaseOu);

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

        public LdapConnection CreateConnection()
        {
            var connection = new LdapConnection()
            {
                SecureSocketLayer = serverInfo.IsSecuredConnection
            };
            connection.Connect(serverInfo.ServerAddress, serverInfo.Port);
            connection.Constraints = new LdapConstraints
            {
                ReferralFollowing = true
            };
            return connection;
        }

        internal void InitContext()
        {
            if (Connection != null && Connection.Connected)
            {
                return;
            }
            Connection = CreateConnection();
            Connection.Bind(serverInfo.Username, serverInfo.Password);
        }

        

        public void Dispose()
        {
            Connection.Disconnect();
            Connection.Dispose();
        }
    }
}
