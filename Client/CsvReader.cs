using Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace Client
{
    public class CsvReader : IDisposable
    {
        StreamReader reader;
        StreamWriter rejectedWriter;
        bool disposed;
        string filePath;
        string rejectedPath;

        public CsvReader(string filePath, string rejectedPath)
        {
            this.FilePath = filePath;
            this.RejectedPath = rejectedPath;
            this.disposed = false;

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV fajl ne postoji: {filePath}");
            }

            this.reader = new StreamReader(filePath);
            this.rejectedWriter = new StreamWriter(rejectedPath);
            this.rejectedWriter.WriteLine("RowIndex,Reason,OriginalLine");

            Console.WriteLine($"[CsvReader] Otvoren CSV: {filePath}");
            Console.WriteLine($"[CsvReader] Rejected fajl: {rejectedPath}");
        }

        public string FilePath { get => filePath; set => filePath = value; }
        public string RejectedPath { get => rejectedPath; set => rejectedPath = value; }
        public List<LoadSample> ReadSamples(string countryCode, DateTime selectedDate)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(CsvReader));
            }

            List<LoadSample> rezultat = new List<LoadSample>();

            string headerLine = reader.ReadLine();
            if (headerLine == null)
            {
                throw new InvalidDataException("CSV fajl je prazan ili nema header.");
            }

            Console.WriteLine($"[CsvReader] Header procitan, broj kolona: {headerLine.Split(',').Length}");

            return rezultat;
        }

        public void OdbaciRed(int rowIndex, string razlog, string original)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(CsvReader));
            }
            string safeOriginal = original.Replace("\"", "\"\"");
            rejectedWriter.WriteLine($"{rowIndex},\"{razlog}\",\"{safeOriginal}\"");
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
                    reader?.Dispose();
                    rejectedWriter?.Dispose();
                    Console.WriteLine("[CsvReader] Dispose: resursi oslobodjeni.");
                }
                disposed = true;
            }
        }

        ~CsvReader()
        {
            Dispose(false);
        }
    }
}