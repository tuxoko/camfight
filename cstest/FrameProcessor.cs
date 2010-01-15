using System;
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

public class FrameProcessor
{
    private HaarCascade _haar;
    private Rectangle face_rect;
    private Stopwatch sw;

    public bool isTracked = false;
    public DenseHistogram _hist;
    public Rectangle face;
    public Rectangle left;
    public Rectangle right;
    public bool have_face;
    public bool have_left;
    public bool have_right;
    public Image<Gray, Byte> backproject;
    public double mass;

    public long t_facedetect;
    public long t_hue;
    public long t_backproject;
    public long t_hand;

    public FrameProcessor()
	{
        _haar = new HaarCascade(  "..\\..\\haarcascade_frontalface_alt2.xml");
        sw = new Stopwatch();
        Reset();
	}
    public void ProcessFrame(Image<Bgr, Byte> frame)
    {
        sw.Reset();
        sw.Start();
        MCvAvgComp[] faces = FaceDetect(frame);
        sw.Stop();
        t_facedetect = sw.ElapsedMilliseconds;

        sw.Reset();
        sw.Start();
        Image<Hsv, Byte> hsv = frame.Convert<Hsv, Byte>();
        Image<Gray, Byte> hue = new Image<Gray, byte>(frame.Width, frame.Height);
        Image<Gray, Byte> mask = new Image<Gray, byte>(frame.Width, frame.Height);
        Emgu.CV.CvInvoke.cvInRangeS(hsv, new MCvScalar(0, 30, 10, 0), new MCvScalar(180, 256, 256, 0), mask);
        Emgu.CV.CvInvoke.cvSplit(hsv, hue, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        sw.Stop();
        t_hue = sw.ElapsedMilliseconds;

        if (isTracked == false)
        {
            if (faces.Length != 0)
            {
                var ff = faces[0];
                Rectangle smallFaceROI = new Rectangle(ff.rect.X + ff.rect.Width / 8, ff.rect.Y + ff.rect.Height / 8, ff.rect.Width / 4, ff.rect.Height / 4);
                _hist = GetHist(hue, smallFaceROI, mask);
                isTracked=true;
            }
            else
            {
                have_face=false;
                have_left=false;
                have_right=false;
                return;
            }
        }

        if (faces.Length != 0)
        {
            face_rect = faces[0].rect;
            face = face_rect;
            have_face = true;
        }
        else
        {
            face = face_rect;
            have_face = false;
        }
        sw.Reset();
        sw.Start();
        backproject=GetBackproject(hue,_hist,mask,face_rect);
        sw.Stop();
        t_backproject = sw.ElapsedMilliseconds;

        sw.Reset();
        sw.Start();
        Emgu.CV.CvInvoke.cvSetImageROI(backproject, face_rect);
        MCvMoments mom=backproject.GetMoments(false);
        mass = mom.m00;
        Emgu.CV.CvInvoke.cvResetImageROI(backproject);
        sw.Stop();
        t_hand = sw.ElapsedMilliseconds;

    }

    private MCvAvgComp[] FaceDetect(Image<Bgr, Byte> frame)
    {
        return frame.Convert<Gray, Byte>().DetectHaarCascade(_haar, 1.2, 3, HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT |
            HAAR_DETECTION_TYPE.SCALE_IMAGE | HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(40, 40))[0];
    }

    public void Reset()
    {
        isTracked = false;
        face_rect = new Rectangle();
    }
    private DenseHistogram GetHist(Image<Gray, Byte> hue, Rectangle ROI, Image<Gray, Byte> mask)
    {
        DenseHistogram hist=new DenseHistogram(16,new RangeF(0,180));

        Emgu.CV.CvInvoke.cvSetImageROI(hue, ROI);
        Emgu.CV.CvInvoke.cvSetImageROI(mask, ROI);

        IntPtr[] imgs = new IntPtr[1] { hue };

        Emgu.CV.CvInvoke.cvCalcHist(imgs, hist, false, mask);

        Emgu.CV.CvInvoke.cvResetImageROI(hue);
        Emgu.CV.CvInvoke.cvResetImageROI(mask);

        return hist;
    }

    private Image<Gray, Byte> GetBackproject(Image<Gray, Byte> hue, DenseHistogram _hist,Image<Gray,Byte> mask,Rectangle hide)
    {
        Image<Gray, Byte> backproject = new Image<Gray, byte>(hue.Width, hue.Height);
        var imgs = new IntPtr[1] { hue };
        Emgu.CV.CvInvoke.cvCalcBackProject(imgs, backproject, _hist);
        Emgu.CV.CvInvoke.cvAnd(backproject, mask, backproject, IntPtr.Zero);

        hide.Height += 50;
        Emgu.CV.CvInvoke.cvSetImageROI(backproject, hide);
        try
        {
            //Emgu.CV.CvInvoke.cvZero(backproject);
        }
        catch { }
        Emgu.CV.CvInvoke.cvResetImageROI(backproject);

        return backproject;
    }
}
