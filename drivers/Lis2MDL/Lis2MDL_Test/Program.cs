using System;
using System.Threading;

namespace Lis2MDL_Test
{
    public class Program
    {
        public static void Main()
        {
            char[] magneticAxis = { 'X', 'Y', 'Z' };
            using (Lis2MDL.Lis2MDL lis2MDLDrv = new Lis2MDL.Lis2MDL("I2C1"))
            {
                double[] data = lis2MDLDrv.GetData();
                for (int i = 0; i < data.Length; i++)
                {
                    Console.WriteLine(String.Format("Reading from magnetic {0}: {1}mG", magneticAxis[i], data[i].ToString("n2")));
                }
            }

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
