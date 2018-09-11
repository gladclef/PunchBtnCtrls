using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsSnapshots
{
    public class ImageMagic
    {
        public static Palette CalculatePalette(Bitmap img, int paletteSize)
        {
            Palette palette = new Palette();
            bool first = true;

            // sanity check
            if (paletteSize < 8)
            {
                throw new ArgumentException("Palette size must be at least 8.");
            }
            if (paletteSize > 256)
            {
                throw new ArgumentException("Palette size must be at most 256.");
            }

            // reduce the mask length until we have the right number of colors
            do
            {
                // generate smaller masks for the next run
                if (first)
                    palette.ReduceSpectrum();
                first = false;

                // count the number of colors, maybe there are only paletteSize colors?!
                for (int x = 0; x < img.Width; x++)
                {
                    for (int y = 0; y < img.Height; y++)
                    {
                        if (palette.map.Count >= 255)
                            break;
                        palette.GetPaletteColor(img.GetPixel(x, y));
                    }
                    if (palette.map.Count > paletteSize)
                        break;
                }
            } while (palette.map.Count > paletteSize);

            return palette;
        }

        /// <summary>
        /// color encoding:
        ///  - bits: RRRR RGGG GGGB BBBB
        ///  - hex: R=F800 G=07C0 B=001F
        /// </summary>
        /// <param name="color24">The 24 bit color value (assumes bytes 0, 1, and 2 are colors red, green, and blue)</param>
        /// <returns>The 16 bit color.</returns>
        public static ushort ColorConvert24To16(int color24, byte rMask = 0xF8, byte gMask = 0xFC, byte bMask = 0xF8)
        {
            byte[] colorBytes = BitConverter.GetBytes(color24);
            return ColorConvert24To16(colorBytes, rMask, gMask, bMask);
        }
        
        /// <summary>
        /// <see cref="ColorConvert24To16(int, byte, byte, byte)"/>
        /// </summary>
        public static ushort ColorConvert24To16(Color color24, byte rMask = 0xF8, byte gMask = 0xFC, byte bMask = 0xF8)
        {
            byte[] colorBytes = new byte[4];
            colorBytes[0] = color24.R;
            colorBytes[1] = color24.G;
            colorBytes[2] = color24.B;
            return ColorConvert24To16(colorBytes, rMask, gMask, bMask);
        }

        /// <summary>
        /// <see cref="ColorConvert24To16(int, byte, byte, byte)"/>
        /// </summary>
        public static ushort ColorConvert24To16(byte[] color24, byte rMask = 0xF8, byte gMask = 0xFC, byte bMask = 0xF8)
        {
            ushort color16 = 0;

            color16 |= Convert.ToUInt16((color24[0] & rMask) << 8); // red
            color16 |= Convert.ToUInt16((color24[1] & gMask) << 3); // green
            color16 |= Convert.ToUInt16((color24[2] & bMask) >> 3); // blue

            return color16;
        }

        public static int ColorConvertColorToInt(Color color24)
        {
            byte[] colorBytes = new byte[4];
            colorBytes[0] = color24.R;
            colorBytes[1] = color24.G;
            colorBytes[2] = color24.B;
            return BitConverter.ToInt32(colorBytes, 0);
        }

        public static int ColorConvertRGBTo24(uint r, uint g, uint b)
        {
            byte[] bc = new byte[4];
            bc[0] = Convert.ToByte(r);
            bc[1] = Convert.ToByte(g);
            bc[2] = Convert.ToByte(b);
            return BitConverter.ToInt32(bc, 0);
        }
    }

    public class Palette
    {
        byte rMask = 0xE0, gMask = 0xF0, bMask = 0xE0;
        ushort cMask = 0xE000 | (0xF0 << 3) | (0xE0 >> 3);
        bool reduceGreenMask = true;
        public Dictionary<ushort, byte> map = new Dictionary<ushort, byte>();

        /// <summary>
        /// Reduces the masks used to produce a color map.
        /// Also clears the map.
        /// </summary>
        public void ReduceSpectrum()
        {
            if (reduceGreenMask)
            {
                gMask = Convert.ToByte((gMask & 0x7F) << 1);
            }
            else
            {
                rMask = Convert.ToByte((rMask & 0x7F) << 1);
                bMask = Convert.ToByte((bMask & 0x7F) << 1);
            }
            cMask = (ushort)((rMask << 8) | (gMask << 3) | (bMask >> 3));
            map.Clear();
        }

        /// <summary>
        /// Gets the mapped color, adding the given color as necessary.
        /// </summary>
        /// <returns>The index of the given color in the map.</returns>
        public byte GetPaletteColor(Color val)
        {
            return GetPaletteColor(ImageMagic.ColorConvert24To16(val));
        }
        
        /// <summary>
        /// Gets the mapped color, adding the given color as necessary.
        /// </summary>
        /// <returns>The index of the given color in the map.</returns>
        public byte GetPaletteColor(ushort val)
        {
            ushort maskedVal = GetReducedColor(val);

            // get the value if it exists
            if (map.ContainsKey(maskedVal))
                return map[maskedVal];

            // add the value, then return the new value
            byte mappedVal = (byte)map.Count;
            map[maskedVal] = mappedVal;
            return mappedVal;
        }

        /// <summary>
        /// Gets the color that the given val maps to in the reduced color space.
        /// </summary>
        public ushort GetReducedColor(ushort val)
        {
            return (ushort)(val & cMask);
        }
    }
}
