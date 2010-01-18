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
    private bool th_check;
    private Point[] center;

    //some parameters
    private int backproj_threshold=100;
    private double kmeans_scale = 16;

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
    public long t_kmeans;

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

        if (isTracked == false)
        {
            if (faces.Length != 0)
            {
                var ff = faces[0];
                Rectangle smallFaceROI = new Rectangle(ff.rect.X + ff.rect.Width / 8, ff.rect.Y + ff.rect.Height / 8, ff.rect.Width / 4, ff.rect.Height / 4);
                _hist = GetHist(hue, smallFaceROI, mask);
                isTracked=true;
                th_check = true;
                center = new Point[] { new Point(0, 0), new Point(0, 0) };
            }
            else
            {
                have_face=false;
                have_left=false;
                have_right=false;
                return;
            }
        }
        sw.Stop();
        t_hue = sw.ElapsedMilliseconds;


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
        backproject = GetBackproject(hue, _hist, mask, face_rect).ThresholdToZero(new Gray(backproj_threshold));
        sw.Stop();
        t_backproject = sw.ElapsedMilliseconds;

        sw.Reset();
        sw.Start();
        center = kmeans(center, backproject, face_rect, kmeans_scale);
        foreach (Point p in center)
        {
            frame.Draw(new CircleF(p, 20f), new Bgr(Color.Red), 2);
        }
        sw.Stop();
        t_kmeans = sw.ElapsedMilliseconds;
    }

    private MCvAvgComp[] FaceDetect(Image<Bgr, Byte> frame)
    {
        return frame.Convert<Gray, Byte>().DetectHaarCascade(_haar, 1.4, 1, HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT |
            HAAR_DETECTION_TYPE.SCALE_IMAGE | HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(40, 40))[0];
    }

    public void Reset()
    {
        isTracked = false;
        face_rect = new Rectangle();
        th_check = false;
        center=new Point[]{new Point(0,0), new Point(0,0)};
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

        if (th_check)
        {
            backproject.ROI = face_rect;
            if (backproject.GetAverage().Intensity < backproj_threshold/2)
            {
                isTracked = false;
            }
            th_check = false;
            Emgu.CV.CvInvoke.cvResetImageROI(backproject);
        }

        hide.Height += 50;
        Emgu.CV.CvInvoke.cvSetImageROI(backproject, hide);
        try
        {
            Emgu.CV.CvInvoke.cvZero(backproject);
        }
        catch { }
        Emgu.CV.CvInvoke.cvResetImageROI(backproject);

        return backproject;
    }

    private Point[] kmeans(Point[] last_center,Image<Gray, Byte> img0, Rectangle face, double scale)
    {

        Image<Gray, Byte> img = img0.Resize(1/scale, INTER.CV_INTER_LINEAR);

        Point[] center = new Point[5] {new Point(img.Width / 4, img.Height / 2), new Point(img.Width * 3 / 4, img.Height / 2),new Point(),new Point(),new Point()};
        double[] x_accu = new double[5] { 0, 0 ,0,0,0};
        double[] y_accu = new double[5] { 0, 0,0,0,0};
        double[] mass = new double[5] { 0, 0,0,0,0};
        int n = 2;
        if (last_center[0].X != 0 || last_center[0].Y != 0)
        {
            for (int i = 0; i < 2; i++)
            {
                //center[i].X = (int)((double)last_center[i].X / scale);
                //center[i].Y = (int)((double)last_center[i].Y / scale);
            }
        }

        n = dummy_center(center, img.Size);

        bool term = false;
        for (int iter = 0; iter < 10; iter++)
        {
            if (term)
            {
                break;
            }
            for (int x = 0; x < img.Width; x++)
            {
                for (int y = 0; y < img.Height; y++)
                {
                    if (img.Data[y,x,0]==0)
                    {
                        continue;
                    }
                    double min = 1E10;
                    int minj = 0;
                    for (int j = 0; j < n; j++)
                    {
                        double temp = (x - center[j].X) * (x - center[j].X) + (y - center[j].Y) * (x - center[j].Y);
                        if (min > temp)
                        {
                            min = temp;
                            minj = j;
                        }
                    }
                    mass[minj] += img.Data[y,x,0];
                    x_accu[minj] += img.Data[y, x, 0] * x;
                    y_accu[minj] += img.Data[y, x, 0] * y;
                }
            }
            for (int j = 0; j < 2; j++)
            {
                if (mass[j] != 0)
                {
                    center[j] = new Point((int)(x_accu[j] / mass[j]), (int)(y_accu[j] / mass[j]));
                }
                x_accu[j] = 0;
                y_accu[j] = 0;
                mass[j] = 0;
            }
            n = dummy_center(center, img.Size);
        }
        for (int j = 0; j < 5; j++)
        {
            center[j].X = (int)(scale * center[j].X);
            center[j].Y = (int)(scale * center[j].Y);
        }
        return center;
    }

    private int dummy_center(Point[] center, Size img_size)
    {
        int n = 2;
        int[] region = new int[4] { 0, 0, 0, 0 };
        for (int i = 0; i < 2; i++)
        {
            if (center[i].X < img_size.Width / 2)
            {
                if (center[i].Y < img_size.Height / 2)
                {
                    region[0]++;
                }
                else
                {
                    region[2]++;
                }
            }
            else
            {
                if (center[i].Y < img_size.Height / 2)
                {
                    region[1]++;
                }
                else
                {
                    region[3]++;
                }
            }
        }
        for (int i = 0; i < 4; i++)
        {
            if (region[i] == 0)
            {
                switch (i)
                {
                    case 0:
                        center[n++] = new Point(0, 0);
                        break;
                    case 1:
                        center[n++] = new Point(img_size.Width, 0);
                        break;
                    case 2:
                        center[n++] = new Point(0, img_size.Height);
                        break;
                    case 3:
                        center[n++] = new Point(img_size);
                        break;
                }
            }
        }
        return n;
    }
}
