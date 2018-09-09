using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsSnapshots
{
    public abstract class BtnManager
    {
        public static Mutex clickLock = new Mutex();
        public delegate void clickCallback(int idx);

        public List<BtnProps> btns = new List<BtnProps>();
        public int startIdx = 0;
        public int endIdx = 0;
        public Form parent = null;
        public uint[] screenWidths;
        public uint[] screenHeights;

        /// <summary>
        /// Create a new button manager.
        /// </summary>
        /// <param name="parent">The form with PictureBox child controls that the buttons will bind to click events of.</param>
        /// <param name="screenWidths">The widths of the screens for the buttons.
        ///                            If there aren't enough values in here for the number of buttons then the value at index 0 is used for all other buttons.</param>
        /// <param name="screenHeights">The heights of the screens for the buttons.
        ///                             If there aren't enough values in here for the number of buttons then the value at index 0 is used for all other buttons.</param>
        /// <param name="startIdx">The starting index of addressable buttons by this button manager (inclusive).</param>
        /// <param name="endIdx">The ending index of addressable buttons by this button manager (exclusive).</param>
        public BtnManager(Form parent, uint[] screenWidths, uint[] screenHeights, int startIdx = 0, int endIdx = 0)
        {
            this.startIdx = startIdx;
            this.endIdx = endIdx;
            this.parent = parent;
            this.screenWidths = screenWidths;
            this.screenHeights = screenHeights;
            PopulateList();
        }

        /// <summary>
        /// This method will be executed when a button registers a click.
        /// There is no need to implement bounds checks of the given index.
        /// </summary>
        /// <param name="idx">The index of the button. This is the global button index
        /// (aka subtract the startIdx to get the relative button index to this manager).</param>
        public abstract void OnClick(int idx);

        /// <summary>
        /// Checks the index of the button, then calls OnClick(int).
        /// </summary>
        /// <param name="idx"></param>
        public void OnClickGeneral(int idx)
        {
            try
            {
                clickLock.WaitOne();

                if (idx < startIdx || idx >= endIdx)
                {
                    throw new IndexOutOfRangeException("Index for this button manager must be in [" + startIdx + ", " + (endIdx - 1) + "], was " + idx);
                }
                OnClick(idx);
            }
            finally
            {
                clickLock.ReleaseMutex();
            }
        }

        /// <summary>
        /// Update the communication device for the given button.
        /// </summary>
        /// <param name="screenIdx">The index of the button/screen.</param>
        /// <param name="comm">The communication channel for the device.</param>
        public void SetComm(int screenIdx, AbstractCommunication comm)
        {
            btns[screenIdx].comm = comm;
        }

        /// <summary>
        /// Creates new buttons, as necessary.
        /// </summary>
        public void PopulateList()
        {
            try
            {
                clickLock.WaitOne();
                
                for (int screenIdx = btns.Count; screenIdx < startIdx; screenIdx++)
                {
                    btns.Add(new BtnProps(screenIdx, GetWidth(screenIdx), GetHeight(screenIdx), parent, null));
                }
                int startCreateIdx = Math.Max(startIdx, btns.Count);
                for (int i = startCreateIdx; i < endIdx; i++)
                {
                    btns.Add(new BtnProps(i, GetWidth(i), GetHeight(i), parent, OnClickGeneral));
                }
            }
            finally
            {
                clickLock.ReleaseMutex();
            }
        }

        public uint GetWidth(int screenIdx)
        {
            return ElementAtOrDefault(screenWidths, screenIdx, screenWidths[0]);
        }

        public uint GetHeight(int screenIdx)
        {
            return ElementAtOrDefault(screenHeights, screenIdx, screenHeights[0]);
        }

        private uint ElementAtOrDefault(uint[] array, int idx, uint defaultValue)
        {
            if (array.Length - 1 < idx)
                return defaultValue;
            return array[idx];
        }

        /// <summary>
        /// Clears the button at the given index and moves it to the endIdx-1 position.
        /// </summary>
        /// <param name="screenIdx">The index of the button to clear.</param>
        public void ClearBtn(int screenIdx)
        {
            if (screenIdx < startIdx || screenIdx >= endIdx)
            {
                throw new IndexOutOfRangeException("Index for this button manager must be in [" + startIdx + ", " + (endIdx - 1) + "], was " + screenIdx);
            }

            try
            {
                clickLock.WaitOne();
                for (int i = screenIdx + 1; i < endIdx; i++)
                {
                    GetBtn(i).SetIndex(i - 1);
                }
                BtnProps btn = btns[screenIdx];
                btns.RemoveAt(screenIdx);
                btns.Insert(endIdx - 1, btn);
                btn.SetIndex(endIdx - 1);
                btn.SetImage(null);
            }
            finally
            {
                clickLock.ReleaseMutex();
            }
        }

        /// <summary>
        /// Changes the range of addressable indexes for this button manager.
        /// </summary>
        /// <param name="startIdx">The starting index of addressable buttons by this button manager (inclusive).</param>
        /// <param name="endIdx">The ending index of addressable buttons by this button manager (exclusive).</param>
        public void SetRange(int startIdx, int endIdx)
        {
            try
            {
                clickLock.WaitOne();

                // clear previous range
                for (int i = this.startIdx; i < startIdx; i++)
                {
                    GetBtn(i).OnClick = null;
                }
                for (int i = endIdx; i < this.endIdx; i++)
                {
                    GetBtn(i).OnClick = null;
                }

                // register callback for new buttons in range
                for (int i = startIdx; i < this.startIdx; i++)
                {
                    GetBtn(i).OnClick += OnClickGeneral;
                }
                for (int i = this.endIdx; i < endIdx; i++)
                {
                    GetBtn(i).OnClick += OnClickGeneral;
                }

                // update internal state and add new buttons, as necessary
                this.startIdx = startIdx;
                this.endIdx = endIdx;
                PopulateList();
            }
            finally
            {
                clickLock.ReleaseMutex();
            }
        }

        public BtnProps GetBtn(int screenIdx)
        {
            if (screenIdx < startIdx || screenIdx >= endIdx)
            {
                throw new IndexOutOfRangeException("Index for this button manager must be in [" + startIdx + ", " + (endIdx - 1) + "], was " + screenIdx);
            }
            BtnProps btn = btns[screenIdx];
            if (btn.idx != screenIdx)
            {
                throw new InvalidOperationException("The button at index " + screenIdx + " has an internally registered index of " + screenIdx);
            }
            return btn;
        }

        public Bitmap GetBtnImg(int idx)
        {
            return GetBtn(idx).img;
        }
    }
}
