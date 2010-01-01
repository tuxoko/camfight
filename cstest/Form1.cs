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
            histogramBox1.Show();
        }

        private Capture _capture;
        private bool _captureInProgress;
        private HaarCascade _haar;
        private static RangeF mrangef=new RangeF(0,180);
        private DenseHistogram _hist = new DenseHistogram(16, mrangef);
        private bool isTracked = false;

        private Image<Hsv, Byte> hsv = null;
        private Image<Gray, Byte> hue = null;
        private Image<Gray, Byte> mask = null;
        private Image<Gray, Byte> backproject =null;
        private IntPtr[] imgs = null;
        private Rectangle track_window;
        private MCvConnectedComp track_comp = new MCvConnectedComp();
        private MCvBox2D track_box = new MCvBox2D();
        private Rectangle head_rect;


        private void ProcessFrame(object sender, EventArgs arg)
        {
            Image<Bgr, Byte> frame = _capture.QueryFrame();
            if (isTracked == false)
            {

                //hsv = cvCreateImage( cvGetSize(frame), 8, 3 );
                //hue = cvCreateImage( cvGetSize(frame), 8, 1 );
                //mask = cvCreateImage( cvGetSize(frame), 8, 1 );
                //backproject = cvCreateImage(cvGetSize(frame), 8, 1);
                hsv = new Image<Hsv, byte>(frame.Width, frame.Height);
                hsv = frame.Convert<Hsv, Byte>();
                hue = new Image<Gray, byte>(frame.Width, frame.Height);
                mask = new Image<Gray, byte>(frame.Width, frame.Height);
                backproject = new Image<Gray, byte>(frame.Width, frame.Height);

                Emgu.CV.CvInvoke.cvInRangeS(hsv, new MCvScalar(0, 30, 10, 0), new MCvScalar(180, 256, 256, 0), mask);
                Emgu.CV.CvInvoke.cvSplit(hsv, hue, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                var faces = frame.DetectHaarCascade(_haar, 1.4, 4, HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT | HAAR_DETECTION_TYPE.SCALE_IMAGE, new Size(40, 40))[0];


                foreach (var face in faces)
                {
                    //frame.Draw(face.rect, new Bgr(0, double.MaxValue, 0), 3);

                    Emgu.CV.CvInvoke.cvSetImageROI(hue, face.rect);
                    Emgu.CV.CvInvoke.cvSetImageROI(mask, face.rect);

                    imgs = new IntPtr[1] { hue };

                    Emgu.CV.CvInvoke.cvCalcHist(imgs, _hist, false, mask);

                    Emgu.CV.CvInvoke.cvResetImageROI(hue);
                    Emgu.CV.CvInvoke.cvResetImageROI(mask);

                    histogramBox1.ClearHistogram();
                    histogramBox1.AddHistogram("test", Color.Blue, _hist);
                    histogramBox1.Refresh();

                    isTracked = true;
                    //track_window = face.rect;
                }
            }
            else
            {
                hsv = frame.Convert<Hsv, Byte>();
                Emgu.CV.CvInvoke.cvInRangeS(hsv, new MCvScalar(0, 30, 10, 0), new MCvScalar(180, 256, 256, 0), mask);
                Emgu.CV.CvInvoke.cvSplit(hsv, hue, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                imgs = new IntPtr[1] { hue };
                Emgu.CV.CvInvoke.cvCalcBackProject(imgs, backproject, _hist);
                Emgu.CV.CvInvoke.cvAnd(backproject, mask, backproject, IntPtr.Zero); 

                var faces = backproject.DetectHaarCascade(_haar, 1.4, 4, HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT | HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(40, 40))[0];


                foreach (var face in faces)
                {
                    head_rect = face.rect;
                    frame.Draw(face.rect,new Bgr(255,0,0),3);
                }
            }


            

            
            
           
            //ignore head region
            Emgu.CV.CvInvoke.cvSetImageROI(backproject, head_rect);

            try
            {
                Emgu.CV.CvInvoke.cvZero(backproject);
            }
            catch { }
            Emgu.CV.CvInvoke.cvResetImageROI(backproject);


            if (track_window.Width == 0) track_window.Width = 40;
            if (track_window.Height == 0) track_window.Height = 40;
            Emgu.CV.CvInvoke.cvCamShift(backproject,track_window,new MCvTermCriteria(10,0.5),out track_comp,out track_box);
            track_window = track_comp.rect;
            frame.Draw(track_window, new Bgr(0, double.MaxValue, 0), 3);
            captureImageBox.Image = frame;

            imageBox1.Image = backproject;
            
            
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

                _captureInProgress = !_captureInProgress;              }
            }
        }
}
