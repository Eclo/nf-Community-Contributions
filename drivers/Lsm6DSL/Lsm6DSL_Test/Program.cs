using System;
using System.Threading;

namespace Lsm6DSL_Test
{
    public class Program
    {
        public static void Main()
        {
            char[] axis = { 'X', 'Y', 'Z' };
            using (Lsm6DSL.Lsm6DSL lsm6DSL = new Lsm6DSL.Lsm6DSL("I2C1"))
            {
                for (; ; )
                {
                    double[] dataAcc = lsm6DSL.GetACCValues();
                    for (int i = 0; i < dataAcc.Length; i++)
                    {
                        Console.WriteLine(String.Format("Reading from accelerometer {0}: {1}", axis[i], dataAcc[i].ToString("n2")));
                    }
                    double[] dataGyro = lsm6DSL.GetGyroValues();
                    for (int i = 0; i < dataAcc.Length; i++)
                    {
                        Console.WriteLine(String.Format("Reading from gyroscope {0}: {1}", axis[i], dataGyro[i].ToString("n2")));
                    }
                    Console.WriteLine("");
                    Thread.Sleep(5000);
                }
            }

            //Thread.Sleep(Timeout.Infinite);
        }
    }
}
