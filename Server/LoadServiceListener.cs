using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class LoadServiceListener
    {
        LoadService publisher;

        public LoadServiceListener(LoadService publisher)
        {
            this.publisher = publisher;

            publisher.OnTransferStarted += OnTransferStartedHandler;
            publisher.OnBatchReceived += OnBatchReceivedHandler;
            publisher.OnTransferCompleted += OnTransferCompletedHandler;
            publisher.OnWarningRaised += OnWarningRaisedHandler;

            Console.WriteLine("[Listener] Pretplata na sve dogadjaje LoadService-a izvrsena.");
        }

        private void OnTransferStartedHandler(object sender, TransferStartedEventArgs e)
        {
            Console.WriteLine($"[Listener] >> OnTransferStarted u {e.When:HH:mm:ss}: " +
                              $"zemlja {e.Meta.CountryCode}, datum {e.Meta.Date:yyyy-MM-dd}, " +
                              $"ocekivano {e.Meta.TotalSamples} uzoraka po N={e.Meta.BatchSize}.");
        }

        private void OnBatchReceivedHandler(object sender, BatchReceivedEventArgs e)
        {
            Console.WriteLine($"[Listener] >> OnBatchReceived u {e.When:HH:mm:ss}: " +
                              $"primljeno {e.BatchSize} uzoraka, ukupno {e.TotalReceived}, " +
                              $"kumulativ {e.CurrentCumulative:F2} MWh.");
        }

        private void OnTransferCompletedHandler(object sender, TransferCompletedEventArgs e)
        {
            Console.WriteLine($"[Listener] >> OnTransferCompleted u {e.When:HH:mm:ss}: " +
                              $"zavrsen prenos za {e.CountryCode} ({e.Date:yyyy-MM-dd}), " +
                              $"ukupno {e.TotalReceived} uzoraka, finalni kumulativ {e.FinalCumulative:F2} MWh.");
        }

        private void OnWarningRaisedHandler(object sender, WarningEventArgs e)
        {
            Console.WriteLine($"[Listener] !! OnWarningRaised u {e.When:HH:mm:ss} ({e.Type}): {e.Message}");
        }

        public void Detach()
        {
            publisher.OnTransferStarted -= OnTransferStartedHandler;
            publisher.OnBatchReceived -= OnBatchReceivedHandler;
            publisher.OnTransferCompleted -= OnTransferCompletedHandler;
            publisher.OnWarningRaised -= OnWarningRaisedHandler;

            Console.WriteLine("[Listener] Otpisao sa svih dogadjaja.");
        }
    }
}