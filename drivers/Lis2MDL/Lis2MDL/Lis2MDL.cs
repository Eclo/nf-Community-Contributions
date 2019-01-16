using System;
using Windows.Devices.I2c;

namespace Lis2MDL
{
    public class Lis2MDL : IDisposable
    {
        I2cDevice _i2cController;

        /// <summary>
        /// I2C address of the Lis2MDL sensor.
        /// </summary>
        private int Address = 0x1E;

        // register
        private const byte WHO_AM_I       = 0x4F; // Device identification
        private const byte CFG_REG_A      = 0x60; // Control register 1
        private const byte CFG_REG_B      = 0x61; // Control register 2
        private const byte CFG_REG_C      = 0x62; // Control register 3
        private const byte STATUS_REG     = 0x67; // Status register
        private const byte OUTX_L_REG     = 0x68; // Magnetic sensor data (6 bytes, x y z)
        private const byte TEMP_OUT_L_REG = 0x6E; // Temperature sensor data (2 bytes)

        public Lis2MDL(string i2cBus)
        {
            // start new controller
            _i2cController = I2cDevice.FromId(i2cBus, new I2cConnectionSettings(Address));

            // initial configurations
            InitRegisterConfig();
        }

        private void InitRegisterConfig()
        {
            byte[] config = new byte[1];

            // Temperature compensation enabled, ODR = 10Hz, continuous and high-resolution modes
            config[0] = 0x80;
            Write(CFG_REG_A, config);
            // enable block data read (bit 4 == 1)
            config[0] = 0x10;
            Write(CFG_REG_C, config);
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

        public void Dispose()
        {
            // reset sensor
            Reset();
        }

        //public int GetTemperature()
        //{
        //    // Read the 2 raw data registers into data array
        //    byte[] rawData = Read(0x80 | TEMP_OUT_L_REG, 2);
        //    // Turn the MSB and LSB into a signed 16-bit value
        //    int temp = (rawData[1] << 8) | rawData[0];
        //    // Calculate signed decimal value (two's complement format)
        //    if ((temp & 0x8000) == 0x8000)
        //    {
        //        temp = twosComp(temp, 0x7FFF);
        //    }
        //    return temp;
        //}

        /// <summary>
        /// Get magnetic sensor axis values x, y and z.
        /// </summary>
        /// <returns>Buffer with the magnetic sensor axis values x, y and z.</returns>
        public double[] GetData()
        {
            int[] values = new int[3];
            double[] result = new double[3];

            // x/y/z mag register data stored here
            // Read the 6 raw data registers into data array
            byte[] rawData = Read(OUTX_L_REG, 6);
            // Turn the MSB and LSB into a signed 16-bit value
            values[0] = (rawData[1] << 8) | rawData[0];
            values[1] = (rawData[3] << 8) | rawData[2];
            values[2] = (rawData[5] << 8) | rawData[4];

            // Calculate signed decimal value (two's complement format)
            if ((values[0] & 0x8000) == 0x8000)
            {
                values[0] = twosComp(values[0], 0x7FFF);
            }
            if ((values[1] & 0x8000) == 0x8000)
            {
                values[1] = twosComp(values[1], 0x7FFF);
            }
            if ((values[2] & 0x8000) == 0x8000)
            {
                values[2] = twosComp(values[2], 0x7FFF);
            }

            // Apply sensitivity
            result[0] = values[0] * 1.5;
            result[1] = values[1] * 1.5;
            result[2] = values[2] * 1.5;

            return result;
        }

        /// <summary>
        /// Reset sensor.
        /// </summary>
        private void Reset()
        {
            // set SOFT_RST bit of the CFG_REG_A register to 1
            byte[] config = { 0x20 };
            Write(CFG_REG_C, config);
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
