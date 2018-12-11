using Newtonsoft.Json;
using System;
using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace idsip_terminal
{
    class UITerminal : WebSocketBehavior
    {
        public static event EventHandler UiEvent;

        protected override void OnMessage(MessageEventArgs e)
        {
            Console.WriteLine("Got command from ui: " + e.Data);

            var command = JsonConvert.DeserializeObject<TerminalCommand>(e.Data);
            var eventArgs = new UIEventArgs();

            switch (command.Action)
            {
                case "ConfirmTransaction":
                    eventArgs.UIEvent = UIEvent.ConfirmTransaction;
                    break;
                case "DeclineTransaction":
                    eventArgs.UIEvent = UIEvent.DeclineTransaction;
                    break;
                case "ConfirmPayment":
                    eventArgs.UIEvent = UIEvent.ConfirmPayment;
                    break;
                case "DeclinePayment":
                    eventArgs.UIEvent = UIEvent.DeclinePayment;
                    break;
                case "ConfirmAll":
                    eventArgs.UIEvent = UIEvent.ConfirmAll;
                    break;
                default:
                    break;
            }

            UiEvent?.Invoke(this, eventArgs);
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
