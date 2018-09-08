using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsSnapshots
{
    public class ArduinoCommunication
    {
        public SerialPort arduinoSerial = null;
        string serialLineIn = "";
        private bool ackReceived = false;

        public ArduinoCommunication(SerialPort arduinoSerial)
        {
            this.arduinoSerial = arduinoSerial;
            arduinoSerial.Open();//Opening the serial port
            arduinoSerial.DataReceived += arduinoSerial_DataReceived;
        }

        public void arduinoSerial_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            string s = arduinoSerial.ReadExisting();//reads the serialport buffer
            if (s.Length == 0)
                return;
            
            if (s.Contains("\n"))
            {
                string[] parts = s.Split(new char[] { '\n', '\r' });
                for (int i = 0; i < parts.Length-1; i++)
                {
                    serialLineIn += parts[i];
                    if (serialLineIn.Length > 0)
                        LineReceived(serialLineIn);
                    serialLineIn = "";
                }
                if (s[s.Length-1] == '\n')
                {
                    serialLineIn += parts[parts.Length-1];
                    if (serialLineIn.Length > 0)
                        LineReceived(serialLineIn);
                    serialLineIn = "";
                }
            }
            else
            {
                serialLineIn += s;
            }
        }

        private void LineReceived(string line)
        {
            //Debug.WriteLine(line);
            if (line.Trim().Equals("ACK"))
            {
                ackReceived = true;
            }
            else
            {
                Console.WriteLine(line);
            }
        }

        /**
         * Creates a left-to-right gradient image to send to the Arduino.
         * Milliseconds taken to draw the entire image at was measured for several baud rates as follows:
         *  CSxRS @115200 @250000
         *    1x1    0000    7300
         *    2x2    3400    2500
         *    2x3    2300    1760
         *    4x3    1900    1060
         *    4x4    1400     790
         *    5x3    1350       ?
         *    8x3    1240       ?
         *    5x4    1000       ?
         *    5x5     860       ?
         *    8x4     920       ?
         *    8x5     750       ?
         *    8x6     630       ?
         *    8x7     550       ?
         *    8x8     460       ?
         *  10x10     325     320
         *  20x20     250     190
         *  160x1    2100    1100
         *  160x2    1050     530
         *  160x4     530     400
         *160x128       ?      90
         * 
         * @parm colSpan The number of columns to send as a single color.
         * @parm rowSpan The number of rows to send as a single color.
         */
        public void SendTestImage(int colSpan, int rowSpan)
        {
            int[] line = new int[160 / colSpan];
            byte[] colorBytes = new byte[4];
            for (int i = 0; i < line.Length; i++)
            {
                int effectiveColor = i * colSpan + ((colSpan - 1) / 2);
                byte color = Convert.ToByte(effectiveColor * 255 / 160);
                colorBytes[0] = color;
                colorBytes[1] = color;
                colorBytes[2] = color;
                colorBytes[3] = color;
                line[i] = BitConverter.ToInt32(colorBytes, 0);
            }

            Stopwatch timer = new Stopwatch();
            timer.Restart();
            for (int i = 0; i < 128; i += rowSpan)
            {
                SendLine(line, i, rowSpan);
            }
            Console.WriteLine(timer.ElapsedMilliseconds);
        }

        public void SendLine(int[] line, int startRow, int rowSpan)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            int idx = 0;
            int displayWidth = 160;

            // check that displayWidth is a multiple of the line length
            int colSpan = displayWidth / line.Length;
            if (line.Length * colSpan != displayWidth)
            {
                return; // TODO throw exception
            }

            // send packetized data
            if (startRow == 0)
            {
                byte[] restart = new byte[62];                // buffer to be sent
                idx = BB.UIntToDecimalBytes(ref restart, 0, 1, 0);
                restart[idx++] = Convert.ToByte(':');
                restart[idx++] = Convert.ToByte('R');         // "Reset Draw" command
                arduinoSerial.Write(restart, 0, idx);
                waitForAck(1000);
            }

            // send colSpan and rowSpan
            byte[] spans = new byte[62];                      // buffer to be sent
            idx = BB.SIntToDecimalBytes(ref spans, 0, 5, 0);
            spans[idx++] = Convert.ToByte(':');
            spans[idx++] = Convert.ToByte('S');               // "Span Size" command
            idx += BB.SIntToHexBytes(ref spans, idx, colSpan, 2, 2);
            idx += BB.SIntToHexBytes(ref spans, idx, rowSpan, 2, 2);
            arduinoSerial.Write(spans, 0, idx);
            waitForAck(1000);

            // send packetized line color data
            byte[] linePart = new byte[62];                   // buffer to be sent
            int linePartLen = Math.Min(28, line.Length);      // packetized line message length
            for (int i = 0; i < line.Length; i += linePartLen)
            {
                int sendCnt = Math.Min(linePartLen, line.Length - i);

                // prepare the message header
                idx = BB.SIntToDecimalBytes(ref linePart, 0, sendCnt*2 + 1, 0);
                linePart[idx++] = Convert.ToByte(':');
                linePart[idx++] = Convert.ToByte('L');     // "Line Part" command

                // add the color values
                for (int j = 0; j < sendCnt; j++)
                {
                    // get the color
                    byte[] color = BitConverter.GetBytes(line[i + j]);

                    // color encoding:
                    //  - bits: RRRR RGGG GGGB BBBB
                    //  - hex: R=F800 G=07C0 B=001F
                    ushort color16 = 0;
                    color16 |= Convert.ToUInt16((color[0] & 0xF8) << 8); // red
                    color16 |= Convert.ToUInt16((color[0] & 0xFC) << 3); // green
                    color16 |= Convert.ToUInt16((color[0] & 0xF8) >> 3); // blue

                    linePart[idx + j*2 + 1] = BitConverter.GetBytes(color16)[0];
                    linePart[idx + j*2 + 0] = BitConverter.GetBytes(color16)[1];
                }

                // send the message
                arduinoSerial.Write(linePart, 0, sendCnt * 2 + idx);
                waitForAck(1000);
            }

            //Console.WriteLine("c#: " + timer.ElapsedMilliseconds);
        }

        private void waitForAck(int millsWait)
        {
            Stopwatch timeout = new Stopwatch();
            timeout.Start();
            while (!ackReceived && timeout.ElapsedMilliseconds < millsWait) { }
            ackReceived = false;
        }

        public void SendString(string data, bool includeNewline = false)
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
    }
}
