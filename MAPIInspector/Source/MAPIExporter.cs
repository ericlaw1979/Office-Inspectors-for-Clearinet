using Clearinet;
using System;
using System.Collections.Generic;

namespace MapiInspector
{
    [OfferFormat("MAPI", "Parsed MAPI exchanges")]
    public sealed class MAPIExporter : IExchangeExporter
    {
        public bool ExportExchanges(string format, Exchange[] exchanges, Dictionary<string, object> options,
            EventHandler<ProgressEventArgs> progress)
        {
            string filePath = options != null && options.TryGetValue("FilePath", out object value) ? value as string : null;
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Clearinet.Utilities.ObtainSaveFilename("Export As " + format, "JSON File (*.json)|*.json");
            }
            if (string.IsNullOrEmpty(filePath)) return false;

            try
            {
                return MAPIParser.ParseCaptureFile(exchanges, filePath);
            }
            catch (Exception ex)
            {
                CApp.ReportException(ex, "MAPI export failed");
                return false;
            }
        }

        public void Dispose() { }
    }
}