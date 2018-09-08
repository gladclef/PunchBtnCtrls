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
            Debug.Write(s);
        }

        private void LineReceived(string line)
        {
            //Debug.WriteLine(line);
            if (line.Equals("ACK"))
            {
                ackReceived = true;
            }
        }

        public void SendLine(int[] line, int row)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            // input validation
            if (line.Length != 160)
            {
                return; // TODO throw exception
            }

            // send packetized data
            if (row == 0)
            {
                byte[] restart = new byte[62];
                restart[0] = Convert.ToByte('1');
                restart[1] = Convert.ToByte(':');
                restart[2] = Convert.ToByte('R');
                arduinoSerial.Write(restart, 0, 3);
                waitForAck(1000);
            }

            // send packetized data
            byte[] toSend = new byte[400];
            toSend[0] = Convert.ToByte('5');
            toSend[1] = Convert.ToByte('7');
            toSend[2] = Convert.ToByte(':');
            toSend[3] = Convert.ToByte('D');
            for (int i = 0; i < 160; i += 28)
            {
                int offset = 4;// (i == 0 ? 5 : 0);
                int sendCnt = Math.Min(28, 160 - i);
                int byteCnt = sendCnt * 2 + 1;
                for (int j = 0; j < sendCnt; j++)
                {
                    byte[] color = BitConverter.GetBytes(line[i + j]);

                    // color encoding:
                    //  - bits: RRRR RGGG GGGB BBBB
                    //  - hex: R=F800 G=07C0 B=001F
                    ushort color16 = 0;
                    color16 |= Convert.ToUInt16((color[0] & 0xF8) << 8); // red
                    color16 |= Convert.ToUInt16((color[0] & 0xFC) << 3); // green
                    color16 |= Convert.ToUInt16((color[0] & 0xF8) >> 3); // blue

                    int idx = j * 2 + offset;
                    toSend[idx + 1] = BitConverter.GetBytes(color16)[0];
                    toSend[idx + 0] = BitConverter.GetBytes(color16)[1];
                }
                arduinoSerial.Write(toSend, 0, sendCnt*2 + offset);
                waitForAck(1000);
            }

            Console.WriteLine("c#: " + timer.ElapsedMilliseconds);
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
