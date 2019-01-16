using System;
using System.Threading;

namespace Lps22HB_Test
{
    public class Program
    {
        public static void Main()
        {
            using (Lps22HB.Lps22HB lps22HBDrv = new Lps22HB.Lps22HB("I2C1", Lps22HB.FIFOMode.Bypass))
            {
                for (; ; )
                {
                    Console.WriteLine(String.Format("Pressure: {0}mBar", lps22HBDrv.GetPressure()));
                    Console.WriteLine(String.Format("Temperature: {0}°C", lps22HBDrv.GetTemperature()));

                    Thread.Sleep(5000);
                }
            }

            //Thread.Sleep(Timeout.Infinite);
        }
    }
}
