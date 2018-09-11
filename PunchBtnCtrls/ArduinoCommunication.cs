using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsSnapshots
{
    /// <summary>
    /// For use with the ScreenManager Arduino program.
    /// </summary>
    public class ArduinoCommunication : AbstractCommunication
    {
        public SerialPort arduinoSerial = null;
        private string serialLineIn = "";
        private bool ackReceived = false;
        private bool waitingForAck = false;

        public ArduinoCommunication(SerialPort arduinoSerial, uint screenWidth, StatusChangedDelegate ConnectedCallback = null) : base(screenWidth, ConnectedCallback)
        {
            this.arduinoSerial = arduinoSerial;
            try
            {
                Connect();
            }
            catch (IOException)
            {
                // do nothing
            }
            catch (UnauthorizedAccessException)
            {
                // do nothing
            }
        }

        /// <summary>
        /// Tries to connect to the assigned port.
        /// </summary>
        /// <exception cref="IOException">If connecting fails.</exception>
        override public void Connect()
        {
            if (isConnected)
                return;

            arduinoSerial.Open();//Opening the serial port
            arduinoSerial.DataReceived += arduinoSerial_DataReceived;
            isConnected = true;
            ConnectedCallback?.Invoke();
        }

        /// <summary>
        /// Disconnects from the assigned port.
        /// </summary>
        /// <exception cref="IOException">If disconnecting fails.</exception>
        override public void Disconnect()
        {
            if (!isConnected)
                return;

            isConnected = false;
            try
            {
                arduinoSerial.Close();
            }
            finally
            {
                DisconnectedCallback?.Invoke();
            }
        }
        
        private void arduinoSerial_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            string s;
            try
            {
                s = arduinoSerial.ReadExisting();//reads the serialport buffer
            }
            catch (InvalidOperationException)
            {
                Disconnect();
                return;
            }
            if (s.Length == 0)
                return;
            
            if (s.Contains("\n"))
            {
                string[] parts = s.Split(new char[] { '\n', '\r' });
                for (int i = 0; i < parts.Length-1; i++)
                {
                    serialLineIn += parts[i];
                    if (serialLineIn.Length > 0)
                        ReceivedLine(serialLineIn);
                    serialLineIn = "";
                }
                if (s[s.Length-1] == '\n')
                {
                    serialLineIn += parts[parts.Length-1];
                    if (serialLineIn.Length > 0)
                        ReceivedLine(serialLineIn);
                    serialLineIn = "";
                }
            }
            else
            {
                serialLineIn += s;
            }
        }

        private void ReceivedLine(string line)
        {
            //Debug.WriteLine(line);
            ReceivedLineCallback?.Invoke(line);
            if (line.Trim().Equals("ACK"))
            {
                ackReceived = true;
            }
            else
            {
                Console.WriteLine(line);
            }
        }

        public override bool SendImageRow(int[] line, uint startRow, uint rowSpan, BtnProps btn)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            int idx = 0;

            // check that WIDTH is a multiple of the line length
            uint colSpan = screenWidth / (uint)line.Length;
            if (line.Length * colSpan != screenWidth)
            {
                throw new ArgumentException($"The display width ({screenWidth}) must be a multiple of the line length ({line.Length})");
            }

            // send packetized data
            if (startRow == 0)
            {
                byte[] restart = new byte[62];                // buffer to be sent
                idx = BitBashing.UIntToDecimalBytes(ref restart, 0, 2, 0);
                restart[idx++] = Convert.ToByte(':');
                restart[idx++] = Convert.ToByte('R');         // "Reset Draw" command
                restart[idx++] = Convert.ToByte('L');
                if (waitingForAck) WaitForAck(1000);
                if (!CheckedWrite(restart, 0, idx))
                    return false;
                waitingForAck = true;
            }

            // send colSpan and rowSpan
            byte[] spans = new byte[62];                      // buffer to be sent
            idx = BitBashing.SIntToDecimalBytes(ref spans, 0, 5, 0);
            spans[idx++] = Convert.ToByte(':');
            spans[idx++] = Convert.ToByte('S');               // "Span Size" command
            idx += BitBashing.UIntToHexBytes(ref spans, idx, colSpan, 2, 2);
            idx += BitBashing.UIntToHexBytes(ref spans, idx, rowSpan, 2, 2);
            if (waitingForAck) WaitForAck(1000);
            if (!CheckedWrite(spans, 0, idx))
                return false;
            waitingForAck = true;

            // determine the necessity of a palette and send the palette
            bool usePalette = btn.palette != null && colSpan < 16 && rowSpan < 16;
            if (usePalette && startRow == 0 && btn.doUpdatePalette)
            {
                SendColorPalette(btn.palette);
                btn.SetDoUpdatePalette(false);
            }
            usePalette = false;

            // send packetized line color data
            byte[] linePart = new byte[62];                   // buffer to be sent
            int linePartLen;                                  // packetized line message length
            int bytesPerPixel = (usePalette) ? 1 : 2;
            linePartLen = (int)(56 / bytesPerPixel);
            char command = (usePalette) ? 'l' : 'L';
            for (int i = 0; i < line.Length; i += linePartLen)
            {
                int sendCnt = Math.Min(linePartLen, line.Length - i);

                // prepare the message header
                int pixelCnt = (usePalette) ? sendCnt : sendCnt * bytesPerPixel;
                idx = BitBashing.SIntToDecimalBytes(ref linePart, 0, pixelCnt + 1, 0);
                linePart[idx++] = Convert.ToByte(':');
                linePart[idx++] = Convert.ToByte(command);    // "Line Part" or "line (palette) Part" command

                // add the color values
                for (int j = 0; j < sendCnt; j++)
                {
                    int col = idx + j * bytesPerPixel;
                    ushort color16 = ImageMagic.ColorConvert24To16(line[i + j]);
                    if (usePalette)
                    {
                        linePart[col] = btn.palette.GetPaletteColor(color16);
                    }
                    else
                    {
                        linePart[col + 1] = BitConverter.GetBytes(color16)[0];
                        linePart[col + 0] = BitConverter.GetBytes(color16)[1];
                    }
                }

                // send the message
                if (waitingForAck) WaitForAck(1000);
                if (!CheckedWrite(linePart, 0, pixelCnt + idx))
                    return false;
                waitingForAck = true;
            }

            //Console.WriteLine("c#: " + timer.ElapsedMilliseconds);

            return true;
        }

        private bool CheckedWrite(byte[] toWrite, int offset, int sendCnt)
        {
            if (!isConnected)
                return false;

            try
            {
                arduinoSerial.Write(toWrite, offset, sendCnt);
            }
            catch (ArgumentNullException ee)
            {
                isConnected = false;
                return false;
            }
            catch (InvalidOperationException ee)
            {
                isConnected = false;
                return false;
            }
            catch (ArgumentOutOfRangeException ee)
            {
                isConnected = false;
                return false;
            }
            catch (ArgumentException ee)
            {
                isConnected = false;
                return false;
            }

            return true;
        }

        private void WaitForAck(int millsWait)
        {
            Stopwatch timeout = new Stopwatch();
            timeout.Start();
            while (!ackReceived && timeout.ElapsedMilliseconds < millsWait) { }
            ackReceived = false;
            waitingForAck = false;
        }

        public override bool SendString(string data, bool includeNewline = false)
        {
            try
            {
                if (includeNewline)
                {
                    arduinoSerial.WriteLine(data);
                }
                else
                {
                    arduinoSerial.Write(data);
                }
            }
            catch (Exception ee)
            {
                if (ee is ArgumentNullException || ee is InvalidOperationException || ee is TimeoutException)
                {
                    isConnected = false;
                    return false;
                }
                throw ee;
            }

            return true;
        }

        /// <summary>
        /// 
        /// Creates a left-to-right gradient image to send to the Arduino.
        /// Milliseconds taken to draw the entire image at was measured for several baud rates as follows:
        /// 
        ///   CSxRS @115200 @250000
        ///     1x1   10000    7300
        ///     2x2    3400    2500
        ///     2x3    2300    1760
        ///     4x3    1900    1060
        ///     4x4    1400     790
        ///     5x3    1350       ?
        ///     8x3    1240       ?
        ///     5x4    1000       ?
        ///     5x5     860       ?
        ///     8x4     920       ?
        ///     8x5     750       ?
        ///     8x6     630       ?
        ///     8x7     550       ?
        ///     8x8     460       ?
        ///   10x10     325     320
        ///   20x20     250     190
        ///   160x1    2100    1100
        ///   160x2    1050     530
        ///   160x4     530     400
        /// 160x128       ?      90
        /// 
        /// </summary>
        /// <param name="colSpan">The number of columns to send as a single color.</param>
        /// <param name="rowSpan">The number of rows to send as a single color.</param>
        public override void SendTestImage(uint colSpan, uint rowSpan)
        {
            int[] line = new int[screenWidth / colSpan];
            byte[] colorBytes = new byte[4];
            for (uint i = 0; i < line.Length; i++)
            {
                uint effectiveColor = i * colSpan + ((colSpan - 1) / 2);
                byte color = Convert.ToByte(effectiveColor * 255 / screenWidth);
                colorBytes[0] = color;
                colorBytes[1] = color;
                colorBytes[2] = color;
                colorBytes[3] = color;
                line[i] = BitConverter.ToInt32(colorBytes, 0);
            }

            Stopwatch timer = new Stopwatch();
            timer.Restart();
            for (uint i = 0; i < 128; i += rowSpan)
            {
                SendImageRow(line, i, rowSpan, null);
            }
            Console.WriteLine(timer.ElapsedMilliseconds);
        }

        public override bool SendColorPalette(Palette palette)
        {
            int idx = 0;

            // send restart message
            //if (btn.paletteWriteIdx == 0)
            {
                byte[] restart = new byte[62];                // buffer to be sent
                idx = BitBashing.UIntToDecimalBytes(ref restart, 0, 2, 0);
                restart[idx++] = Convert.ToByte(':');
                restart[idx++] = Convert.ToByte('R');         // "Reset Palette" command
                restart[idx++] = Convert.ToByte('P');
                if (waitingForAck) WaitForAck(1000);
                if (!CheckedWrite(restart, 0, idx))
                    return false;
                waitingForAck = true;
            }

            // send packetized palette data
            byte[] palettePart = new byte[62];                // buffer to be sent
            int palettePartCnt = Math.Min(28, palette.map.Count); // packetized line message length
            for (int i = 0; i < palette.map.Count; i += palettePartCnt)
            {
                int sendCnt = Math.Min(palettePartCnt, palette.map.Count - i);

                // prepare the message header
                idx = BitBashing.SIntToDecimalBytes(ref palettePart, 0, sendCnt * 2 + 1, 0);
                palettePart[idx++] = Convert.ToByte(':');
                palettePart[idx++] = Convert.ToByte('P');     // "Palette Part" command

                // add the color values
                for (int j = 0; j < sendCnt; j++)
                {
                    ushort color16 = palette.map.ElementAt(i + j).Key;
                    int col = idx + j * 2;
                    palettePart[col + 1] = BitConverter.GetBytes(color16)[0];
                    palettePart[col + 0] = BitConverter.GetBytes(color16)[1];
                }

                // send the message
                if (waitingForAck) WaitForAck(1000);
                if (!CheckedWrite(palettePart, 0, sendCnt * 2 + idx))
                    return false;
                waitingForAck = true;
            }

            return true;
        }
    }
}
