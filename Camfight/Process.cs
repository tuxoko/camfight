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

                int face_sector = GetSector(new Point(FPU.face.X + FPU.face.Width / 2, FPU.face.Y + FPU.face.Height / 2),frame.Size);
                int left_sector = GetSector(FPU.center[1],frame.Size);
                int right_sector = GetSector(FPU.center[0],frame.Size);
            }
        }

        private int GetSector(Point center,Size frame_size)
        {
            int sec=0;
            sec = center.X / (frame_size.Width / 5) * 3;
            sec += center.Y / (frame_size.Height / 3);
            return sec;
        }
    }
}
