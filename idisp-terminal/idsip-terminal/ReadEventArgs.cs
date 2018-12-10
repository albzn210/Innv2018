using System;

namespace idsip_terminal
{
    class ReadEventArgs : EventArgs
    {
        public byte[] Message { get; private set; }

        public ReadEventArgs(byte[] Message)
        {
            this.Message = Message;
        }
    }
}
