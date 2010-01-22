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

namespace Camfight
{
    public partial class Form1 : Form
    {
        public void SetAnimation()
        {
            int []index = new int[12] { 0, 1, 2, 3, 4, 5, 6, 7, 8,9,10,11 };
            for (int i = 0; i < 9; i++)
            {
                animationMove[i] = new ArrayList();
            }
            //set idle
            for (int i = 0; i < 2; i++) animationMove[0].Add(index[10]);
            for (int i = 0; i < 2; i++) animationMove[0].Add(index[0]);
            for (int i = 0; i < 2; i++) animationMove[0].Add(index[11]);
            for (int i = 0; i < 2; i++) animationMove[0].Add(index[0]);
            for (int i = 0; i < 2; i++) animationMove[0].Add(index[11]);
            for (int i = 0; i < 2; i++) animationMove[0].Add(index[10]);
            
            //set idle_lh
            animationMove[1].Add(index[0]);
            for (int i = 0; i < 4; i++) animationMove[1].Add(index[1]);
            animationMove[1].Add(index[0]);

            //set idle_rh
            animationMove[2].Add(index[0]);
            for (int i = 0; i < 4; i++) animationMove[2].Add(index[2]);
            animationMove[2].Add(index[0]);

            //set left
            animationMove[3].Add(index[0]);
            for (int i = 0; i < 4; i++) animationMove[3].Add(index[3]);
            animationMove[3].Add(index[0]);

            //set left_lh
            animationMove[4].Add(index[0]);
            animationMove[4].Add(index[3]);
            for (int i = 0; i < 4; i++) animationMove[4].Add(index[4]);
            animationMove[4].Add(index[3]);
            animationMove[4].Add(index[0]);

            //set left_rh
            animationMove[5].Add(index[0]);
            animationMove[5].Add(index[3]);
            for (int i = 0; i < 4; i++) animationMove[5].Add(index[5]);
            animationMove[5].Add(index[3]);
            animationMove[5].Add(index[0]);

            //set left
            animationMove[6].Add(index[0]);
            for (int i = 0; i < 4; i++) animationMove[6].Add(index[6]);
            animationMove[6].Add(index[0]);

            //set right_lh
            animationMove[7].Add(index[0]);
            animationMove[7].Add(index[6]);
            for (int i = 0; i < 4; i++) animationMove[7].Add(index[7]);
            animationMove[7].Add(index[6]);
            animationMove[7].Add(index[0]);

            //set right_rh
            animationMove[8].Add(index[0]);
            animationMove[8].Add(index[6]);
            for (int i = 0; i < 4; i++) animationMove[8].Add(index[8]);
            animationMove[8].Add(index[6]);
            animationMove[8].Add(index[0]);
        }
    }
}
