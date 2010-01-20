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
using mypacket;

namespace Camfight
{
    public partial class Form1 : Form
    {
        enum GameState
        {
            MENU=0,
            INTERNET=1,
            GAME=2
        };

        //player object
        private Player myplayer=null;
        private Player enemy=null;
        private string enemyname;
        private string username;

        //use for rendering
        private Graphics g = null;
        private Image background = null;
        private Image picShow = new Bitmap(640,480);
        System.Windows.Forms.Timer myTimer = new System.Windows.Forms.Timer();
        private Queue<Animation> myAnimation = new Queue<Animation>();
        private Animation nowplay = null;
        private int playindex = 0;

        //use for input control
        System.Windows.Forms.Timer controlTimer = new System.Windows.Forms.Timer();
        private bool acceptControl = true;

        //use for updating player state
        System.Windows.Forms.Timer myplayerTimer = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer enemyTimer = new System.Windows.Forms.Timer();
        
        //use for time counting
        private int count = 0;
        private int playtime = 120;
        private bool playAnimation = false;
        
        //Thread for listening
        private TcpClient _tcpl = null;
        private Thread listenthr = null;
        
        //GameState
        private GameState gamestate = GameState.MENU;

        //Animation move
        private ArrayList [] animationMove=new ArrayList[9];

        //mutex

        private Mutex aniMutex = new Mutex();
        private Mutex playMutex = new Mutex();
        private Mutex controlMutex = new Mutex();
        private Mutex playerStateMutex = new Mutex();
        private Mutex enemyStateMutex = new Mutex();

        public Form1()
        {
            InitializeComponent();
            SetAnimation();
            username="test1";
            LoadingContent();
            ConnectToServer();        
            //gamestate = GameState.GAME;
            myTimer.Tick += new EventHandler(GameDraw);
            myTimer.Interval = 100;
            myTimer.Start();
            controlTimer.Tick += new EventHandler(GameControlReset);
            controlTimer.Interval = 500;
            myplayerTimer.Tick += new EventHandler(PlayerStateReset);
            myplayerTimer.Interval = 5000;
        }
        private void ConnectToServer()
        {
            try
            {
                IPAddress serverip = IPAddress.Parse("140.112.18.203");
                IPEndPoint serverhost = new IPEndPoint(serverip, 800);
                _tcpl = new TcpClient();
                _tcpl.Connect(serverhost);
                NetworkStream nets = _tcpl.GetStream();

                packet mypacket = new packet("l",username,null,0);

                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(nets,mypacket);

                listenthr = new Thread(new ThreadStart(listenThread));
                listenthr.Start();

            }
            catch
            {
                MessageBox.Show("連線錯誤");
                this.Close();
            }
        }

        private void listenThread()
        {
            try
            {
                while (true)
                {
                    IFormatter formatter = new BinaryFormatter();
                    NetworkStream nets = _tcpl.GetStream();
                    packet receiveobj = (packet)formatter.Deserialize(nets);
                    switch (receiveobj.Cmd)
                    {
                        case ("e"):
                            quit();
                            break;
                        case ("match"):
                            GameStart(receiveobj);
                            break;
                        case ("quit"):
                            break;
                        case ("play"):
                            EnemyMove(receiveobj);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch { }
        }

        private void quit()
        {
            if (listenthr != null)
                listenthr.Abort();
        }

        private void SendPacket(packet senddata)
        {
            try
            {
                NetworkStream nets = _tcpl.GetStream();
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(nets, senddata);
            }
            catch
            {
            }
        }

        private void GameStart(packet receiveobj)
        {
            enemyname = receiveobj.Name;
            LoadingEnemyContent(receiveobj.Msg);
            gamestate = GameState.GAME;
        }

        private void LoadingEnemyContent(string type)
        {
            if(type=="0")
                enemy = new Player("player1",Resources.player1, Resources.player1_lh, Resources.player1_rh, Resources.player1_left, Resources.player1_left_lh, Resources.player1_left_rh, Resources.player1_right, Resources.player1_right_lh, Resources.player1_right_rh);
            else if(type=="1")
                enemy = new Player("player2",Resources.player2, Resources.player2_lh, Resources.player2_rh, Resources.player2_left, Resources.player2_left_lh, Resources.player2_left_rh, Resources.player2_right, Resources.player2_right_lh, Resources.player2_right_rh);
        }
        private void LoadingContent()
        {   
            background = Resources.SPback;
            myplayer = new Player("player2",Resources.player2, Resources.player2_lh, Resources.player2_rh, Resources.player2_left, Resources.player2_left_lh, Resources.player2_left_rh, Resources.player2_right, Resources.player2_right_lh, Resources.player2_right_rh);
            //enemy = new Player("player1", Resources.player1, Resources.player1_lh, Resources.player1_rh, Resources.player1_left, Resources.player1_left_lh, Resources.player1_left_rh, Resources.player1_right, Resources.player1_right_lh, Resources.player1_right_rh);
        }

        public void GameDraw(Object myObject,EventArgs myEventArgs)
        {
            if (gamestate == GameState.GAME)
            {
                if (++count >= 10)
                {
                    count -= 10;
                    playtime--;
                }
                if (playAnimation == false)//no animation playing now
                {
                    if(myAnimation.Count!=0)
                    {
                        aniMutex.WaitOne();
                        nowplay = myAnimation.Dequeue() as Animation;
                        aniMutex.ReleaseMutex();
                        playindex = 0;
                        playMutex.WaitOne();
                        playAnimation = true;
                        playMutex.ReleaseMutex();
                    }
                    else
                    {
                        Render(-1);
                    }
                }
                if (playAnimation == true)
                {
                    if (playindex < nowplay.PlaySeq.Count)
                        Render((int)nowplay.PlaySeq[playindex++]);
                    if (playindex == nowplay.PlaySeq.Count)
                    {
                        playMutex.WaitOne();
                        playAnimation = false;
                        playMutex.ReleaseMutex();
                    }
                }
            }
            

            if (playtime == 0) myTimer.Stop();
        }

        public void Render(int index)
        {
            //background 
            g = Graphics.FromImage(picShow);
            g.DrawImage(background, new Rectangle(0, 0, 640, 480));
            Font myfont = new Font("Arial Rounded MT Bold", 30.0f);

            g.DrawString("LIFE:" + myplayer.Life.ToString(), myfont, Brushes.Yellow, new PointF(10, 10));
            

            //Draw game time
            g.DrawString("TIME " + playtime.ToString(), myfont, Brushes.Red, new PointF(300, 10));

            //player drawing
            if (index == -1)
                enemy.draw(g, 0);
            else
                enemy.draw(g, index);

            gamebox.Image = picShow;
            gamebox.Refresh();
            gamebox.Show();
        }

        public void EnemyMove(packet receiveobj)
        {
            enemy.update(receiveobj.Move);
            myplayer.isHit(receiveobj.Move);
            ArrayList seq = animationMove[receiveobj.Move].Clone() as ArrayList;
            aniMutex.WaitOne();
            myAnimation.Enqueue(new Animation("player1", seq));
            aniMutex.ReleaseMutex();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (gamestate == GameState.GAME && acceptControl==true)
            {
                if (e.KeyData == Keys.D1)
                {
                    ArrayList seq = animationMove[0].Clone() as ArrayList;
                    aniMutex.WaitOne();
                    myplayer.isHit(0);
                    enemy.update(0);
                    myAnimation.Enqueue(new Animation("player1", seq));
                    aniMutex.ReleaseMutex();
                }
                else if (e.KeyData == Keys.D2)
                {
                    ArrayList seq = animationMove[1].Clone() as ArrayList;
                    aniMutex.WaitOne();
                    myplayer.isHit(1);
                    enemy.update(1);
                    myAnimation.Enqueue(new Animation("player1", seq));
                    aniMutex.ReleaseMutex();
                }
                else if (e.KeyData == Keys.D3)
                {
                    ArrayList seq = animationMove[2].Clone() as ArrayList;
                    aniMutex.WaitOne();
                    myplayer.isHit(2);
                    enemy.update(2);
                    myAnimation.Enqueue(new Animation("player1", seq));
                    aniMutex.ReleaseMutex();
                }
                else if (e.KeyData == Keys.D4)
                {
                    ArrayList seq = animationMove[3].Clone() as ArrayList;
                    aniMutex.WaitOne();
                    myplayer.isHit(3);
                    enemy.update(3);
                    myAnimation.Enqueue(new Animation("player1", seq));
                    aniMutex.ReleaseMutex();
                }
                else if (e.KeyData == Keys.D5)
                {
                    ArrayList seq = animationMove[4].Clone() as ArrayList;
                    aniMutex.WaitOne();
                    myplayer.isHit(4);
                    enemy.update(4);
                    myAnimation.Enqueue(new Animation("player1", seq));
                    aniMutex.ReleaseMutex();
                }
                else if (e.KeyData == Keys.D6)
                {
                    ArrayList seq = animationMove[5].Clone() as ArrayList;
                    aniMutex.WaitOne();
                    myplayer.isHit(5);
                    enemy.update(5);
                    myAnimation.Enqueue(new Animation("player1", seq));
                    aniMutex.ReleaseMutex();
                }
                else if (e.KeyData == Keys.D7)
                {
                    ArrayList seq = animationMove[6].Clone() as ArrayList;
                    aniMutex.WaitOne();
                    myplayer.isHit(6);
                    enemy.update(6);
                    myAnimation.Enqueue(new Animation("player1", seq));
                    aniMutex.ReleaseMutex();
                }
                else if (e.KeyData == Keys.D8)
                {
                    ArrayList seq = animationMove[7].Clone() as ArrayList;
                    aniMutex.WaitOne();
                    myplayer.isHit(7);
                    enemy.update(7);
                    myAnimation.Enqueue(new Animation("player1", seq));
                    aniMutex.ReleaseMutex();
                }
                else if (e.KeyData == Keys.D9)
                {
                    ArrayList seq = animationMove[8].Clone() as ArrayList;
                    aniMutex.WaitOne();
                    myplayer.isHit(8);
                    enemy.update(8);
                    myAnimation.Enqueue(new Animation("player1", seq));
                    aniMutex.ReleaseMutex();
                }

                controlMutex.WaitOne();
                acceptControl = false;
                controlMutex.ReleaseMutex();

                controlTimer.Start();
            }

            if (gamestate == GameState.GAME)
            {
                if (e.KeyData == Keys.J)
                {
                    playerStateMutex.WaitOne();
                    myplayer.update(3);
                    playerStateMutex.ReleaseMutex();
                }
                else if (e.KeyData == Keys.L)
                {
                    playerStateMutex.WaitOne();
                    myplayer.update(6);
                    playerStateMutex.ReleaseMutex();
                }
                myplayerTimer.Start();
            }
        }

        public void GameControlReset(Object myObject, EventArgs myEventArgs)
        {
            controlMutex.WaitOne();
            acceptControl = true;
            controlMutex.ReleaseMutex();

            controlTimer.Stop();
        }

        public void PlayerStateReset(Object myObject, EventArgs myEventArgs)
        {
            playerStateMutex.WaitOne();
            myplayer.update(0);
            playerStateMutex.ReleaseMutex();
            myplayerTimer.Stop();   
        }
    }
}
