using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class LoadSample
    {
        DateTime timestampUtc;
        DateTime timestampLocal;
        double actualMW;
        double forecastMW;
        double cumulativeMWh;
        string countryCode;
        int rowIndex;

        public LoadSample(DateTime timestampUtc, DateTime timestampLocal, double actualMW, double forecastMW,
            double cumulativeMWh, string countryCode, int rowIndex)
        {
            this.TimestampUtc = timestampUtc;
            this.TimestampLocal = timestampLocal;
            this.ActualMW = actualMW;
            this.ForecastMW = forecastMW;
            this.CumulativeMWh = cumulativeMWh;
            this.CountryCode = countryCode;
            this.RowIndex = rowIndex;
        }

        [DataMember]
        public DateTime TimestampUtc { get => timestampUtc; set => timestampUtc = value; }
        [DataMember]
        public DateTime TimestampLocal { get => timestampLocal; set => timestampLocal = value; }
        [DataMember]
        public double ActualMW { get => actualMW; set => actualMW = value; }
        [DataMember]
        public double ForecastMW { get => forecastMW; set => forecastMW = value; }
        [DataMember]
        public double CumulativeMWh { get => cumulativeMWh; set => cumulativeMWh = value; }
        [DataMember]
        public string CountryCode { get => countryCode; set => countryCode = value; }
        [DataMember]
        public int RowIndex { get => rowIndex; set => rowIndex = value; }

        public override string ToString()
        {
            return $"Row : {RowIndex} UTC : {TimestampUtc:yyyy-MM-dd HH:mm} Local : {TimestampLocal:yyyy-MM-dd HH:mm} " +
                $"Actual : {ActualMW} MW Forecast : {ForecastMW} MW Cumulative : {CumulativeMWh} MWh Country : {CountryCode}";
        }
    }
}