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
        public Form parent;
        public Control pictureBox;
        public BtnManager.clickCallback OnClick = null;

        public BtnProps(int idx, Form parent, BtnManager.clickCallback OnClick)
        {
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
        }
    }
}
