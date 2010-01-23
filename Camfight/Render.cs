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
                /*if (++count >= 40)
                {
                    count -= 40;
                    playtime--;
                }*/

                playtime = 120 - (int)sw.ElapsedMilliseconds / 1000;
                /*
                if (playAnimation == false)//no animation playing now
                {
                    if (myAnimation.Count != 0)
                    {
                        aniMutex.WaitOne();
                        nowplay = myAnimation.Dequeue() as Animation;
                        aniMutex.ReleaseMutex();
                        playindex = 0;
                        playMutex.WaitOne();
                        playAnimation = true;
                        playIdle = false;
                        playMutex.ReleaseMutex();
                    }
                    else
                    {
                        aniMutex.WaitOne();
                        nowplay = new Animation("player1", animationMove[0].Clone() as ArrayList);
                        aniMutex.ReleaseMutex();
                        playMutex.WaitOne();
                        playindex = 0;
                        playAnimation = true;
                        playIdle = true;
                        playMutex.ReleaseMutex();
                    }
                }
                if (playIdle == true)
                {
                    if (myAnimation.Count != 0)
                    {
                        aniMutex.WaitOne();
                        nowplay = myAnimation.Dequeue() as Animation;
                        aniMutex.ReleaseMutex();
                        playindex = 0;
                        playMutex.WaitOne();
                        playAnimation = true;
                        playIdle = false;
                        playMutex.ReleaseMutex();
                    }
                }
                
                if (playAnimation == true)
                {
                    if (playindex < nowplay.PlaySeq.Count && ++anicount >= 4)
                    {
                        Render((int)nowplay.PlaySeq[playindex++]);
                        anicount -= 4;
                    }
                    else
                    {
                        Render((int)nowplay.PlaySeq[playindex]);
                    }
                    if (playindex == nowplay.PlaySeq.Count)
                    {
                        playMutex.WaitOne();
                        playAnimation = false;
                        playMutex.ReleaseMutex();
                    }
                }*/

                aniMutex.WaitOne();
                Render(playstate);
                aniMutex.ReleaseMutex();
            }
            if (playtime == 0) myTimer.Stop();
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
            PointF[] myp = new PointF[2] { new PointF(10, 10), new PointF(10, 90) };
            //draw text
            for (int i = 0; i < 2; i++)
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
            Font name=new Font("Arial",15.0f);
            

            g.FillPolygon(Brushes.Red, new PointF[4]{p1[0],new PointF(p1[1].X-(100-myplayer.Life)*(280/100),p1[1].Y),new PointF(p1[2].X-(100-myplayer.Life)*(280/100),p1[2].Y),p1[3]});
            g.DrawPolygon(new Pen(Brushes.Yellow, 3), p1);
            g.FillPolygon(Brushes.Red, new PointF[4]{new PointF(p2[0].X+(100-enemy.Life)*(280/100),p2[0].Y),p2[1],p2[2],new PointF(p2[3].X+(100-enemy.Life)*(280/100),p2[3].Y)});
            g.DrawPolygon(new Pen(Brushes.Yellow, 3), p2);

            g.DrawString(playtime.ToString(), myfont, Brushes.Blue, new PointF(286, 10));

            g.DrawString(username, name, Brushes.Blue, new PointF(15, 35));
            g.DrawString(enemyname, name, Brushes.DeepSkyBlue, new PointF(600, 35));

            //player drawing
            if (index == -1)
            {
                enemy.draw(g, 0);
            }
            else
            {
                enemy.draw(g, index);
            }
            gamebox.Image = picShow;
            gamebox.Refresh();
            gamebox.Show();
        }
    }
}
