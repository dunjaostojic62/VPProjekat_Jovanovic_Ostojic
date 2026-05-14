using Common;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Configuration;
using System.IO;

namespace Server
{

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class LoadService : ILoadService
    {
        SessionMeta currentMeta;
        double lastCumulative;
        bool sessionActive;
        FileStream sessionStream;
        StreamWriter sessionWriter;
        FileStream rejectsStream;
        StreamWriter rejectsWriter;
        string sessionFolder;

        public event EventHandler<TransferStartedEventArgs> OnTransferStarted;
        public event EventHandler<BatchReceivedEventArgs> OnBatchReceived;
        public event EventHandler<TransferCompletedEventArgs> OnTransferCompleted;
        public event EventHandler<WarningEventArgs> OnWarningRaised;
        int totalReceived;
        public LoadService()
        {
            this.lastCumulative = 0;
            this.sessionActive = false;
        }

        [OperationBehavior(AutoDisposeParameters = true)]
        public void StartSession(SessionMeta meta)
        {
            if (meta == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Meta podatak ne sme biti null."));
            }
            if (string.IsNullOrWhiteSpace(meta.CountryCode))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("CountryCode je obavezan."));
            }
            if (meta.TotalSamples <= 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("TotalSamples mora biti > 0."));
            }
            if (meta.BatchSize <= 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("BatchSize mora biti > 0."));
            }
            this.currentMeta = meta;
            this.sessionActive = true;
            this.lastCumulative = 0;
            Console.WriteLine($"[Server] StartSession primljen:");
            Console.WriteLine($"         {meta}");
            //za data strukturu
            string root = ConfigurationManager.AppSettings["dataFolderPath"];
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("U App.config nije definisan kljuc 'dataFolderPath'."));
            }
            this.sessionFolder = Path.Combine(root,
                                              meta.CountryCode,
                                              meta.Date.ToString("yyyy-MM-dd"));

            Directory.CreateDirectory(sessionFolder);

            string sessionPath = Path.Combine(sessionFolder, "session.csv");
            string rejectsPath = Path.Combine(sessionFolder, "rejects.csv");

            sessionStream = new FileStream(sessionPath, FileMode.Create, FileAccess.Write);
            sessionWriter = new StreamWriter(sessionStream);
            sessionWriter.WriteLine("TimestampUtc,TimestampLocal,ActualMW,ForecastMW,CumulativeMWh,CountryCode,RowIndex");

            rejectsStream = new FileStream(rejectsPath, FileMode.Create, FileAccess.Write);
            rejectsWriter = new StreamWriter(rejectsStream);
            rejectsWriter.WriteLine("RowIndex,Reason,OriginalSample");
            Console.WriteLine($"         Izlazni folder: {sessionFolder}");
            this.totalReceived = 0;
            RaiseTransferStarted(meta);
        }

        [OperationBehavior(AutoDisposeParameters = true)]
        public void PushBatch(List<LoadSample> samples)
        {
            if (!sessionActive)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Sesija nije aktivna. Prvo pozovi StartSession."));
            }
            if (samples == null || samples.Count == 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Batch je prazan."));
            }
            foreach (LoadSample s in samples)
            {
                try
                {
                    ValidateSample(s);
                    sessionWriter.WriteLine(
                        $"{s.TimestampUtc:yyyy-MM-ddTHH:mm:ssZ}," +
                        $"{s.TimestampLocal:yyyy-MM-ddTHH:mm:sszzz}," +
                        $"{s.ActualMW}," +
                        $"{s.ForecastMW}," +
                        $"{s.CumulativeMWh}," +
                        $"{s.CountryCode}," +
                        $"{s.RowIndex}");
                }
                catch (FaultException<DataFormatFault> fe)
                {
                    UpisiURejects(s, fe.Detail.Message);
                    throw;
                }
                catch (FaultException<ValidationFault> fe)
                {
                    UpisiURejects(s, fe.Detail.Message);
                    throw;
                }
            }
            Console.WriteLine($"[Server] Blok primljen: {samples.Count} uzoraka. " + $"Trenutni kumulativ: {lastCumulative:F2} MWh");
            this.totalReceived += samples.Count;
            RaiseBatchReceived(samples.Count, totalReceived, lastCumulative);
        }

        public void EndSession()
        {
            this.sessionActive = false;
            Console.WriteLine($"[Server] EndSession - prenos zavrsen za " +
                              $"{currentMeta?.CountryCode} ({currentMeta?.Date:yyyy-MM-dd}). " +
                              $"Finalni kumulativ: {lastCumulative:F2} MWh");

            RaiseTransferCompleted(currentMeta.CountryCode, currentMeta.Date, totalReceived, lastCumulative);
            //zatvaranje stream-a
            sessionWriter?.Flush();
            rejectsWriter?.Flush();
            sessionWriter?.Dispose();
            sessionStream?.Dispose();
            rejectsWriter?.Dispose();
            rejectsStream?.Dispose();
            Console.WriteLine($"[Server] Snimljeno u: {sessionFolder}");
        }

        [OperationBehavior(AutoDisposeParameters = true)]
        public SessionFilePackage GetSessionFile(SessionFileRequest request)
        {
            if (request == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Request ne sme biti null."));
            }
            if (string.IsNullOrWhiteSpace(request.CountryCode))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("CountryCode je obavezan."));
            }

            string root = ConfigurationManager.AppSettings["dataFolderPath"];
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("U App.config nije definisan kljuc 'dataFolderPath'."));
            }

            string folder = Path.Combine(root,
                                         request.CountryCode,
                                         request.Date.ToString("yyyy-MM-dd"));
            string sessionPath = Path.Combine(folder, "session.csv");

            if (!File.Exists(sessionPath))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault($"Fajl ne postoji: {sessionPath}"));
            }
            // V5 obrazac - ucitavanje fajla u MemoryStream pa slanje preko mreze
            MemoryStream ms = new MemoryStream();
            using (FileStream fs = new FileStream(sessionPath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[1024];
                int count;
                while ((count = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, count);
                }
            }
            ms.Position = 0;

            Console.WriteLine($"[Server] GetSessionFile: poslat {sessionPath} ({ms.Length} bajtova).");
            return new SessionFilePackage("session.csv", ms);
        }

        private void UpisiURejects(LoadSample s, string razlog)
        {
            string original = $"UTC={s.TimestampUtc:yyyy-MM-ddTHH:mm:ssZ} " +
                              $"Actual={s.ActualMW} Forecast={s.ForecastMW} " +
                              $"Cumulative={s.CumulativeMWh}";
            rejectsWriter.WriteLine($"{s.RowIndex},\"{razlog}\",\"{original}\"");
            rejectsWriter.Flush();
        }
        private void RaiseTransferStarted(SessionMeta meta)
        {
            if (OnTransferStarted != null)
            {
                OnTransferStarted(this, new TransferStartedEventArgs(meta));
            }
        }
        private void RaiseBatchReceived(int batchSize, int totalReceived, double currentCumulative)
        {
            if (OnBatchReceived != null)
            {
                OnBatchReceived(this, new BatchReceivedEventArgs(batchSize, totalReceived, currentCumulative));
            }
        }
        private void RaiseTransferCompleted(string country, DateTime date, int total, double finalCumulative)
        {
            if (OnTransferCompleted != null)
            {
                OnTransferCompleted(this, new TransferCompletedEventArgs(country, date, total, finalCumulative));
            }
        }
        private void RaiseWarning(WarningType type, string message)
        {
            if (OnWarningRaised != null)
            {
                OnWarningRaised(this, new WarningEventArgs(type, message));
            }
        }

        private void ValidateSample(LoadSample s)
        {
            if (s.TimestampUtc == DateTime.MinValue)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault($"Neispravan TimestampUtc u redu {s.RowIndex}."));
            }
            if (s.TimestampLocal == DateTime.MinValue)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault($"Neispravan TimestampLocal u redu {s.RowIndex}."));
            }
            if (s.ActualMW < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault($"ActualMW mora biti >= 0 (red {s.RowIndex}, vrednost {s.ActualMW})."));
            }
            if (s.ForecastMW < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault($"ForecastMW mora biti >= 0 (red {s.RowIndex}, vrednost {s.ForecastMW})."));
            }
            if (s.CumulativeMWh + 1e-9 < this.lastCumulative)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(
                        $"CumulativeMWh ({s.CumulativeMWh:F4}) je manji od prethodnog " +
                        $"({lastCumulative:F4}) - red {s.RowIndex}."));
            }
            this.lastCumulative = s.CumulativeMWh;
        }
    }
}