using System;

namespace idisp_test
{
    class Program
    {
        public static void Main()
        {
            Reader reader = new Reader();
            reader.ReadEvent += ReadEventRaised;

            reader.StartRead();

            System.Threading.Thread.Sleep(100000);
            reader.StopRead();
            reader.Close();
        }

        public static void Log(String s)
        {
            Console.WriteLine("Log: " + s);
        }

        static void ReadEventRaised(object sender, EventArgs e)
        {
            Console.WriteLine("The threshold was reached.");
        }
    }
}
