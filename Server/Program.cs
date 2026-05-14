using Common;
using System;
using System.ServiceModel;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = null;
            LoadService publisher = null;
            LoadServiceListener listener = null;

            try
            {
                // Z8 - kreiramo instancu publisher-a i pretplacujemo Listener PRE pokretanja hosta
                publisher = new LoadService();
                listener = new LoadServiceListener(publisher);

                // ServiceHost prima INSTANCU (ne tip) - V1 obrazac, ali sa instancom umesto tipa
                host = new ServiceHost(publisher);
                host.Open();

                Console.WriteLine("==========================================");
                Console.WriteLine("  WCF Servis pokrenut.");
                Console.WriteLine("  Endpoint: net.tcp://localhost:9000/LoadService");
                Console.WriteLine("  Pritisnite ENTER za zatvaranje servisa.");
                Console.WriteLine("==========================================");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Greska pri pokretanju servisa: {ex.Message}");
            }
            finally
            {
                // V6 - otpisivanje sa dogadjaja (upravljanje memorijom)
                listener?.Detach();

                if (host != null)
                {
                    try
                    {
                        if (host.State == CommunicationState.Faulted)
                        {
                            host.Abort();
                            Console.WriteLine("[Server] Host je bio Faulted - Abort.");
                        }
                        else if (host.State != CommunicationState.Closed)
                        {
                            host.Close();
                            Console.WriteLine("[Server] Host lepo zatvoren - Close.");
                        }
                    }
                    catch (CommunicationException)
                    {
                        host.Abort();
                        Console.WriteLine("[Server] CommunicationException - Abort.");
                    }
                    catch (TimeoutException)
                    {
                        host.Abort();
                        Console.WriteLine("[Server] TimeoutException - Abort.");
                    }
                    catch (Exception)
                    {
                        host.Abort();
                        Console.WriteLine("[Server] Neocekivana greska - Abort.");
                    }
                }
            }
            Console.WriteLine("\n[Server] Pritisnite bilo koji taster za izlaz...");
            Console.ReadKey();
        }
    }
}