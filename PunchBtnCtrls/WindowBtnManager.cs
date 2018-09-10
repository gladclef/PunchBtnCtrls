using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsSnapshots
{
    class WindowBtnManager : BtnManager
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int SetForegroundWindow(IntPtr handle);

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int count);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>The updates to be pushed immediately. Acts like a stack (push and pop from back).</summary>
        private List<int> lowResUpdates = new List<int>();
        /// <summary>The updates to be pushed seconarily. Acts like a queue (push to front, pop off back).</summary>
        private List<int> medResUpdates = new List<int>();
        /// <summary>The updates to be pushed tertiarily. Acts like a queue (push to front, pop off back).</summary>
        private List<int> highResUpdates = new List<int>();
        /// <summary>Used to syncronize accesses to the updates and the QueueImages() method.</summary>
        private System.Threading.Mutex queueMutex = new System.Threading.Mutex();
        
        public IntPtr lastWindow = IntPtr.Zero;
        public string lastWindowText = null;
        public Timer checkTimer = new Timer();
        public Timer captureTimer = new Timer();
        public Timer recaptureTimer = new Timer();
        public List<IntPtr> windowOrder = new List<IntPtr>();

        string[] IgnoredWindowTitles = new string[] { "" };
        string[] IgnoredWindowClassNames = new string[] { "" };

        /// <summary>
        /// See constructor for <see cref="BtnManager"/>.
        /// </summary>
        public WindowBtnManager(Form parent, uint[] screenWidths, uint[] screenHeights, int startIdx = 0, int endIdx = 0) : base(parent, screenWidths, screenHeights, startIdx, endIdx)
        {
            captureTimer.Enabled = false;
            captureTimer.Tick += captureTimer_Tick;
            captureTimer.Interval = 250;
            captureTimer.Stop();

            checkTimer.Enabled = true;
            checkTimer.Tick += checkTimer_Tick;
            checkTimer.Interval = 100;
            checkTimer.Start();

            recaptureTimer.Enabled = false;
            recaptureTimer.Tick += recaptureTimer_Tick;
            recaptureTimer.Interval = 1000;
            recaptureTimer.Start();
        }

        public override void OnClick(int idx)
        {
            Debug.WriteLine("Clicked button at index " + idx + " for window button manager");
            if (idx >= windowOrder.Count || windowOrder[idx] == null)
            {
                return;
            }
            if (SetForegroundWindow(windowOrder[idx]) != 1)
            {
                Debug.WriteLine("Failed to set foreground window");
            }
        }

        // from https://stackoverflow.com/questions/115868/how-do-i-get-the-title-of-the-current-active-window-using-c
        private string GetWindowTitle(IntPtr handle)
        {
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);

            if (GetWindowText(handle, buff, nChars) > 0)
            {
                return buff.ToString();
            }
            return "";
        }

        private string GetWindowClassName(IntPtr handle)
        {
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);

            if (GetClassName(handle, buff, nChars) > 0)
            {
                return buff.ToString();
            }
            return "";
        }

        // from https://stackoverflow.com/questions/1163761/capture-screenshot-of-active-window/9087955#9087955
        private Bitmap GetActiveWindowScreenshot()
        {
            Rectangle bounds;

            var foregroundWindowsHandle = GetForegroundWindow();
            var rect = new Rect();
            GetWindowRect(foregroundWindowsHandle, ref rect);
            bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

            if (bounds.Width == 0 || bounds.Height == 0)
                return null;
            var result = new Bitmap(bounds.Width, bounds.Height);

            using (var g = Graphics.FromImage(result))
            {
                g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
            }

            return result;
        }

        private bool IsIgnoreWindow(IntPtr handle, string title = null, string className = null)
        {
            if (handle == parent.Handle)
                return true;
            title = (title == null) ? GetWindowTitle(handle) : title;
            if (IgnoredWindowTitles.Contains(title))
                return true;
            className = (className == null) ? GetWindowClassName(handle) : className;
            if (IgnoredWindowClassNames.Contains(className))
                return true;
            return false;
        }

        private void checkTimer_Tick(object sender, EventArgs e)
        {
            IntPtr currWindow = GetForegroundWindow();
            string currText = GetWindowTitle(currWindow);

            if ((currWindow != lastWindow || currText != lastWindowText) &&
                !IsIgnoreWindow(currWindow, currText))
            {
                // TODO check if the window closed

                // check if the timer was already started
                if (captureTimer.Enabled)
                {
                    return;
                }

                // start the timer
                captureTimer.Enabled = true;
                captureTimer.Start();
            }
        }

        private void captureTimer_Tick(object sender, EventArgs e)
        {
            captureTimer.Enabled = false;

            IntPtr currWindow = GetForegroundWindow();
            string currText = GetWindowTitle(currWindow);
            Bitmap screen = GetActiveWindowScreenshot();

            // check if this window is valid
            if (screen != null &&
                !IsIgnoreWindow(currWindow, currText))
            {
                // Get the ordered window index.
                // Abort if the equivalent screen idx is not within this manager's range.
                int windowIdx = GetWindowIdx(currWindow);
                int screenIdx = startIdx + windowIdx;
                if (screenIdx >= endIdx)
                    return;

                // check for window change
                if (lastWindow != currWindow)
                    btns[screenIdx].SetUpdated(true);

                // get some window stats
                lastWindow = currWindow;
                lastWindowText = currText;

                // get the capture of the screen, set the window text, and update the button
                parent.Text = currText;
                Debug.WriteLine(lastWindowText);
                Rect winSize = new Rect();
                GetWindowRect(parent.Handle, ref winSize);
                if ((winSize.Right - winSize.Left) > 0 &&
                    (winSize.Bottom - winSize.Top) > 0)
                {
                    btns[screenIdx].SetImage(screen);
                    if (btns[screenIdx].updated)
                    {
                        QueueImage(screenIdx);
                        btns[screenIdx].updated = false;
                    }
                }
            }
        }

        private void recaptureTimer_Tick(object sender, EventArgs e)
        {
            IntPtr currWindow = GetForegroundWindow();
            string currText = GetWindowTitle(currWindow);

            if (currWindow == lastWindow && currText == lastWindowText)
            {
                captureTimer_Tick(sender, e);
            }
        }

        private int GetWindowIdx(IntPtr handle)
        {
            // insert?
            int btnIdx = windowOrder.IndexOf(handle);
            if (btnIdx < 0)
            {
                windowOrder.Add(handle);
                btnIdx = windowOrder.IndexOf(handle);
            }

            // push to start?
            if (btnIdx >= endIdx)
            {
                windowOrder.Remove(handle);
                windowOrder.Insert(0, handle);
                btnIdx = 0;

                // update button images
                for (int screenIdx = endIdx - 1; screenIdx > startIdx; screenIdx--)
                {
                    btns[screenIdx].SetImage(btns[screenIdx - 1].img);
                    QueueImage(screenIdx, false);
                }
            }

            return btnIdx;
        }

        /// <summary>
        /// Queues up an image to be drawn to an Arduino screen.
        /// A low-res image (8x8) will be loaded first and in reverse queue order for speed,
        /// then a medium-res image (40x32) will be loaded in queue order,
        /// then finally a full-res image (160x128) will be loaded in queue order.
        /// 
        /// Queueing up a new image for the same screen will
        /// restart the process imediately and the old image data
        /// will be forgotten.
        /// </summary>
        /// <param name="screenIdx">The screen to draw to.</param>
        /// <param name="updateLowRes">True to push the image for the given screen in low resolution to the <see cref="comm"/></param>
        /// <param name="updateMedRes">True to push the image for the given screen in medium resolution to the <see cref="comm"/></param>
        /// <param name="updateHighRes">True to push the image for the given screen in high resolution to the <see cref="comm"/></param>
        public void QueueImage(int screenIdx, bool updateLowRes = true, bool updateMedRes = true, bool updateHighRes = true)
        {
            try
            {
                queueMutex.WaitOne();

                // discard previous updates
                if (updateLowRes) lowResUpdates.Remove(screenIdx);
                if (updateMedRes) medResUpdates.Remove(screenIdx);
                if (updateHighRes) highResUpdates.Remove(screenIdx);

                // add new update
                if (updateLowRes) lowResUpdates.Add(screenIdx);
                if (updateMedRes) medResUpdates.Insert(0, screenIdx);
                if (updateHighRes) highResUpdates.Insert(0, screenIdx);

                // reset the rowIdx for the updates, as necessary
                BtnProps btn = btns[screenIdx];
                if (updateLowRes) btn.rowIdx[0] = 0;
                if (updateMedRes) btn.rowIdx[1] = 0;
                if (updateHighRes) btn.rowIdx[2] = 0;
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
        /// <returns>True when an update succeeds or there is no update to do, false when an update fails.</returns>
        public bool Update()
        {
            uint colSpan = 0;
            uint rowSpan = 0;
            int screenIdx = -1;
            BtnProps btn = null;
            Bitmap img = null;
            int res = 0; // low (0), med (1), high (2)

            try
            {
                queueMutex.WaitOne();

                // get the quality to draw a line at
                screenIdx = -1;
                btn = null;
                if (lowResUpdates.Count > 0)
                {
                    colSpan = 20;
                    rowSpan = 16;
                    screenIdx = lowResUpdates[0];
                    btn = btns[screenIdx];
                    res = 0;
                    if ((btn.rowIdx[res] + 1) * rowSpan == btn.screenHeight) lowResUpdates.RemoveAt(0);
                    if (btn.rowIdx[res] == 0) Console.WriteLine($"drawing low-rez {screenIdx}");
                }
                else if (medResUpdates.Count > 0)
                {
                    colSpan = 4;
                    rowSpan = 4;
                    screenIdx = medResUpdates[medResUpdates.Count - 1];
                    btn = btns[screenIdx];
                    res = 1;
                    if ((btn.rowIdx[res] + 1) * rowSpan == btn.screenHeight) medResUpdates.RemoveAt(medResUpdates.Count - 1);
                    if (btn.rowIdx[res] == 0) Console.WriteLine($"drawing med-rez {screenIdx}");
                }
                else if (highResUpdates.Count > 0)
                {
                    colSpan = 1;
                    rowSpan = 1;
                    screenIdx = highResUpdates[highResUpdates.Count - 1];
                    btn = btns[screenIdx];
                    res = 2;
                    if ((btn.rowIdx[res] + 1) * rowSpan == btn.screenHeight) highResUpdates.RemoveAt(highResUpdates.Count - 1);
                    if (btn.rowIdx[res] == 0) Console.WriteLine($"drawing high-rez {screenIdx}");
                }

                // for images we can't update, pretend like we did an update
                if (btn == null || btn.comm == null)
                {
                    if (btn != null)
                        btn.rowIdx[res]++;
                    return true;
                }

                // get the image
                img = btn.screenImg;

                // calculate the line to draw
                int[] line = new int[btn.screenWidth / colSpan];
                for (uint col = 0; col < line.Length; col++)
                {
                    int x = (int)(col * colSpan);
                    uint[] uc = new uint[4];
                    for (int i = 0; i < colSpan; i++)
                    {
                        for (int j = 0; j < rowSpan; j++)
                        {
                            int y = (int)(btn.rowIdx[res] * rowSpan);
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
                if (!btn.comm.SendImageRow(line, btn.rowIdx[res], rowSpan))
                    return false;

                // prepare for next time this function is called
                btn.rowIdx[res]++;
                if (btn.rowIdx[res] * rowSpan >= btn.screenHeight)
                    btn.rowIdx[res] = 0;
            }
            finally
            {
                queueMutex.ReleaseMutex();
            }

            return true;
        }
    }
}
