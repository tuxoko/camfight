using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mypacket
{
    public class Packet
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

            public packet(string cmd, string name, string msg, int move)
            {
                this.cmd = cmd;
                this.name = name;
                this.msg = msg;
                this.move = move;
            }
        }
    }
}
