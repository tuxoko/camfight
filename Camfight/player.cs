using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Camfight
{
    class Player
    {
        protected Image []player=null;
        public int Life
        {
            get { return life; }
        }
        private int life;
        public bool IsAlive
        {
            get { return isalive; }
        }
        private bool isalive;
        private int state;

        public string Type
        {
            get { return type; }
        }
        private string type;

        public bool Big_used;


        public int x=320-170;
        public int y=240-170;
        public Player(string type,Image img, Image img_lh, Image img_rh,Image img_left, Image img_left_lh, Image img_left_rh,Image img_right, Image img_right_lh,Image img_right_rh,Image im_idle_1,Image im_idle_2,Image im_idle_3)
        {
            player=new Image[12]{img,img_lh,img_rh,img_left,img_left_lh,img_left_rh,img_right,img_right_lh,img_right_rh,im_idle_1,im_idle_2,im_idle_3};
            life = 100;
            isalive = true;
            state = 0;
            this.type = type;
            Big_used = false;
        }
        public void draw(Graphics g,int state) 
        {
            g.DrawImage(player[state],new Rectangle(x,y,340,340));
        }

        public void drawLeft(Graphics g, int px, int py)
        {
            g.DrawImage(player[10], new Rectangle(px, py, 160, 160));
        }

        public void drawRight(Graphics g, int px, int py)
        {
            g.DrawImage(player[11], new Rectangle(px, py, 160, 160));
        }

        public void drawHead(Graphics g)
        {
            g.DrawImage(player[9], new Rectangle(x+170-100, 380, 200, 100));
        }

        public void update(int sector)
        {
            int v=10;
            if (sector < 6)
            {
                this.x -= v;
                if (this.x < 0) this.x = 0;
            }
            else if (sector >= 9)
            {
                this.x += v;
                if (this.x > 300) this.x = 300;
            }
        }
        public void isHit(int sector)
        {
            if ((x + 170) / (640 / 5) == sector / 3)
            {
                getHurt((3 - (sector % 3)) * 5);
            }
        }
        public void getHurt(int damage)
        {
            life -= damage;
            if (life <= 0) isalive = false;
        }
    }

    class Animation
    {
        public ArrayList PlaySeq
        {
            get { return seq; }
        }
        private ArrayList seq = null;

        public String Type
        {
            get { return type; }
        }
        private string type;

        public Animation(string type,ArrayList playseq)
        {
            this.type = type;
            this.seq = playseq;
        }
    }
}
