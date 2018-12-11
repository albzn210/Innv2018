using Newtonsoft.Json;
using System;
using System.Threading;
using WebSocketSharp.Server;

namespace idsip_terminal
{
    public class Program
    {
        private static ManualResetEvent rfidReadEvent = new ManualResetEvent(false);

        private static ManualResetEvent confirmTransactionEvent = new ManualResetEvent(false);

        private static ManualResetEvent confirmPaymentEvent = new ManualResetEvent(false);

        private static ManualResetEvent confirmAllEvent = new ManualResetEvent(false);

        public static void Main(string[] args)
        {
            var wssv = new WebSocketServer("ws://localhost:8080");
            wssv.AddWebSocketService<UITerminal>("/service");
            wssv.Start();

            UITerminal.UiEvent += UICommandFired;
            WebSocketSessionManager session = wssv.WebSocketServices["/service"].Sessions;

            var nfcReader = new RFIDReader();
            nfcReader.ReadEvent += ReadEventRaised;

            while (true)
            {
                rfidReadEvent.Reset();
                confirmTransactionEvent.Reset();
                confirmPaymentEvent.Reset();
                confirmAllEvent.Reset();

                nfcReader.StartRead();

                if (rfidReadEvent.WaitOne())
                {
                    nfcReader.StopRead();

                    session.Broadcast(JsonConvert.SerializeObject(new Messages
                    {
                        Command = "View",
                        Args = "transaction"
                    }));

                    if (confirmAllEvent.WaitOne())
                    {
                        session.Broadcast(JsonConvert.SerializeObject(new Messages
                        {
                            Command = "View",
                            Args = "idle"
                        }));
                    }

                    /*session.Broadcast(JsonConvert.SerializeObject(new Messages
                    {
                        Command = "Transaction",
                        Args = new Transaction
                        {
                            TransactionID = "1234567",
                            LP = "BGL K 714",
                            Price = 5.0,
                            Time = DateTime.Now
                        }
                    }));

                    if (confirmTransactionEvent.WaitOne())
                    {
                        session.Broadcast(JsonConvert.SerializeObject(new Messages
                        {
                            Command = "View",
                            Args = "payment"
                        }));

                        session.Broadcast(JsonConvert.SerializeObject(new Messages
                        {
                            Command = "Payment",
                            Args = new Payment
                            {
                                Price = 5.0
                            }
                        }));

                        if (confirmPaymentEvent.WaitOne())
                        {
                            session.Broadcast(JsonConvert.SerializeObject(new Messages
                            {
                                Command = "View",
                                Args = "confirm"
                            }));

                            if (confirmAllEvent.WaitOne())
                            {
                                session.Broadcast(JsonConvert.SerializeObject(new Messages
                                {
                                    Command = "View",
                                    Args = "idle"
                                }));
                            }
                        }
                    }*/
                }
            }
        }

        static void ReadEventRaised(object sender, EventArgs e)
        {
            var args = e as NfcReaderReadEventArgs;
            rfidReadEvent.Set();
        }

        static void UICommandFired(object sender, EventArgs e)
        {
            var args = e as UIEventArgs;
            switch (args.UIEvent)
            {
                case UIEvent.ConfirmTransaction:
                    confirmTransactionEvent.Set();
                    break;
                case UIEvent.DeclineTransaction:
                    break;
                case UIEvent.ConfirmPayment:
                    confirmPaymentEvent.Set();
                    break;
                case UIEvent.DeclinePayment:
                    break;
                case UIEvent.ConfirmAll:
                    confirmAllEvent.Set();
                    break;
                default:
                    break;
            }

        }
    }
}