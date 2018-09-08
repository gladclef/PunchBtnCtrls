using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsSnapshots
{
    /// <summary>
    /// Bit Bashing class
    /// </summary>
    class BB
    {
        public static int UIntToDecimalBytes(ref byte[] bytes, int start, uint val, int padZeroesMinLength)
        {
            return IntToDecimalBytes(ref bytes, start, val, 0, padZeroesMinLength);
        }

        public static int SIntToDecimalBytes(ref byte[] bytes, int start, int val, int padZeroesMinLength)
        {
            return IntToDecimalBytes(ref bytes, start, 0, val, padZeroesMinLength);
        }

        public static int IntToDecimalBytes(ref byte[] bytes, int start, uint uval, int sval, int padZeroesMinLength)
        {
            string strVal;
            if (uval != 0)
                strVal = uval.ToString();
            else
                strVal = sval.ToString();
            int padLength = Math.Max(padZeroesMinLength - strVal.Length, 0);
            int idx = start;
            for (int i = 0; i < padLength; i++)
            {
                bytes[idx++] = 0;
            }
            for (int i = 0; i < strVal.Length; i++)
            {
                bytes[idx++] = Convert.ToByte(strVal[i]);
            }
            return idx - start;
        }

        public static int UIntToHexBytes(ref byte[] bytes, int start, uint val, int minLength, int maxLength)
        {
            return IntToHexBytes(ref bytes, start, val, 0, minLength, maxLength);
        }

        public static int SIntToHexBytes(ref byte[] bytes, int start, int val, int minLength, int maxLength)
        {
            return IntToHexBytes(ref bytes, start, 0, val, minLength, maxLength);
        }

        public static int IntToHexBytes(ref byte[] bytes, int start, uint uval, int sval, int minLength, int maxLength)
        {
            if (minLength > maxLength)
                throw new ArgumentException("Minimum hex length must be less than or equal to maximum hex length");

            byte[] valBytes;
            if (uval != 0)
                valBytes = BitConverter.GetBytes(uval);
            else
                valBytes = BitConverter.GetBytes(sval);
            int zeroes = leadingZeroes(valBytes);
            int nonZeroes = valBytes.Length - zeroes;
            int padLength = Math.Max(minLength - nonZeroes, 0);
            int idx = start;
            for (int i = 0; i < padLength; i++)
            {
                bytes[idx++] = 0;
            }
            for (int i = 0; i < nonZeroes && i < maxLength; i++)
            {
                bytes[idx++] = valBytes[valBytes.Length - zeroes - 1 + i];
            }
            return idx - start;
        }

        public static int leadingZeroes(byte[] bytes, bool littleEndian = false)
        {
            if (littleEndian)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (bytes[i] != 0)
                        return i;
                }
            }
            else
            {
                for (int i = bytes.Length-1; i >= 0; i--)
                {
                    if (bytes[i] != 0)
                        return (bytes.Length - i - 1);
                }
            }
            return bytes.Length;
        }
    }
}
