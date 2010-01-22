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
            TITLE=0,
            MENU=1,
            INTERNET=2,
            LOADING=3,
            GAME=4
        };

        //player object
        private Player myplayer=null;
        private Player enemy=null;
        private string enemyname="";
        private string username="";

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
        private NetworkStream nets = null;
        
        //GameState
        private GameState gamestate = GameState.TITLE;

        //Animation move
        private ArrayList [] animationMove=new ArrayList[9];

        //mutex

        private Mutex aniMutex = new Mutex();
        private Mutex playMutex = new Mutex();
        private Mutex controlMutex = new Mutex();
        private Mutex playerStateMutex = new Mutex();
        private Mutex enemyStateMutex = new Mutex();

        //game menu
        private string[] menus = new string[2] { "Network", "Quit" };
        private int menuIndex = 0;

        //game login
        private string password="";
        private int logIndex = 0;
        private string[] log = new string[2] { "Username", "Password" };

        private delegate void InvokeFunction();

        public Form1()
        {
            InitializeComponent();
            SetAnimation();
            LoadingContent();
            //ConnectToServer();        
            //gamestate = GameState.GAME;
            myTimer.Tick += new EventHandler(GameDraw);
            myTimer.Interval = 100;
            myTimer.Start();
            controlTimer.Tick += new EventHandler(GameControlReset);
            controlTimer.Interval = 500;
            myplayerTimer.Tick += new EventHandler(PlayerStateReset);
            myplayerTimer.Interval = 5000;
        }
        private void reset()
        {
            gamestate = GameState.TITLE;
            username = "";
            password = "";
            logIndex = 0;
            menuIndex = 0;
        }
        private void ConnectToServer()
        {
            try
            {
                IPAddress serverip = IPAddress.Parse("140.112.18.203");
                IPEndPoint serverhost = new IPEndPoint(serverip, 800);
                _tcpl = new TcpClient();
                _tcpl.Connect(serverhost);
                nets = _tcpl.GetStream();

                packet mypacket = new packet("l",username,password,0);

                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(nets,mypacket);


                listenthr = new Thread(new ThreadStart(listenThread));
                listenthr.Start();

            }
            catch
            {
                MessageBox.Show("連線錯誤");
                reset();
            }
        }

        private void listenThread()
        {
            try
            {
                while (true)
                {
                    IFormatter formatter = new BinaryFormatter();
                    //NetworkStream nets = _tcpl.GetStream();
                    packet receiveobj = (packet)formatter.Deserialize(nets);
                    MessageBox.Show(receiveobj.Cmd);
                    switch (receiveobj.Cmd)
                    {
                        case ("e"):
                            this.Invoke(new InvokeFunction(this.quit), new object[] { });
                            break;
                        case ("match"):
                            GameStart(receiveobj);
                            break;
                        case ("m"):
                            this.Invoke(new InvokeFunction(this.quit), new object[] {  }); 
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
            catch { quit(); }
        }

        private void quit()
        {
            if (listenthr != null)
                listenthr.Abort();
            listenthr = null;
            reset();
        }

        private void SendPacket(packet senddata)
        {
            try
            {
                NetworkStream net = _tcpl.GetStream();
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(net, senddata);
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
            else if (gamestate == GameState.GAME)
            {
                if (++count >= 10)
                {
                    count -= 10;
                    playtime--;
                }
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

        public void RenderTitle()
        {
            //background
            g = Graphics.FromImage(picShow);
            g.DrawImage(background,new Rectangle(0,0,640,480));
            //title
            Font myfont = new Font("Arial Rounded MT Bold", 70.0f);
            g.DrawString("CAMFIGHT", myfont, Brushes.Black, new PointF(59, 24));
            g.DrawString("CAMFIGHT",myfont,Brushes.DarkOrange,new PointF(55,20));

            Font myfont2 = new Font("Arial Bold",50.0f);
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
            PointF [] myp = new PointF[2]{new PointF(10,10),new PointF(10,90)};
            //draw text
            for (int i = 0; i < 2; i++)
            {
                if (menuIndex == i)
                {
                    g.DrawString(menus[i], myfont, Brushes.Red, myp[i]);
                }
                else
                {
                    g.DrawString(menus[i],myfont,Brushes.Black,myp[i]);
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
            if (username!=null)
            g.DrawString(username,myfont,Brushes.GreenYellow,new PointF(10,80));
            if (password != null)
            {
                string show="";
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
            if(gamestate==GameState.GAME)
            {
                GameInputControl(e);
            }
            else if(gamestate==GameState.TITLE)
            {
                gamestate = GameState.MENU;
                picShow = new Bitmap(640, 480);
            }
            else if (gamestate == GameState.MENU)
            {
                if(e.KeyData==Keys.Up)
                {
                    if (menuIndex > 0)
                    {
                        menuIndex--;
                    }
                }
                else if (e.KeyData == Keys.Down)
                {
                    if (menuIndex < 1)
                    {
                        menuIndex++;
                    }
                }
                else if (e.KeyData == Keys.Enter)
                {
                    if (menuIndex == 0)
                    {
                        gamestate=GameState.INTERNET;
                        picShow = new Bitmap(640, 480);
                    }
                    else if (menuIndex == 1)
                    {
                        gamestate = GameState.TITLE;
                        picShow = new Bitmap(640, 480);
                        menuIndex = 0;
                    }
                }
            }
            else if (gamestate == GameState.INTERNET)
            {
                LoginInputControl(e);
            }
        }

        private void LoginInputControl(KeyEventArgs e)
        {
            if (e.KeyData == Keys.Up)
            {
                logIndex = 0;
            }
            else if (e.KeyData == Keys.Down)
            {
                logIndex = 1;
            }
            else if (e.KeyValue>=65 && e.KeyValue<=90)
            {
                if (logIndex == 0 && username.Length<12)
                    username += e.KeyData.ToString();
                else if(logIndex==1 && password.Length<12)
                    password += e.KeyData.ToString();
            }
            else if (e.KeyValue >= 48 && e.KeyValue <= 57)
            {
                if (logIndex == 0 && username.Length < 12)
                    username += (e.KeyValue-48).ToString();
                else if (logIndex == 1 && password.Length < 12)
                    password += (e.KeyValue-48).ToString();
            }
            else if (e.KeyData == Keys.Back)
            {
                if (logIndex == 0 && username.Length != 0)
                    username = username.Substring(0, username.Length - 1);
                else if (logIndex == 1 && password.Length != 0)
                    password = password.Substring(0, password.Length - 1);
            }
            else if (e.KeyData == Keys.Enter)
            {
                if (username != null && password != null && username != "" && password != "")
                {
                    ConnectToServer();
                    gamestate = GameState.LOADING;
                }
            }
        }

        private void GameInputControl(KeyEventArgs e)
        {
            if (acceptControl == true)
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

            int mystate = 0;
            if (gamestate == GameState.GAME)
            {
                if (e.KeyData == Keys.J)
                {
                    playerStateMutex.WaitOne();
                    mystate = 3;
                    myplayer.update(3);
                    playerStateMutex.ReleaseMutex();
                }
                else if (e.KeyData == Keys.L)
                {
                    playerStateMutex.WaitOne();
                    mystate = 6;
                    myplayer.update(6);
                    playerStateMutex.ReleaseMutex();
                }
                packet play = new packet("play", enemyname, null, mystate);
                SendPacket(play);
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

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            
            try
            {
                packet mypacket = new packet("q",username,null,-1);
                SendPacket(mypacket);
                quit();
            }
            catch 
            { }
        }
    }
}
