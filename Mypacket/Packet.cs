using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mypacket
{
        [Serializable]
        public class packet
        {

            public string Cmd
            {
                get { return cmd; }
            }
            private string cmd;

            public int Move
            {
                get { return move; }
            }
            private int move;

            public string Msg
            {
                get { return msg; }
            }
            private string msg;

            public string Name
            {
                get { return name; }
            }
            private string name;

            public int Time
            {
                get { return time; }
            }
            private int time;

            public int Sector
            {
                get { return sector; }
            }
            private int sector;

            public bool Big
            {
                get { return big; }
            }
            private bool big;

            public packet(string cmd, string name, string msg, int move, int time ,int sector ,bool big)
            {
                this.cmd = cmd;
                this.name = name;
                this.msg = msg;
                this.move = move;
                this.time = time;
                this.sector = sector;
                this.big = big;
            }
        }
}
