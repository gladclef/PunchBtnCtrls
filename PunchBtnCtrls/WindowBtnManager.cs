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
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        
        public IntPtr lastWindow = IntPtr.Zero;
        public string lastWindowText = null;
        public Timer checkTimer = new Timer();
        public Timer captureTimer = new Timer();
        public Timer recaptureTimer = new Timer();
        public List<IntPtr> windowOrder = new List<IntPtr>();

        public WindowBtnManager(Form parent, int startIdx = 0, int endIdx = 0) : base(parent, startIdx, endIdx)
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
        private string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
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

        private void checkTimer_Tick(object sender, EventArgs e)
        {
            IntPtr currWindow = GetForegroundWindow();
            string currText = GetActiveWindowTitle();

            if ((currWindow != lastWindow || currText != lastWindowText) &&
                currWindow != parent.Handle)
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
            string currText = GetActiveWindowTitle();
            Bitmap screen = GetActiveWindowScreenshot();

            // check if this window is valid
            if (screen != null &&
                currWindow != parent.Handle)
            {
                // Get the ordered window index.
                // Don't do anything 
                int windowIdx = GetWindowIdx(currWindow);
                int btnIdx = startIdx + windowIdx;
                if (btnIdx >= endIdx)
                    return;

                // check for window change
                if (lastWindow != currWindow)
                    btns[btnIdx].SetUpdated(true);

                // get some window stats
                lastWindow = currWindow;
                lastWindowText = currText;

                // get the capture the screen, set the window text, and update the button
                parent.Text = currText;
                Debug.WriteLine(lastWindowText);
                Rect winSize = new Rect();
                GetWindowRect(parent.Handle, ref winSize);
                if ((winSize.Right - winSize.Left) > 0 &&
                    (winSize.Bottom - winSize.Top) > 0)
                {
                    btns[btnIdx].SetImage(screen);
                }
            }
        }

        private void recaptureTimer_Tick(object sender, EventArgs e)
        {
            IntPtr currWindow = GetForegroundWindow();
            string currText = GetActiveWindowTitle();

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
                }
            }

            return btnIdx;
        }
    }
}
