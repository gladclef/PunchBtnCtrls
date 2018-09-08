using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsSnapshots
{
    public partial class Form1 : Form
    {
        public List<Bitmap> imgs = new List<Bitmap>();
        public List<BtnManager> managers = new List<BtnManager>();
        public ArduinoCommunication comm = null;

        public Form1()
        {
            InitializeComponent();
            managers.Add(new WindowBtnManager(this, 0, 4));
            comm = new ArduinoCommunication(this.serialPort1);
        }

        public PictureBox GetPictureBox(int idx)
        {
            return (PictureBox)(Controls.Find("pbScreen" + (idx + 1), true)[0]);
        }

        private void ResizeImageForPB(PictureBox pictureBox, Bitmap img)
        {
            if (pictureBox.Size.Width > 0 &&
                pictureBox.Size.Height > 0)
            {
                pictureBox.Image = new Bitmap(img, pictureBox.Size);
            }
        }

        private void guiTimer_Tick(object sender, EventArgs e)
        {
            List<BtnProps> btns = GetBtns();

            for (int screenIdx = 0; screenIdx < btns.Count; screenIdx++)
            {
                if (!btns[screenIdx].updated)
                    continue;
                btns[screenIdx].SetUpdated(false);

                // update the button image in the GUI window
                Bitmap btnImg = btns[screenIdx].img;
                if (btnImg != GetImg(screenIdx))
                {
                    imgs[screenIdx] = btnImg;
                    ResizeImageForPB(GetPictureBox(screenIdx), btnImg);
                }

                // update the button image on the Arduino
                comm.QueueImage(btnImg, screenIdx);
            }

            comm.Update();
        }

        private Bitmap GetImg(int i)
        {
            if (i < 0)
            {
                throw new IndexOutOfRangeException("Image index must be positive, is " + i);
            }

            while (i >= imgs.Count)
            {
                imgs.Add(null);
            }
            return imgs[i];
        }

        private List<BtnProps> GetBtns()
        {
            List<BtnProps> retval = new List<BtnProps>();
            foreach (var manager in managers)
            {
                for (int i = manager.startIdx; i < manager.endIdx; i++)
                {
                    retval.Add(manager.GetBtn(i));
                }
            }
            return retval;
        }
    }
}
