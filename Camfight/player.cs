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

        public int x;
        public int y;
        public Player(string type,Image img, Image img_lh, Image img_rh,Image img_left, Image img_left_lh, Image img_left_rh,Image img_right, Image img_right_lh,Image img_right_rh,Image im_idle_1,Image im_idle_2,Image im_idle_3)
        {
            player=new Image[12]{img,img_lh,img_rh,img_left,img_left_lh,img_left_rh,img_right,img_right_lh,img_right_rh,im_idle_1,im_idle_2,im_idle_3};
            life = 100;
            isalive = true;
            state = 0;
            this.type = type;
        }
        public void draw(Graphics g,int state) 
        {
            int offset = 0;
            if (2 < state && state < 6) offset = 30;
            else if (5 < state && state < 9) offset = -30;
            g.DrawImage(player[state],new Rectangle(170+offset,70,340,340));
        }

        public void update(int move)
        {
            this.state = move/3;
        }
        public void isHit(int move)
        {
            if (state== 0)
            {
                if (move%3!=0)
                {
                    getHurt(10);
                }
            }
            else
            {
                if ((move%3) != state && (move%3) != 0)
                {
                    getHurt(20);
                }
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
