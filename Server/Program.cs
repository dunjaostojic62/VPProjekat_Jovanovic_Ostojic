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
            try
            {
                host = new ServiceHost(typeof(LoadService));
                host.Open();

                Console.WriteLine("==========================================");
                Console.WriteLine("  WCF Servis pokrenut.");
                Console.WriteLine("  Endpoint: net.tcp://localhost:9000/LoadService");
                Console.WriteLine("  Pritisnite ENTER za zatvaranje servisa.");
                Console.WriteLine("==========================================");
                Console.ReadLine();

                host.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Greska pri pokretanju servisa: {ex.Message}");
             
                host?.Abort();
            }
        }
    }
}