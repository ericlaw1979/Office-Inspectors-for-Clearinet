using Clearinet;
using System.Windows.Forms;

namespace MapiInspector
{
    public sealed class MAPIRequestInspector : RequestInspectorBase
    {
        private readonly MAPIInspector inspector = new MAPIInspector(MAPIParser.TrafficDirection.In);
        private Exchange exchange;

        public override string TabTitle => "MAPI";
        public override int GetOrder() => inspector.GetOrder();
        public override byte[] body { get => inspector.body; set => inspector.body = value; }

        public override void AddToTab(TabPage tab) => inspector.AddToTab(tab);
        public override void Clear() => inspector.Clear();
        public override int ScoreForExchange(Exchange exchange) => inspector.ScoreForExchange(exchange);

        public override void Assign(HTTPRequestHeaders headers, byte[] body, bool readOnly)
        {
            this.headers = headers;
            bReadOnly = readOnly;
            inspector.AssignExchange(exchange, headers, body, readOnly);
        }

        public override void AssignExchange(Exchange exchange)
        {
            this.exchange = exchange;
            base.AssignExchange(exchange);
        }
    }
}