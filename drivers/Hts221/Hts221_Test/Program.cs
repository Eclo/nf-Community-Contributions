using System;
using System.Threading;

namespace Hts221_Test
{
    public class Program
    {
        public static void Main()
        {
            using (Hts221.Hts221 hts221Drv = new Hts221.Hts221("I2C1"))
            {
                for (int i = 0; i < 5; i++)
                {
                    double temp = hts221Drv.GetTemperature();
                    Console.WriteLine(String.Format("Current humidity: {0}%", hts221Drv.GetHumidity().ToString("n2")));
                    Console.WriteLine(String.Format("Current temperature: {0}°C", temp.ToString("n2")));
                    Console.WriteLine(String.Format("Current temperature: {0}°F", ((temp * 1.8) + 32).ToString("n2")));

                    Thread.Sleep(5000);
                }
            }


            // infinite loop to keep main thread active
            for (; ; )
            {
                Thread.Sleep(1000);
            }
        }
    }
}
