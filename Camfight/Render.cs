using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Collections;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Camfight.Properties;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Mypacket;

namespace Camfight
{
    public partial class Form1 : Form
    {
        private int playstate = 0;
        private PointF[] p1 = new PointF[4] { new PointF(15, 10), new PointF(295, 10), new PointF(285, 30), new PointF(5, 30) };
        private PointF[] p2 = new PointF[4] { new PointF(345, 10), new PointF(625, 10), new PointF(635, 30), new PointF(355, 30) };
        public void GameDraw(Object myObject, EventArgs myEventArgs)
        {
            if (gamestate == GameState.TITLE)
            {
                RenderTitle();
            }
            else if (gamestate == GameState.MENU)
            {
                RenderMenu();
            }
            else if (gamestate == GameState.INTERNET)
            {
                RenderLog();
            }
            else if (gamestate == GameState.LOADING)
            {
                RenderLoading();
            }
            else if (gamestate == GameState.GAME)
            {

                playtime = 120 - (int)sw.ElapsedMilliseconds / 1000;

                if (playtime == 0)
                {
                    if (enemy.Life > myplayer.Life) GameOver("l");
                    else if (enemy.Life < myplayer.Life) GameOver("w");
                    else GameOver("t");
                }
                else
                {
                    aniMutex.WaitOne();
                    Render(playstate);
                    aniMutex.ReleaseMutex();
                }
            }
            else if (gamestate == GameState.SINGLE)
            {
                playtime = 120 - (int)sw.ElapsedMilliseconds / 1000;

                if (playtime == 0)
                {
                    fpu_thr.Abort();
                    single_reset();
                }
                else
                {
                    aniMutex.WaitOne();
                    Render(playstate);
                    aniMutex.ReleaseMutex();
                }
            }
            else if (gamestate == GameState.END)
            {
                RenderGameOver();
            }
            
        }


        public void RenderTitle()
        {
            //background
            
            g = Graphics.FromImage(picShow);
            g.DrawImage(background, new Rectangle(0, 0, 640, 480));
            
            
            //title
            Font myfont = new Font("Arial Rounded MT Bold", 70.0f);
            g.DrawString("CAMFIGHT", myfont, Brushes.Black, new PointF(59, 24));
            g.DrawString("CAMFIGHT", myfont, Brushes.DarkOrange, new PointF(55, 20));

            Font myfont2 = new Font("Arial Bold", 50.0f);
            g.DrawString("START", myfont2, Brushes.Black, new PointF(204, 304));
            g.DrawString("START", myfont2, Brushes.Red, new PointF(200, 300));
            gamebox.Image = picShow;
            gamebox.Refresh();
            gamebox.Show();

        }

        public void RenderMenu()
        {
            //background
            g = Graphics.FromImage(picShow);
            g.DrawImage(background, new Rectangle(0, 0, 640, 480));

            Font myfont = new Font("Arial Rounded MT Bold", 60.0f);
            PointF[] myp = new PointF[3] { new PointF(10, 10), new PointF(10, 90) , new PointF(10,170)};
            //draw text
            for (int i = 0; i < menus.Length; i++)
            {
                if (menuIndex == i)
                {
                    g.DrawString(menus[i], myfont, Brushes.Red, myp[i]);
                }
                else
                {
                    g.DrawString(menus[i], myfont, Brushes.Black, myp[i]);
                }
            }
            gamebox.Image = picShow;
            gamebox.Refresh();
            gamebox.Show();
        }
        public void RenderLog()
        {
            //background
            g = Graphics.FromImage(picShow);
            g.DrawImage(background, new Rectangle(0, 0, 640, 480));

            Font myfont = new Font("Arial Rounded MT Bold", 50.0f);
            PointF[] myp = new PointF[2] { new PointF(10, 10), new PointF(10, 150) };
            //draw text
            for (int i = 0; i < 2; i++)
            {
                if (logIndex == i)
                {
                    g.DrawString(log[i], myfont, Brushes.Red, myp[i]);
                }
                else
                {
                    g.DrawString(log[i], myfont, Brushes.Black, myp[i]);
                }
            }
            if (username != null)
                g.DrawString(username, myfont, Brushes.GreenYellow, new PointF(10, 80));
            if (password != null)
            {
                string show = "";
                for (int i = 0; i < password.Length; i++)
                {
                    show += "*";
                }
                g.DrawString(show, myfont, Brushes.GreenYellow, new PointF(10, 220));
            }


            gamebox.Image = picShow;
            gamebox.Refresh();
            gamebox.Show();
        }

        public void RenderLoading()
        {
            //Draw background
            g = Graphics.FromImage(picShow);
            g.DrawImage(background, new Rectangle(0, 0, 640, 480));
            Font myfont = new Font("Arial Rounded MT Bold", 30.0f);


            //Draw game time
            g.DrawString("WAITING......", myfont, Brushes.Red, new PointF(200, 200));

            gamebox.Image = picShow;
            gamebox.Refresh();
            gamebox.Show();
        }

        public void Render(int index)
        {
            //background 
            g = Graphics.FromImage(picShow);
            g.DrawImage(background, new Rectangle(0, 0, 640, 480));
            //Font myfont = new Font("Arial Rounded MT Bold", 30.0f);
            /*
            g.DrawString("LIFE:" + myplayer.Life.ToString(), myfont, Brushes.Yellow, new PointF(10, 10));

            
            //Draw game time
            g.DrawString("TIME " + playtime.ToString(), myfont, Brushes.Red, new PointF(300, 10));*/
            //Draw life bar and game info
            Font myfont = new Font("Arial Rounded MT Bold", 23.0f);
            Font name=new Font("Arial Bold",15.0f);
            
            //player drawing
            if (index == -1)
            {
                enemy.draw(g, 0);
            }
            else
            {
                enemy.draw(g, index);
            }

            //flash big move
            if (big_flash > 0)
            {
                if (big_flash % 6 < 3)
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(175, Color.Black)), 0, 0, 640, 480);
                }

                big_flash--;
            }
            else if (hit_flash>0)
            {
                if (hit_flash % 6 < 3)
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(175, Color.Red)), 0, 0, 640, 480);
                }

                hit_flash--;
            }
            
            g.FillPolygon((myplayer.Life<big_threshold)?Brushes.Red:Brushes.LimeGreen, new PointF[4] { p1[0], new PointF(p1[1].X - (100 - myplayer.Life) * (float)(2.8), p1[1].Y), new PointF(p1[2].X - (100 - myplayer.Life) * (float)(2.8), p1[2].Y), p1[3] });
            g.DrawPolygon(new Pen(Brushes.Yellow, 3), p1);

            g.FillPolygon((enemy.Life < big_threshold) ? Brushes.Red : Brushes.LimeGreen, new PointF[4] { new PointF(p2[0].X + (100 - enemy.Life) * (float)(2.8), p2[0].Y), p2[1], p2[2], new PointF(p2[3].X + (100 - enemy.Life) * (float)(2.8), p2[3].Y) });
            g.DrawPolygon(new Pen(Brushes.Yellow, 3), p2);

            g.DrawString(playtime.ToString(), myfont, Brushes.Blue, new PointF(286, 10));
            
            g.DrawString(username, name, Brushes.Black, new PointF(17, 37));
            g.DrawString(username, name, Brushes.HotPink, new PointF(15, 35));

            g.DrawString(enemyname, name, Brushes.Black, new PointF(522, 37));
            g.DrawString(enemyname, name, Brushes.DeepSkyBlue, new PointF(520, 35));
            

            
            fpu_mutex.WaitOne();
            if (fpu_container.have_left_punch)
                g.DrawImage(Resources.Bang, 640 - fpu_container.center[1].X - 115, fpu_container.center[1].Y - 115,300,260);
            if (fpu_container.have_right_punch)
                g.DrawImage(Resources.Bang, 640 - fpu_container.center[0].X - 115, fpu_container.center[0].Y - 115, 300, 260);
            //Draw left hand
            if(fpu_container.have_left)
            myplayer.drawLeft(g,640-fpu_container.center[1].X-80,fpu_container.center[1].Y-80);
            //Draw right hand
            if(fpu_container.have_right)
            myplayer.drawRight(g, 640 - fpu_container.center[0].X-80, fpu_container.center[0].Y-80);
            //Draw head
            fpu_mutex.ReleaseMutex();
            myplayer.drawHead(g);

            gamebox.Image = picShow;
            gamebox.Refresh();
            gamebox.Show();
        }
        public void RenderGameOver()
        {
            g = Graphics.FromImage(picShow);
            g.DrawImage(background, new Rectangle(0, 0, 640, 480));

            Font myfont = new Font("Arial Rounded MT Bold", 30.0f);
            g.DrawString(msg[gameoverIndex],myfont,Brushes.Red,new PointF(200,100));

            if (retry == true)
            {
                g.DrawString("Play Again", myfont, Brushes.Red, new PointF(150, 200));
                g.DrawString("Quit",myfont,Brushes.Black,new PointF(150,300));
            }
            else
            {
                g.DrawString("Play Again", myfont, Brushes.Black, new PointF(150, 200));
                g.DrawString("Quit", myfont, Brushes.Red, new PointF(150, 300));
            }

            gamebox.Image = picShow;
            gamebox.Refresh();
            gamebox.Show();
        }
    }
}
