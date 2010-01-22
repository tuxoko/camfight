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
using System.IO;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.Threading;
using Emgu.CV.CvEnum;
using Hist;

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
        private int anicount = 0;
        private int playtime = 120;
        private bool playAnimation = false;
        private bool playIdle = false;
        
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
        private delegate void InvokeFunction2(packet pac);

        private FrameProcessor FPU;
        private DenseHistogram hist;

        public Form1()
        {
            InitializeComponent();
            SetAnimation();
            LoadingContent();
            //ConnectToServer();        
            //gamestate = GameState.GAME;
            myTimer.Tick += new EventHandler(GameDraw);
            myTimer.Interval = 25;
            myTimer.Start();
            controlTimer.Tick += new EventHandler(GameControlReset);
            controlTimer.Interval = 500;
            myplayerTimer.Tick += new EventHandler(PlayerStateReset);
            myplayerTimer.Interval = 5000;
            FPU = new FrameProcessor();
            FPU.Reset();

            IFormatter formatter = new BinaryFormatter();
            FileStream fs = new FileStream("../../hist.dat", FileMode.Open);
            HistSerial hs = (HistSerial)formatter.Deserialize(fs);
            hist = hs.hist;
            FPU.SetHist(hist);
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

                packet mypacket = new packet("l",username,password,0,0,0,false);

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

                    switch (receiveobj.Cmd)
                    {
                        case ("e"):
                            this.Invoke(new InvokeFunction(this.quit), new object[] { });
                            break;
                        case ("match"):
                            this.Invoke(new InvokeFunction2(this.GameStart), receiveobj);
                            //GameStart(receiveobj);
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

            if (fpu_thr != null)
                fpu_thr.Abort();
            fpu_thr = null;
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

        private Thread fpu_thr;
        private void GameStart(packet receiveobj)
        {
            enemyname = receiveobj.Name;
            LoadingEnemyContent(receiveobj.Msg);
            gamestate = GameState.GAME;
            //Application.Idle += new EventHandler(ProcessFrame);
            fpu_thr = new Thread(new ThreadStart(ProcessFrame));
            fpu_thr.Start();
        }

        private void LoadingEnemyContent(string type)
        {
            //if(type=="0")
                enemy = new Player("player1",Resources.player1, Resources.player1_lh, Resources.player1_rh, Resources.player1_left, Resources.player1_left_lh, Resources.player1_left_rh, Resources.player1_right, Resources.player1_right_lh, Resources.player1_right_rh,Resources.player1_2,Resources.player1_1,Resources.player1_3);
            //else if(type=="1")
              //  enemy = new Player("player2",Resources.player2, Resources.player2_lh, Resources.player2_rh, Resources.player2_left, Resources.player2_left_lh, Resources.player2_left_rh, Resources.player2_right, Resources.player2_right_lh, Resources.player2_right_rh,null,null,null);
        }
        private void LoadingContent()
        {   
            background = Resources.SPback;
            myplayer = new Player("player2",Resources.player2, Resources.player2_lh, Resources.player2_rh, Resources.player2_left, Resources.player2_left_lh, Resources.player2_left_rh, Resources.player2_right, Resources.player2_right_lh, Resources.player2_right_rh,null,null,null);
            //enemy = new Player("player1", Resources.player1, Resources.player1_lh, Resources.player1_rh, Resources.player1_left, Resources.player1_left_lh, Resources.player1_left_rh, Resources.player1_right, Resources.player1_right_lh, Resources.player1_right_rh);
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
                packet play = new packet("play", enemyname, null, mystate,0,0,false);
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
                packet mypacket = new packet("q",username,null,-1,0,0,false);
                SendPacket(mypacket);
                quit();
            }
            catch 
            { }
        }
    }
}
