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
        public int idx = -1;
        public Bitmap img = null;
        /// <summary>Like <see cref="img"/>, but appropriatly resized for the screen.</summary>
        public Bitmap screenImg = null;
        public uint screenWidth, screenHeight;
        public Form parent;
        public Control pictureBox;
        public BtnManager.clickCallback OnClick = null;
        public bool updated = false;
        public AbstractCommunication comm = null;
        /// <summary>row drawing update indexes for low (0), medium (1), and high resolution (2)</summary>
        public uint[] rowIdx = new uint[3];

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

        public void SetImage(Bitmap img)
        {
            this.img = img;
            this.screenImg = new Bitmap(img, (int)screenWidth, (int)screenHeight);
        }

        public void SetUpdated(bool updated)
        {
            this.updated = updated;
        }
    }
}
