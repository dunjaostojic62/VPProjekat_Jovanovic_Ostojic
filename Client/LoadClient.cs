using Common;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Client
{
    public class LoadClient : IDisposable
    {
        ChannelFactory<ILoadService> factory;
        ILoadService proxy;
        bool disposed;
        string endpointName;

        public LoadClient(string endpointName)
        {
            this.EndpointName = endpointName;
            this.disposed = false;

            this.factory = new ChannelFactory<ILoadService>(endpointName);
            this.proxy = factory.CreateChannel();

            Console.WriteLine($"[LoadClient] Kanal otvoren prema endpoint-u '{endpointName}'.");
        }

        public string EndpointName { get => endpointName; set => endpointName = value; }

        public void StartSession(SessionMeta meta)
        {
            if (disposed) throw new ObjectDisposedException(nameof(LoadClient));
            proxy.StartSession(meta);
        }

        public void PushBatch(List<LoadSample> samples)
        {
            if (disposed) throw new ObjectDisposedException(nameof(LoadClient));
            proxy.PushBatch(samples);
        }

        public void EndSession()
        {
            if (disposed) throw new ObjectDisposedException(nameof(LoadClient));
            proxy.EndSession();
        }
        public SessionFilePackage GetSessionFile(SessionFileRequest request)
        {
            if (disposed) throw new ObjectDisposedException(nameof(LoadClient));
            return proxy.GetSessionFile(request);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ICommunicationObject kanal = proxy as ICommunicationObject;
                    if (kanal != null)
                    {
                        try
                        {
                            if (kanal.State == CommunicationState.Faulted)
                            {
                                kanal.Abort();
                                Console.WriteLine("[LoadClient] Dispose: kanal je bio Faulted - Abort.");
                            }
                            else
                            {
                                kanal.Close();
                                Console.WriteLine("[LoadClient] Dispose: kanal lepo zatvoren - Close.");
                            }
                        }
                        catch (CommunicationException)
                        {
                            kanal.Abort();
                            Console.WriteLine("[LoadClient] Dispose: CommunicationException - Abort.");
                        }
                        catch (TimeoutException)
                        {
                            kanal.Abort();
                            Console.WriteLine("[LoadClient] Dispose: TimeoutException - Abort.");
                        }
                        catch (Exception)
                        {
                            kanal.Abort();
                            Console.WriteLine("[LoadClient] Dispose: neocekivana greska - Abort.");
                        }
                    }
                    try
                    {
                        if (factory != null && factory.State != CommunicationState.Closed)
                        {
                            factory.Close();
                        }
                    }
                    catch
                    {
                        factory?.Abort();
                    }
                }
                disposed = true;
            }
        }
        ~LoadClient()
        {
            Dispose(false);
        }
    }
}