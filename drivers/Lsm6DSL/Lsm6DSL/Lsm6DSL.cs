using System;
using Windows.Devices.I2c;

namespace Lsm6DSL
{
    public class Lsm6DSL : IDisposable
    {
        I2cDevice _i2cController;

        /// <summary>
        /// I2C address of the Hts221 device.
        /// </summary>
        private int Address = 0x6A;

        // register
        private const byte INT1_CTRL  = 0x0D;
        private const byte INT2_CTRL  = 0x0E;
        private const byte WHO_AM_I   = 0x0F; // Device identification
        private const byte CTRL1_XL   = 0x10;
        private const byte CTRL2_G    = 0x11;
        private const byte CTRL3_C    = 0x12;
        private const byte STATUS_REG = 0x1E;
        // Temperature output data registers
        private const byte OUT_TEMP_L = 0x20;
        private const byte OUT_TEMP_H = 0x21; 
        // Gyroscope output registers
        private const byte OUTX_L_G = 0x22;
        private const byte OUTX_H_G = 0x23;
        private const byte OUTY_L_G = 0x24;
        private const byte OUTY_H_G = 0x25;
        private const byte OUTZ_L_G = 0x26;
        private const byte OUTZ_H_G = 0x27;
        // Accelerometer output registers
        private const byte OUTX_L_XL = 0x28;
        private const byte OUTX_H_XL = 0x29;
        private const byte OUTY_L_XL = 0x2A;
        private const byte OUTY_H_XL = 0x2B;
        private const byte OUTZ_L_XL = 0x2C;
        private const byte OUTZ_H_XL = 0x2D;

        // STATUS_REG register bits
        private const byte XLDA = 0x1;
        private const byte GDA  = 0x2;
        private const byte TDA  = 0x4;

        private const byte ACC_GYRO_FS_XL_2g = 0x00;
        private const byte ACC_GYRO_FS_XL_16g = 0x04;
        private const byte ACC_GYRO_FS_XL_4g = 0x08;
        private const byte ACC_GYRO_FS_XL_8g = 0x0C;
        private const byte ACC_GYRO_FS_G_250dps = 0x00;
        private const byte ACC_GYRO_FS_G_500dps = 0x04;
        private const byte ACC_GYRO_FS_G_1000dps = 0x08;
        private const byte ACC_GYRO_FS_G_2000dps = 0x0C;

        private const double ACC_SENSITIVITY_FOR_FS_2G  = 0.061; // Sensitivity value for 2 g full scale [mg/LSB]
        private const double ACC_SENSITIVITY_FOR_FS_4G  = 0.122; // Sensitivity value for 4 g full scale [mg/LSB]
        private const double ACC_SENSITIVITY_FOR_FS_8G  = 0.244; // Sensitivity value for 8 g full scale [mg/LSB]
        private const double ACC_SENSITIVITY_FOR_FS_16G = 0.488; // Sensitivity value for 16 g full scale [mg/LSB]
        private const double GYRO_SENSITIVITY_FOR_FS_125DPS  = 04.375; // Sensitivity value for 125 dps full scale [mdps/LSB]
        private const double GYRO_SENSITIVITY_FOR_FS_250DPS  = 08.750; // Sensitivity value for 245 dps full scale [mdps/LSB]
        private const double GYRO_SENSITIVITY_FOR_FS_500DPS  = 17.500; // Sensitivity value for 500 dps full scale [mdps/LSB]
        private const double GYRO_SENSITIVITY_FOR_FS_1000DPS = 35.000; // Sensitivity value for 1000 dps full scale [mdps/LSB]
        private const double GYRO_SENSITIVITY_FOR_FS_2000DPS = 70.000; // Sensitivity value for 2000 dps full scale [mdps/LSB]


        public Lsm6DSL(string i2cBus)
        {
            // start new controller
            _i2cController = I2cDevice.FromId(i2cBus, new I2cConnectionSettings(Address));

            // initial configurations
            InitRegisterConfig();
        }

        /// <summary>
        /// Initialization
        /// Basic initialization. Accelerometer and gyroscope enabled in high-performance mode with 
        /// a ODR (output data rate) of 416Hz
        /// </summary>
        private void InitRegisterConfig()
        {
            // Acc = 416 Hz (High-Performance mode) 
            byte[] config = new byte[1];
            config[0] = 0x60;
            bool result = Write(CTRL1_XL, config);
            // Gyro = 416 Hz (High-Performance mode) 
            config[0] = 0x60;
            result = Write(CTRL2_G, config);
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

        #region Accelerometer
        /// <summary>
        /// Get accelerometer values for the 3 axis already affected with sensor sensitivity
        /// </summary>
        /// <returns>16 bits array with sensor 3 axis raw values</returns>
        public double[] GetACCValues()
        {
            double[] data = new double[3];
            short[] rawData = GetACCRawValues();
            double sensitivity = GetACCSensitivity();

            data[0] = rawData[0] * sensitivity;
            data[1] = rawData[1] * sensitivity;
            data[2] = rawData[2] * sensitivity;

            return data;
        }

        /// <summary>
        /// Get accelerometer sensor sensitivity from CTRL1_XL register
        /// </summary>
        /// <returns>Accelerometer sensitivity value</returns>
        private double GetACCSensitivity()
        {
            byte[] buf = Read(CTRL1_XL, 1);

            switch(buf[0] & 0x0C)
            {
                case ACC_GYRO_FS_XL_2g:
                    return ACC_SENSITIVITY_FOR_FS_2G;
                case ACC_GYRO_FS_XL_4g:
                    return ACC_SENSITIVITY_FOR_FS_4G;
                case ACC_GYRO_FS_XL_8g:
                    return ACC_SENSITIVITY_FOR_FS_8G;
                case ACC_GYRO_FS_XL_16g:
                    return ACC_SENSITIVITY_FOR_FS_16G;
                default:
                    return -1.0;
            }
        }

        /// <summary>
        /// Get accelerometer raw values for the 3 axis
        /// </summary>
        /// <returns>16 bits array with sensor 3 axis raw values</returns>
        private short[] GetACCRawValues()
        {
            short[] data = new short[3];
            byte[] buf = Read(STATUS_REG, 1);
            if ((buf[0] & XLDA) == XLDA)
            {
                buf = Read(OUTX_L_XL, 6);

                // Format the data
                data[0] = (short)(((short)buf[1] << 8) + buf[0]);
                data[1] = (short)(((short)buf[3] << 8) + buf[2]);
                data[2] = (short)(((short)buf[5]) << 8 + buf[4]);

                // Calculate signed decimal value (two's complement format)
                if ((data[0] & 0x8000) == 0x8000)
                {
                    data[0] = twosComp(data[0], 0x7FFF);
                }
                if ((data[1] & 0x8000) == 0x8000)
                {
                    data[1] = twosComp(data[1], 0x7FFF);
                }
                if ((data[2] & 0x8000) == 0x8000)
                {
                    data[2] = twosComp(data[2], 0x7FFF);
                }
            }
            else
                return null;

            return data;
        }
        #endregion

        #region Gyroscope
        /// <summary>
        /// Get gyroscope values for the 3 axis already affected with sensor sensitivity
        /// </summary>
        /// <returns>16 bits array with sensor 3 axis raw values</returns>
        public double[] GetGyroValues()
        {
            double[] data = new double[3];
            short[] rawData = GetGyroRawValues();
            double sensitivity = GetGyroSensitivity();

            data[0] = rawData[0] * sensitivity;
            data[1] = rawData[1] * sensitivity;
            data[2] = rawData[2] * sensitivity;

            return data;
        }

        /// <summary>
        /// Get gyroscope sensor sensitivity from CTRL2_G register
        /// </summary>
        /// <returns>Gyroscope sensitivity value</returns>
        private double GetGyroSensitivity()
        {
            byte[] buf = Read(CTRL2_G, 1);

            switch (buf[0] & 0x0C)
            {
                case ACC_GYRO_FS_G_250dps:
                    return GYRO_SENSITIVITY_FOR_FS_250DPS;
                case ACC_GYRO_FS_G_500dps:
                    return GYRO_SENSITIVITY_FOR_FS_500DPS;
                case ACC_GYRO_FS_G_1000dps:
                    return GYRO_SENSITIVITY_FOR_FS_1000DPS;
                case ACC_GYRO_FS_G_2000dps:
                    return GYRO_SENSITIVITY_FOR_FS_2000DPS;
                default:
                    if ((buf[0] & 0x02) == 0x02)
                        return GYRO_SENSITIVITY_FOR_FS_125DPS;
                    else
                        return -1.0;
            }
        }

        /// <summary>
        /// Get gyroscope raw values for the 3 axis
        /// </summary>
        /// <returns>16 bits array with sensor 3 axis raw values</returns>
        private short[] GetGyroRawValues()
        {
            short[] data = new short[3];
            byte[] buf = Read(STATUS_REG, 1);
            if ((buf[0] & GDA) == GDA)
            {
                buf = Read(OUTX_L_G, 6);

                // Format the data
                data[0] = (short)(((short)buf[1] << 8) + buf[0]);
                data[1] = (short)(((short)buf[3] << 8) + buf[2]);
                data[2] = (short)(((short)buf[5]) << 8 + buf[4]);

                // Calculate signed decimal value (two's complement format)
                if ((data[0] & 0x8000) == 0x8000)
                {
                    data[0] = twosComp(data[0], 0x7FFF);
                }
                if ((data[1] & 0x8000) == 0x8000)
                {
                    data[1] = twosComp(data[1], 0x7FFF);
                }
                if ((data[2] & 0x8000) == 0x8000)
                {
                    data[2] = twosComp(data[2], 0x7FFF);
                }
            }
            else
                return null;

            return data;
        }
        #endregion

        public void Dispose()
        {
            // software reset

            // get register
            byte[] config = Read(CTRL3_C, 1);
            // set SW_RESET bit
            config[0] = (byte)(config[0] | 0x1);
            Write(CTRL3_C, config);
        }

        /// <summary>
        ///  Two's complement
        /// </summary>
        /// <param name="value">Value to translate</param>
        /// <param name="mask">Mask to apply</param>
        /// <returns></returns>
        private short twosComp(short value, short mask)
        {

            int res = ~(value & mask) + 1;

            return (short)(-1 * (res & mask));

        }
    }
}
