using Newtonsoft.Json;
using System;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace idsip_terminal
{
    public class Laputa : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {

        }

        protected override void OnOpen()
        {
            base.OnOpen();
            Send(JsonConvert.SerializeObject(new Messages
            {
                Command = "View",
                Args = "idle"
            }));

            Send(JsonConvert.SerializeObject(new Messages
            {
                Command = "View",
                Args = "transaction"
            }));

            Send(JsonConvert.SerializeObject(new Messages
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

            Send(JsonConvert.SerializeObject(new Messages
            {
                Command = "View",
                Args = "payment"
            }));

            Send(JsonConvert.SerializeObject(new Messages
            {
                Command = "Payment",
                Args = new Payment
                {
                    Price = 5.0
                }
            }));
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var wssv = new WebSocketServer("ws://localhost:8080");
            wssv.AddWebSocketService<Laputa>("/service");
            wssv.Start();

            Console.ReadKey(true);
            wssv.Stop();
        }
    }
 }