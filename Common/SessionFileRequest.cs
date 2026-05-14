using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class SessionFileRequest
    {
        string countryCode;
        DateTime date;

        public SessionFileRequest(string countryCode, DateTime date)
        {
            this.CountryCode = countryCode;
            this.Date = date;
        }

        [DataMember]
        public string CountryCode { get => countryCode; set => countryCode = value; }
        [DataMember]
        public DateTime Date { get => date; set => date = value; }
    }
}