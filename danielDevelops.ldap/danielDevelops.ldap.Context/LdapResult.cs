using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novell.Directory.Ldap;

namespace danielDevelops.ldap
{
    internal class LdapResult<T> : ILdapResult<T>
    {
        public string PropertyName { get; }
        private readonly List<T> allValues = new();
        public IEnumerable<T> AllValues => allValues;
        public bool ResultFound => AllValues.Any();
        public T FirstOrDefault
        {
            get
            {
                if (AllValues.Any())
                    return AllValues.First();
                return default(T);

            }
        }

        public LdapResult(LdapEntry searchResults, string property)
        {
            PropertyName = property.ToUpper();
            CreateResult(searchResults);
        }

        private void CreateResult(LdapEntry searchResults)
        {
            if (searchResults == null)
                return;

            var attributeSet = searchResults.getAttributeSet();

            if (searchResults.getAttribute(PropertyName) == null || searchResults.getAttribute(PropertyName)?.size() == 0)
                return;
            var conversionType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            var isPropertyInt64 = Int64.TryParse(searchResults.getAttribute(PropertyName).StringValue, out long int64Value);
            var isStrangeADDate = isPropertyInt64 && conversionType == typeof(DateTime);

            if (isStrangeADDate)
            {
                var weirdDateFormat = int64Value;
                if (weirdDateFormat == Int64.MaxValue)
                    return;
                var date = DateTime.FromFileTime(weirdDateFormat);
                allValues.Add((T)Convert.ChangeType(date, conversionType));
            }
            else if (conversionType == typeof(byte[]))
            {
                var bytes = searchResults.getAttribute(PropertyName).ByteValue.Select(t => (byte)t).ToArray();

                allValues.Add((T)Convert.ChangeType(bytes, conversionType));
            }
            else
            {
                foreach (var item in searchResults.getAttribute(PropertyName).StringValueArray)
                {
                    allValues.Add((T)Convert.ChangeType(item, conversionType));
                }

            }
        }
    }
}
