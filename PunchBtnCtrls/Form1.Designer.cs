namespace WindowsSnapshots
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.guiTimer = new System.Windows.Forms.Timer(this.components);
            this.pbScreen1 = new System.Windows.Forms.PictureBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.pbScreen4 = new System.Windows.Forms.PictureBox();
            this.pbScreen3 = new System.Windows.Forms.PictureBox();
            this.pbScreen2 = new System.Windows.Forms.PictureBox();
            this.serialPort1 = new System.IO.Ports.SerialPort(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pbScreen1)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbScreen4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbScreen3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbScreen2)).BeginInit();
            this.SuspendLayout();
            // 
            // guiTimer
            // 
            this.guiTimer.Enabled = true;
            this.guiTimer.Tick += new System.EventHandler(this.guiTimer_Tick);
            // 
            // pbScreen1
            // 
            this.pbScreen1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pbScreen1.BackgroundImage")));
            this.pbScreen1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pbScreen1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbScreen1.InitialImage = null;
            this.pbScreen1.Location = new System.Drawing.Point(0, 0);
            this.pbScreen1.Margin = new System.Windows.Forms.Padding(0);
            this.pbScreen1.Name = "pbScreen1";
            this.pbScreen1.Size = new System.Drawing.Size(400, 225);
            this.pbScreen1.TabIndex = 0;
            this.pbScreen1.TabStop = false;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.pbScreen4, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.pbScreen3, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.pbScreen2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.pbScreen1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(800, 450);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // pbScreen4
            // 
            this.pbScreen4.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pbScreen4.BackgroundImage")));
            this.pbScreen4.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pbScreen4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbScreen4.Location = new System.Drawing.Point(400, 225);
            this.pbScreen4.Margin = new System.Windows.Forms.Padding(0);
            this.pbScreen4.Name = "pbScreen4";
            this.pbScreen4.Size = new System.Drawing.Size(400, 225);
            this.pbScreen4.TabIndex = 3;
            this.pbScreen4.TabStop = false;
            // 
            // pbScreen3
            // 
            this.pbScreen3.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pbScreen3.BackgroundImage")));
            this.pbScreen3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pbScreen3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbScreen3.Location = new System.Drawing.Point(0, 225);
            this.pbScreen3.Margin = new System.Windows.Forms.Padding(0);
            this.pbScreen3.Name = "pbScreen3";
            this.pbScreen3.Size = new System.Drawing.Size(400, 225);
            this.pbScreen3.TabIndex = 2;
            this.pbScreen3.TabStop = false;
            // 
            // pbScreen2
            // 
            this.pbScreen2.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pbScreen2.BackgroundImage")));
            this.pbScreen2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pbScreen2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbScreen2.Location = new System.Drawing.Point(400, 0);
            this.pbScreen2.Margin = new System.Windows.Forms.Padding(0);
            this.pbScreen2.Name = "pbScreen2";
            this.pbScreen2.Size = new System.Drawing.Size(400, 225);
            this.pbScreen2.TabIndex = 1;
            this.pbScreen2.TabStop = false;
            // 
            // serialPort1
            // 
            this.serialPort1.BaudRate = 115200;
            this.serialPort1.PortName = "COM3";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.pbScreen1)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbScreen4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbScreen3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbScreen2)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer guiTimer;
        private System.Windows.Forms.PictureBox pbScreen1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.PictureBox pbScreen2;
        private System.Windows.Forms.PictureBox pbScreen4;
        private System.Windows.Forms.PictureBox pbScreen3;
        private System.IO.Ports.SerialPort serialPort1;
    }
}

