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
    public class BtnProps
    {
        /// <summary>The button index (from left to right, starting with 0).</summary>
        public int idx = -1;
        /// <summary>The latest image for this button.</summary>
        public Bitmap img = null;
        /// <summary>Like <see cref="img"/>, but appropriatly resized for the screen.</summary>
        public Bitmap screenImg = null;
        public uint screenWidth, screenHeight;
        public Form parent;
        /// <summary>If there is a GUI element that this button draws to, this would be it.</summary>
        public Control pictureBox;
        public BtnManager.clickCallback OnClick = null;
        public AbstractCommunication comm = null;
        /// <summary>row drawing update indexes for low (0), medium (1), and high resolution (2)</summary>
        public uint[] rowIdx = new uint[3];
        /// <summary>A highly reduced color set to write with instead of the full color set (so that we can send pixel data with one byte instead of two).</summary>
        public Palette palette = new Palette();
        /// <summary>The location in the palette that will be written out next.</summary>
        public int paletteWriteIdx = 0;
        /// <summary>True if we need to push updated image data to this button.</summary>
        public bool doUpdateImg = false;
        /// <summary>True if we need to push an updated palette to this button.</summary>
        public bool doUpdatePalette = false;

        public BtnProps(int idx, uint screenWidth, uint screenHeight, Form parent, BtnManager.clickCallback OnClick)
        {
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            this.parent = parent;
            this.OnClick = OnClick;
            SetIndex(idx);
        }

        private void OnBtnClick(object sender, EventArgs e)
        {
            if (OnClick != null)
            {
                try
                {
                    BtnManager.clickLock.WaitOne();
                    OnClick(this.idx);
                }
                finally
                {
                    BtnManager.clickLock.ReleaseMutex();
                }
            }
        }

        public void SetIndex(int idx)
        {
            this.idx = idx;

            // remove references to old picture box
            if (pictureBox != null)
            {
                pictureBox.Click -= OnBtnClick;
            }

            // add references to new picture box
            pictureBox = parent.Controls.Find("pbScreen" + (this.idx + 1), true)[0];
            pictureBox.Click += OnBtnClick;
        }

        public void SetImage(Bitmap img, bool doUpdate = false)
        {
            this.img = img;
            if (doUpdate) SetDoUpdateImg(true);
        }

        public void SetDoUpdateImg(bool doUpdate)
        {
            this.doUpdateImg = doUpdate;

            if (doUpdate)
            {
                this.screenImg = new Bitmap(this.img, (int)screenWidth, (int)screenHeight);
                this.palette = ImageMagic.CalculatePalette(this.img, 256);
                SetDoUpdatePalette(true);
                paletteWriteIdx = 0;
            }
        }

        public void SetDoUpdatePalette(bool doUpdate)
        {
            this.doUpdatePalette = doUpdate;
        }
    }
}
