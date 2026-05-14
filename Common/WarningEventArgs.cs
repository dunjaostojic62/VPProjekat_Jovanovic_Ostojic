using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public enum WarningType
    {
        LowLoadFactor,
        Flatline,
        ConsumptionSpike
    }

    public class WarningEventArgs : EventArgs
    {
        public WarningType Type { get; }
        public string Message { get; }
        public DateTime When { get; }

        public WarningEventArgs(WarningType type, string message)
        {
            this.Type = type;
            this.Message = message;
            this.When = DateTime.Now;
        }
    }
}