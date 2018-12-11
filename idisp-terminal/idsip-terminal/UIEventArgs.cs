using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace idsip_terminal
{
    enum UIEvent
    {
        ConfirmTransaction,
        DeclineTransaction,
        ConfirmPayment,
        DeclinePayment,
        ConfirmAll
    }

    class UIEventArgs : EventArgs
    {
        public UIEvent UIEvent { get; set; }
    }
}
