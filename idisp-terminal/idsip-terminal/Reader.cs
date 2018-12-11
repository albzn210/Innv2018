using Skidata.Delite;
using System;
using System.Threading;

namespace idsip_terminal
{
    class RFIDReader
    {
        public event EventHandler ReadEvent;
        private bool _continue;
        private byte[] read = { 0x4c, 0x00, 0x30, 0x01, 0x00, 0x04 };
        private byte[] commands = { 0x4c, 0x00, 0x30, 0x01, 0x00, 0x04 };
        private SdSerial s;

        public RFIDReader()
        {
            s = new SdSerial((string log) =>
            {
                //Console.WriteLine("Log: " + log);
            },
            (byte[] message) =>
            {
                Console.WriteLine("NFC read with ID: " + BitConverter.ToString(message, 0, message.Length));
                ReadEvent?.Invoke(this, new NfcReaderReadEventArgs(message));
            },
            commands,
            31, 115200);
            s.setTrace(true);
            s.connect("COM6", "");
        }

        public void Close()
        {
            s.disconnect();
        }

        public void StartRead()
        {
            _continue = true;
            Thread readThread = new Thread(Read);
            readThread.Start();
        }

        public void StopRead()
        {
            _continue = false;
        }

        private void Read()
        {
            while (_continue)
            {
                Thread.Sleep(100);
                if (!s.send(read, read.Length))
                {
                    Console.WriteLine("send read not succesful!");
                }
            }
        }
    }
}