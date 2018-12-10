using Newtonsoft.Json;
using System;
using System.Threading;
using WebSocketSharp.Server;

namespace idsip_terminal
{
    public class Program
    {
        private static ManualResetEvent rfidReadEvent = new ManualResetEvent(false);

        public static void Main(string[] args)
        {
            var wssv = new WebSocketServer("ws://localhost:8080");
            wssv.AddWebSocketService<UITerminal>("/service");
            wssv.Start();

            var nfcReader = new RFIDReader();
            nfcReader.ReadEvent += ReadEventRaised;

            while (true)
            {
                rfidReadEvent.Reset();
                UITerminal.confirmTransactionEvent.Reset();
                UITerminal.confirmPaymentEvent.Reset();

                nfcReader.StartRead();

                if (rfidReadEvent.WaitOne())
                {
                    nfcReader.StopRead();

                    wssv.WebSocketServices["/service"].Sessions.Broadcast(JsonConvert.SerializeObject(new Messages
                    {
                        Command = "View",
                        Args = "transaction"
                    }));

                    wssv.WebSocketServices["/service"].Sessions.Broadcast(JsonConvert.SerializeObject(new Messages
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

                    if (UITerminal.confirmTransactionEvent.WaitOne())
                    {
                        wssv.WebSocketServices["/service"].Sessions.Broadcast(JsonConvert.SerializeObject(new Messages
                        {
                            Command = "View",
                            Args = "payment"
                        }));

                        wssv.WebSocketServices["/service"].Sessions.Broadcast(JsonConvert.SerializeObject(new Messages
                        {
                            Command = "Payment",
                            Args = new Payment
                            {
                                Price = 5.0
                            }
                        }));

                        if (UITerminal.confirmPaymentEvent.WaitOne())
                        {
                            wssv.WebSocketServices["/service"].Sessions.Broadcast(JsonConvert.SerializeObject(new Messages
                            {
                                Command = "View",
                                Args = "confirm"
                            }));
                        }
                    }
                }
            }
        }

        static void ReadEventRaised(object sender, EventArgs e)
        {
            rfidReadEvent.Set();
        }
    }
}