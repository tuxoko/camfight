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

namespace cstest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            _haar = new HaarCascade("..\\..\\haarcascade_frontalface_alt2.xml");
        }

            private Capture _capture;
            private bool _captureInProgress;
            private HaarCascade _haar;
            

            private void ProcessFrame(object sender, EventArgs arg)
            {
                Image<Bgr, Byte> frame = _capture.QueryFrame();
                
                var faces = frame.DetectHaarCascade(_haar, 1.4, 4,HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT|HAAR_DETECTION_TYPE.SCALE_IMAGE, new Size(40,40))[0];
               
            
                foreach (var face in faces) 
                {
                    frame.Draw(face.rect, new Bgr(0, double.MaxValue, 0), 3); 
                }

                captureImageBox.Image = frame;
                /*
                // 請再加上以下四行程式碼
                Image<Gray, Byte> grayFrame = frame.Convert<Gray, Byte>();
                Image<Gray, Byte> cannyFrame = grayFrame.Canny(new Gray(100), new Gray(60));
                grayscaleImageBox.Image = grayFrame;
                cannyImageBox.Image = cannyFrame;*/
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
