using System;
using Windows.Devices.I2c;

namespace Lps22HB
{
    public class Lps22HB : IDisposable
    {
        I2cDevice _i2cController;

        /// <summary>
        /// I2C address of the Hts221 device.
        /// </summary>
        private int Address = 0x5C;

        // register
        private const byte WHO_AM_I  = 0x0F; // Device identification
        private const byte CTRL_REG1 = 0x10; // Control register 1
        private const byte CTRL_REG2 = 0x11; // Control register 2
        private const byte FIFO_CTRL = 0x14; // FIFO control

        private const byte PRESS_OUT_XL = 0x28;
        private const byte PRESS_OUT_L  = 0x29;
        private const byte PRESS_OUT_H  = 0x2A;
        private const byte TEMP_OUT_L   = 0x2B;
        private const byte TEMP_OUT_H   = 0x2C;

        private const int PRESSURE_SCALE    = 4096;
        private const int TEMPERATURE_SCALE = 100;


        public Lps22HB(string i2cBus, FIFOMode mode)
        {
            // start new controller
            _i2cController = I2cDevice.FromId(i2cBus, new I2cConnectionSettings(Address));

            // initial configurations
            InitRegisterConfig(mode);
        }

        private void InitRegisterConfig(FIFOMode mode)
        {
            byte[] config = new byte[1];

            switch(mode)
            {
                case FIFOMode.Bypass:
                    // ODR = 25 Hz(continuous mode), LPF active with ODR/9, BDU activ
                    config[0] = 0x3A;
                    Write(CTRL_REG1, config);
                    // FIFO OFF and Multiple reading ON 
                    config[0] = 0x10;
                    Write(CTRL_REG2, config);
                    break;
                case FIFOMode.FIFO:
                case FIFOMode.Stream:
                case FIFOMode.StreamToFifo:
                case FIFOMode.BypassToStream:
                case FIFOMode.BypassToFifo:
                case FIFOMode.DynamicStream:
                    // not implemented
                    throw new NotImplementedException();
            }
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

            var readResult = _i2cController.WriteReadPartial(new byte[] { regAddr }, buffer);

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

        public void Dispose()
        {
            // Clear register to default value (Power off)
            byte[] config = { 0x0 };
            Write(CTRL_REG1, config);
        }

        /// <summary>
        /// Get pressure value.
        /// </summary>
        /// <returns>Pressure value in mBar</returns>
        public double GetPressure()
        {
            byte[] data = Read(PRESS_OUT_XL, 3);
            int raw = ((data[2] << 16) | (data[1] << 8)) | data[0];
            if((raw & 0x800000) == 0x800000)
            {
                raw = twosComp(raw, 0x7FFFFF);
            }

            return raw / PRESSURE_SCALE;
        }

        /// <summary>
        /// Get temperature value.
        /// </summary>
        /// <returns>Temperature value in celsius</returns>
        public double GetTemperature()
        {
            byte[] data = Read(TEMP_OUT_L, 2);
            int raw = (data[1] << 8) | data[0];
            if ((raw & 0x8000) == 0x8000)
            {
                raw = twosComp(raw, 0x7FFF);
            }

            return raw / TEMPERATURE_SCALE;
        }

        /// <summary>
        ///  Two's complement
        /// </summary>
        /// <param name="value">Value to translate</param>
        /// <param name="mask">Mask to apply</param>
        /// <returns></returns>
        private int twosComp(int value, int mask)
        {

            int res = ~(value & mask) + 1;

            return -1 * (res & mask);

        }
    }
}
