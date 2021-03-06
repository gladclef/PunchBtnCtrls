﻿using System;
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
        /// <summary>Screens resolution in the x dimension.</summary>
        public const uint WIDTH = 160;
        /// <summary>Screens resolution in the y dimension.</summary>
        public const uint HEIGHT = 128;

        public List<Bitmap> imgs = new List<Bitmap>();
        public List<BtnManager> managers = new List<BtnManager>();
        public ArduinoCommunication comm = null;

        public Form1()
        {
            InitializeComponent();
            comm = new ArduinoCommunication(this.serialPort1, WIDTH);
            managers.Add(new WindowBtnManager(this, new uint[] { WIDTH }, new uint[] { HEIGHT }, 0, 4));
            foreach (var manager in managers)
            {
                manager.SetComm(0, comm);
            }
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
                if (!btns[screenIdx].doUpdateImg)
                    continue;
                btns[screenIdx].SetDoUpdateImg(false);

                // update the button image in the GUI window
                Bitmap btnImg = btns[screenIdx].img;
                if (btnImg != GetImg(screenIdx))
                {
                    imgs[screenIdx] = btnImg;
                    ResizeImageForPB(GetPictureBox(screenIdx), btnImg);
                }
            }
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
