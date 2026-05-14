using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class TransferStartedEventArgs : EventArgs
    {
        public SessionMeta Meta { get; }
        public DateTime When { get; }

        public TransferStartedEventArgs(SessionMeta meta)
        {
            this.Meta = meta;
            this.When = DateTime.Now;
        }
    }
}