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
using System.Diagnostics;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
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
            GAME=4,
            END=5,
            SINGLE=6
        };

        //player object
        private Player myplayer=null;
        private Player enemy=null;
        private string enemyname="";
        private string username="";

        //use for rendering
        private Graphics g = null;
        private Image background = Resources.SPback;
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
        private int playtime = 20;
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
        private string[] menus = new string[3] { "Network","Single", "Quit" };
        private int menuIndex = 0;

        //game login
        private string password="";
        private int logIndex = 0;
        private string[] log = new string[2] { "Username", "Password" };

        //game over
        private string[] msg = new string[3] { "You Win","You Lose","Game Tied"};
        private int gameoverIndex = 0;
        private bool retry = true;

        private delegate void InvokeFunction();
        private delegate void InvokeFunction2(packet pac);
        private delegate void InvokeFunction3(string cmd);

        private FrameProcessor FPU;
        private DenseHistogram hist;

        private Stopwatch sw = new Stopwatch();

        public Form1()
        {
            InitializeComponent();
            SetAnimation();
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
            gameoverIndex = 0;
            FPU.Reset();
            FPU.SetHist(hist);
        }
        private void ConnectToServer()
        {
            try
            {
                IPAddress serverip = IPAddress.Parse("140.112.18.202");
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
                quit();
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
                            this.Invoke(new InvokeFunction(this.quit), new object[] { });
                            break;
                        case ("q"):
                            this.Invoke(new InvokeFunction3(this.GameOver), new object[] { "w" });
                            break;
                        case ("play"):
                            if (receiveobj.Name == username)
                                EnemyMove(receiveobj);
                            else
                                mymove(receiveobj);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (ThreadAbortException e)
            { }
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
            big_flash = 0;
            enemyname = receiveobj.Name;
            LoadingEnemyContent(receiveobj);
            LoadingContent(receiveobj);
            gamestate = GameState.GAME;
            //Application.Idle += new EventHandler(ProcessFrame);
            fpu_thr = new Thread(new ThreadStart(ProcessFrame));
            fpu_thr.Start();
            sw.Reset();
            sw.Start();
        }

        private void LoadingEnemyContent(packet pac)
        {
            switch (pac.Time)
            {
                case 0:
                    enemy = new Player("player1", Resources.player1, Resources.player1_lh, Resources.player1_rh, Resources.player1_left, Resources.player1_left_lh, Resources.player1_left_rh, Resources.player1_right, Resources.player1_right_lh, Resources.player1_right_rh, Resources.H1, Resources.BGL, Resources.BGR);
                    break;
                case 1:
                    enemy = new Player("player1", Resources.player2, Resources.player2_lh, Resources.player2_rh, Resources.player2_left, Resources.player2_left_lh, Resources.player2_left_rh, Resources.player2_right, Resources.player2_right_lh, Resources.player2_right_rh, Resources.H2, Resources.BGL, Resources.BGR);
                    break;
                case 2:
                    //enemy = new Player("player1", Resources.player3, Resources.player3_lh, Resources.player3_rh, Resources.player3_left, Resources.player3_left_lh, Resources.player3_left_rh, Resources.player3_right, Resources.player3_right_lh, Resources.player3_right_rh, Resources.H3, Resources.BGL, Resources.BGR);
                    break;
                case 3:
                    //enemy = new Player("player1", Resources.player4, Resources.player4_lh, Resources.player4_rh, Resources.player4_left, Resources.player4_left_lh, Resources.player4_left_rh, Resources.player4_right, Resources.player4_right_lh, Resources.player4_right_rh, Resources.H4, Resources.BGL, Resources.BGR);
                    break;
                default:
                    enemy = new Player("player1", Resources.player2, Resources.player2_lh, Resources.player2_rh, Resources.player2_left, Resources.player2_left_lh, Resources.player2_left_rh, Resources.player2_right, Resources.player2_right_lh, Resources.player2_right_rh, Resources.H2, Resources.BGL, Resources.BGR);
                    break;
            }
        }
        private void LoadingContent(packet pac)
        {
            switch (pac.Move)
            {
                case 0:
                    myplayer = new Player("player2", Resources.player1, Resources.player1_lh, Resources.player1_rh, Resources.player1_left, Resources.player1_left_lh, Resources.player1_left_rh, Resources.player1_right, Resources.player1_right_lh, Resources.player1_right_rh, Resources.H1, Resources.BGL, Resources.BGR);
                    break;
                case 1:
                    myplayer = new Player("player2", Resources.player2, Resources.player2_lh, Resources.player2_rh, Resources.player2_left, Resources.player2_left_lh, Resources.player2_left_rh, Resources.player2_right, Resources.player2_right_lh, Resources.player2_right_rh, Resources.H2, Resources.BGL, Resources.BGR);
                    break;
                case 2:
                    //myplayer = new Player("player2", Resources.player3, Resources.player3_lh, Resources.player3_rh, Resources.player3_left, Resources.player3_left_lh, Resources.player3_left_rh, Resources.player3_right, Resources.player3_right_lh, Resources.player3_right_rh, Resources.H3, Resources.BGL, Resources.BGR);
                    break;
                case 3:
                    //myplayer = new Player("player2", Resources.player4, Resources.player4_lh, Resources.player4_rh, Resources.player4_left, Resources.player4_left_lh, Resources.player4_left_rh, Resources.player4_right, Resources.player4_right_lh, Resources.player4_right_rh, Resources.H4, Resources.BGL, Resources.BGR);
                    break;
                default:
                    myplayer = new Player("player2", Resources.player2, Resources.player2_lh, Resources.player2_rh, Resources.player2_left, Resources.player2_left_lh, Resources.player2_left_rh, Resources.player2_right, Resources.player2_right_lh, Resources.player2_right_rh, Resources.H2, Resources.BGL, Resources.BGR);
                    break;
            }
        }    

        public void EnemyMove(packet receiveobj)
        {
            enemy.update(receiveobj.Sector & 0xF);
            int i = 0;

            if (receiveobj.Big && (enemy.Big_used==false) && enemy.Life<big_threshold)
            {
                big_flash = 40;
                myplayer.getHurt(big_damage);
                enemy.Big_used = true;
            }
            else
            {
                //right
                if ((receiveobj.Sector & 0xF0) >> 4 == 0xF)
                {
                    myplayer.isHit((receiveobj.Sector & 0xF00) >> 8);
                    i = 2;
                }
                //left
                if ((receiveobj.Sector & 0xF00) >> 8 == 0xF)
                {
                    myplayer.isHit((receiveobj.Sector & 0xF0) >> 4);
                    i = 1;
                }
            }
            int face_sec = receiveobj.Sector & 0xF;

            if (face_sec / 3 < 2)
            {
                i += 6;
            }
            else if (face_sec / 3 > 2)
            {
                i += 3;
            }
             /*
                ArrayList seq = animationMove[i].Clone() as ArrayList;
                aniMutex.WaitOne();
                myAnimation.Enqueue(new Animation("player1", seq));
                aniMutex.ReleaseMutex();
            */
            
            if (myplayer.IsAlive == false)//win this game
            {
               // this.Invoke(new InvokeFunction(this.quit), new object[] { });
                this.Invoke(new InvokeFunction3(this.GameOver), new object[] {"l" });
            }

            aniMutex.WaitOne();
            playstate = i;
            aniMutex.ReleaseMutex();
        }

        private int big_flash=0;
        private int big_damage = 30;
        private int big_threshold = 30;

        public void mymove(packet receiveobj)
        {
            int sector=14-((receiveobj.Sector & 0xF));
            if (sector % 3 == 0) sector += 2;
            else if (sector % 3 == 2) sector -= 2;
           
            myplayer.update(sector);

            int i = 0;

            if (receiveobj.Big && (myplayer.Big_used==false) && myplayer.Life<big_threshold)
            {
                big_flash = 40;
                enemy.getHurt(big_damage);
                myplayer.Big_used = true;
            }
            else
            {
                //right
                if ((receiveobj.Sector & 0xF0) >> 4 == 0xF)
                {
                    int sector1 = 14 - ((receiveobj.Sector & 0xF00) >> 8);
                    if (sector1 % 3 == 0) sector1 += 2;
                    else if (sector1 % 3 == 2) sector1 -= 2;
                    enemy.isHit(sector1);
                    i = 2;
                }
                //left
                if ((receiveobj.Sector & 0xF00) >> 8 == 0xF)
                {
                    int sector1 = 14 - ((receiveobj.Sector & 0xF0) >> 4);
                    if (sector1 % 3 == 0) sector1 += 2;
                    else if (sector1 % 3 == 2) sector1 -= 2;
                    enemy.isHit(sector1);
                    i = 1;
                }
            }
            if (enemy.IsAlive == false)//win this game
            {
            //    this.Invoke(new InvokeFunction(this.quit), new object[] { });
                this.Invoke(new InvokeFunction3(this.GameOver), new object[] { "w" });
            }
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
                    if (menuIndex < menus.Length-1)
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
                        single_reset();
                        gamestate = GameState.SINGLE;
                    }
                    else if (menuIndex == 2)
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
            else if (gamestate == GameState.END)
            {
                if (e.KeyData == Keys.Left || e.KeyData == Keys.Up) retry = true;
                else if (e.KeyData == Keys.Right || e.KeyData == Keys.Down) retry = false;
                else if (e.KeyData == Keys.Enter)
                {
                    if (retry == true)
                    {
                        reset();
                    }
                    else
                    {
                        this.Close();
                    }
                }
            }
        }

        private void single_reset()
        {
            enemyname = "Bot";
            Random rd = new Random();
            LoadingEnemyContent(new packet("", "", "", 0, (rd.Next() % 2), 0, false));
            //Application.Idle += new EventHandler(ProcessFrame);
            fpu_thr = new Thread(new ThreadStart(ProcessFrame));
            fpu_thr.Start();
            sw.Reset();
            sw.Start();

            big_flash = 0;

            username = "Practice";
            LoadingContent(new packet("", "", "", (rd.Next() % 2), 0, 0, false));
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
                    if(listenthr!=null) gamestate = GameState.LOADING;
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

        private void GameOver(string cmd)
        {
            
            try
            {
                packet mypacket = new packet("q", username, null, -1, 0, 0, false);
                SendPacket(mypacket);
            }
            catch { }
            try
            {
                if (listenthr != null)
                    listenthr.Abort();
                   listenthr = null;            
                if (fpu_thr != null)
                    fpu_thr.Abort();
                fpu_thr = null;
            }
            catch { }
            if (cmd == "w")
            {
                gameoverIndex = 0;
            }
            else if(cmd=="l")
            {
                gameoverIndex = 1;
            }
            else
            {
                gameoverIndex = 2 ;
            }
            gamestate = GameState.END;
        }
    }
}
