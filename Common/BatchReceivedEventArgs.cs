using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class BatchReceivedEventArgs : EventArgs
    {
        public int BatchSize { get; }
        public int TotalReceived { get; }
        public double CurrentCumulative { get; }
        public DateTime When { get; }

        public BatchReceivedEventArgs(int batchSize, int totalReceived, double currentCumulative)
        {
            this.BatchSize = batchSize;
            this.TotalReceived = totalReceived;
            this.CurrentCumulative = currentCumulative;
            this.When = DateTime.Now;
        }
    }
}