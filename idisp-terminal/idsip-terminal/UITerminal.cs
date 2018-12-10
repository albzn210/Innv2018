using Newtonsoft.Json;
using System;
using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace idsip_terminal
{
    class UITerminal : WebSocketBehavior
    {
        public static ManualResetEvent confirmTransactionEvent = new ManualResetEvent(false);

        public static ManualResetEvent confirmPaymentEvent = new ManualResetEvent(false);

        protected override void OnMessage(MessageEventArgs e)
        {
            Console.WriteLine("Got command from ui: " + e.Data);

            var command = JsonConvert.DeserializeObject<TerminalCommand>(e.Data);
            switch (command.Action)
            {
                case "ConfirmTransaction":
                    confirmTransactionEvent.Set();
                    break;
                case "ConfirmPayment":
                    confirmPaymentEvent.Set();
                    break;
                default:
                    break;
            }
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            Send(JsonConvert.SerializeObject(new Messages
            {
                Command = "View",
                Args = "idle"
            }));
        }
    }
}
