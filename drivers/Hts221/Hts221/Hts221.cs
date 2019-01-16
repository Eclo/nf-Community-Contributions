using System;
using System.Threading;
using Windows.Devices.I2c;

namespace Hts221
{
    public class Hts221 : IDisposable
    {
        I2cDevice _i2cController;

        /// <summary>
        /// I2C address of the Hts221 device.
        /// </summary>
        private int Address = 0x5F;

        // register
        private const byte WHO_AM_I    = 0x0F; // Device identification
        private const byte AV_CONF_REG = 0x10; // Humidity and temperature resolution mode
        private const byte CTRL_REG1   = 0x20; // Control register 1
        private const byte CTRL_REG2   = 0x21; // Control register 2
        private const byte CTRL_REG3   = 0x22; // Control register 3
        private const byte STATUS_REG  = 0x27; // Status register
        // output register
        private const byte H_OUT       = 0x28; // Relative humidity data (LSB):0x28 and Relative humidity data (MSB):0x29
        private const byte T_OUT       = 0x2A; // Temperature data (LSB):0x2A and Temperature data (MSB):0x2B
        // calibration register
        private const byte H0_rH_x2    = 0x30;
        private const byte H1_rH_x2    = 0x31;
        private const byte T0_degC_x8  = 0x32;
        private const byte T1_degC_x8  = 0x33;
        private const byte T0_T1_MSB   = 0x35;
        private const byte H0_T0_OUT   = 0x36;
        private const byte H1_T0_OUT   = 0x3A;
        private const byte T0_OUT      = 0x3C;
        private const byte T1_OUT      = 0x3E;

        public Hts221(string i2cBus)
        {
            // start new controller
            _i2cController = I2cDevice.FromId(i2cBus, new I2cConnectionSettings(Address));

            // initial configurations
            InitRegisterConfig();
        }

        /// <summary>
        /// Init Hts221 to read temperature and humidity.
        /// </summary>
        private void InitRegisterConfig()
        {
            // Select average configuration register(0x10)

            // Temperature average samples = 16, humidity average samples = 32(0x1B)
            byte[] config = new byte[1];
            config[0] = 0x1B;
            bool result = Write(AV_CONF_REG, config);

            // Select control register1(0x20)

            // Power on, block data update, data rate o/p = 1 Hz(0x85)
            config[0] = 0x85;
            result = Write(CTRL_REG1, config);
        }

        /// <summary>
        /// Read one or multiple bytes from device.
        /// </summary>
        /// <param name="regAddr">Address to read or initial address to read multiple and consecutive bytes</param>
        /// <param name="bytesToRead">Number of bytes to read from <ref><paramref name="regAddr"/></ref></param>
        /// <returns>Byte array with read bytes, otherwise NULL</returns>
        private byte[] Read(byte regAddr, int bytesToRead)
        {
            byte[] buffer = new byte[bytesToRead];

            // In order to read multiple bytes incrementing the register address, it is necessary to assert the register address MSB
            // for 1 byte read it is indifferent
            var readResult = _i2cController.WriteReadPartial(new byte[] { (byte)(regAddr | 0x80) }, buffer);

            return readResult.Status == I2cTransferStatus.FullTransfer ? buffer : null;
        }

        /// <summary>
        /// Write byte(s) to device.
        /// </summary>
        /// <param name="regAddr">Address to write to</param>
        /// <param name="buf">Data to write to <ref><paramref name="regAddr"/></ref></param>
        /// <returns>True if write succeed</returns>
        private bool Write(byte regAddr, byte[] buf)
        {
            byte[] buffer = new byte[buf.Length + 1];
            buffer[0] = regAddr;

            Array.Copy(buf, 0, buffer, 1, buf.Length);

            var writeResult = _i2cController.WritePartial(buffer);

            return writeResult.Status == I2cTransferStatus.FullTransfer;
        }

        /// <summary>
        /// Get humidity value.
        /// </summary>
        /// <returns>Humidity value</returns>
        public double GetHumidity()
        {
            // Read Calibration values from the non-volatile memory of the device

            // Humidity Calibration values

            //Read 2 byte of data from address(0x30 and 0x31)
            byte[] reg = Read(H0_rH_x2, 2);
            byte data_0 = reg[0];
            byte data_1 = reg[1];

            int H0 = data_0 / 2;
            int H1 = data_1 / 2;

            //Read 2 byte of data from address(0x36 and 0x37)
            reg = Read(H0_T0_OUT, 2);
            data_0 = reg[0];
            data_1 = reg[1];

            int H2 = (data_1 * 256) + data_0;

            //Read 2 byte of data from address(0x3A and 0x3B)
            reg = Read(H1_T0_OUT, 2);
            data_0 = reg[0];
            data_1 = reg[1];

            int H3 = (data_1 * 256) + data_0;

            // Read 2 bytes of data (0x28 and 0x29)
            // hum lsb, hum msb
            byte[] data = Read(H_OUT, 2);

            // Convert the data
            int hum = (data[1] * 256) + data[0];
            return ((1.0 * H1) - (1.0 * H0)) * (1.0 * hum - 1.0 * H2) / (1.0 * H3 - 1.0 * H2) + (1.0 * H0);
        }

        /// <summary>
        /// Get temperature value.
        /// </summary>
        /// <returns>Temperature value</returns>
        public double GetTemperature()
        {
            // Read Calibration values from the non-volatile memory of the device

            // Temperature Calibration values

            // Read 2 byte of data from address(0x32 and 0x33)
            byte[] reg = Read(T0_degC_x8, 2);
            int T0 = reg[0];
            int T1 = reg[1];

            // Read 1 byte of data from address(0x35)
            reg = Read(T0_T1_MSB, 1);
            int raw = reg[0];

            // Convert the temperature Calibration values to 10-bits
            T0 = ((raw & 0x03) * 256) + T0;
            T1 = ((raw & 0x0C) * 64) + T1;

            //Read 2 byte of data from address(0x3C and 0x3D)
            reg = Read(T0_OUT, 2);
            byte data_0 = reg[0];
            byte data_1 = reg[1];

            int T2 = (data_1 * 256) + data_0;

            //Read 2 byte of data from address(0x3E and 0x3F)
            reg = Read(T1_OUT, 2);
            data_0 = reg[0];
            data_1 = reg[1];

            int T3 = (data_1 * 256) + data_0;

            // Read 2 bytes of data(0x2A and 0x2B)
            // temp lsb, temp msb
            byte[] data = Read(T_OUT, 2);
            int temp = (data[1] * 256) + data[0];
            // check for negative value
            if (temp > 32767)
            {
                temp -= 65536;
            }
            return ((T1 - T0) / 8.0) * (temp - T2) / (T3 - T2) + (T0 / 8.0);
        }

        public void Dispose()
        {
            // Clear register to default value (Power off)
            byte[] config = { 0x0 };
            Write(CTRL_REG1, config);
        }
    }
}
