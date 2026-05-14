using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.ServiceModel;

namespace Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string csvPath = ConfigurationManager.AppSettings["csvFilePath"];
                string countryCode = ConfigurationManager.AppSettings["countryCode"];
                string dateStr = ConfigurationManager.AppSettings["selectedDate"];
                string batchSizeStr = ConfigurationManager.AppSettings["batchSize"];
                string rejectedPath = ConfigurationManager.AppSettings["rejectedFilePath"];

                if (!DateTime.TryParse(dateStr, CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out DateTime selectedDate))
                {
                    Console.WriteLine($"Neispravan selectedDate u App.config: '{dateStr}'");
                    return;
                }
                if (!int.TryParse(batchSizeStr, out int batchSize) || batchSize <= 0)
                {
                    Console.WriteLine($"Neispravan batchSize u App.config: '{batchSizeStr}'");
                    return;
                }

                Console.WriteLine("==========================================");
                Console.WriteLine($"  CSV: {csvPath}");
                Console.WriteLine($"  Zemlja: {countryCode}");
                Console.WriteLine($"  Dan: {selectedDate:yyyy-MM-dd}");
                Console.WriteLine($"  BatchSize: {batchSize}");
                Console.WriteLine("==========================================");

                List<LoadSample> uzorci;
                using (CsvReader reader = new CsvReader(csvPath, rejectedPath))
                {
                    uzorci = reader.ReadSamples(countryCode, selectedDate);
                }
                Console.WriteLine($"[Client] Procitano {uzorci.Count} uzoraka.");

                using (LoadClient client = new LoadClient("LoadEndpoint"))
                {
                    SessionMeta meta = new SessionMeta(
                        countryCode,
                        selectedDate,
                        Path.GetFileName(csvPath),
                        uzorci.Count,
                        batchSize);

                    client.StartSession(meta);
                    Console.WriteLine("[Client] StartSession poslat.");
                    bool simulateAbort = bool.TryParse(
                            ConfigurationManager.AppSettings["simulateAbort"], out bool sa) && sa;
                    int poslato = 0;
                    int brojBlokova = 0;
                    int ukupnoBlokova = (int)Math.Ceiling((double)uzorci.Count / batchSize);

                    for (int i = 0; i < uzorci.Count; i += batchSize)
                    {
                        int velicina = Math.Min(batchSize, uzorci.Count - i);
                        List<LoadSample> batch = uzorci.GetRange(i, velicina);

                        brojBlokova++;
                        Console.WriteLine($"[Client] Saljem blok {brojBlokova}/{ukupnoBlokova} ({velicina} uzoraka)...");

                        client.PushBatch(batch);

                        poslato += velicina;
                        Console.WriteLine($"[Client] Blok {brojBlokova}/{ukupnoBlokova} primljen na serveru. Ukupno uzoraka: {poslato}/{uzorci.Count}");
                    }

                    client.EndSession();
                    Console.WriteLine($"[Client] Prenos zavrsen. Poslato {brojBlokova} blokova, {poslato} uzoraka.");

                    Console.WriteLine("[Client] Preuzimam session.csv sa servera...");
                    SessionFileRequest req = new SessionFileRequest(countryCode, selectedDate);
                    using (SessionFilePackage package = client.GetSessionFile(req))
                    {
                        string lokalniPath = $"received_{countryCode}_{selectedDate:yyyy-MM-dd}_{package.FileName}";
                        using (FileStream fs = new FileStream(lokalniPath, FileMode.Create, FileAccess.Write))
                        {
                            package.Content.Position = 0;
                            byte[] buffer = new byte[1024];
                            int count;
                            while ((count = package.Content.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fs.Write(buffer, 0, count);
                            }
                        }
                        Console.WriteLine($"[Client] Sacuvano kao: {lokalniPath} ({package.Content.Length} bajtova).");
                    }
                }
            }
            catch (FaultException<DataFormatFault> fe)
            {
                Console.WriteLine($"[DataFormatFault] {fe.Detail.Message}");
            }
            catch (FaultException<ValidationFault> fe)
            {
                Console.WriteLine($"[ValidationFault] {fe.Detail.Message}");
            }
            catch (FaultException fe)
            {
                Console.WriteLine($"[FaultException] {fe.Message}");
            }
            catch (CommunicationException ce)
            {
                Console.WriteLine($"[CommunicationException] {ce.Message}");
            }
            catch (TimeoutException te)
            {
                Console.WriteLine($"[TimeoutException] {te.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Greska] {ex.Message}");
            }

            Console.WriteLine("\nPritisnite ENTER za izlaz.");
            Console.ReadLine();
        }
    }
}