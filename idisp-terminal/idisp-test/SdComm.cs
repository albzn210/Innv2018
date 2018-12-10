// #define WITH_TIME

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

/// <summary>
/// Communication functions for Skidata Devices
/// </summary
namespace Skidata.Delite
{
    /// <summary>
    /// function to send log messages to the foreground task
    /// </summary>
    /// <param name="s">message to display in the log window</param>
    public delegate void logfunction(String s);

    /// <summary>
    /// this function will be called on unexpected messages ids set by the constructor
    /// </summary>
    /// <param name="msg">received protocol</param>
    public delegate void msgfunction(byte[] msg);

    /// <summary>
    /// first byte of a message defines protocol type
    /// </summary>
    enum Flags{
        CONTROL_CMD = 0x20,
        SYSTEM_CMD = 0x0
    }

    /// <summary>
    /// interface to the Skidata Comm classes
    /// </summary>
    public interface SdComm
    {
        /// <summary>
        /// connect to the device with either filename (for SDUSB, COMx) or alt_filename (for WinUsb)
        /// opens the file and starts the reader thread
        /// </summary>
        /// <param name="filename">filename for SDUSB or COMx</param>
        /// <param name="alt_filename">alternate filename for WINUSB</param>
        /// <returns>false on error</returns>
        bool connect(String filename, String alt_filename);

        /// <summary>
        /// close the connection and stops the reader thread
        /// </summary>
        void disconnect();

        /// <summary>
        /// enable tracing via logfunction <see cref="logfunction"/>
        /// </summary>
        /// <param name="on">true if traceing should be enabled</param>
        void setTrace(bool on);

        /// <summary>
        /// sends a protocol
        /// </summary>
        /// <param name="data">control byte, command byte, optional data</param>
        /// <param name="len">length of data</param>
        /// <returns>false on error</returns>
        /// <example><code>
        /// byte[] msg = new byte[3];
        /// msg[0] = (byte)Flags.CONTROL_CMD;
        /// msg[1] = (byte)'x'; // set option x
        /// msg[2] = 0x01;
        /// if (connection.send(msg, 3, 500))
        /// {
        ///     // go on
        /// }
        /// else
        /// {
        ///     // handle error
        /// }
        /// </code></example>
        bool send(byte[] data, int len);

        /// <summary>
        /// sends a protocol and waits for an answer
        /// </summary>
        /// <param name="data">control byte, command byte, optional data</param>
        /// <param name="len">length of data</param>
        /// <param name="timeout">max time in millisecs to wait for the answer</param>
        /// <returns>protocol received or null</returns>
        /// <example><code>
        /// byte[] msg = new byte[2];
        /// msg[0] = (byte)Flags.CONTROL_CMD;
        /// msg[1] = (byte)'y'; // get option y
        /// byte[] answer = connection.sendWaitAnswer(msg, 3, 500);
        /// if (answer != null)
        /// {
        ///     // go on
        /// }
        /// else
        /// {
        ///     // handle error
        /// }
        /// </code></example>
        byte[] sendWaitAnswer(byte[] data, int len, int timeout);

        /// <summary>
        /// is this a COM connection
        /// </summary>
        /// <returns>true if its a COM port</returns>
        bool isSerial();

        /// <summary>
        /// sends an enter boot command
        /// </summary>
        /// <returns>false on error</returns>
        bool enterBoot();

        /// <summary>
        /// sends an enter boot bootkernel command
        /// </summary>
        /// <returns>false on error</returns>
        bool enterBootBootkernel();

        /// <summary>
        /// sends a boot data command
        /// </summary>
        /// <param name="addr">the programming address, also the offset to the data[]</param>
        /// <param name="data">the data[]</param>
        /// <param name="len">number of bytes</param>
        /// <returns>false on error</returns>
        bool sendBoot(int addr, byte[] data, int len);

        /// <summary>
        /// sends the exit boot command
        /// </summary>
        /// <returns>false on error</returns>
        bool exitBoot();

    }

    /// <summary>
    /// loads a hexfile into a 4MB byte array
    /// </summary>
    public class Hexfile
    {
        /// <summary>
        /// is the start address of the hexfile or > end_addr on error
        /// </summary>
        public int start_addr;

        /// <summary>
        /// is the end address of the hexfile or 0 on error
        /// </summary>
        public int end_addr;

        /// <summary>
        /// contains the hexfile after successfull loading
        /// </summary>
        public byte[] buffer = new byte[4 * 1024 * 1024];   // 4MB

        int hex2int(string s, int offset, int len)
        {
            int val = 0;
            for (int i = offset; i < offset + len; i++)
            {
                int x;
                switch (s[i])
                {
                    case '0': x = 0; break;
                    case '1': x = 1; break;
                    case '2': x = 2; break;
                    case '3': x = 3; break;
                    case '4': x = 4; break;
                    case '5': x = 5; break;
                    case '6': x = 6; break;
                    case '7': x = 7; break;
                    case '8': x = 8; break;
                    case '9': x = 9; break;
                    case 'A': x = 10; break;
                    case 'B': x = 11; break;
                    case 'C': x = 12; break;
                    case 'D': x = 13; break;
                    case 'E': x = 14; break;
                    case 'F': x = 15; break;
                    case 'a': x = 10; break;
                    case 'b': x = 11; break;
                    case 'c': x = 12; break;
                    case 'd': x = 13; break;
                    case 'e': x = 14; break;
                    case 'f': x = 15; break;
                    default: x = 0; break;
                }
                val <<= 4;
                val += x;
            }
            return val;
        }

        /// <summary>
        /// loads and parses the motorola s-rec file
        /// into <see cref="buffer"/>
        /// </summary>
        /// <param name="name">filename</param>
        public Hexfile(string name)
        {
            string line;
            int addr = 0;
            int count;
            int offset = 0;
            start_addr = 0x7fffffff;
            end_addr = 0;

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = 0xFF;

            FileStream fs = new FileStream(name, FileMode.Open, FileAccess.Read);
            if (fs == null)
                Console.WriteLine("open error");
            StreamReader f = new StreamReader(fs, System.Text.Encoding.ASCII);
            if (f == null)
                Console.WriteLine("stream error");
            while ((line = f.ReadLine()) != null)
            {
                // S1nnaaaadd....ddcc\r\n
                // S2nnaaaaaadd....ddcc\r\n
                // S3nnaaaaaaaadd....ddcc\r\n
                if (line.Length > 2 && line[0] == 'S')
                {
                    count = 0;
                    switch (line[1])
                    {
                        case '0':   // start record
                            break;
                        case '1':   // 16bit address data record
                            count = hex2int(line, 2, 2) - 3;
                            addr = hex2int(line, 4, 4);
                            offset = 8;
                            break;
                        case '2':   // 24 bit address data record
                            count = hex2int(line, 2, 2) - 4;
                            addr = hex2int(line, 4, 6);
                            offset = 10;
                            break;
                        case '3':   // 32 bit address data record
                            count = hex2int(line, 2, 2) - 5;
                            addr = hex2int(line, 4, 8);
                            offset = 12;
                            break;
                        case '7':   // end record 32 bit
                        case '8':   // end record 24 bit
                        case '9':   // end record 16 bit
                            break;
                        default:
                            break;
                    }
                    if (count > 0)
                    {
                        if (addr < start_addr)
                            start_addr = addr;
                        if (addr + count > end_addr)
                            end_addr = addr + count;

                        for (int i = offset; i < offset + 2 * count; i += 2)
                        {
                            byte b = (byte)hex2int(line, i, 2);
                            buffer[addr++] = b;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// implements a trace connection to a sdwinusb device interface
    /// </summary>
    public class SdTrace
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(string lpFileName,
                                        [MarshalAs(UnmanagedType.U4)]FileAccess fileaccess,
                                        [MarshalAs(UnmanagedType.U4)]FileShare fileshare,
                                        int securityattributes,
                                        [MarshalAs(UnmanagedType.U4)]FileMode creationdisposition,
                                        [MarshalAs(UnmanagedType.U4)]FileOptions flags,
                                        IntPtr template);
        Stream file;
        public SdTrace(string filename)
        {
            SafeFileHandle i = CreateFile(filename, FileAccess.ReadWrite, FileShare.None, 0,
                                FileMode.Open, FileOptions.WriteThrough | FileOptions.Asynchronous, IntPtr.Zero);
            if (i.IsInvalid)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            file = new WinUsbStream(i, 1);
            file.ReadTimeout = 1000;
        }
        public int Read(byte[] buffer)
        {
            return file.Read(buffer, 0, buffer.Length);
        }
    }

    /// <summary>
    /// implements the <see cref="SdComm"/> interface for the SDUSB (aka arcnet) protocol
    /// also for WINUSB
    /// </summary>
    public class SdUsb : SdComm
    {
        /// <summary>
        /// standard WIN32 CreateFile() function
        /// </summary>
        /// <param name="lpFileName"></param>
        /// <param name="fileaccess"></param>
        /// <param name="fileshare"></param>
        /// <param name="securityattributes"></param>
        /// <param name="creationdisposition"></param>
        /// <param name="flags"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(string lpFileName,
                                        [MarshalAs(UnmanagedType.U4)]FileAccess fileaccess,
                                        [MarshalAs(UnmanagedType.U4)]FileShare fileshare,
                                        int securityattributes,
                                        [MarshalAs(UnmanagedType.U4)]FileMode creationdisposition,
                                        [MarshalAs(UnmanagedType.U4)]FileOptions flags,
                                        IntPtr template);

        // static int FILE_FLAG_NO_BUFFERING = 0x20000000; 

        Stream file;
        bool running;
        Thread reader;
        logfunction logFun;
        msgfunction msgFun;
        bool trace;
        byte[] answers;
        byte[] msg;
        object key = new object();
        DateTime stamp = DateTime.Now;
        bool winusb = false;

        /// <inheritdoc />
        public bool isSerial()
        {
            return false;
        }

        /// <summary>
        /// create a SdUsb Device with an optional <see cref="logfunction"/>
        /// </summary>
        /// <param name="f">the logfunction or null</param>
        public SdUsb(logfunction f)
        {
            logFun = f;
        }

        /// <summary>
        /// create a SdUsb Device with an optional <see cref="logfunction"/>
        /// and a callback <see cref="msgfunction"/> which will be called if
        /// the answer protocol has a entry in the cmds array
        /// </summary>
        /// <param name="f">the log function</param>
        /// <param name="m">the callback function</param>
        /// <param name="cmds">the answer ids</param>
        public SdUsb(logfunction f, msgfunction m, byte[] cmds)
        {
            logFun = f;
            msgFun = m;
            answers = cmds;
        }

        /// <inheritdoc />
        public bool connect(String filename, String alt_filename)
        {
            if (file == null)
            {
                try
                {
                    SafeFileHandle i = CreateFile(filename, FileAccess.ReadWrite, FileShare.ReadWrite, 0,
                                        FileMode.Open, FileOptions.WriteThrough | FileOptions.Asynchronous, IntPtr.Zero);
                    if (i.IsInvalid)
                    {
                        // look for alternate filename
                        i = CreateFile(alt_filename, FileAccess.ReadWrite, FileShare.None, 0,
                                            FileMode.Open, FileOptions.WriteThrough | FileOptions.Asynchronous, IntPtr.Zero);
                        if (i.IsInvalid)
                            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                        winusb = true;
                    }
                    if (winusb)
                    {
                        file = new WinUsbStream(i, 2);
                    }
                    else
                    {
                        file = new FileStream(i, FileAccess.ReadWrite, 1, true);
                        // file = new FileStream(filename,FileMode.Open, FileAccess.ReadWrite, FileShare.None, 512);
                    }
                    running = true;
                    reader = new Thread(new ThreadStart(ReaderTask));
                    reader.Start();
                    logFun("sdusb connected");
                    return true;
                }
                catch (Exception e)
                {
                    if (logFun != null)
                        logFun(e.Message);
                    return false;
                }
            }
            return false;
        }

        /// <inheritdoc />
        public void disconnect()
        {
            logFun("sdusb: disconnect");
            if (file != null)
            {
                running = false;
                file.Close();
                file = null;
                reader.Interrupt();
            }
        }
        private string byte2string(byte[] buff, int start, int len)
        {
            StringBuilder s = new StringBuilder(30);
            for (int i = start; i < len && i < buff.Length; i++)
            {
                if (buff[i] >= 32 && buff[i] < 127)
                    s.Append((char)buff[i]);
                else
                    s.Append('.');
            }
            return s.ToString();
        }

        void ReaderTask()
        {
            logFun("sdusb: reader task started");
            byte[] rxBuffer = new byte[512];
            while (running)
            {
                try
                {
                    int len = file.Read(rxBuffer, 0, 512);
                    if (len <= 0)
                        break;
                    if (trace && logFun != null)
                    {
                        logFun("recv:" + BitConverter.ToString(rxBuffer, 0, len));
                        logFun("recv:" + byte2string(rxBuffer,0,len));
                    }
                    byte[] m = new byte[len - 2];
                    System.Array.Copy(rxBuffer, 2, m, 0, len - 2);

                    if (answers != null && msgFun != null)
                    {
                        foreach (byte b in answers)
                        {
                            if (rxBuffer[3] == b)
                            {
                                msgFun(m);
                                m = null;
                                break;
                            }
                        }
                    }

                    if (m != null)
                    {
                        lock (key)
                        {
                            msg = m;
                            stamp = DateTime.Now;
                            // Console.Write('>');
                            Monitor.PulseAll(key);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (logFun != null)
                        logFun(e.Message);
                    disconnect();
                    break;
                }
            }
            logFun("sdusb: reader task exited");
        }

        /// <inheritdoc />
        public void setTrace(bool on)
        {
            trace = on;
            if (logFun != null)
                logFun("sdusb: trace is " + trace.ToString());
        }

        /// <inheritdoc />
        public bool send(byte[] data, int len)
        {
            byte[] buffer = new byte[512];

            buffer[0] = 0x10;
            buffer[1] = 0x01;
            System.Array.Copy(data, 0, buffer, 2, len);
            if (trace && logFun != null)
                logFun("send:" + BitConverter.ToString(buffer, 0, len + 2));
            try
            {
                file.Write(buffer, 0, len + 2);
                return true;
            }
            catch (Exception e)
            {
                if (logFun != null)
                    logFun(e.Message);
                running = false;
                disconnect();
                return false;
            }
        }

        /// <inheritdoc />
        public byte[] sendWaitAnswer(byte[] data, int len, int timeout)
        {
#if WITH_TIME
            DateTime start = DateTime.Now;
#endif
            lock (key)
            {
                send(data, len);
                // Console.Write('<');
                if (Monitor.Wait(key, timeout))
                {
#if WITH_TIME
                    TimeSpan ts = DateTime.Now - stamp;
                    Console.WriteLine("OK: "+ts.TotalMilliseconds);
#endif
                    return msg;
                }
            }
#if WITH_TIME
            TimeSpan t = DateTime.Now - start;
            Console.WriteLine("ERROR: "+t.TotalMilliseconds);
#endif
            return null;
        }

        #region sdcom Members

        /// <inheritdoc />
        public bool enterBoot()
        {
            byte[] ans;
            ans = sendWaitAnswer(new byte[] { 0x80, 0x00 }, 2, 1000);
            return ans != null;
        }

        /// <inheritdoc />
        public bool enterBootBootkernel()
        {
            byte[] ans;
            ans = sendWaitAnswer(new byte[] { 0x80, 0x03 }, 2, 1000);
            return ans != null;
        }

        /// <inheritdoc />
        public bool sendBoot(int addr, byte[] data, int len)
        {
            int offset = addr;
            byte[] msg;
            byte[] ans;
            msg = new byte[2 + 4 + 1 + len];
            msg[0] = 0x80;
            msg[1] = 0x01;
            msg[5] = (byte)(addr & 0xff); addr >>= 8;
            msg[4] = (byte)(addr & 0xff); addr >>= 8;
            msg[3] = (byte)(addr & 0xff); addr >>= 8;
            msg[2] = (byte)(addr & 0xff);
            msg[6] = (byte)len;
            System.Array.Copy(data, offset, msg, 7, len);
            ans = sendWaitAnswer(msg, 7 + len, 2000);
            if (ans != null && ans[1] == 0x01)
                return true;
            return false;
        }

        /// <inheritdoc />
        public bool exitBoot()
        {
            byte[] ans;
            ans = sendWaitAnswer(new byte[] { 0x80, 0x02 }, 2, 1000);
            return ans != null;
        }

        #endregion
    }

    /// <summary>
    /// implements the <see cref="SdComm"/> Interface for serial Skidata Devices with the
    /// Sio450 or Serial450 protocols
    /// </summary>
    public class SdSerial : SdComm
    {
        System.IO.Ports.SerialPort file;
        bool running;
        Thread reader;
        logfunction logFun;
        msgfunction msgFun;
        bool trace;
        byte[] answers;
        byte[] msg;
        object key = new object();
        DateTime stamp = DateTime.Now;

        byte device = 31;
        int baudrate = 115200;

        /// <summary>
        /// creates a SdSerial Device 
        /// </summary>
        /// <param name="f">the logfunction</param>
        /// <param name="dev">the device id 0 .. 31</param>
        /// <param name="baudrate">the baudrate</param>
        public SdSerial(logfunction f, int dev, int baudrate)
        {
            logFun = f;
            msgFun = null;
            answers = new byte[0];
            device = (byte)dev;
            this.baudrate = baudrate;
        }

        /// <summary>
        /// <see cref="SdSerial"/>
        /// <see cref="SdUsb"/>
        /// </summary>
        /// <param name="f">logfunction</param>
        /// <param name="m">msgfunction</param>
        /// <param name="cmds">answer ids</param>
        /// <param name="dev">device id 0..31</param>
        /// <param name="baudrate">baudrate</param>
        public SdSerial(logfunction f, msgfunction m, byte[] cmds, int dev, int baudrate)
        {
            logFun = f;
            msgFun = m;
            answers = cmds;
            device = (byte)dev;
            this.baudrate = baudrate;
        }

        #region sdcom Members

        /// <inheritdoc />
        public bool isSerial()
        {
            return true;
        }

        /// <inheritdoc />
        public bool connect(string filename, string alt_filename)
        {
            if (file == null)
            {
                try
                {
                    file = new System.IO.Ports.SerialPort(filename, baudrate, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    file.Open();
                    // file = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 1, true);
                    running = true;
                    reader = new Thread(new ThreadStart(ReaderTask));
                    reader.Start();
                    logFun("sdsio connected");
                    return true;
                }
                catch (Exception e)
                {
                    if (logFun != null)
                        logFun(e.Message);
                    return false;
                }
            }
            return false;
        }

        /// <inheritdoc />
        public void disconnect()
        {
            logFun("sdsio: disconnect");
            if (file != null)
            {
                running = false;
                file.Close();
                file = null;
                if (reader != null)
                    reader.Interrupt();
            }
        }

        /// <inheritdoc />
        public void setTrace(bool on)
        {
            trace = on;
            if (logFun != null)
                logFun("sdsio: trace is " + trace.ToString());
        }

        /// <inheritdoc />
        public bool send(byte[] data, int len)
        {
            bool result = false;
            lock (key)
            {
                sendCmd(data, len);
                // wait for ack
                if (Monitor.Wait(key, 200))
                {
                    result = true;
                }
            }
            return result;
        }

        /// <inheritdoc />
        public byte[] sendWaitAnswer(byte[] data, int len, int timeout)
        {
            msg = null;
            send(data, len);
            lock (key)
            {
                // already received ?
                if (msg != null)
                    return msg;

                // wait for answer
                if (Monitor.Wait(key, timeout))
                {
                    return msg;
                }
            }
            return null;
        }

        #endregion

        const byte SIO450_START = 0xa5;
        const byte SIO450_END = 0x7c;
        const byte SIO450_STUFF = 0xda;

        const byte SIO_ACK = 2;
        const byte SIO_SYSTEM = 5;
        const byte SIO_CMD = 9;
        const byte SIO_DATA = 16;

        const int SIOPOS_FRAME = 0;
        const int SIOPOS_DEST = 1;
        const int SIOPOS_SRC = 2;

        enum STATE { WAITING = 1, RECEIVING, STUFF_RECEIVED };

        STATE rxState = STATE.WAITING;
        byte[] txBuffer = new byte[512];
        int txLen;
        byte txChksum;
        byte[] rxBuffer = new byte[256];
        int rxLen;
        byte rxChksum;

        void dump(string txt, byte[] buffer, int len)
        {
            System.Console.WriteLine(txt + ":" + BitConverter.ToString(buffer, 0, len));
        }

        void putbyte(byte b)
        {

            switch (b)
            {
                case SIO450_START:
                case SIO450_END:
                case SIO450_STUFF:
                    txBuffer[txLen++] = SIO450_STUFF;
                    txChksum += SIO450_STUFF;
                    b -= SIO450_STUFF;
                    break;
            }
            txBuffer[txLen++] = b;
            txChksum += b;
        }

        void putProtocolbyte(byte b)
        {
            txBuffer[txLen++] = b;
            txChksum += b;
        }

        void putBuffer(byte frame, byte dest, byte[] buffer, int offset, int len)
        {
            txChksum = 0;
            txLen = 0;
            putProtocolbyte(SIO450_START);
            putbyte(frame);
            putbyte(dest);
            putbyte((byte)1);

            if (len > 0)
            {
                int i;
                for (i = 0; i < len; i++)
                {
                    putbyte(buffer[offset + i]);
                }
            }
            putbyte((byte)(txChksum ^ 255));
            putProtocolbyte(SIO450_END);
        }

        void ReaderTask()
        {
            logFun("sdsio: reader task started");
            while (running)
            {
                try
                {
                    int len = getBuffer();
                    //dump("recv", rxBuffer, len);
                    if (trace && logFun != null)
                        logFun("recv: " + BitConverter.ToString(rxBuffer, 0, len));

                    // not for me - ignore
                    if (rxBuffer[SIOPOS_DEST] != 0x01)
                        continue;

                    if (rxBuffer[SIOPOS_FRAME] == SIO_ACK)
                    {
                        lock (key)
                        {
                            Monitor.PulseAll(key);
                        }
                        continue;
                    }

                    // ack required ?
                    //if ((rxBuffer[SIOPOS_FRAME] & 1) == 1)
                        sendAck(rxBuffer[2]);

                    // convert back to sdusb format
                    // frame dest src cmd data ...
                    byte[] m = new byte[len - 2];
                    System.Array.Copy(rxBuffer, 3, m, 1, len - 3);
                    m[0] = (byte)(rxBuffer[2] | 0x20);

                    if (answers != null && msgFun != null)
                    {
                        foreach (byte b in answers)
                        {
                            if (rxBuffer[3] == b)
                            {
                                msgFun(m);
                                m = null;
                                break;
                            }
                        }
                    }

                    if (m != null)
                    {
                        lock (key)
                        {
                            msg = m;
                            stamp = DateTime.Now;
                            // Console.Write('>');
                            Monitor.PulseAll(key);
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                    if (logFun != null)
                        logFun(e.Message);
                    disconnect();
                    break;
                }
            }
            logFun("sdsio: reader task exited");
        }

        int getBuffer()
        {
            rxState = STATE.WAITING;
            rxLen = 0;

            while (true)
            {
                byte b;
                int n;
                n = file.ReadByte();
                b = (byte)n;
                switch (rxState)
                {
                    case STATE.WAITING:
                        // other bytes are ignored
                        if (b == SIO450_START)
                        {
                            rxLen = 0;
                            rxChksum = b;
                            rxState = STATE.RECEIVING;
                        }
                        break;
                    case STATE.RECEIVING:
                        // restart the reciver
                        if (b == SIO450_START)
                        {
                            rxLen = 0;
                            rxChksum = b;
                            rxState = STATE.RECEIVING;
                        }
                        else if (b == SIO450_STUFF)
                        {
                            rxChksum += b;
                            rxState = STATE.STUFF_RECEIVED;
                        }
                        else if (b == SIO450_END)
                        {
                            rxState = STATE.WAITING;
                            if ((rxChksum & 0xFF) == 0xFF)
                            {
                                if (rxBuffer[1] == 1)
                                {
                                    //dump("OK", rxBuffer, rxLen);
                                    --rxLen;    // remove chksum byte
                                    return rxLen;   // we got a message
                                }
                                else
                                {
                                    //dump("ign", rxBuffer, rxLen);
                                    continue;
                                }
                            }
                            else
                            {
                                //dump("err", rxBuffer, rxLen);
                                continue;
                            }
                        }
                        else
                        {
                            rxChksum += b;
                            rxBuffer[rxLen++] = b;
                            if (rxLen >= rxBuffer.Length)
                            {
                                rxState = STATE.WAITING;
                                System.Console.WriteLine("getbyte() to max bytes");
                            }
                        }
                        break;
                    case STATE.STUFF_RECEIVED:
                        rxChksum += b;
                        rxBuffer[rxLen++] = (byte)(b + SIO450_STUFF);
                        rxState = STATE.RECEIVING;
                        break;
                }
            }
        }

        /// <summary>
        /// converts a sdusb compliant buffer to sio450/serial450
        /// </summary>
        /// <param name="data"></param>
        /// <param name="len"></param>
        void sendCmd(byte[] data, int len)
        {
            // byte dev = (byte)(data[0] & 0x1F);
            // byte dev = 16;
            byte frame = (data[0] & 0x20) == 0x20 ? SIO_CMD : SIO_SYSTEM;
            putBuffer(frame, device, data, 0, len);

            if (trace && logFun != null)
                logFun("send: " + BitConverter.ToString(data, 0, len));

            file.Write(txBuffer, 0, txLen);
            //dump("sendCmd", txBuffer, txLen);
        }

        void sendAck(byte dev)
        {
            putBuffer(SIO_ACK, dev, null, 0, 0);
            file.Write(txBuffer, 0, txLen);
            //dump("sendAck", txBuffer, txLen);
        }


        #region sdcom Members

        /// <inheritdoc />
        public bool enterBoot()
        {
            byte[] ans;
            ans = sendWaitAnswer(new byte[] { 0x00, 0x05, 0x00 }, 3, 2000);
            return ans != null;
        }

        /// <inheritdoc />
        public bool enterBootBootkernel()
        {
            return false;
        }

        /// <inheritdoc />
        public bool sendBoot(int addr, byte[] data, int len)
        {
            int offset = addr;
            byte[] msg;
            byte[] ans;
            msg = new byte[3 + 4 + 1 + len];
            msg[0] = 0x00;
            msg[1] = 0x05;
            msg[2] = 0x05; // for 32 bit address
            msg[3] = (byte)(addr >> 24);
            msg[4] = (byte)(addr >> 16);
            msg[5] = (byte)(addr >> 8);
            msg[6] = (byte)(addr);
            msg[7] = (byte)len;
            System.Array.Copy(data, offset, msg, 8, len);
            ans = sendWaitAnswer(msg, 8 + len, 2000);
            return ans != null;
        }

        /// <inheritdoc />
        public bool exitBoot()
        {
            byte[] ans;
            ans = sendWaitAnswer(new byte[] { 0x00, 0x05, 0x02 }, 3, 1000);
            return ans != null;
        }

        #endregion
    }
    public class SdOpos : SdComm
    {
        System.IO.Ports.SerialPort file;
        bool running;
        Thread reader;
        logfunction logFun;
        msgfunction msgFun;
        bool trace;
        byte[] answers;
        byte[] msg;
        object key = new object();
        DateTime stamp = DateTime.Now;

        byte device = 31;
        int baudrate = 115200;

        /// <summary>
        /// creates a SdSerial Device 
        /// </summary>
        /// <param name="f">the logfunction</param>
        /// <param name="dev">the device id 0 .. 31</param>
        /// <param name="baudrate">the baudrate</param>
        public SdOpos(logfunction f, int dev, int baudrate)
        {
            logFun = f;
            msgFun = null;
            answers = new byte[0];
            device = (byte)dev;
            this.baudrate = baudrate;
        }

        /// <summary>
        /// <see cref="SdSerial"/>
        /// <see cref="SdUsb"/>
        /// </summary>
        /// <param name="f">logfunction</param>
        /// <param name="m">msgfunction</param>
        /// <param name="cmds">answer ids</param>
        /// <param name="dev">device id 0..31</param>
        /// <param name="baudrate">baudrate</param>
        public SdOpos(logfunction f, msgfunction m, byte[] cmds, int dev, int baudrate)
        {
            logFun = f;
            msgFun = m;
            answers = cmds;
            device = (byte)dev;
            this.baudrate = baudrate;
        }

        #region sdcom Members

        /// <inheritdoc />
        public bool isSerial()
        {
            return true;
        }

        /// <inheritdoc />
        public bool connect(string filename, string alt_filename)
        {
            if (file == null)
            {
                try
                {
                    file = new System.IO.Ports.SerialPort(filename, baudrate, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    file.Open();
                    // file = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 1, true);
                    running = true;
                    reader = new Thread(new ThreadStart(ReaderTask));
                    reader.Start();
                    logFun("sdopos connected");
                    return true;
                }
                catch (Exception e)
                {
                    if (logFun != null)
                        logFun(e.Message);
                    return false;
                }
            }
            return false;
        }

        /// <inheritdoc />
        public void disconnect()
        {
            logFun("sdopos: disconnect");
            if (file != null)
            {
                running = false;
                if (reader != null)
                    reader.Join();
                file.Close();
                file = null;
            }
        }

        /// <inheritdoc />
        public void setTrace(bool on)
        {
            trace = on;
            if (logFun != null)
                logFun("sdopos: trace is " + trace.ToString());
        }

        /// <inheritdoc />
        public bool send(byte[] data, int len)
        {
            bool result = false;
            download = false;
            lock (key)
            {
                sendCmd(data, len);
                // wait for ack
                if (Monitor.Wait(key, 300))
                {
                    result = true;
                }
            }
            return result;
        }

        /// <inheritdoc />
        public byte[] sendWaitAnswer(byte[] data, int len, int timeout)
        {
            msg = null;
            send(data, len);
            lock (key)
            {
                // already received ?
                if (msg != null)
                    return msg;

                // wait for answer
                if (Monitor.Wait(key, timeout))
                {
                    return msg;
                }
            }
            return null;
        }

        #endregion

        /*
         * Opos protocol
         * US <dst> <src> <cmd> data ... <chksum> EOT
         * chksum = ~(dst+src+cmd+data...)
         */

        const byte US = 0x1F;
        const byte STX = 0x02;
        const byte ETX = 0x03;
        const byte EOT = 0x04;
        const byte STUFF = 0xA0;
        const byte ACK = (byte)'Q';
        const byte NAK = (byte)'R';

        enum STATE { WAITING = 1, RECEIVING, STUFF_RECEIVED };

        STATE rxState = STATE.WAITING;
        byte[] txBuffer = new byte[512];
        int txLen;
        byte txChksum;
        byte[] rxBuffer = new byte[256];
        int rxLen;
        byte rxChksum;

        void dump(string txt, byte[] buffer, int len)
        {
            System.Console.WriteLine(txt + ":" + BitConverter.ToString(buffer, 0, len));
        }

        void putbyte(byte b)
        {
            txChksum += b;
            switch (b)
            {
                case STX:
                case ETX:
                case EOT:
                case US:
                case STUFF:
                    txBuffer[txLen++] = STUFF;
                    b += 0x60;
                    break;
            }
            txBuffer[txLen++] = b;
        }

        void putProtocolbyte(byte b)
        {
            txBuffer[txLen++] = b;
        }

        void putBuffer(byte dest, byte[] buffer, int offset, int len)
        {
            txChksum = 0;
            txLen = 0;
            putProtocolbyte(US);
            putbyte(dest);
            putbyte((byte)1);

            if (len > 0)
            {
                int i;
                for (i = 0; i < len; i++)
                {
                    putbyte(buffer[offset + i]);
                }
            }
            putbyte((byte)(~txChksum));
            putProtocolbyte(EOT);
        }

        volatile bool download = false;
        volatile byte ack_byte;

        void ReaderTask()
        {
            logFun("sdopos: reader task started");
            file.ReadTimeout = 100;
            while (running)
            {
                try
                {
                    int len = getBuffer();
                    if (len <= 0)
                        break;
                    //dump("recv", rxBuffer, len);
                    if (trace && logFun != null)
                        logFun("recv:" + BitConverter.ToString(rxBuffer, 0, len));

                    // not for me - ignore
                    if (rxBuffer[0] != 1)
                        continue;

                    if (rxBuffer[2] == ACK)
                    {
                        lock (key)
                        {
                            Monitor.PulseAll(key);
                        }
                        continue;
                    }

                    // ack required ?
                    // if ((rxBuffer[SIOPOS_FRAME] & 1) == 1)
                    sendAck(rxBuffer[1]);

                    // convert back to sdusb format
                    // dest src cmd data ...
                    byte[] m = new byte[len - 1];
                    System.Array.Copy(rxBuffer, 2, m, 1, len - 2);
                    m[0] = (byte)(rxBuffer[1] | 0x20);

                    if (answers != null && msgFun != null)
                    {
                        foreach (byte b in answers)
                        {
                            if (rxBuffer[3] == b)
                            {
                                msgFun(m);
                                m = null;
                                break;
                            }
                        }
                    }

                    if (m != null)
                    {
                        lock (key)
                        {
                            msg = m;
                            stamp = DateTime.Now;
                            // Console.Write('>');
                            Monitor.PulseAll(key);
                        }
                    }
                }
                catch (ObjectDisposedException e)
                {
                    break;
                }
                catch (TimeoutException)
                {
                    // do nothing
                }
                catch (Exception e)
                {
                    //disconnect();
                    break;
                }
            }
            logFun("sdopos: reader task exited");
        }

        int getBuffer()
        {
            rxState = STATE.WAITING;
            rxLen = 0;

            while (running)
            {
                byte b;
                int n;
                n = file.ReadByte();
                if (n <= 0)
                    return -1;
                b = (byte)n;
                if (download)
                {
                    lock (key)
                    {
                        ack_byte = b;
                        Monitor.PulseAll(key);
                    }
                    continue;
                }
                switch (rxState)
                {
                    case STATE.WAITING:
                        // other bytes are ignored
                        if (b == US)
                        {
                            rxLen = 0;
                            rxChksum = 0;
                            rxState = STATE.RECEIVING;
                        }
                        break;
                    case STATE.RECEIVING:
                        // restart the reciver
                        if (b == US)
                        {
                            rxLen = 0;
                            rxChksum = 0;
                            rxState = STATE.RECEIVING;
                        }
                        else if (b == STUFF)
                        {
                            rxState = STATE.STUFF_RECEIVED;
                        }
                        else if (b == EOT)
                        {
                            rxState = STATE.WAITING;
                            if (rxChksum == (byte)0xFF)
                            {
                                if (rxBuffer[0] == 1)
                                {
                                    //dump("OK", rxBuffer, rxLen);
                                    --rxLen;    // remove chksum byte
                                    return rxLen;   // we got a message
                                }
                                else
                                {
                                    //dump("ign", rxBuffer, rxLen);
                                    continue;
                                }
                            }
                            else
                            {
                                //dump("err", rxBuffer, rxLen);
                                continue;
                            }
                        }
                        else
                        {
                            rxChksum += b;
                            rxBuffer[rxLen++] = b;
                            if (rxLen >= rxBuffer.Length)
                            {
                                rxState = STATE.WAITING;
                                System.Console.WriteLine("getbyte() to max bytes");
                            }
                        }
                        break;
                    case STATE.STUFF_RECEIVED:
                        b -= 0x60;
                        rxChksum += b;
                        rxBuffer[rxLen++] = (byte)b;
                        rxState = STATE.RECEIVING;
                        break;
                }
            }
            return 0;
        }

        /// <summary>
        /// converts a sdusb compliant buffer to sio450/serial450
        /// </summary>
        /// <param name="data"></param>
        /// <param name="len"></param>
        void sendCmd(byte[] data, int len)
        {
            if (data[0] < 32)
                device = data[0];
            putBuffer(device, data, 1, len - 1);
            file.Write(txBuffer, 0, txLen);
            //dump("sendCmd", txBuffer, txLen);
        }

        void sendAck(byte dev)
        {
            putBuffer(dev, new byte[]{ACK}, 0, 1);
            file.Write(txBuffer, 0, txLen);
            //dump("sendAck", txBuffer, txLen);
        }


        #region sdcom Members

        /*
         * enter boot via 'd' command
         * the send S0 Record len and address and data and chksum is binary
         * answer is a plain ACK (0x06) (0x25 on error)
         * then the S2-records line by line
         * answer is a plain ACK (0x06) (0x25 on error)
         * exit boot sends an end record (S7,S8 or S9)
         * answer is a plain ACK (0x06) NAK on error (0x15 on error)
         */
        bool sendBootCmd(byte[] data, int len)
        {
            bool result = false;
            lock (key)
            {
                ack_byte = 0;
                file.Write(data, 0, len);
                Monitor.Wait(key, 2000);
                result = ack_byte == 0x06;
            }
            return result;
        }
        /// <inheritdoc />
        public bool enterBoot()
        {
            if (send(new byte[] { 0x20, (byte)'d' }, 2))
            {
                download = true;
                lock (key)
                {
                    ack_byte = 0;
                    Monitor.Wait(key, 2000);
                }
                byte[] srec = new byte[6];
                srec[0] = (byte)'S';
                srec[1] = (byte)'0';
                srec[2] = 3;
                srec[3] = 0;
                srec[4] = 0;
                srec[5] = (byte)(255 - 3);
                return sendBootCmd(srec, 6);
            }
            return false;
        }

        /// <inheritdoc />
        public bool enterBootBootkernel()
        {
            return false;
        }

        /// <inheritdoc />
        public bool sendBoot(int addr, byte[] data, int len)
        {
            int offset = addr;
            byte[] srec = new byte[len+7];
            srec[0] = (byte)'S';
            srec[1] = (byte)'2';
            srec[2] = (byte)(len + 4);
            srec[3] = (byte)(addr >> 16);
            srec[4] = (byte)(addr >> 8);
            srec[5] = (byte)(addr);
            System.Array.Copy(data, offset, srec, 6, len);
            len += 6;
            int chksum = 0;
            for (int i = 2; i < len; i++)
                chksum += srec[i];
            srec[len++] = (byte)(255 - chksum);
            return sendBootCmd(srec, len);
        }

        /// <inheritdoc />
        public bool exitBoot()
        {
            byte[] srec = new byte[8];
            srec[0] = (byte)'S';
            srec[1] = (byte)'8';
            srec[2] = 4;
            srec[3] = 0;
            srec[4] = 0;
            srec[5] = 0;
            srec[6] = 0;
            srec[7] = (byte)(255 - 4);
            bool result = sendBootCmd(srec, 8);
            download = false;
            return result;
        }

        #endregion
    }

    /// <summary>
    ///  These declarations are translated from the C declarations in various files
    ///  in the Windows DDK. The files are:
    ///  
    ///  winddk\6001\inc\api\usb.h
    ///  winddk\6001\inc\api\usb100.h
    ///  winddk\6001\inc\api\winusbio.h
    ///  
    ///  (your home directory and release number may vary)
    ///  
    ///  refer to the WINSDK documentation for WinUsb
    /// <summary>
    public class WinUsbDevice
    {
        public const UInt32 DEVICE_SPEED = ((UInt32)(1));
        public const byte USB_ENDPOINT_DIRECTION_MASK = ((byte)(0X80));

        public enum POLICY_TYPE
        {
            SHORT_PACKET_TERMINATE = 1,
            AUTO_CLEAR_STALL,
            PIPE_TRANSFER_TIMEOUT,
            IGNORE_SHORT_PACKETS,
            ALLOW_PARTIAL_READS,
            AUTO_FLUSH,
            RAW_IO,
        }

        public enum USBD_PIPE_TYPE
        {
            UsbdPipeTypeControl,
            UsbdPipeTypeIsochronous,
            UsbdPipeTypeBulk,
            UsbdPipeTypeInterrupt,
        }

        public enum USB_DEVICE_SPEED
        {
            UsbLowSpeed = 1,
            UsbFullSpeed,
            UsbHighSpeed,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct USB_CONFIGURATION_DESCRIPTOR
        {
            public byte bLength;
            public byte bDescriptorType;
            public ushort wTotalLength;
            public byte bNumInterfaces;
            public byte bConfigurationValue;
            public byte iConfiguration;
            public byte bmAttributes;
            public byte MaxPower;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct USB_INTERFACE_DESCRIPTOR
        {
            public byte bLength;
            public byte bDescriptorType;
            public byte bInterfaceNumber;
            public byte bAlternateSetting;
            public byte bNumEndpoints;
            public byte bInterfaceClass;
            public byte bInterfaceSubClass;
            public byte bInterfaceProtocol;
            public byte iInterface;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINUSB_PIPE_INFORMATION
        {
            public USBD_PIPE_TYPE PipeType;
            public byte PipeId;
            public ushort MaximumPacketSize;
            public byte Interval;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct WINUSB_SETUP_PACKET
        {
            public byte RequestType;
            public byte Request;
            public ushort Value;
            public ushort Index;
            public ushort Length;
        }

        [DllImport("winusb.dll", SetLastError = true)]
        public static extern Boolean WinUsb_ControlTransfer(IntPtr InterfaceHandle, WINUSB_SETUP_PACKET SetupPacket, byte[] Buffer, UInt32 BufferLength, ref UInt32 LengthTransferred, IntPtr Overlapped);

        [DllImport("winusb.dll", SetLastError = true)]
        public static extern Boolean WinUsb_Free(IntPtr InterfaceHandle);

        [DllImport("winusb.dll", SetLastError = true)]
        public static extern Boolean WinUsb_Initialize(SafeFileHandle DeviceHandle, ref IntPtr InterfaceHandle);

        [DllImport("winusb.dll", SetLastError = true)]
        public static extern Boolean WinUsb_GetAssociatedInterface(IntPtr DeviceHandle, byte interface_index, ref IntPtr InterfaceHandle);

        //  Use this declaration to retrieve DEVICE_SPEED (the only currently defined InformationType).

        [DllImport("winusb.dll", SetLastError = true)]
        public static extern Boolean WinUsb_QueryDeviceInformation(IntPtr InterfaceHandle, UInt32 InformationType, ref UInt32 BufferLength, ref byte Buffer);

        [DllImport("winusb.dll", SetLastError = true)]
        public static extern Boolean WinUsb_QueryInterfaceSettings(IntPtr InterfaceHandle, byte AlternateInterfaceNumber, ref USB_INTERFACE_DESCRIPTOR UsbAltInterfaceDescriptor);

        [DllImport("winusb.dll", SetLastError = true)]
        public static extern Boolean WinUsb_QueryPipe(IntPtr InterfaceHandle, byte AlternateInterfaceNumber, byte PipeIndex, ref WINUSB_PIPE_INFORMATION PipeInformation);

        [DllImport("winusb.dll", SetLastError = true)]
        public static extern Boolean WinUsb_ReadPipe(IntPtr InterfaceHandle, byte PipeID, byte[] Buffer, UInt32 BufferLength, ref UInt32 LengthTransferred, IntPtr Overlapped);

        //  Two declarations for WinUsb_SetPipePolicy. 
        //  Use this one when the returned Value is a byte (all except PIPE_TRANSFER_TIMEOUT):

        [DllImport("winusb.dll", SetLastError = true)]
        public static extern Boolean WinUsb_SetPipePolicy(IntPtr InterfaceHandle, byte PipeID, UInt32 PolicyType, UInt32 ValueLength, ref byte Value);

        //  Use this alias when the returned Value is a UInt32 (PIPE_TRANSFER_TIMEOUT only):

        [DllImport("winusb.dll", SetLastError = true, EntryPoint = "WinUsb_SetPipePolicy")]
        public static extern Boolean WinUsb_SetPipePolicy1(IntPtr InterfaceHandle, byte PipeID, UInt32 PolicyType, UInt32 ValueLength, ref UInt32 Value);

        [DllImport("winusb.dll", SetLastError = true)]
        public static extern Boolean WinUsb_WritePipe(IntPtr InterfaceHandle, byte PipeID, byte[] Buffer, UInt32 BufferLength, ref UInt32 LengthTransferred, IntPtr Overlapped);
    }

    /// <summary>
    /// do nothing device may be used to avoid null pointer dereferencing
    /// </summary>
    public class SdNull : SdComm
    {
        #region sdcom Members

        /// <inheritdoc />
        public bool isSerial()
        {
            return false;
        }

        /// <inheritdoc />
        public bool connect(string filename, string alt_filename)
        {
            return false;
        }

        /// <inheritdoc />
        public void disconnect()
        {
        }

        /// <inheritdoc />
        public void setTrace(bool on)
        {
        }

        /// <inheritdoc />
        public bool send(byte[] data, int len)
        {
            return false;
        }

        /// <inheritdoc />
        public byte[] sendWaitAnswer(byte[] data, int len, int timeout)
        {
            return null;
        }

        /// <inheritdoc />
        public bool enterBoot()
        {
            return false;
        }

        /// <inheritdoc />
        public bool enterBootBootkernel()
        {
            return false;
        }

        /// <inheritdoc />
        public bool sendBoot(int addr, byte[] data, int len)
        {
            return false;
        }

        /// <inheritdoc />
        public bool exitBoot()
        {
            return false;
        }

        #endregion
    }

    /// <summary>
    /// <see cref="Stream"/> implementation for the WinUsbDevice API
    /// used by the <see cref="SdUsb"/> class 
    /// </summary>
    public class WinUsbStream : Stream
    {
        SafeFileHandle file_handle;
        IntPtr winusb_handle;
        byte in_pipe;
        byte out_pipe = 255;

        public WinUsbStream(SafeFileHandle filehandle, int endpoints)
        {
            file_handle = filehandle;
            if (!WinUsbDevice.WinUsb_Initialize(filehandle, ref winusb_handle))
                throw new Exception("can't initialize WinUsb");
            WinUsbDevice.USB_INTERFACE_DESCRIPTOR interface_desc = new WinUsbDevice.USB_INTERFACE_DESCRIPTOR();
            if (!WinUsbDevice.WinUsb_QueryInterfaceSettings(winusb_handle, 0, ref interface_desc))
            {
                Close();
                throw new Exception("could not get interface decriptor");
            }

            if (interface_desc.bNumEndpoints != endpoints)
            {
                IntPtr alt_handle = new IntPtr();
                if (!WinUsbDevice.WinUsb_GetAssociatedInterface(winusb_handle, (byte)0, ref alt_handle))
                {
                    Close();
                    throw new Exception("no second interface found");
                }
                if (!WinUsbDevice.WinUsb_QueryInterfaceSettings(alt_handle, 0, ref interface_desc))
                {
                    Close();
                    throw new Exception("could not get interface decriptor");
                }
                if (interface_desc.bNumEndpoints != endpoints)
                {
                    Close();
                    throw new Exception("second interface has not 2 endpoints");
                }
                WinUsbDevice.WinUsb_Free(winusb_handle);
                winusb_handle = alt_handle;
            }
            for (int i = 0; i < interface_desc.bNumEndpoints; i++)
            {
                WinUsbDevice.WINUSB_PIPE_INFORMATION pipe_info = new WinUsbDevice.WINUSB_PIPE_INFORMATION();
                WinUsbDevice.WinUsb_QueryPipe(winusb_handle, (byte)0, (byte)i, ref pipe_info);
                if (pipe_info.PipeType == WinUsbDevice.USBD_PIPE_TYPE.UsbdPipeTypeBulk)
                {
                    byte bval;
                    uint lval;
                    if ((pipe_info.PipeId & (byte)0x80) == (byte)0x80)
                    {
                        in_pipe = pipe_info.PipeId;
                        bval = 0;
                        WinUsbDevice.WinUsb_SetPipePolicy(winusb_handle, in_pipe, (uint)WinUsbDevice.POLICY_TYPE.IGNORE_SHORT_PACKETS, 1, ref bval);
                    }
                    else
                    {
                        out_pipe = pipe_info.PipeId;
                        bval = 1;
                        WinUsbDevice.WinUsb_SetPipePolicy(winusb_handle, out_pipe, (uint)WinUsbDevice.POLICY_TYPE.SHORT_PACKET_TERMINATE, 1, ref bval);
                        lval = 1000;
                        WinUsbDevice.WinUsb_SetPipePolicy1(winusb_handle, out_pipe, (uint)WinUsbDevice.POLICY_TYPE.PIPE_TRANSFER_TIMEOUT, 4, ref lval);
                    }
                }
            }
        }

        override
        public int Read(byte[] buffer, int offset, int len)
        {
            uint count = 0;
            WinUsbDevice.WinUsb_ReadPipe(winusb_handle, in_pipe, buffer, (uint)len, ref count, System.IntPtr.Zero);
            return (int)count;
        }

        override
        public void Write(byte[] buffer, int offset, int len)
        {
            uint count = 0;
            if (out_pipe != 255)
                WinUsbDevice.WinUsb_WritePipe(winusb_handle, out_pipe, buffer, (uint)len, ref count, System.IntPtr.Zero);
        }

        override
        public void Close()
        {
            WinUsbDevice.WinUsb_Free(winusb_handle);
            file_handle.Close();
        }

        override
         public bool CanRead
        {
            get { return true; }
        }

        // If CanSeek is false, Position, Seek, Length, and SetLength should throw.
        override
         public bool CanSeek
        {
            get { return false; }
        }
        override
         public bool CanWrite
        {
            get { return true; }
        }

        override
         public long Length
        {
            get { return 0; }
        }

        override
         public long Position
        {
            get { return 0; }
            set { }
        }

        override
        public void Flush()
        {
        }

        override
        public void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }
    }
}