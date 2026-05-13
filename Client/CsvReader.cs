using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.ServiceModel;

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
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("CSV fajl je prazan ili nema header."));
            }

            string[] headers = headerLine.Split(',');

            int idxUtc = Array.IndexOf(headers, "utc_timestamp");
            int idxCet = Array.IndexOf(headers, "cet_cest_timestamp");

            string actualKolona = $"{countryCode}_load_actual_entsoe_transparency";
            string forecastKolona = $"{countryCode}_load_forecast_entsoe_transparency";
            int idxActual = Array.IndexOf(headers, actualKolona);
            int idxForecast = Array.IndexOf(headers, forecastKolona);

            
            if (idxUtc == -1 || idxCet == -1)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault(
                        "CSV nema obavezne timestamp kolone (utc_timestamp / cet_cest_timestamp)."));
            }

            
            if (idxActual == -1 || idxForecast == -1)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault(
                        $"Za zemlju '{countryCode}' nedostaju kolone " +
                        $"'{actualKolona}' i/ili '{forecastKolona}'."));
            }

            Console.WriteLine($"[CsvReader] Pronadjene kolone za '{countryCode}': " +
                              $"actual={idxActual}, forecast={idxForecast}");

           
            string line;
            int rowIndex = 0;
            double kumulativMWh = 0;
            int maxIdx = Math.Max(Math.Max(idxUtc, idxCet), Math.Max(idxActual, idxForecast));

            while ((line = reader.ReadLine()) != null)
            {
                rowIndex++;
                string[] cols = line.Split(',');

                
                if (cols.Length <= maxIdx)
                {
                    OdbaciRed(rowIndex, "Nedovoljan broj kolona", line);
                    continue;
                }

               
                if (!DateTime.TryParse(cols[idxUtc],
                                       CultureInfo.InvariantCulture,
                                       DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal,
                                       out DateTime utcTime))
                {
                    OdbaciRed(rowIndex, "Neispravan utc_timestamp", line);
                    continue;
                }

                
                if (utcTime.Date != selectedDate.Date)
                {
                    continue;
                }

               
                if (!DateTime.TryParse(cols[idxCet],
                                       CultureInfo.InvariantCulture,
                                       DateTimeStyles.RoundtripKind,
                                       out DateTime cetTime))
                {
                    OdbaciRed(rowIndex, "Neispravan cet_cest_timestamp", line);
                    continue;
                }

                string actualStr = cols[idxActual];
                string forecastStr = cols[idxForecast];

                if (string.IsNullOrWhiteSpace(actualStr) ||
                    actualStr.Equals("NaN", StringComparison.OrdinalIgnoreCase))
                {
                    OdbaciRed(rowIndex, "Prazno polje ili NaN u ActualMW", line);
                    continue;
                }
                if (string.IsNullOrWhiteSpace(forecastStr) ||
                    forecastStr.Equals("NaN", StringComparison.OrdinalIgnoreCase))
                {
                    OdbaciRed(rowIndex, "Prazno polje ili NaN u ForecastMW", line);
                    continue;
                }

                if (!double.TryParse(actualStr, NumberStyles.Float,
                                     CultureInfo.InvariantCulture, out double actualMW))
                {
                    OdbaciRed(rowIndex, "Nevalidan broj u ActualMW", line);
                    continue;
                }
                if (!double.TryParse(forecastStr, NumberStyles.Float,
                                     CultureInfo.InvariantCulture, out double forecastMW))
                {
                    OdbaciRed(rowIndex, "Nevalidan broj u ForecastMW", line);
                    continue;
                }

               
                double energyMWh = actualMW * 0.25;
                kumulativMWh += energyMWh;

                LoadSample sample = new LoadSample(
                    utcTime,
                    cetTime,
                    actualMW,
                    forecastMW,
                    kumulativMWh,
                    countryCode,
                    rowIndex);

                rezultat.Add(sample);
            }

            Console.WriteLine($"[CsvReader] Parsirano {rezultat.Count} validnih uzoraka za dan " +
                              $"{selectedDate:yyyy-MM-dd}.");
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