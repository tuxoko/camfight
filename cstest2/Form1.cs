using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

// 要引用的類別
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.Threading;

namespace cstest2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private Capture _capture;
        private bool _captureInProgress;

        private void ProcessFrame(object sender, EventArgs arg)
        {
            Image<Bgr, Byte> frame = _capture.QueryFrame();
            captureImageBox.Image = frame;

            // 請再加上以下四行程式碼
            Image<Gray, Byte> grayFrame = frame.Convert<Gray, Byte>();
            Image<Gray, Byte> cannyFrame = grayFrame.Canny(new Gray(100), new Gray(60));
            grayscaleImageBox.Image = grayFrame;
            cannyImageBox.Image = cannyFrame;
        }

        private void captureButton_Click(object sender, EventArgs e)
        {
            #region if capture is not created, create it now
            if (_capture == null)
            {
                try
                {
                    _capture = new Capture();
                }
                catch (NullReferenceException excpt)
                {
                    MessageBox.Show(excpt.Message);
                }
            }
            #endregion

            if (_capture != null)
            {
                if (_captureInProgress)
                {  //stop the capture
                    Application.Idle -= new EventHandler(ProcessFrame);
                    captureButton.Text = "Start Capture";
                }
                else
                {
                    //start the capture
                    captureButton.Text = "Stop";
                    Application.Idle += new EventHandler(ProcessFrame);
                }

                _captureInProgress = !_captureInProgress;
            }
        }
    }
}
