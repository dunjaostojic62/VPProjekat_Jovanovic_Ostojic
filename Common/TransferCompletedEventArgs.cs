using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class TransferCompletedEventArgs : EventArgs
    {
        public string CountryCode { get; }
        public DateTime Date { get; }
        public int TotalReceived { get; }
        public double FinalCumulative { get; }
        public DateTime When { get; }

        public TransferCompletedEventArgs(string countryCode, DateTime date, int totalReceived, double finalCumulative)
        {
            this.CountryCode = countryCode;
            this.Date = date;
            this.TotalReceived = totalReceived;
            this.FinalCumulative = finalCumulative;
            this.When = DateTime.Now;
        }
    }
}