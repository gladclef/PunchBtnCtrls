using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsSnapshots
{
    /// <summary>
    /// Collection to do all the bit bashing stuff that would normally be done in lower level languages like C.
    /// </summary>
    class BitBashing
    {
        /// <summary>
        /// Converts the given val with a call to <see cref="IntToDecimalBytes(ref byte[], int, uint, int, int)"/>
        /// </summary>
        public static int UIntToDecimalBytes(ref byte[] bytes, int start, uint val, int padZeroesMinLength)
        {
            return IntToDecimalBytes(ref bytes, start, val, 0, padZeroesMinLength);
        }

        /// <summary>
        /// Converts the given val with a call to <see cref="IntToDecimalBytes(ref byte[], int, uint, int, int)"/>
        /// </summary>
        public static int SIntToDecimalBytes(ref byte[] bytes, int start, int val, int padZeroesMinLength)
        {
            return IntToDecimalBytes(ref bytes, start, 0, val, padZeroesMinLength);
        }

        /// <summary>
        /// Like <see cref="IntToHexBytes(ref byte[], int, uint, int, int, int)"/>, except that the bytes inserted are
        /// decimal (ascii encoded) values.
        /// </summary>
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

        /// <summary>
        /// Converts the given val with a call to <see cref="IntToHexBytes(ref byte[], int, uint, int, int, int)"/>
        /// </summary>
        public static int UIntToHexBytes(ref byte[] bytes, int start, uint val, int minLength, int maxLength)
        {
            return IntToHexBytes(ref bytes, start, val, 0, minLength, maxLength);
        }

        /// <summary>
        /// Converts the given val with a call to <see cref="IntToHexBytes(ref byte[], int, uint, int, int, int)"/>
        /// </summary>
        public static int SIntToHexBytes(ref byte[] bytes, int start, int val, int minLength, int maxLength)
        {
            return IntToHexBytes(ref bytes, start, 0, val, minLength, maxLength);
        }

        /// <summary>
        /// Converts a signed or unsigned integer to a number of bytes in the given bytes array.
        /// The bytes are inserted in order of the system endianness (<see cref="BitConverter.IsLittleEndian"/>).
        /// </summary>
        /// <param name="bytes">The array to stuff bytes into.</param>
        /// <param name="start">The index of bytes to start writing to.</param>
        /// <param name="uval">An unsigned value to convert. If 0, then sval will be used, instead.</param>
        /// <param name="sval">A signed value to convert. Ignored if uval is not zero.</param>
        /// <param name="minLength">The minimum number of bytes to insert. The front will be padded with zeroes to fill up space.</param>
        /// <param name="maxLength">The maximum number of bytes to insert. The most significant bytes of the value are given priority.</param>
        /// <returns></returns>
        public static int IntToHexBytes(ref byte[] bytes, int start, uint uval, int sval, int minLength, int maxLength)
        {
            if (minLength > maxLength)
                throw new ArgumentException("Minimum hex length must be less than or equal to maximum hex length");

            byte[] valBytes;
            if (uval != 0)
                valBytes = BitConverter.GetBytes(uval);
            else
                valBytes = BitConverter.GetBytes(sval);
            int zeroes = CountLeadingZeroes(valBytes, !BitConverter.IsLittleEndian);
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

        /// <summary>
        /// Counts the number of leading zero bytes in the given bytes array.
        /// </summary>
        /// <param name="bytes">The array to count the number of leading zeroes in.</param>
        /// <param name="littleEndian">True if the first byte is the smallest (least significat) value,
        ///                            false if the first byte is the largest (most significant) byte.</param>
        /// <returns>The number of leading bytes that are zeroes (range: 0 - bytes.Length).</returns>
        public static int CountLeadingZeroes(byte[] bytes, bool littleEndian = false)
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
