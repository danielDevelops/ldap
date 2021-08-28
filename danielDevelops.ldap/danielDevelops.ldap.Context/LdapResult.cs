using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novell.Directory.Ldap;

namespace danielDevelops.ldap
{
    internal class LdapResult<T>
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

        public void CreateResult(LdapEntry searchResults)
        {
            if (searchResults == null)
                return;

            var attributeSet = searchResults.GetAttributeSet();
            if (!attributeSet.ContainsKey(PropertyName))
                return;

            if (searchResults.GetAttribute(PropertyName) == null || searchResults.GetAttribute(PropertyName)?.Size() == 0)
                return;
            var conversionType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            var isPropertyInt64 = Int64.TryParse(searchResults.GetAttribute(PropertyName).StringValue, out long int64Value);
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
                var bytes = searchResults.GetAttribute(PropertyName).ByteValue.Select(t => (byte)t).ToArray();

                allValues.Add((T)Convert.ChangeType(bytes, conversionType));
            }
            else
            {
                foreach (var item in searchResults.GetAttribute(PropertyName).StringValueArray)
                {
                    allValues.Add((T)Convert.ChangeType(item, conversionType));
                }

            }
        }
    }
}
