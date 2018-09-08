using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsSnapshots
{
    public class ArduinoCommunication
    {
        public const uint WIDTH = 160;
        public const uint HEIGHT = 128;

        public SerialPort arduinoSerial = null;
        private string serialLineIn = "";
        private bool ackReceived = false;
        /// <summary>The updates to be pushed immediately. Acts like a stack (push and pop from back).</summary>
        private List<Update> lowResUpdates  = new List<Update>();
        /// <summary>The updates to be pushed seconarily. Acts like a queue (push to front, pop off back).</summary>
        private List<Update> medResUpdates  = new List<Update>();
        /// <summary>The updates to be pushed tertiarily. Acts like a queue (push to front, pop off back).</summary>
        private List<Update> highResUpdates = new List<Update>();
        /// <summary>The images to draw. The indexes in low-med-high-ResUpdates point to this list.</summary>
        private List<Bitmap> images = new List<Bitmap>();
        /** Used to syncronize accesses to the updates and the QueueImages() method. */
        private Mutex queueMutex = new Mutex();

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
        public void SendTestImage(uint colSpan, uint rowSpan)
        {
            int[] line = new int[160 / colSpan];
            byte[] colorBytes = new byte[4];
            for (uint i = 0; i < line.Length; i++)
            {
                uint effectiveColor = i * colSpan + ((colSpan - 1) / 2);
                byte color = Convert.ToByte(effectiveColor * 255 / 160);
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
                SendLine(line, i, rowSpan);
            }
            Console.WriteLine(timer.ElapsedMilliseconds);
        }

        /// <summary>
        /// Queues up an image to be draw to an Arduino screen.
        /// A low-res image (8x8) will be loaded first and in reverse queue order for speed,
        /// then a medium-res image (40x32) will be loaded in queue order,
        /// then finally a full-res image (160x128) will be loaded in queue order.
        /// 
        /// Queueing up a new image for the same screen will
        /// restart the process imediately and the old image data
        /// will be forgotten.
        /// </summary>
        /// <param name="img">The image to add to the drawing queue.</param>
        /// <param name="screenIdx">The screen to draw to.</param>
        public void QueueImage(Bitmap img, int screenIdx)
        {
            // add indexes to the queue as necessary
            for (int i = images.Count; i <= screenIdx; i++)
            {
                images.Add(null);
            }

            try
            {
                queueMutex.WaitOne();

                // discard previous images
                List<Update> toRemoveFrom = null;
                for (int i = 0; i < 3; i++)
                {
                    if (i == 0) toRemoveFrom = lowResUpdates;
                    if (i == 1) toRemoveFrom = medResUpdates;
                    if (i == 2) toRemoveFrom = highResUpdates;
                    for (int j = 0; j < toRemoveFrom.Count; j++)
                    {
                        if (toRemoveFrom[j].screenIdx == screenIdx)
                        {
                            toRemoveFrom.RemoveAt(j);
                            break;
                        }
                    }
                }

                // add new images
                images[screenIdx] = new Bitmap(img);
                lowResUpdates.Add(new Update(screenIdx));
                medResUpdates.Insert(0, new Update(screenIdx));
                highResUpdates.Insert(0, new Update(screenIdx));
            }
            finally
            {
                queueMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Draws lines from the images waiting in the update stacks/queues.
        /// Only draws one line of the most low-res image to keep the thread responsive.
        /// </summary>
        public void Update()
        {
            uint colSpan = 0;
            uint rowSpan = 0;
            Update update = null;
            Bitmap img = null;

            try
            {
                queueMutex.WaitOne();

                // get the quality to draw a line at
                update = null;
                if (lowResUpdates.Count > 0) {
                    colSpan = 20;
                    rowSpan = 16;
                    update = lowResUpdates[0];
                    if ((update.rowIdx + 1) * rowSpan == HEIGHT) lowResUpdates.RemoveAt(0);
                    if (update.rowIdx == 0) Console.WriteLine("drawing low-rez");
                } else if (medResUpdates.Count > 0) {
                    colSpan = 4;
                    rowSpan = 4;
                    update = medResUpdates[medResUpdates.Count-1];
                    if ((update.rowIdx + 1) * rowSpan == HEIGHT) medResUpdates.RemoveAt(medResUpdates.Count - 1);
                    if (update.rowIdx == 0) Console.WriteLine("drawing med-rez");
                } else if (highResUpdates.Count > 0) {
                    colSpan = 1;
                    rowSpan = 1;
                    update = highResUpdates[highResUpdates.Count - 1];
                    if ((update.rowIdx + 1) * rowSpan == HEIGHT) highResUpdates.RemoveAt(highResUpdates.Count - 1);
                    if (update.rowIdx == 0) Console.WriteLine("drawing high-rez");
                }
                if (update == null)
                    return;

                // get the image and resize as necessary
                img = images[update.screenIdx];
                if (img.Width != WIDTH || img.Height != HEIGHT) {
                    img = new Bitmap(img, (int)WIDTH, (int)HEIGHT);
                    images[update.screenIdx] = img;
                }

                // calculate the line to draw
                int[] line = new int[WIDTH / colSpan];
                for (uint col = 0; col < line.Length; col++)
                {
                    int x = (int)(col * colSpan);
                    uint[] uc = new uint[4];
                    for (int i = 0; i < colSpan; i++)
                    {
                        for (int j = 0; j < rowSpan; j++)
                        {
                            int y = (int)(update.rowIdx * rowSpan);
                            Color c = img.GetPixel(x + i, y + j);
                            uc[0] += c.R;
                            uc[1] += c.G;
                            uc[2] += c.B;
                        }
                    }
                    byte[] bc = new byte[4];
                    bc[0] = Convert.ToByte(uc[0] / (colSpan * rowSpan));
                    bc[1] = Convert.ToByte(uc[1] / (colSpan * rowSpan));
                    bc[2] = Convert.ToByte(uc[2] / (colSpan * rowSpan));
                    line[col] = BitConverter.ToInt32(bc, 0);
                }

                // draw the line
                SendLine(line, update.rowIdx, rowSpan);

                // prepare for next time this function is called
                update.rowIdx++;
            }
            finally
            {
                queueMutex.ReleaseMutex();
            }
        }

        public void SendLine(int[] line, uint startRow, uint rowSpan)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            int idx = 0;

            // check that displayWidth is a multiple of the line length
            uint colSpan = WIDTH / (uint)line.Length;
            if (line.Length * colSpan != WIDTH)
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
            idx += BB.UIntToHexBytes(ref spans, idx, colSpan, 2, 2);
            idx += BB.UIntToHexBytes(ref spans, idx, rowSpan, 2, 2);
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
                    color16 |= Convert.ToUInt16((color[1] & 0xFC) << 3); // green
                    color16 |= Convert.ToUInt16((color[2] & 0xF8) >> 3); // blue

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

    public class Update
    {
        public int screenIdx;
        public uint rowIdx;

        public Update(int screenIdx)
        {
            this.screenIdx = screenIdx;
        }
    }
}
