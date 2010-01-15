﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

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
            //_haar = new HaarCascade("..\\..\\haarcascade_frontalface_alt2.xml");
            FPU = new FrameProcessor();
            sw = new Stopwatch();
            histogramBox1.Show();
        }
        private Stopwatch sw;
        private FrameProcessor FPU;
        private Capture _capture;
        private bool _captureInProgress;
        /*
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

        private bool handtrack=false;*/


        private void ProcessFrame(object sender, EventArgs arg)
        {
            Image<Bgr, Byte> frame = _capture.QueryFrame();
            #region old
            /*
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

                    Rectangle roi = new Rectangle(face.rect.X + face.rect.Width / 8, face.rect.Y + face.rect.Height / 8, face.rect.Width / 4, face.rect.Height / 4);

                    Emgu.CV.CvInvoke.cvSetImageROI(hue, roi);
                    Emgu.CV.CvInvoke.cvSetImageROI(mask, roi);

                    imgs = new IntPtr[1] { hue };

                    Emgu.CV.CvInvoke.cvCalcHist(imgs, _hist, false, mask);

                    Emgu.CV.CvInvoke.cvResetImageROI(hue);
                    Emgu.CV.CvInvoke.cvResetImageROI(mask);



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

                Image<Gray, Byte> gray = frame.Convert<Gray, Byte>();

                var faces = gray.DetectHaarCascade(_haar, 1.4, 4, HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT | HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(40, 40))[0];


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

            if (handtrack == false)
            {
                int mx = 0, my = 0;

                MCvMoments mom = backproject.GetMoments(false);
                if (mom.m00 > 2500000)
                {
                    mx = (int)mom.GravityCenter.x;
                    my = (int)mom.GravityCenter.y;
                }
                else
                {
                    mx = 0;
                    my = 0;
                }
                frame.Draw(new Rectangle(mx - 5, my - 5, 10, 10), new Bgr(0, double.MaxValue, 0), 3);
                if (mx != 0 || my != 0)
                {
                    track_window = new Rectangle(mx - 10, my - 10, 20, 20);
                    handtrack = true;
                }
            }
            else
            {
                if (track_window.Width == 0) track_window.Width = 40;
                if (track_window.Height == 0) track_window.Height = 40; 

                Emgu.CV.CvInvoke.cvCamShift(backproject,track_window,new MCvTermCriteria(10,0.5),out track_comp,out track_box);
                track_window = track_comp.rect;



                frame.Draw(track_window, new Bgr(0, double.MaxValue, 0), 3);
            }*/
            #endregion
            FPU.ProcessFrame(frame);

            frame.Draw(FPU.face, new Bgr(255,0,0), 3);

            captureImageBox.Image = frame;
            imageBox1.Image = FPU.backproject;

            if (FPU.isTracked)
            {
                histogramBox1.ClearHistogram();
                histogramBox1.AddHistogram("test", Color.Blue, FPU._hist);
                histogramBox1.Refresh();
            }
            sw.Stop();
            long t_interval = sw.ElapsedMilliseconds;

            rtbLog.AppendText(String.Format("face {0} hue {1} back {2} hand {3} total {4} mass {5}\n", FPU.t_facedetect, FPU.t_hue, FPU.t_backproject,FPU.t_hand, t_interval,FPU.mass));
            rtbLog.ScrollToCaret();
            sw.Reset();
            sw.Start();
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
                    FPU.Reset();
                    sw.Reset();
                    captureButton.Text = "Stop";
                    Application.Idle += new EventHandler(ProcessFrame);
                }

                _captureInProgress = !_captureInProgress;              }
            }

        private void imageBox1_Click(object sender, EventArgs e)
        {

        }
        }
}
