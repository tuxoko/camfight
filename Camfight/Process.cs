using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.Threading;
using Emgu.CV.CvEnum;

namespace Camfight
{
    public partial class Form1 : Form
    {
        private Capture _capture=new Capture();
        private void ProcessFrame()
        {
            while (true)
            {
                Image<Bgr, Byte> frame = _capture.QueryFrame();
                FPU.ProcessFrame(frame);
                pictureBox1.Image = FPU.backproject.Bitmap;
                this.Invoke(new InvokeFunction(pictureBox1.Refresh));
                this.Invoke(new InvokeFunction(pictureBox1.Show));
                //pictureBox1.Refresh();
                //pictureBox1.Show();
            }
        }
    }
}
