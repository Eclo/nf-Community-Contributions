using nanoframework.i2c.SSD1306;
using System.Threading;

namespace OLED_SSD1306_Test
{
    public class Program
    {
        public static void Main()
        {
            // turn off RGB Led
            Windows.Devices.Gpio.GpioController gpio = new Windows.Devices.Gpio.GpioController();
            var gpioPB3 = gpio.OpenPin(16 + 3);
            gpioPB3.SetDriveMode(Windows.Devices.Gpio.GpioPinDriveMode.Output);
            gpioPB3.Write(Windows.Devices.Gpio.GpioPinValue.Low);

            var gpioPB4 = gpio.OpenPin(16 + 4);
            gpioPB4.SetDriveMode(Windows.Devices.Gpio.GpioPinDriveMode.Output);
            gpioPB4.Write(Windows.Devices.Gpio.GpioPinValue.Low);

            var gpioPC7 = gpio.OpenPin(16 + 16 + 7);
            gpioPC7.SetDriveMode(Windows.Devices.Gpio.GpioPinDriveMode.Output);
            gpioPC7.Write(Windows.Devices.Gpio.GpioPinValue.Low);


            //Using SSD1306 OLED display
            OLED oled = new OLED("I2C1", 0x3C);
            // init display
            oled.Initialize();

            for (byte i = 57; i > 48; i--)
            {
                oled.ShowChar(60, 2, i);
                Thread.Sleep(500);
            }
            oled.Clear();

            oled.ShowString(0, 3, "nanoFramework   is awesome !!!");

            for (; ; )
            {
                Thread.Sleep(800);
                oled.DisplayOff();
                Thread.Sleep(300);
                oled.DisplayOn();
            }
        }
    }
}
