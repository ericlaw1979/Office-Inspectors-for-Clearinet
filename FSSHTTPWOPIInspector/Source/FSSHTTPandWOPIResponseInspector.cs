using Clearinet;
using System.Windows.Forms;

namespace FSSHTTPandWOPIInspector
{
    public sealed class FSSHTTPandWOPIResponseInspector : ResponseInspectorBase
    {
        private readonly FSSHTTPandWOPIInspector inspector = new FSSHTTPandWOPIInspector(FSSHTTPandWOPIInspector.TrafficDirection.Out);

        public override string TabTitle => "FSSHTTPandWOPI";
        public override int GetOrder() => inspector.GetOrder();
        public override byte[] body { get => inspector.body; set => inspector.body = value; }

        public override void AddToTab(TabPage tab) => inspector.AddToTab(tab);
        public override void Clear() => inspector.Clear();
        public override int ScoreForExchange(Exchange exchange) => inspector.ScoreForExchange(exchange);

        public override void Assign(HTTPResponseHeaders headers, byte[] body, bool readOnly)
        {
            this.headers = headers;
            bReadOnly = readOnly;
            inspector.Assign(headers, body, readOnly);
        }

        public override void AssignExchange(Exchange exchange)
        {
            inspector.session = exchange;
            base.AssignExchange(exchange);
        }
    }
}