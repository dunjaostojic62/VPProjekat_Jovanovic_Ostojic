using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class SessionMeta
    {
        string countryCode;
        DateTime date;
        string sourceFileName;
        int totalSamples;
        int batchSize;

        public SessionMeta(string countryCode, DateTime date, string sourceFileName, int totalSamples, int batchSize)
        {
            this.CountryCode = countryCode;
            this.Date = date;
            this.SourceFileName = sourceFileName;
            this.TotalSamples = totalSamples;
            this.BatchSize = batchSize;
        }

        [DataMember]
        public string CountryCode { get => countryCode; set => countryCode = value; }
        [DataMember]
        public DateTime Date { get => date; set => date = value; }
        [DataMember]
        public string SourceFileName { get => sourceFileName; set => sourceFileName = value; }
        [DataMember]
        public int TotalSamples { get => totalSamples; set => totalSamples = value; }
        [DataMember]
        public int BatchSize { get => batchSize; set => batchSize = value; }

        public override string ToString()
        {
            return $"CountryCode : {CountryCode} Date : {Date:yyyy-MM-dd} SourceFile : {SourceFileName} " +
                $"TotalSamples : {TotalSamples} BatchSize : {BatchSize}";
        }
    }
}