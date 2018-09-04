﻿using System;
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

        /// <summary>
        /// Create a new button manager.
        /// </summary>
        /// <param name="parent">The form with PictureBox child controls that the buttons will bind to click events of.</param>
        /// <param name="startIdx">The starting index of addressable buttons by this button manager (inclusive).</param>
        /// <param name="endIdx">The ending index of addressable buttons by this button manager (exclusive).</param>
        public BtnManager(Form parent, int startIdx = 0, int endIdx = 0)
        {
            this.startIdx = startIdx;
            this.endIdx = endIdx;
            this.parent = parent;
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
        /// Creates new buttons, as necessary.
        /// </summary>
        public void PopulateList()
        {
            try
            {
                clickLock.WaitOne();
                
                for (int i = btns.Count; i < startIdx; i++)
                {
                    btns.Add(new BtnProps(i, parent, null));
                }
                int startCreateIdx = Math.Max(startIdx, btns.Count);
                for (int i = startCreateIdx; i < endIdx; i++)
                {
                    btns.Add(new BtnProps(i, parent, OnClickGeneral));
                }
            }
            finally
            {
                clickLock.ReleaseMutex();
            }
        }

        /// <summary>
        /// Clears the button at the given index and moves it to the endIdx-1 position.
        /// </summary>
        /// <param name="idx">The index of the button to clear.</param>
        public void ClearBtn(int idx)
        {
            if (idx < startIdx || idx >= endIdx)
            {
                throw new IndexOutOfRangeException("Index for this button manager must be in [" + startIdx + ", " + (endIdx - 1) + "], was " + idx);
            }

            try
            {
                clickLock.WaitOne();
                for (int i = idx + 1; i < endIdx; i++)
                {
                    GetBtn(i).SetIndex(i - 1);
                }
                BtnProps btn = btns[idx];
                btns.RemoveAt(idx);
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

        public BtnProps GetBtn(int idx)
        {
            if (idx < startIdx || idx >= endIdx)
            {
                throw new IndexOutOfRangeException("Index for this button manager must be in [" + startIdx + ", " + (endIdx - 1) + "], was " + idx);
            }
            BtnProps btn = btns[idx];
            if (btn.idx != idx)
            {
                throw new InvalidOperationException("The button at index " + idx + " has an internally registered index of " + idx);
            }
            return btn;
        }

        public Bitmap GetBtnImg(int idx)
        {
            return GetBtn(idx).img;
        }
    }
}
