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

using Mypacket;

namespace Camfight
{
    public partial class Form1 : Form
    {
        private Capture _capture=new Capture();

        private Queue<int> big_move_q=new Queue<int>(30);
        private int last_big_move;

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


                int sector = (right_sector << 8) + (left_sector << 4) + face_sector;

                if (FPU.have_left_punch == true)
                {
                    sector = sector | 0xF00;
                }
                else if (FPU.have_right_punch == true)
                {
                    sector = sector | 0xF0;
                }
                bool big = false;
                if (FPU.have_right)
                {
                    if (last_big_move != right_sector)
                    {
                        if (big_move_q.Count >= 30)
                        {
                            big_move_q.Dequeue();
                        }
                        big_move_q.Enqueue(right_sector);
                        big=check_big_move(big_move_q);
                    }
                }
                else if(FPU.have_left) {
                    if (last_big_move != left_sector)
                    {
                        if (big_move_q.Count >= 20)
                        {
                            big_move_q.Dequeue();
                        }
                        big_move_q.Enqueue(left_sector);
                        big=check_big_move(big_move_q);
                    }
                }

                packet pac = new packet("play", enemyname, "", 0, 0, sector, big);
                SendPacket(pac);
                
            }
        }

        private int[] big_move=new int[8]{8,2,4,6,2,4,8,6};

        private bool check_big_move(Queue<int> big_move_q)
        {
            int i=0;
            bool big=false;
            foreach (int sec in big_move_q)
            {
                if (big_move[i] == 8)
                {
                    if (sec % 3 == 0)
                    {
                        i++;
                    }
                }
                else if (big_move[i] == 6)
                {
                    if (sec / 3 < 2)
                    {
                        i++;
                    }
                }
                else if (big_move[i] == 2)
                {
                    if (sec % 3 == 2)
                    {
                        i++;
                    }
                }
                else if (big_move[i] == 4)
                {
                    if (sec / 3 > 2)
                    {
                        i++;
                    }
                }
                if (i >= 8)
                {
                    big = true;
                    break;
                }
            }
            return big;
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
