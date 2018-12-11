using System;

namespace idsip_terminal
{
    class NfcReaderReadEventArgs : EventArgs
    {
        public byte[] Message { get; private set; }

        public NfcReaderReadEventArgs(byte[] Message)
        {
            this.Message = new byte[9];
            Array.Copy(Message, 5, this.Message, 0, 9);
            BitConverter.ToString(this.Message, 0, 9);
        }
    }
}
