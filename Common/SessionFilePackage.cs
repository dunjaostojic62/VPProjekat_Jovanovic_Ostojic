using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class SessionFilePackage : IDisposable
    {
        string fileName;
        MemoryStream content;
        bool disposed;

        public SessionFilePackage(string fileName, MemoryStream content)
        {
            this.FileName = fileName;
            this.Content = content;
            this.disposed = false;
        }

        [DataMember]
        public string FileName { get => fileName; set => fileName = value; }

        [DataMember]
        public MemoryStream Content { get => content; set => content = value; }

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
                    content?.Dispose();
                }
                disposed = true;
            }
        }

        ~SessionFilePackage()
        {
            Dispose(false);
        }
    }
}