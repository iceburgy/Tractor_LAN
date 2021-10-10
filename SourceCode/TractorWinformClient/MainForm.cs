using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;
using Duan.Xiugang.Tractor.Objects;
using Duan.Xiugang.Tractor.Player;
using Duan.Xiugang.Tractor.Properties;
using Kuaff.CardResouces;
using AutoUpdaterDotNET;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;

namespace Duan.Xiugang.Tractor
{
    internal partial class MainForm : Form
    {
        #region 变量声明

        //缓冲区图像
        internal Dictionary<string, int> PlayerPosition;
        internal Dictionary<int, string> PositionPlayer;
        internal int Scores = 0;
        internal List<int> SelectedCards;
        internal TractorPlayer ThisPlayer;
        internal object[] UserAlgorithms = {null, null, null, null};
        internal Bitmap bmp = null;
        internal CalculateRegionHelper calculateRegionHelper = null;
        internal Bitmap[] cardsImages = new Bitmap[54];
        internal int cardsOrderNumber = 0;
        internal bool updateOnLoad = false;
        internal bool enableSound = false;
        internal bool enableCutCards = false;
        internal string soundVolume = FormSettings.DefaultSoundVolume;
        internal bool showSuitSeq = false;
        internal string videoCallUrl = FormSettings.DefaultVideoCallUrl;
        internal MciSoundPlayer[] soundPlayersShowCard;
        internal MciSoundPlayer soundPlayerTrumpUpdated;
        internal MciSoundPlayer soundPlayerDiscardingLast8CardsFinished;
        internal MciSoundPlayer soundPlayerGameOver;
        internal MciSoundPlayer soundPlayerDraw;
        internal MciSoundPlayer soundPlayerDrawx;
        internal MciSoundPlayer soundPlayerDumpFailure;

        internal CurrentPoker[] currentAllSendPokers =
        {
            new CurrentPoker(), new CurrentPoker(), new CurrentPoker(),
            new CurrentPoker()
        };

        private log4net.ILog log;

        //原始背景图片

        //画图的次数（仅在发牌时使用）
        internal int currentCount = 0;

        internal CurrentPoker[] currentPokers =
        {
            new CurrentPoker(), new CurrentPoker(), new CurrentPoker(),
            new CurrentPoker()
        };

        internal int currentRank = 0;

        //当前一轮各家的出牌情况
        internal ArrayList[] currentSendCards = new ArrayList[4];
        internal CurrentState currentState;
        internal DrawingFormHelper drawingFormHelper = null;
        //应该谁出牌
        //一次出来中谁最先开始出的牌
        internal int firstSend = 0;
        internal GameConfig gameConfig = new GameConfig();
        internal Bitmap image = null;
        internal bool isNew = true;
        internal ArrayList myCardIsReady = new ArrayList();

        //*辅助变量
        //当前手中牌的坐标
        internal ArrayList myCardsLocation = new ArrayList();
        //当前手中牌的数值
        internal ArrayList myCardsNumber = new ArrayList();
        internal ArrayList[] pokerList = null;
        //当前手中牌的是否被点出
        //当前扣底的牌
        internal ArrayList send8Cards = new ArrayList();
        internal int showSuits = 0;

        //*画我的牌的辅助变量
        //画牌顺序
        internal long sleepMaxTime = 2000;
        internal long sleepTime;
        internal CardCommands wakeupCardCommands;

        //*绘画辅助类
        //DrawingForm变量

        //出牌时目前牌最大的那一家
        internal int whoIsBigger = 0;
        internal int whoShowRank = 0;
        internal int whoseOrder = 0; //0未定,1我，2对家，3西家,4东家

        internal int timerCountDown = 0;

        //音乐文件

        #endregion // 变量声明

        private readonly string roomControlPrefix = "roomControl";

        string fullLogFilePath = string.Format("{0}\\logs\\logfile.txt", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        public string rootReplayFolderPath = string.Format("{0}\\replays", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        string[] knownErrors = new string[] { 
            "Could not load file or assembly 'AutoUpdater.NET.XmlSerializers",
            "Application identity is not set",
            "does not exist in the appSettings configuration section",
            "because it is in the Faulted state",
            "The socket connection was aborted",
            "cannot be used for communication because it is in the Faulted state",
            "An existing connection was forcibly closed by the remote host",
            "while closing. You should only close your channel when you are not expecting any more input messages",
            "Exception has been thrown by the target of an invocation",
            "The server did not provide a meaningful reply; this might be caused by a contract mismatch, a premature session shutdown or an internal server error",
            "You should only close your channel when you are not expecting any more input messages",
            "No connection could be made",
            "connection attempt failed because the connected party did not properly respond after a period of time",
        };
        int firstWinNormal = 1;
        int firstWinBySha = 3;

        internal MainForm()
        {
            InitializeComponent();
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.StandardDoubleClick, true);


            //读取程序配置
            InitAppSetting();

            notifyIcon.Text = Text;
            BackgroundImage = image;

            //变量初始化
            bmp = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
            ThisPlayer = new TractorPlayer();

            LoadSoundResources();

            //加载当前设置
            LoadSettings();

            // setup logger
            log = LogUtilClient.Setup("logfile", fullLogFilePath);
            log.Debug(string.Format("player {0} launched game", ThisPlayer.MyOwnId));

            AppDomain currentDomain = default(AppDomain);
            currentDomain = AppDomain.CurrentDomain;
            // Handler for unhandled exceptions.
            currentDomain.FirstChanceException += GlobalFirstChanceExceptionHandler;
            // Handler for exceptions in threads behind forms.
            System.Windows.Forms.Application.ThreadException += GlobalThreadExceptionHandler;

            //创建大牌标记labels
            CreateOverridingLabels();

            //ping host
            this.progressBarPingHost.Visible = true;
            this.tmrGeneral.Start();
            this.lblSouthStarter.Text = "Connecting...";
            ThisPlayer.PingHost();
            
            ThisPlayer.PlayerOnGetCard += PlayerGetCard;
            ThisPlayer.GameOnStarted += StartGame;
            ThisPlayer.HostIsOnline += ThisPlayer_HostIsOnlineEventHandler;
            
            ThisPlayer.TrumpChanged += ThisPlayer_TrumpUpdated;
            ThisPlayer.AllCardsGot += ResortMyCards;
            ThisPlayer.ReenterFromOfflineEvent += ThisPlayer_ReenterFromOfflineEventHandler;            
            ThisPlayer.PlayerShowedCards += ThisPlayer_PlayerShowedCards;
            ThisPlayer.ShowingCardBegan += ThisPlayer_ShowingCardBegan;
            ThisPlayer.GameHallUpdatedEvent += ThisPlayer_GameHallUpdatedEventHandler;
            ThisPlayer.RoomSettingUpdatedEvent += ThisPlayer_RoomSettingUpdatedEventHandler;
            ThisPlayer.ShowAllHandCardsEvent += ThisPlayer_ShowAllHandCardsEventHandler;
            ThisPlayer.NewPlayerJoined += ThisPlayer_NewPlayerJoined;
            ThisPlayer.ReplayStateReceived += ThisPlayer_ReplayStateReceived;
            ThisPlayer.NewPlayerReadyToStart += ThisPlayer_NewPlayerReadyToStart;
            ThisPlayer.PlayerToggleIsRobot += ThisPlayer_PlayerToggleIsRobot;
            ThisPlayer.PlayersTeamMade += ThisPlayer_PlayersTeamMade;
            ThisPlayer.TrickFinished += ThisPlayer_TrickFinished;
            ThisPlayer.TrickStarted += ThisPlayer_TrickStarted;
            ThisPlayer.HandEnding += ThisPlayer_HandEnding;
            ThisPlayer.SpecialEndingEvent += ThisPlayer_SpecialEndingEventHandler;
            ThisPlayer.StarterFailedForTrump += ThisPlayer_StarterFailedForTrump;
            ThisPlayer.StarterChangedEvent += ThisPlayer_StarterChangedEventHandler;
            ThisPlayer.NotifyMessageEvent += ThisPlayer_NotifyMessageEventHandler;
            ThisPlayer.NotifyStartTimerEvent += ThisPlayer_NotifyStartTimerEventHandler;
            ThisPlayer.NotifyCardsReadyEvent += ThisPlayer_NotifyCardsReadyEventHandler;
            ThisPlayer.CutCardShoeCardsEvent += ThisPlayer_CutCardShoeCardsEventHandler;
            ThisPlayer.SpecialEndGameShouldAgreeEvent += ThisPlayer_SpecialEndGameShouldAgreeEventHandler;            
            ThisPlayer.ResortMyCardsEvent += ThisPlayer_ResortMyCardsEventHandler;
            ThisPlayer.Last8Discarded += ThisPlayer_Last8Discarded;
            ThisPlayer.DistributingLast8Cards += ThisPlayer_DistributingLast8Cards;
            ThisPlayer.DiscardingLast8 += ThisPlayer_DiscardingLast8;
            ThisPlayer.DumpingFail += ThisPlayer_DumpingFail;
            SelectedCards = new List<int>();
            PlayerPosition = new Dictionary<string, int>();
            PositionPlayer = new Dictionary<int, string>();
            drawingFormHelper = new DrawingFormHelper(this);
            calculateRegionHelper = new CalculateRegionHelper(this);

            for (int i = 0; i < 54; i++)
            {
                cardsImages[i] = null; //初始化
            }
        }

        private void LoadSettings()
        {
            var nickName = FormSettings.GetSettingString(FormSettings.KeyNickName);
            this.lblSouthNickName.Text = nickName;
            ThisPlayer.PlayerId = nickName;
            ThisPlayer.MyOwnId = nickName;
            updateOnLoad = FormSettings.GetSettingBool(FormSettings.KeyUpdateOnLoad);
            enableSound = FormSettings.GetSettingBool(FormSettings.KeyEnableSound);
            enableCutCards = FormSettings.GetSettingBool(FormSettings.KeyEnableCutCards);
            soundVolume = FormSettings.GetSettingString(FormSettings.KeySoundVolume);
            showSuitSeq = FormSettings.GetSettingBool(FormSettings.KeyShowSuitSeq);
            videoCallUrl = FormSettings.GetSettingString(FormSettings.KeyVideoCallUrl);
            setGameSoundVolume();
        }

        private void GlobalFirstChanceExceptionHandler(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            LogAndAlert(e.Exception.Message + "\n" + e.Exception.StackTrace);
        }

        private void GlobalThreadExceptionHandler(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Exception ex = default(Exception);
            ex = e.Exception;
            LogAndAlert(ex.Message + "\n" + ex.StackTrace);
        }

        private void LogAndAlert(string errMsg)
        {
            foreach (string knownErr in knownErrors)
            {
                if (errMsg.Contains(knownErr)) return;
            }
            log.Error("===game error encountered\n" + errMsg);
            MessageBox.Show(string.Format("游戏出错，请尝试重启游戏\n\n日志文件：\n{0}", fullLogFilePath));
        }

        private void LoadSoundResources()
        {
            //加载音效
            //get the full location of the assembly with DaoTests in it
            string fullPath = Assembly.GetExecutingAssembly().Location;
            //get the folder that's in
            string fullFolder = Path.GetDirectoryName(fullPath);
            soundPlayersShowCard = new MciSoundPlayer[] 
            { 
                new MciSoundPlayer(Path.Combine(fullFolder, "music\\equip1.mp3"), "equip1"),
                new MciSoundPlayer(Path.Combine(fullFolder, "music\\equip2.mp3"), "equip2"),
                new MciSoundPlayer(Path.Combine(fullFolder, "music\\zhu_junlve.mp3"), "zhu_junlve"),
                new MciSoundPlayer(Path.Combine(fullFolder, "music\\sha.mp3"), "sha"),
                new MciSoundPlayer(Path.Combine(fullFolder, "music\\sha_fire.mp3"), "sha_fire"),
                new MciSoundPlayer(Path.Combine(fullFolder, "music\\sha_thunder.mp3"), "sha_thunder"),
            };
            soundPlayerTrumpUpdated = new MciSoundPlayer(Path.Combine(fullFolder, "music\\biyue1.mp3"), "biyue1");
            soundPlayerDiscardingLast8CardsFinished = new MciSoundPlayer(Path.Combine(fullFolder, "music\\tie.mp3"), "tie");
            soundPlayerGameOver = new MciSoundPlayer(Path.Combine(fullFolder, "music\\win.mp3"), "win");
            soundPlayerDraw = new MciSoundPlayer(Path.Combine(fullFolder, "music\\draw.mp3"), "draw");
            soundPlayerDrawx = new MciSoundPlayer(Path.Combine(fullFolder, "music\\drawx.mp3"), "drawx");
            soundPlayerDumpFailure = new MciSoundPlayer(Path.Combine(fullFolder, "music\\fankui2.mp3"), "fankui2");

            foreach (MciSoundPlayer sp in soundPlayersShowCard)
            {
                sp.LoadMediaFiles();
            }
            soundPlayerTrumpUpdated.LoadMediaFiles();
            soundPlayerDiscardingLast8CardsFinished.LoadMediaFiles();
            soundPlayerGameOver.LoadMediaFiles();
            soundPlayerDraw.LoadMediaFiles();
            soundPlayerDrawx.LoadMediaFiles();
            soundPlayerDumpFailure.LoadMediaFiles();
        }

        private void setGameSoundVolume()
        {
            foreach (MciSoundPlayer sp in soundPlayersShowCard)
            {
                sp.SetVolume(Int32.Parse(soundVolume));
            }
            soundPlayerTrumpUpdated.SetVolume(Int32.Parse(soundVolume));
            soundPlayerDiscardingLast8CardsFinished.SetVolume(Int32.Parse(soundVolume));
            soundPlayerGameOver.SetVolume(Int32.Parse(soundVolume));
            soundPlayerDraw.SetVolume(Int32.Parse(soundVolume)); ;
            soundPlayerDrawx.SetVolume(Int32.Parse(soundVolume));
            soundPlayerDumpFailure.SetVolume(Int32.Parse(soundVolume));
        }

        private void CreateOverridingLabels()
        {
            //自己
            this.imbOverridingFlag_1.AutoSize = false;
            this.imbOverridingFlag_1.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.imbOverridingFlag_1);

            //下家
            this.imbOverridingFlag_2.AutoSize = false;
            this.imbOverridingFlag_2.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.imbOverridingFlag_2);

            //对家
            this.imbOverridingFlag_3.AutoSize = false;
            this.imbOverridingFlag_3.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.imbOverridingFlag_3);

            //上家
            this.imbOverridingFlag_4.AutoSize = false;
            this.imbOverridingFlag_4.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.imbOverridingFlag_4);
        }


        private void InitAppSetting()
        {
            //没有配置文件，则从config文件中读取
            if (!File.Exists("gameConfig"))
            {
                var reader = new AppSettingsReader();
                try
                {
                    Text = (String) reader.GetValue("title", typeof (String));
                }
                catch (Exception ex)
                {
                    Text = "拖拉机大战";
                }

                try
                {
                    gameConfig.MustRank = (String) reader.GetValue("mustRank", typeof (String));
                }
                catch (Exception ex)
                {
                    gameConfig.MustRank = ",3,8,11,12,13,";
                }

                try
                {
                    gameConfig.IsDebug = (bool) reader.GetValue("debug", typeof (bool));
                }
                catch (Exception ex)
                {
                    gameConfig.IsDebug = false;
                }

                try
                {
                    gameConfig.BottomAlgorithm = (int) reader.GetValue("bottomAlgorithm", typeof (int));
                }
                catch (Exception ex)
                {
                    gameConfig.BottomAlgorithm = 1;
                }
            }
            else
            {
                //实际从gameConfig文件中读取
                Stream stream = null;
                try
                {
                    IFormatter formatter = new BinaryFormatter();
                    stream = new FileStream("gameConfig", FileMode.Open, FileAccess.Read, FileShare.Read);
                    gameConfig = (GameConfig) formatter.Deserialize(stream);
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                }
            }

            //未序列化的值
            var myreader = new AppSettingsReader();
            gameConfig.CardsResourceManager = Kuaff_Cards.ResourceManager;
            try
            {
                var bkImage = (String) myreader.GetValue("backImage", typeof (String));
                image = new Bitmap(bkImage);
            }
            catch (Exception ex)
            {
                image = Resources.Backgroud;
            }

            try
            {
                Text = (String) myreader.GetValue("title", typeof (String));
            }
            catch (Exception ex)
            {
            }

            gameConfig.CardImageName = "";
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (AllOnline() && !ThisPlayer.isObserver && !ThisPlayer.isReplay && ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing)
            {
                DialogResult dialogResult = MessageBox.Show("游戏正在进行中，是否确定退出？", "是否确定退出", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            try
            {
                ThisPlayer.Quit();
            }
            catch (Exception)
            {
            }
        }

        //设置暂停的最大时间，以及暂停结束后的执行命令
        internal void SetPauseSet(int max, CardCommands wakeup)
        {
            sleepMaxTime = max;
            sleepTime = DateTime.Now.Ticks;
            wakeupCardCommands = wakeup;
            currentState.CurrentCardCommands = CardCommands.Pause;
        }

        #region 窗口事件处理程序

        private void init()
        {
            //每次初始化都重绘背景
            Graphics g = Graphics.FromImage(bmp);
            drawingFormHelper.DrawBackground(g);

            //目前不可以反牌
            showSuits = 0;
            whoShowRank = 0;

            //得分清零
            Scores = 0;


            //绘制Sidebar
            drawingFormHelper.DrawSidebar(g);
            //绘制东南西北

            drawingFormHelper.Starter();

            //绘制Rank
            drawingFormHelper.Rank();


            //绘制花色
            drawingFormHelper.Trump();

            send8Cards = new ArrayList();
            //调整花色
            if (currentRank == 53)
            {
                currentState.Suit = 5;
            }

            Refresh();
        }

        private void MainForm_MouseClick(object sender, MouseEventArgs e)
        {
            //录像回放不触发数标点击效果
            if (ThisPlayer.isReplay) return;

            //旁观不能触发选牌效果
            //出牌或者埋底时响应鼠标事件选牌
            if (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing ||
                ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8Cards)
            {
                if (!ThisPlayer.isObserver && e.Button == MouseButtons.Left) //左键
                {
                    if ((e.X >= (int)myCardsLocation[0] &&
                         e.X <= ((int)myCardsLocation[myCardsLocation.Count - 1] + 71 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor)) && (e.Y >= 355 + drawingFormHelper.offsetY && e.Y < 472 + drawingFormHelper.offsetY + 96 * (drawingFormHelper.scaleDividend - drawingFormHelper.scaleDivisor) / drawingFormHelper.scaleDivisor))
                    {
                        if (calculateRegionHelper.CalculateClickedRegion(e, 1))
                        {
                            drawingFormHelper.DrawMyPlayingCards(ThisPlayer.CurrentPoker);
                            Refresh();

                            ThisPlayer.CardsReady(ThisPlayer.PlayerId, myCardIsReady);
                        }
                    }
                }
                else if (e.Button == MouseButtons.Right) //右键
                {
                    if (!ThisPlayer.isObserver && 
                        e.X >= (int)myCardsLocation[0] &&
                        e.X <= ((int)myCardsLocation[myCardsLocation.Count - 1] + 71 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor) && 
                        e.Y >= 355 + drawingFormHelper.offsetY && e.Y < 472 + drawingFormHelper.offsetY + 96 * (drawingFormHelper.scaleDividend - drawingFormHelper.scaleDivisor) / drawingFormHelper.scaleDivisor)
                    {
                        int i = calculateRegionHelper.CalculateRightClickedRegion(e);
                        if (i > -1 && i < myCardIsReady.Count)
                        {
                            int readyCount = 0;
                            for (int ri = 0; ri < myCardIsReady.Count; ri++)
                            {
                                if (ri != i && (bool)myCardIsReady[ri]) readyCount++;
                            }
                            bool b = (bool)myCardIsReady[i];
                            int x = (int)myCardsLocation[i];
                            int clickedCardNumber = (int)myCardsNumber[i];
                            //响应右键的3种情况：
                            //1. 首出（默认）
                            int selectMoreCount = i;
                            bool isLeader = ThisPlayer.CurrentTrickState.Learder == ThisPlayer.PlayerId;
                            if (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8Cards)
                            {
                                //2. 埋底牌
                                selectMoreCount = Math.Min(i, 8 - 1 - readyCount);
                            }
                            else if (ThisPlayer.CurrentTrickState.LeadingCards != null && ThisPlayer.CurrentTrickState.LeadingCards.Count > 0)
                            {
                                //3. 跟出
                                selectMoreCount = Math.Min(i, ThisPlayer.CurrentTrickState.LeadingCards.Count - 1 - readyCount);
                            }
                            if (b)
                            {
                                var showingCardsCp = new CurrentPoker();
                                showingCardsCp.TrumpInt = (int)ThisPlayer.CurrentHandState.Trump;
                                showingCardsCp.Rank = ThisPlayer.CurrentHandState.Rank;

                                bool selectAll = false; //如果右键点的散牌，则向左选中所有本门花色的牌
                                for (int j = 1; j <= selectMoreCount; j++)
                                {
                                    //如果候选牌是同一花色
                                    if ((int)myCardsLocation[i - j] == (x - 12 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor))
                                    {
                                        if (isLeader)
                                        {
                                            //第一个出，候选牌为对子，拖拉机
                                            if (!selectAll)
                                            {
                                                int toAddCardNumber = (int)myCardsNumber[i - j];
                                                int toAddCardNumberOnRight = (int)myCardsNumber[i - j + 1];
                                                showingCardsCp.AddCard(toAddCardNumberOnRight);
                                                showingCardsCp.AddCard(toAddCardNumber);
                                            }

                                            if (showingCardsCp.Count == 2 && (showingCardsCp.GetPairs().Count == 1) || //如果是一对
                                                ((showingCardsCp.GetTractorOfAnySuit().Count > 1) &&
                                                showingCardsCp.Count == showingCardsCp.GetTractorOfAnySuit().Count * 2))  //如果是拖拉机
                                            {
                                                myCardIsReady[i - j] = b;
                                                myCardIsReady[i - j + 1] = b;
                                                x = x - 12 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor;
                                                j++;
                                            }
                                            else if (j == 1 || selectAll)
                                            {
                                                selectAll = true;
                                                myCardIsReady[i - j] = b;
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            //埋底或者跟出
                                            myCardIsReady[i - j] = b;
                                        }
                                        x = x - 12 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                for (int j = 1; j <= i; j++)
                                {
                                    if ((int)myCardsLocation[i - j] == (x - 12 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor))
                                    {
                                        myCardIsReady[i - j] = b;
                                        x = x - 12 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                            }
                            SelectedCards.Clear();
                            for (int k = 0; k < myCardIsReady.Count; k++)
                            {
                                if ((bool)myCardIsReady[k])
                                {
                                    SelectedCards.Add((int)myCardsNumber[k]);
                                }
                            }

                            drawingFormHelper.DrawMyPlayingCards(ThisPlayer.CurrentPoker);
                            Refresh();

                            ThisPlayer.CardsReady(ThisPlayer.PlayerId, myCardIsReady);
                        }
                    }
                    else
                    {
                        this.ThisPlayer.ShowLastTrickCards = !this.ThisPlayer.ShowLastTrickCards;
                        if (this.ThisPlayer.ShowLastTrickCards)
                        {
                            ShowLastTrickAndTumpMade();
                        }
                        else
                        {
                            ThisPlayer_PlayerCurrentTrickShowedCards();
                        }
                    }
                }
            }
            //旁观不能触发亮牌效果
            //发牌时响应鼠标事件亮牌
            else if (!ThisPlayer.isObserver &&
                (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DistributingCards ||
                ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DistributingCardsFinished))
            {
                ExposeTrump(e);
            }
            //一局结束时右键查看最后一轮各家所出的牌，缩小至一半，放在左下角
            else if (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Ending && e.Button == MouseButtons.Right) //右键
            {
                this.ThisPlayer.ShowLastTrickCards = !this.ThisPlayer.ShowLastTrickCards;
                if (this.ThisPlayer.ShowLastTrickCards)
                {
                    ShowLastTrickAndTumpMade();
                }
                else
                {
                    ThisPlayer_ShowEnding();
                }
            }
        }

        private void ShowLastTrickAndTumpMade()
        {
            //擦掉上一把
            drawingFormHelper.DrawCenterImage();
            drawingFormHelper.DrawScoreImageAndCards();

            this.drawingFormHelper.DrawMessages(new string[] { "回看上轮出牌、谁亮的牌" });
            //查看谁亮过什么牌
            drawingFormHelper.LastTrumpMadeCardsShow();
            //绘制上一轮各家所出的牌，缩小至一半，放在左下角，或者重画当前轮各家所出的牌
            ThisPlayer_PlayerLastTrickShowedCards();
        }

        private void ExposeTrump(MouseEventArgs e)
        {
            List<Suit> availableTrumps = ThisPlayer.AvailableTrumps();
            var trumpExposingToolRegion = new Dictionary<Suit, Region>();
            var heartRegion = new Region(new Rectangle(417 + drawingFormHelper.offsetCenter + 0 * 25 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor, 327 - 12 + drawingFormHelper.offsetY, 25 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor, 25 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor));
            trumpExposingToolRegion.Add(Suit.Heart, heartRegion);
            var spadeRegion = new Region(new Rectangle(417 + drawingFormHelper.offsetCenter + 1 * 25 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor, 327 - 12 + drawingFormHelper.offsetY, 25 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor, 25 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor));
            trumpExposingToolRegion.Add(Suit.Spade, spadeRegion);
            var diamondRegion = new Region(new Rectangle(417 + drawingFormHelper.offsetCenter + 2 * 25 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor, 327 - 12 + drawingFormHelper.offsetY, 25 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor, 25 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor));
            trumpExposingToolRegion.Add(Suit.Diamond, diamondRegion);
            var clubRegion = new Region(new Rectangle(417 + drawingFormHelper.offsetCenter + 3 * 25 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor, 327 - 12 + drawingFormHelper.offsetY, 25 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor, 25 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor));
            trumpExposingToolRegion.Add(Suit.Club, clubRegion);
            var jokerRegion = new Region(new Rectangle(417 + drawingFormHelper.offsetCenter + 4 * 25 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor, 327 - 12 + drawingFormHelper.offsetY, 25 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor, 25 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor));
            trumpExposingToolRegion.Add(Suit.Joker, jokerRegion);
            foreach (var keyValuePair in trumpExposingToolRegion)
            {
                if (keyValuePair.Value.IsVisible(e.X, e.Y))
                {
                    foreach (Suit trump in availableTrumps)
                    {
                        if (trump == keyValuePair.Key)
                        {
                            var next =
                                (TrumpExposingPoker)
                                    (Convert.ToInt32(ThisPlayer.CurrentHandState.TrumpExposingPoker) + 1);
                            if (trump == Suit.Joker)
                            {
                                if (ThisPlayer.CurrentPoker.RedJoker == 2)
                                    next = TrumpExposingPoker.PairRedJoker;
                                else if (ThisPlayer.CurrentPoker.BlackJoker == 2)
                                    next = TrumpExposingPoker.PairBlackJoker;
                            }
                            ThisPlayer.ExposeTrump(next, trump);
                        }
                    }
                }
            }
        }

        private void MainForm_MouseDoubleClick(object sender, MouseEventArgs e)
        {
        }

        private void ToDiscard8Cards()
        {
            //判断是否处在扣牌阶段
            if (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8Cards &&
                ThisPlayer.CurrentHandState.Last8Holder == ThisPlayer.PlayerId) //如果等我扣牌
            {
                if (SelectedCards.Count == 8)
                {
                    //扣牌,所以擦去小猪
                    this.btnPig.Visible = false;

                    foreach (int card in SelectedCards)
                    {
                        ThisPlayer.CurrentPoker.RemoveCard(card);
                    }

                    ThisPlayer.DiscardCards(ThisPlayer.MyOwnId, SelectedCards.ToArray());

                    ResortMyCards();
                }
            }
        }

        private void ToShowCards()
        {
            if ((ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing || ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8CardsFinished) &&
                ThisPlayer.CurrentTrickState.NextPlayer() == ThisPlayer.PlayerId)
            {
                ShowingCardsValidationResult showingCardsValidationResult =
                    TractorRules.IsValid(ThisPlayer.CurrentTrickState, SelectedCards, ThisPlayer.CurrentPoker);
                //如果我准备出的牌合法
                if (showingCardsValidationResult.ResultType == ShowingCardsValidationResultType.Valid)
                {
                    //擦去小猪
                    this.btnPig.Visible = false;

                    foreach (int card in SelectedCards)
                    {
                        ThisPlayer.CurrentPoker.RemoveCard(card);
                    }
                    ThisPlayer.ShowCards(ThisPlayer.MyOwnId, SelectedCards);
                    drawingFormHelper.DrawMyHandCards();
                    SelectedCards.Clear();
                }
                else if (showingCardsValidationResult.ResultType == ShowingCardsValidationResultType.TryToDump)
                {
                    //擦去小猪
                    this.btnPig.Visible = false;

                    ShowingCardsValidationResult result = ThisPlayer.ValidateDumpingCards(ThisPlayer.MyOwnId, SelectedCards);
                    if (result.ResultType == ShowingCardsValidationResultType.DumpingSuccess) //甩牌成功.
                    {
                        foreach (int card in SelectedCards)
                        {
                            ThisPlayer.CurrentPoker.RemoveCard(card);
                        }
                        ThisPlayer.ShowCards(ThisPlayer.MyOwnId, SelectedCards);

                        drawingFormHelper.DrawMyHandCards();
                        SelectedCards.Clear();
                    }
					//甩牌失败
                    else
                    {
                        this.drawingFormHelper.DrawMessages(new string[] { string.Format("甩牌{0}张失败", SelectedCards.Count), string.Format("罚分：{0}", SelectedCards.Count * 10) });
                        //甩牌失败播放提示音
                        soundPlayerDumpFailure.Play(this.enableSound);

                        Thread.Sleep(5000);
                        foreach (int card in result.MustShowCardsForDumpingFail)
                        {
                            ThisPlayer.CurrentPoker.RemoveCard(card);
                        }
                        ThisPlayer.ShowCards(ThisPlayer.MyOwnId, result.MustShowCardsForDumpingFail);

                        drawingFormHelper.DrawMyHandCards();
                        SelectedCards = result.MustShowCardsForDumpingFail;
                        SelectedCards.Clear();
                    }
                }
            }
        }

        //窗口绘画处理,将缓冲区图像画到窗口上
        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            //将bmp画到窗口上
            g.DrawImage(bmp, 0, 0);
        }

        #endregion

        #region 菜单事件处理

        internal void MenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem) sender;
            if (menuItem.Text.Equals("退出"))
            {
                Close();
            }
            else if (menuItem.Name.StartsWith("toolStripMenuItemBeginRank") && !ThisPlayer.isObserver)
            {
                if (AllOnline() && !ThisPlayer.isObserver && ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing)
                {
                    this.ThisPlayer_NotifyMessageEventHandler(new string[] { "游戏中途不允许修改从几打起", "请完成此盘游戏后重试" });
                    return;
                }

                string beginRankString = menuItem.Name.Substring("toolStripMenuItemBeginRank".Length, 1);
                ThisPlayer.SetBeginRank(ThisPlayer.MyOwnId, beginRankString);
            }
        }

        //托盘事件处理
        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            Show();
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            Activate();
        }
    

        #endregion // 菜单事件处理

        #region handle player event

        private void StartGame()
        {
            //游戏开始前重置各种变量
            this.ThisPlayer.ShowLastTrickCards = false;
            this.ThisPlayer.playerLocalCache = new PlayerLocalCache();
            this.btnSurrender.Visible = false;
            this.btnRiot.Visible = false;
            this.ThisPlayer.CurrentTrickState.serverLocalCache.lastShowedCards = new Dictionary<string, List<int>>();
            this.timerCountDown = 0;

            init();
        }

        private void PlayerGetCard(int cardNumber)
        {
            //发牌播放提示音
            if (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DistributingCards)
            {
                soundPlayerDraw.Play(this.enableSound);
            }

            drawingFormHelper.IGetCard(cardNumber);

            //托管代打：亮牌
            if (gameConfig.IsDebug && (ThisPlayer.CurrentRoomSetting.IsFullDebug || ThisPlayer.CurrentRoomSetting.AllowRobotMakeTrump) && !ThisPlayer.isObserver)
            {
                var availableTrump = ThisPlayer.AvailableTrumps();
                Suit trumpToExpose = Algorithm.TryExposingTrump(availableTrump, this.ThisPlayer.CurrentPoker, ThisPlayer.CurrentRoomSetting.IsFullDebug);
                if (trumpToExpose == Suit.None) return;

                var next =
                    (TrumpExposingPoker)
                        (Convert.ToInt32(ThisPlayer.CurrentHandState.TrumpExposingPoker) + 1);
                if (trumpToExpose == Suit.Joker)
                {
                    if (ThisPlayer.CurrentPoker.BlackJoker == 2)
                        next = TrumpExposingPoker.PairBlackJoker;
                    else if (ThisPlayer.CurrentPoker.RedJoker == 2)
                        next = TrumpExposingPoker.PairRedJoker;
                }
                ThisPlayer.ExposeTrump(next, trumpToExpose);
            }
        }


        private void ThisPlayer_TrumpUpdated(CurrentHandState currentHandState)
        {
            if (HandStep.DistributingCards <= this.ThisPlayer.CurrentHandState.CurrentHandStep &&
                this.ThisPlayer.CurrentHandState.CurrentHandStep < HandStep.DistributingLast8Cards)
            {
                soundPlayerTrumpUpdated.Play(this.enableSound);
            }

            ThisPlayer.CurrentHandState = currentHandState;
            drawingFormHelper.Trump();
            if (this.ThisPlayer.CurrentHandState.CurrentHandStep < HandStep.DistributingLast8Cards) drawingFormHelper.TrumpMadeCardsShow();
            drawingFormHelper.ReDrawToolbar();
            if (ThisPlayer.CurrentHandState.IsFirstHand)
            {
                drawingFormHelper.Rank();
                drawingFormHelper.Starter();
            }
        }

        private void ResortMyCards()
        {
            drawingFormHelper.DrawMySortedCards(ThisPlayer.CurrentPoker, ThisPlayer.CurrentPoker.Count);
        }

        //断线重连，重画手牌和出的牌
        private void ThisPlayer_ReenterFromOfflineEventHandler()
        {
            if (ThisPlayer.IsTryingReenter)
            {
                this.ThisPlayer.playerLocalCache.ShowedCardsInCurrentTrick = ThisPlayer.CurrentTrickState.ShowedCards.ToDictionary(entry => entry.Key, entry => entry.Value.ToList());
                this.ThisPlayer_PlayerCurrentTrickShowedCards();
            }
            drawingFormHelper.DrawMyPlayingCards(ThisPlayer.CurrentPoker);
        }

        //检查当前出牌者的牌是否为大牌：0 - 否；1 - 是；2 - 是且为吊主；3 - 是且为主毙牌
        private int IsWinningWithTrump(CurrentTrickState trickState, string playerID)
        {
            bool isLeaderTrump = PokerHelper.IsTrump(trickState.LeadingCards[0], ThisPlayer.CurrentHandState.Trump, ThisPlayer.CurrentHandState.Rank);
            if (playerID == trickState.Learder)
            {
                if (isLeaderTrump) return 2;
                else return 1;
            }
            string winnerID = TractorRules.GetWinnerWithoutAllShowedCards(trickState);
            if (playerID == winnerID)
            {
                bool isWinnerTrump = PokerHelper.IsTrump(trickState.ShowedCards[winnerID][0], ThisPlayer.CurrentHandState.Trump, ThisPlayer.CurrentHandState.Rank);
                if (!isLeaderTrump && isWinnerTrump) return 3;
                return 1;
            }
            return 0;
        }

        private void ThisPlayer_PlayerShowedCards()
        {
            if (!ThisPlayer.CurrentHandState.PlayerHoldingCards.ContainsKey(ThisPlayer.CurrentTrickState.Learder)) return;

            //如果新的一轮开始，重置缓存信息
            if (ThisPlayer.CurrentTrickState.CountOfPlayerShowedCards() == 1)
            {
                this.ThisPlayer.playerLocalCache = new PlayerLocalCache();
            }

            if (ThisPlayer.CurrentHandState.PlayerHoldingCards[ThisPlayer.CurrentTrickState.Learder].Count == 0)
            {
                this.ThisPlayer.playerLocalCache.isLastTrick = true;
            }

            string latestPlayer = ThisPlayer.CurrentTrickState.LatestPlayerShowedCard();
            this.ThisPlayer.playerLocalCache.ShowedCardsInCurrentTrick = ThisPlayer.CurrentTrickState.ShowedCards.ToDictionary(entry => entry.Key, entry => entry.Value.ToList());

            int winResult = this.IsWinningWithTrump(ThisPlayer.CurrentTrickState, latestPlayer);
            int position = PlayerPosition[latestPlayer];
            //如果大牌变更，更新缓存相关信息
            if (winResult >= firstWinNormal)
            {
                if (winResult < firstWinBySha || this.ThisPlayer.playerLocalCache.WinResult < firstWinBySha)
                {
                    this.ThisPlayer.playerLocalCache.WinResult = winResult;
                }
                else
                {
                    this.ThisPlayer.playerLocalCache.WinResult++;
                }
                this.ThisPlayer.playerLocalCache.WinnerPosition = position;
                this.ThisPlayer.playerLocalCache.WinnderID = latestPlayer;
            }

            //如果不在回看上轮出牌，才重画刚刚出的牌
            if (!this.ThisPlayer.ShowLastTrickCards)
            {
                //擦掉上一把
                if (ThisPlayer.CurrentTrickState.CountOfPlayerShowedCards() == 1)
                {
                    drawingFormHelper.DrawCenterImage();
                    drawingFormHelper.DrawScoreImageAndCards();
                }

                //播放出牌音效
                int soundInex = winResult;
                if (winResult > 0) soundInex = this.ThisPlayer.playerLocalCache.WinResult;
                if (!this.ThisPlayer.playerLocalCache.isLastTrick &&
                    !gameConfig.IsDebug &&
                    !ThisPlayer.CurrentTrickState.serverLocalCache.muteSound)
                {
                    soundPlayersShowCard[soundInex].Play(this.enableSound);
                }

                if (position == 1)
                {
                    drawingFormHelper.DrawMyShowedCards();
                }
                else if (position == 2)
                {
                    drawingFormHelper.DrawNextUserSendedCards();
                }
                else if (position == 3)
                {
                    drawingFormHelper.DrawFriendUserSendedCards();
                }
                else if (position == 4)
                {
                    drawingFormHelper.DrawPreviousUserSendedCards();
                }
            }

            //如果正在回看并且自己刚刚出了牌，则重置回看，重新画牌
            if (this.ThisPlayer.ShowLastTrickCards && latestPlayer == ThisPlayer.PlayerId)
            {
                this.ThisPlayer.ShowLastTrickCards = false;
                ThisPlayer_PlayerCurrentTrickShowedCards();
            }

            if (!gameConfig.IsDebug && ThisPlayer.CurrentTrickState.NextPlayer() == ThisPlayer.PlayerId)
                drawingFormHelper.DrawMyPlayingCards(ThisPlayer.CurrentPoker);

            //即时更新旁观手牌
            if (ThisPlayer.isObserver && ThisPlayer.PlayerId == latestPlayer)
            {
                ThisPlayer.CurrentPoker = ThisPlayer.CurrentHandState.PlayerHoldingCards[ThisPlayer.PlayerId];
                ResortMyCards();
            }

            if (winResult > 0) drawingFormHelper.DrawOverridingFlag(this.PlayerPosition[this.ThisPlayer.playerLocalCache.WinnderID], this.ThisPlayer.playerLocalCache.WinResult - 1, 1);

            RobotPlayFollowing();
        }

        //绘制上一轮各家所出的牌，缩小至一半，放在左下角
        private void ThisPlayer_PlayerLastTrickShowedCards()
        {
            string lastLeader = ThisPlayer.CurrentTrickState.serverLocalCache.lastLeader;
            if (string.IsNullOrEmpty(lastLeader) ||
                ThisPlayer.CurrentTrickState.serverLocalCache.lastShowedCards.Count == 0) return;

            CurrentTrickState trickState = new CurrentTrickState();
            trickState.Learder = lastLeader;
            trickState.Trump = ThisPlayer.CurrentTrickState.Trump;
            trickState.Rank = ThisPlayer.CurrentTrickState.Rank;

            foreach (var entry in ThisPlayer.CurrentTrickState.serverLocalCache.lastShowedCards)
            {
                trickState.ShowedCards.Add((string)entry.Key, (List<int>)entry.Value);
            }

            foreach (var entry in trickState.ShowedCards)
            {
                int position = PlayerPosition[entry.Key];
                if (position == 1)
                {
                    drawingFormHelper.DrawMyLastSendedCardsAction(new ArrayList(entry.Value));
                }
                else if (position == 2)
                {
                    drawingFormHelper.DrawNextUserLastSendedCardsAction(new ArrayList(entry.Value));
                }
                else if (position == 3)
                {
                    drawingFormHelper.DrawFriendUserLastSendedCardsAction(new ArrayList(entry.Value));
                }
                else if (position == 4)
                {
                    drawingFormHelper.DrawPreviousUserLastSendedCardsAction(new ArrayList(entry.Value));
                }
            }
            string winnerID = TractorRules.GetWinner(trickState);
            int tempIsWinByTrump = this.IsWinningWithTrump(trickState, winnerID);
            drawingFormHelper.DrawOverridingFlag(this.PlayerPosition[winnerID], tempIsWinByTrump - 1, 0);
            Refresh();
        }

        //绘制当前轮各家所出的牌（仅用于切换视角或当前回合大牌变更时）
        private void ThisPlayer_PlayerCurrentTrickShowedCards()
        {
            //擦掉出牌区
            drawingFormHelper.DrawCenterImage();
            drawingFormHelper.DrawScoreImageAndCards();

            if (this.ThisPlayer.playerLocalCache.ShowedCardsInCurrentTrick != null)
            {
                foreach (var entry in this.ThisPlayer.playerLocalCache.ShowedCardsInCurrentTrick)
                {
                    string player = entry.Key;
                    int position = PlayerPosition[player];
                    if (position == 1)
                    {
                        drawingFormHelper.DrawMySendedCardsAction(new ArrayList(entry.Value));
                    }
                    else if (position == 2)
                    {
                        drawingFormHelper.DrawNextUserSendedCardsAction(new ArrayList(entry.Value));
                    }
                    else if (position == 3)
                    {
                        drawingFormHelper.DrawFriendUserSendedCardsAction(new ArrayList(entry.Value));
                    }
                    else if (position == 4)
                    {
                        drawingFormHelper.DrawPreviousUserSendedCardsAction(new ArrayList(entry.Value));
                    }
                }
            }
            //重画亮过的牌
            if (this.ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8Cards)
            {
                drawingFormHelper.TrumpMadeCardsShow();
            }

            //重画大牌标记
            if (!string.IsNullOrEmpty(this.ThisPlayer.playerLocalCache.WinnderID))
            {
                drawingFormHelper.DrawOverridingFlag(this.PlayerPosition[this.ThisPlayer.playerLocalCache.WinnderID], this.ThisPlayer.playerLocalCache.WinResult - 1, 1);
            }
            Refresh();
        }

        //绘制当前结束画面（仅用于切换视角时）
        private void ThisPlayer_ShowEnding()
        {
            //擦掉出牌区
            drawingFormHelper.DrawCenterImage();

            ThisPlayer_HandEnding();
        }

        private void ThisPlayer_ShowingCardBegan()
        {
            ThisPlayer_DiscardingLast8();
            drawingFormHelper.RemoveToolbar();
            drawingFormHelper.DrawCenterImage();
            drawingFormHelper.DrawScoreImageAndCards();

            //出牌开始前，去掉不需要的controls
            this.btnSurrender.Visible = false;
            this.btnRiot.Visible = false;

            Refresh();
        }

        private void ThisPlayer_PlayersTeamMade()
        {
            //set player position
            PlayerPosition.Clear();
            PositionPlayer.Clear();
            string nextPlayer = ThisPlayer.PlayerId;
            int postion = 1;
            PlayerPosition.Add(nextPlayer, postion);
            PositionPlayer.Add(postion, nextPlayer);
            nextPlayer = ThisPlayer.CurrentGameState.GetNextPlayerAfterThePlayer(nextPlayer).PlayerId;
            while (nextPlayer != ThisPlayer.PlayerId)
            {
                postion++;
                PlayerPosition.Add(nextPlayer, postion);
                PositionPlayer.Add(postion, nextPlayer);
                nextPlayer = ThisPlayer.CurrentGameState.GetNextPlayerAfterThePlayer(nextPlayer).PlayerId;
            }
        }

        private void ThisPlayer_NewPlayerJoined()
        {
            if (this.ToolStripMenuItemEnterRoom0.Enabled || ThisPlayer.IsTryingReenter || ThisPlayer.IsTryingResumeGame)
            {
                this.ToolStripMenuItemEnterRoom0.Enabled = false;
                HideRoomControls();
                init();
            }
            if (!ThisPlayer.isObserver)
            {
                this.btnReady.Show();
                this.btnRobot.Show();
                this.ToolStripMenuItemInRoom.Visible = true;
            }
            else
            {
                this.btnObserveNext.Show();
                this.ToolStripMenuItemObserve.Visible = true;
            }
            this.btnRoomSetting.Show();

            //在大厅里，左边的label挡住了online玩家名字，只好先隐藏，进了房间再显示
            this.lblWestNickName.Show();
            this.lblWestStarter.Show();

            int curIndex = -1;
            for (int i = 0; i < 4; i++)
            {
                var curPlayer = ThisPlayer.CurrentGameState.Players[i];
                if (curPlayer != null && curPlayer.PlayerId == ThisPlayer.PlayerId)
                {
                    curIndex = i;
                    break;
                }
            }
            System.Windows.Forms.Label[] nickNameLabels = new System.Windows.Forms.Label[] { this.lblSouthNickName, this.lblEastNickName, this.lblNorthNickName, this.lblWestNickName };
            for (int i = 0; i < 4; i++)
            {
                var curPlayer = ThisPlayer.CurrentGameState.Players[curIndex];
                if (curPlayer != null)
                {
                    nickNameLabels[i].Text = curPlayer.PlayerId;
                    foreach (string ob in curPlayer.Observers)
                    {
                        string newLine = i == 0 ? "" : "\n";
                        nickNameLabels[i].Text += string.Format("{0}【{1}】", newLine, ob);
                    }
                }
                else
                {
                    nickNameLabels[i].Text = "";
                }
                curIndex = (curIndex + 1) % 4;
            }

            bool isHelpSeen = FormSettings.GetSettingBool(FormSettings.KeyIsHelpSeen);
            if (!isHelpSeen)
            {
                this.ToolStripMenuItemUserManual.PerformClick();
            }
        }

        private void ThisPlayer_ReplayStateReceived(ReplayEntity replayState)
        {
            string[] fileInfo = CommonMethods.GetReplayEntityFullFilePath(replayState, rootReplayFolderPath);
            if (fileInfo != null)
            {
                CommonMethods.WriteObjectToFile(replayState, fileInfo[0], string.Format("{0}", fileInfo[1]));
            }
            else
            {
                this.drawingFormHelper.DrawMessages(new string[] { "录像文件名错误", replayState.ReplayId });
            }
        }

        private void DisplayRoomSetting(bool isRoomSettingModified)
        {
            List<string> msgs = new List<string>();
            if (this.ThisPlayer.CurrentRoomSetting.DisplaySignalCardInfo)
            {
                msgs.AddRange(new string[] { "信号牌机制声明：", "8、9、J、Q代表本门有进手张", "级牌调主代表寻求对家帮忙清主", "" });
            }
            if (isRoomSettingModified)
            {
                msgs.Add("房间设置已更改！");
            }
            else
            {
                msgs.Add("房间设置：");
            }
            msgs.Add(string.Format("允许投降：{0}", this.ThisPlayer.CurrentRoomSetting.AllowSurrender ? "是" : "否"));
            msgs.Add(string.Format("允许J到底：{0}", this.ThisPlayer.CurrentRoomSetting.AllowJToBottom ? "是" : "否"));
            msgs.Add(string.Format("允许托管自动亮牌：{0}", this.ThisPlayer.CurrentRoomSetting.AllowRobotMakeTrump ? "是" : "否"));
            msgs.Add(string.Format("允许分数小于等于X时革命：{0}", this.ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewScoreCards >= 0 ? this.ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewScoreCards.ToString() : "否"));
            msgs.Add(string.Format("允许主牌小于等于X张时革命：{0}", this.ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewTrumpCards >= 0 ? this.ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewTrumpCards.ToString() : "否"));
            msgs.Add(string.Format("断线重连等待时长：{0}秒", this.ThisPlayer.CurrentRoomSetting.secondsToWaitForReenter));

            List<int> mandRanks = this.ThisPlayer.CurrentRoomSetting.GetManditoryRanks();
            string[] mandRanksStr = mandRanks.Select(x => CommonMethods.cardNumToValue[x].ToString()).ToArray();
            msgs.Add(mandRanks.Count > 0 ? string.Format("必打：{0}", string.Join(",", mandRanksStr)) : "没有必打牌");

            ThisPlayer_NotifyMessageEventHandler(msgs.ToArray());
        }

        private void ThisPlayer_NewPlayerReadyToStart(bool readyToStart)
        {
            this.btnReady.Enabled = !readyToStart;
            
            //看看谁不点就绪
            System.Windows.Forms.Label[] readyLabels = new System.Windows.Forms.Label[] { this.lblSouthStarter, this.lblEastStarter, this.lblNorthStarter, this.lblWestStarter };
            int curIndex = -1;
            for (int i = 0; i < 4; i++)
            {
                var curPlayer = ThisPlayer.CurrentGameState.Players[i];
                if (curPlayer != null && curPlayer.PlayerId == ThisPlayer.PlayerId)
                {
                    curIndex = i;
                    break;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                var curPlayer = ThisPlayer.CurrentGameState.Players[curIndex];
                if (curPlayer != null && curPlayer.IsOffline)
                {
                    readyLabels[i].Text = "离线中";
                }
                else if (curPlayer != null && curPlayer.IsRobot)
                {
                    readyLabels[i].Text = "托管中";
                }
                else if (curPlayer != null && !curPlayer.IsReadyToStart)
                {
                    readyLabels[i].Text = "思索中";
                }
                else if (curPlayer != null && !string.IsNullOrEmpty(ThisPlayer.CurrentHandState.Starter) && curPlayer.PlayerId == ThisPlayer.CurrentHandState.Starter)
                {
                    readyLabels[i].Text = "庄家";
                }
                else
                {
                    readyLabels[i].Text = (curIndex + 1).ToString();
                }
                curIndex = (curIndex + 1) % 4;
            }
        }

        private void ThisPlayer_PlayerToggleIsRobot(bool isRobot)
        {
            bool shouldTrigger = isRobot && isRobot != gameConfig.IsDebug;
            this.ToolStripMenuItemRobot.Checked = isRobot;
            gameConfig.IsDebug = isRobot;
            this.btnRobot.Text = isRobot ? "取消" : "托管";

            //看看谁在托管中
            System.Windows.Forms.Label[] readyLabels = new System.Windows.Forms.Label[] { this.lblSouthStarter, this.lblEastStarter, this.lblNorthStarter, this.lblWestStarter };
            int curIndex = -1;
            for (int i = 0; i < 4; i++)
            {
                var curPlayer = ThisPlayer.CurrentGameState.Players[i];
                if (curPlayer != null && curPlayer.PlayerId == ThisPlayer.PlayerId)
                {
                    curIndex = i;
                    break;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                var curPlayer = ThisPlayer.CurrentGameState.Players[curIndex];
                if (curPlayer != null && curPlayer.IsOffline)
                {
                    readyLabels[i].Text = "离线中";
                }
                else if (curPlayer != null && curPlayer.IsRobot)
                {
                    readyLabels[i].Text = "托管中";
                }
                else if (curPlayer != null && !curPlayer.IsReadyToStart)
                {
                    readyLabels[i].Text = "思索中";
                }
                else if (curPlayer != null && !string.IsNullOrEmpty(ThisPlayer.CurrentHandState.Starter) && curPlayer.PlayerId == ThisPlayer.CurrentHandState.Starter)
                {
                    readyLabels[i].Text = "庄家";
                }
                else
                {
                    readyLabels[i].Text = (curIndex + 1).ToString();
                }
                curIndex = (curIndex + 1) % 4;
            }

            if (shouldTrigger)
            {
                if (!ThisPlayer.CurrentTrickState.IsStarted()) this.RobotPlayStarting();
                else this.RobotPlayFollowing();
            }
        }

        private void ThisPlayer_RoomSettingUpdatedEventHandler(RoomSetting roomSetting, bool isRoomSettingModified)
        {
            this.ThisPlayer.CurrentRoomSetting = roomSetting;
            if (this.ThisPlayer.CurrentRoomSetting.RoomOwner == this.ThisPlayer.MyOwnId)
            {
                //显示仅房主可见的菜单
                this.BeginRankToolStripMenuItem.Visible = true;
                this.ResumeGameToolStripMenuItem.Visible = true;
                this.TeamUpToolStripMenuItem.Visible = true;
            }
            this.lblRoomName.Text = this.ThisPlayer.CurrentRoomSetting.RoomName;
            this.DisplayRoomSetting(isRoomSettingModified);
        }

        private void ThisPlayer_ShowAllHandCardsEventHandler()
        {
            //擦掉出牌区
            drawingFormHelper.DrawCenterImage();
            drawingFormHelper.DrawScoreImageAndCards();
            //重画手牌，从而把被选中的牌放回去
            ResortMyCards();
            for (int position = 2; position <= 4; position++)
            {
                drawingFormHelper.DrawOtherSortedCards(this.ThisPlayer.CurrentHandState.PlayerHoldingCards[PositionPlayer[position]], position, true);
            }
        }
        
        private void ThisPlayer_GameHallUpdatedEventHandler(List<RoomState> roomStates, List<string> names)
        {
            this.ToolStripMenuItemEnterHall.Enabled = false;
            this.btnEnterHall.Hide();
            this.btnReplay.Hide();
            ClearRoom();
            HideRoomControls();

            CreateRoomControls(roomStates, names);
            this.ToolStripMenuItemEnterRoom0.Enabled = true;
        }

        private void ClearRoom()
        {
            ThisPlayer.PlayerId = ThisPlayer.MyOwnId;
            ThisPlayer.isObserver = false;
            this.btnReady.Hide();
            this.btnRobot.Hide();
            this.btnSurrender.Hide();
            this.btnRiot.Hide();
            this.btnRoomSetting.Hide();
            this.btnPig.Hide();
            this.lblRoomName.Text = "";
            this.btnObserveNext.Hide();
            this.lblEastNickName.Text = "";
            this.lblNorthNickName.Text = "";
            this.lblWestNickName.Text = "";
            this.lblWestNickName.Hide();
            this.lblSouthNickName.Text = ThisPlayer.MyOwnId;
            this.lblEastStarter.Text = "";
            this.lblNorthStarter.Text = "";
            this.lblWestStarter.Text = "";
            this.lblWestStarter.Hide();
            this.lblSouthStarter.Text = "";
            this.ToolStripMenuItemInRoom.Visible = false;
            this.ToolStripMenuItemObserve.Visible = false;

            //隐藏仅房主可见的菜单
            this.BeginRankToolStripMenuItem.Visible = false;
            this.ResumeGameToolStripMenuItem.Visible = false;
            this.TeamUpToolStripMenuItem.Visible = false;

            //旁观玩家若在游戏中退出房间，则应重置状态，否则会因仍在游戏中而无法退出游戏
            this.ThisPlayer.CurrentGameState = new GameState();
            this.ThisPlayer.CurrentHandState = new CurrentHandState(this.ThisPlayer.CurrentGameState);

            //停止倒计时
            this.timerCountDown = 0;

            Graphics g = Graphics.FromImage(bmp);
            drawingFormHelper.DrawBackground(g);
            Refresh();
            g.Dispose();
        }

        private void CreateRoomControls(List<RoomState> roomStates, List<string> names)
        {
            int offsetX = 50;
            int offsetY = 150;
            int offsetYLower = offsetY + 100;

            Button btnJoinVideoCall = new Button();
            btnJoinVideoCall.Location = new System.Drawing.Point(offsetX, offsetY - 50);

            btnJoinVideoCall.Name = string.Format("{0}_btnJoinVideoCall", roomControlPrefix);
            btnJoinVideoCall.Size = new System.Drawing.Size(80, 40);
            btnJoinVideoCall.BackColor = System.Drawing.Color.LightBlue;
            btnJoinVideoCall.Text = "加入语音";
            btnJoinVideoCall.Click += new System.EventHandler(this.btnJoinVideoCall_Click);
            this.Controls.Add(btnJoinVideoCall);

            Label labelOnline = new Label();
            labelOnline.AutoSize = true;
            labelOnline.BackColor = System.Drawing.Color.Transparent;
            labelOnline.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            labelOnline.ForeColor = System.Drawing.SystemColors.Control;
            labelOnline.Location = new System.Drawing.Point(offsetX, offsetY);
            labelOnline.Name = string.Format("{0}_Online", roomControlPrefix);
            labelOnline.Size = new System.Drawing.Size(0, 37);
            labelOnline.Text = "在线";
            this.Controls.Add(labelOnline);

            Label labelNames = new Label();
            labelNames.AutoSize = true;
            labelNames.BackColor = System.Drawing.Color.Transparent;
            labelNames.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            labelNames.ForeColor = System.Drawing.SystemColors.Control;
            labelNames.Location = new System.Drawing.Point(offsetX, offsetYLower);
            labelNames.Name = string.Format("{0}_Names", roomControlPrefix);
            labelNames.Size = new System.Drawing.Size(0, 37);
            for (int i = 0; i < names.Count; i++)
            {
                if (i > 0) labelNames.Text += "\n";
                labelNames.Text += names[i];
            }
            this.Controls.Add(labelNames);

            offsetX += 200;
            int seatSize = 40;
            int tableSize = seatSize * 2;

            for (int roomInd = 0; roomInd < roomStates.Count; roomInd++)
            {
                int roomOffsetX = offsetX + seatSize * 8 * (roomInd % 2);
                int roomOffsetY = offsetY + seatSize * 8 * (roomInd / 2);

                RoomState room = roomStates[roomInd];
                Button btnEnterRoom = new Button();
                btnEnterRoom.Location = new System.Drawing.Point(roomOffsetX + seatSize * 5 / 4, roomOffsetY + seatSize * 5 / 4);

                btnEnterRoom.Name = string.Format("{0}_btnEnterRoom_{1}", roomControlPrefix, room.RoomID);
                btnEnterRoom.Size = new System.Drawing.Size(tableSize, tableSize);
                btnEnterRoom.BackColor = System.Drawing.Color.LightBlue;
                btnEnterRoom.Text = room.roomSetting.RoomName;
                btnEnterRoom.Click += new System.EventHandler(this.btnEnterRoom_Click);
                this.Controls.Add(btnEnterRoom);

                List<PlayerEntity> players = room.CurrentGameState.Players;
                for (int j = 0; j < players.Count; j++)
                {
                    int offsetXSeat = roomOffsetX;
                    int offsetYSeat = roomOffsetY;
                    switch (j)
                    {
                        case 0:
                            offsetXSeat += seatSize * 7 / 4;
                            break;
                        case 1:
                            offsetYSeat += seatSize * 7 / 4;
                            break;
                        case 2:
                            offsetXSeat += seatSize * 7 / 4;
                            offsetYSeat += seatSize * 7 / 2;
                            break;
                        case 3:
                            offsetXSeat += seatSize * 7 / 2;
                            offsetYSeat += seatSize * 7 / 4;
                            break;
                        default:
                            break;
                    }
                    if (players[j] == null)
                    {
                        Button btnEnterRoomByPos = new Button();
                        btnEnterRoomByPos.Location = new System.Drawing.Point(offsetXSeat, offsetYSeat);

                        btnEnterRoomByPos.Name = string.Format("{0}_btnEnterRoom_{1}_{2}", roomControlPrefix, room.RoomID, j);
                        btnEnterRoomByPos.Size = new System.Drawing.Size(seatSize, seatSize);
                        btnEnterRoomByPos.BackColor = System.Drawing.Color.LightPink;
                        btnEnterRoomByPos.Text = (j + 1).ToString();
                        btnEnterRoomByPos.Click += new System.EventHandler(this.btnEnterRoom_Click);
                        this.Controls.Add(btnEnterRoomByPos);
                    }
                    else
                    {
                        int labelOffsetX = 0;
                        int labelOffsetY = 0;
                        switch (j)
                        {
                            case 0:
                                labelOffsetY = seatSize / 4;
                                break;
                            case 1:
                                labelOffsetX = seatSize / 4;
                                break;
                            case 2:
                                labelOffsetY = -seatSize / 4;
                                break;
                            case 3:
                                labelOffsetX = -seatSize / 4;
                                break;
                            default:
                                break;
                        }
                        Label labelRoomByPos = new Label();
                        labelRoomByPos.AutoSize = true;
                        labelRoomByPos.BackColor = System.Drawing.Color.Transparent;
                        labelRoomByPos.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
                        labelRoomByPos.ForeColor = System.Drawing.SystemColors.Control;
                        labelRoomByPos.Location = new System.Drawing.Point(offsetXSeat + labelOffsetX, offsetYSeat + labelOffsetY);
                        labelRoomByPos.Name = string.Format("{0}_lblRoom_{1}_{2}", roomControlPrefix, room.RoomID, j);
                        labelRoomByPos.Size = new System.Drawing.Size(seatSize, seatSize);

                        if (j % 2 == 0)
                        {
                            labelRoomByPos.AutoSize = false;
                            labelRoomByPos.Size = new System.Drawing.Size(seatSize * 9 / 2, seatSize);
                            labelRoomByPos.Location = new System.Drawing.Point(offsetXSeat - seatSize * 7 / 4 + labelOffsetX, offsetYSeat + labelOffsetY);
                            labelRoomByPos.TextAlign = ContentAlignment.MiddleCenter;
                        }

                        if (j == 1)
                        {
                            labelRoomByPos.Location = new System.Drawing.Point(0, 0);
                            labelRoomByPos.Size = new System.Drawing.Size(0, seatSize);

                            labelRoomByPos.Dock = System.Windows.Forms.DockStyle.Right;
                            labelRoomByPos.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

                            TableLayoutPanel tlpRoomByPos = new TableLayoutPanel();
                            tlpRoomByPos.SuspendLayout();

                            tlpRoomByPos.Anchor = System.Windows.Forms.AnchorStyles.Right;
                            tlpRoomByPos.Size = new System.Drawing.Size(0, seatSize);
                            tlpRoomByPos.AutoSize = true;
                            tlpRoomByPos.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
                            tlpRoomByPos.BackColor = System.Drawing.Color.Transparent;
                            tlpRoomByPos.ColumnCount = 1;
                            tlpRoomByPos.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
                            tlpRoomByPos.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
                            tlpRoomByPos.Controls.Add(labelRoomByPos, 0, 0);
                            tlpRoomByPos.Location = new System.Drawing.Point(offsetXSeat + seatSize + labelOffsetX, offsetYSeat + labelOffsetY);
                            tlpRoomByPos.Name = string.Format("{0}_lblRoom_{1}_{2}_tlp", roomControlPrefix, room.RoomID, j);
                            tlpRoomByPos.RowCount = 1;
                            tlpRoomByPos.RowStyles.Add(new System.Windows.Forms.RowStyle());
                            tlpRoomByPos.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));

                            this.Controls.Add(tlpRoomByPos);

                            tlpRoomByPos.ResumeLayout(false);
                            tlpRoomByPos.PerformLayout();
                        }
                        else
                        {
                            this.Controls.Add(labelRoomByPos);
                        }

                        labelRoomByPos.Text += players[j].PlayerId;
                    }
                }
            }
        }

        private void HideRoomControls()
        {
            List<Control> roomCtrls = new List<Control>();
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl.Name.StartsWith(this.roomControlPrefix))
                {
                    roomCtrls.Add(ctrl);
                }
            }
            foreach (Control ctrl in roomCtrls)
            {
                this.Controls.Remove(ctrl);
            }
        }

        private void Mainform_SettingsUpdatedEventHandler(bool needRestart)
        {
            if (needRestart) Application.Restart();
            else LoadSettings();
        }

        private void Mainform_SettingsSoundVolumeUpdatedEventHandler(int volume)
        {
            soundPlayerTrumpUpdated.SetVolume(volume);
            soundPlayerTrumpUpdated.Play(true);
        }

        private void Mainform_RoomSettingChangedByClientEventHandler()
        {
            this.ThisPlayer.SaveRoomSetting(ThisPlayer.MyOwnId, this.ThisPlayer.CurrentRoomSetting);
        }

        private void ThisPlayer_TrickStarted()
        {
            if (!gameConfig.IsDebug && ThisPlayer.CurrentTrickState.Learder == ThisPlayer.PlayerId)
            {
                drawingFormHelper.DrawMyPlayingCards(ThisPlayer.CurrentPoker);
            }
            RobotPlayStarting();
        }

        //托管代打
        private void RobotPlayFollowing()
        {
            //跟出
            if ((this.ThisPlayer.playerLocalCache.isLastTrick || gameConfig.IsDebug) && !ThisPlayer.isObserver &&
                ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing &&
                ThisPlayer.CurrentTrickState.NextPlayer() == ThisPlayer.PlayerId &&
                ThisPlayer.CurrentTrickState.IsStarted())
            {
                SelectedCards.Clear();
                Algorithm.MustSelectedCards(this.SelectedCards, this.ThisPlayer.CurrentTrickState, this.ThisPlayer.CurrentPoker);
                ShowingCardsValidationResult showingCardsValidationResult =
                    TractorRules.IsValid(ThisPlayer.CurrentTrickState, SelectedCards, ThisPlayer.CurrentPoker);
                if (showingCardsValidationResult.ResultType == ShowingCardsValidationResultType.Valid)
                {
                    this.ToShowCards();
                }
                else
                {
                    MessageBox.Show(string.Format("failed to auto select cards: {0}, please manually select", SelectedCards));
                }
                return;
            }

            //跟选：如果玩家没有事先手动选牌，在有必选牌的情况下自动选择必选牌，方便玩家快捷出牌
            if (SelectedCards.Count == 0 &&
                !ThisPlayer.isObserver &&
                ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing &&
                ThisPlayer.CurrentTrickState.NextPlayer() == ThisPlayer.PlayerId &&
                ThisPlayer.CurrentTrickState.IsStarted())
            {
                //如果选了牌，则重画手牌，方便直接点确定出牌
                List<int> tempSelectedCards = new List<int>();
                Algorithm.MustSelectedCardsNoShow(tempSelectedCards, this.ThisPlayer.CurrentTrickState, this.ThisPlayer.CurrentPoker);
                if (tempSelectedCards.Count > 0)
                {
                    SelectedCards.Clear();
                    SelectedCards.AddRange(tempSelectedCards);
                    //将选定的牌向上提升 via myCardIsReady
                    for (int i = 0; i < myCardsNumber.Count; i++)
                    {
                        if (SelectedCards.Contains((int)myCardsNumber[i]))
                        {
                            myCardIsReady[i] = true;
                        }
                    }

                    drawingFormHelper.DrawMyPlayingCards(ThisPlayer.CurrentPoker);
                    Refresh();

                    ThisPlayer.CardsReady(ThisPlayer.PlayerId, myCardIsReady);
                }
            }
        }

        //托管代打，先手
        private void RobotPlayStarting()
        {
            if (gameConfig.IsDebug && !ThisPlayer.isObserver &&
                (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing || ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8CardsFinished))
            {
                if (string.IsNullOrEmpty(ThisPlayer.CurrentTrickState.Learder)) return;
                if (ThisPlayer.CurrentTrickState.NextPlayer() != ThisPlayer.PlayerId) return;
                if (ThisPlayer.CurrentTrickState.IsStarted()) return;

                SelectedCards.Clear();
                Algorithm.ShouldSelectedCards(this.SelectedCards, this.ThisPlayer.CurrentTrickState, this.ThisPlayer.CurrentPoker);
                ShowingCardsValidationResult showingCardsValidationResult =
                    TractorRules.IsValid(ThisPlayer.CurrentTrickState, SelectedCards, ThisPlayer.CurrentPoker);
                if (showingCardsValidationResult.ResultType == ShowingCardsValidationResultType.Valid)
                {
                    this.ToShowCards();
                }
                else
                {
                    MessageBox.Show(string.Format("failed to auto select cards: {0}, please manually select", SelectedCards));
                }
            }
        }

        private void ThisPlayer_TrickFinished()
        {
            drawingFormHelper.DrawScoreImageAndCards();

            Refresh();
        }

        private void ThisPlayer_HandEnding()
        {
            drawingFormHelper.DrawFinishedSendedCards();
        }

        private void ThisPlayer_SpecialEndingEventHandler()
        {
            this.btnSurrender.Visible = false;
            this.btnRiot.Visible = false;
            this.btnPig.Visible = false;
            drawingFormHelper.DrawFinishedBySpecialEnding();
        }

        private void ThisPlayer_StarterFailedForTrump()
        {
            Graphics g = Graphics.FromImage(bmp);

            //绘制Sidebar
            drawingFormHelper.DrawSidebar(g);
            //绘制东南西北

            drawingFormHelper.Starter();

            //绘制Rank
            drawingFormHelper.Rank();

            //绘制花色
            drawingFormHelper.Trump();

            ResortMyCards();

            drawingFormHelper.ReDrawToolbar();

            Refresh();
            g.Dispose();
        }

        private void ThisPlayer_StarterChangedEventHandler()
        {
            System.Windows.Forms.Label[] starterLabels = new System.Windows.Forms.Label[] { this.lblSouthStarter, this.lblEastStarter, this.lblNorthStarter, this.lblWestStarter };
            int curIndex = -1;
            for (int i = 0; i < 4; i++)
            {
                var curPlayer = ThisPlayer.CurrentGameState.Players[i];
                if (curPlayer != null && curPlayer.PlayerId == ThisPlayer.PlayerId)
                {
                    curIndex = i;
                    break;
                }
            }
            //看看谁是庄家
            for (int i = 0; i < 4; i++)
            {
                var curPlayer = ThisPlayer.CurrentGameState.Players[curIndex];
                if (curPlayer != null && curPlayer.IsOffline)
                {
                    starterLabels[i].Text = "离线中";
                }
                else if (curPlayer != null && curPlayer.IsRobot)
                {
                    starterLabels[i].Text = "托管中";
                }
                else if (curPlayer != null && !curPlayer.IsReadyToStart)
                {
                    starterLabels[i].Text = "思索中";
                }
                else if (curPlayer != null && !string.IsNullOrEmpty(ThisPlayer.CurrentHandState.Starter) &&
                    curPlayer.PlayerId == ThisPlayer.CurrentHandState.Starter &&
                    ThisPlayer.CurrentHandState.CurrentHandStep != HandStep.Ending)
                {
                    starterLabels[i].Text = "庄家";
                }
                else
                {
                    starterLabels[i].Text = (curIndex + 1).ToString();
                }
                curIndex = (curIndex + 1) % 4;
            }
        }

        private void ThisPlayer_NotifyMessageEventHandler(string[] msgs)
        {
            foreach (string m in msgs)
            {
                if (m.Contains("获胜！"))
                {
                    soundPlayerGameOver.Play(this.enableSound);
                }
                else if (m.Equals(CommonMethods.reenterRoomSignal))
                {
                    ThisPlayer.IsTryingReenter = true;
                    this.btnEnterHall.Hide();
                    this.btnReplay.Hide();
                }
                else if (m.Equals(CommonMethods.resumeGameSignal))
                {
                    ThisPlayer.IsTryingResumeGame = true;
                }
                else if (m.Contains("新游戏即将开始"))
                {
                    //新游戏开始前播放提示音，告诉玩家要抢庄
                    soundPlayerGameOver.Play(this.enableSound);
                }
                else if (m.Contains("罚分"))
                {
                    //甩牌失败播放提示音
                    soundPlayerDumpFailure.Play(this.enableSound);
                }
            }
            this.drawingFormHelper.DrawMessages(msgs);
        }

        private void ThisPlayer_NotifyStartTimerEventHandler(int timerLength)
        {
            this.timerCountDown = timerLength;
            this.drawingFormHelper.DrawCountDown(true);
            this.theTimer.Start();
        }

        private void ThisPlayer_NotifyCardsReadyEventHandler(ArrayList mcir)
        {
            for (int k = 0; k < myCardIsReady.Count; k++)
            {
                myCardIsReady[k] = mcir[k];
            }

            drawingFormHelper.DrawMyPlayingCards(ThisPlayer.CurrentPoker);
            Refresh();
        }

        private string ThisPlayer_CutCardShoeCardsEventHandler()
        {
            if (gameConfig.IsDebug || !enableCutCards) return "取消,0";
            FormCutCards frmCutCards = new FormCutCards();
            frmCutCards.StartPosition = FormStartPosition.CenterParent;
            frmCutCards.TopMost = true;
            frmCutCards.ShowDialog();
            if (frmCutCards.noMoreCut)
            {
                FormSettings.SetSetting(FormSettings.KeyEnableCutCards, "false");
                this.LoadSettings();
            }
            return string.Format("{0},{1}", frmCutCards.cutType, frmCutCards.cutPoint);
        }

        private void ThisPlayer_ResortMyCardsEventHandler()
        {
            ThisPlayer.CurrentPoker = ThisPlayer.CurrentHandState.PlayerHoldingCards[ThisPlayer.PlayerId];
            if (ThisPlayer.isObserverChanged)
            {
                ResortMyCards();
                ThisPlayer.isObserverChanged = false;
            }
        }

        private void ThisPlayer_Last8Discarded()
        {
            soundPlayerDiscardingLast8CardsFinished.Play(this.enableSound);

            Graphics g = Graphics.FromImage(bmp);
            for (int i = 0; i < 8; i++)
            {
                g.DrawImage(gameConfig.BackImage, 200 + drawingFormHelper.offsetCenterHalf + i * 2 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor, 186 + drawingFormHelper.offsetCenterHalf, 71 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor, 96 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor);
            }

            if (ThisPlayer.isObserver && ThisPlayer.CurrentHandState.Last8Holder == ThisPlayer.PlayerId)
            {
                ThisPlayer.CurrentPoker = ThisPlayer.CurrentHandState.PlayerHoldingCards[ThisPlayer.PlayerId];
                ResortMyCards();
            }
            if (ThisPlayer.CurrentPoker != null && ThisPlayer.CurrentPoker.Count > 0 &&
                ThisPlayer.CurrentHandState.Last8Holder == ThisPlayer.PlayerId &&
                ThisPlayer.CurrentHandState.DiscardedCards != null &&
                ThisPlayer.CurrentHandState.DiscardedCards.Length == 8)
            {
                drawingFormHelper.DrawDiscardedCards();
            }
            Refresh();
            g.Dispose();
        }

        private void ThisPlayer_DistributingLast8Cards()
        {
            //先去掉反牌按钮，再放发底牌动画
            drawingFormHelper.ReDrawToolbar();
            //重画手牌，从而把被提升的自己亮的牌放回去
            ResortMyCards();

            int position = PlayerPosition[ThisPlayer.CurrentHandState.Last8Holder];
            //自己摸底不用画
            if (position > 1)
            {
                drawingFormHelper.DrawDistributingLast8Cards(position);
            }
            else
            {
                //播放摸底音效
                soundPlayerDrawx.Play(this.enableSound);
            }

            if (ThisPlayer.isObserver)
            {
                return;
            }

            //摸牌结束，如果处于托管状态，则取消托管
            if (gameConfig.IsDebug && !ThisPlayer.CurrentRoomSetting.IsFullDebug)
            {
                this.btnRobot.PerformClick();
            }

            //摸牌结束，如果允许投降，则显示投降按钮
            if (ThisPlayer.CurrentRoomSetting.AllowSurrender)
            {
                this.btnSurrender.Visible = true;
            }

            //仅允许台下的玩家可以革命
            if (!this.ThisPlayer.CurrentGameState.ArePlayersInSameTeam(this.ThisPlayer.CurrentHandState.Starter, this.ThisPlayer.PlayerId))
            {
                //摸牌结束，如果允许分数革命，则判断是否该显示革命按钮
                int riotScoreCap = ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewScoreCards;
                if (ThisPlayer.CurrentPoker.GetTotalScore() <= riotScoreCap)
                {
                    this.btnRiot.Visible = true;
                }

                //摸牌结束，如果允许主牌革命，则判断是否该显示革命按钮
                int riotTrumpCap = ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewTrumpCards;
                if (ThisPlayer.CurrentPoker.GetMasterCardsCount() <= riotTrumpCap && ThisPlayer.CurrentHandState.Trump != Suit.Joker)
                {
                    this.btnRiot.Visible = true;
                }
            }
        }

        private void ThisPlayer_DiscardingLast8()
        {
            Graphics g = Graphics.FromImage(bmp);

            g.DrawImage(image, 200 + drawingFormHelper.offsetCenterHalf, 186 + drawingFormHelper.offsetCenterHalf, 85 * drawingFormHelper.scaleDividend, 96 * drawingFormHelper.scaleDividend);
            Refresh();
            g.Dispose();

            //托管代打：埋底
            if (ThisPlayer.CurrentRoomSetting.IsFullDebug && gameConfig.IsDebug && !ThisPlayer.isObserver)
            {
                if (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8Cards &&
                    ThisPlayer.CurrentHandState.Last8Holder == ThisPlayer.PlayerId) //如果等我扣牌
                {
                    SelectedCards.Clear();
                    Algorithm.ShouldSelectedLast8Cards(this.SelectedCards, this.ThisPlayer.CurrentPoker);
                    if (SelectedCards.Count == 8)
                    {
                        this.ToDiscard8Cards();
                    }
                    else
                    {
                        MessageBox.Show(string.Format("failed to auto select last 8 cards: {0}, please manually select", SelectedCards));
                    }
                }
            }
        }

        private void ThisPlayer_DumpingFail(ShowingCardsValidationResult result)
        {
            //擦掉上一把
            if (ThisPlayer.CurrentTrickState.AllPlayedShowedCards() || ThisPlayer.CurrentTrickState.IsStarted() == false)
            {
                drawingFormHelper.DrawCenterImage();
                drawingFormHelper.DrawScoreImageAndCards();
            }
            ThisPlayer.CurrentTrickState.ShowedCards[result.PlayerId] = result.CardsToShow;

            string latestPlayer = result.PlayerId;
            int position = PlayerPosition[latestPlayer];
            if (latestPlayer == ThisPlayer.PlayerId)
            {
                drawingFormHelper.DrawMyShowedCards();
            }
            if (position == 2)
            {
                drawingFormHelper.DrawNextUserSendedCards();
            }
            if (position == 3)
            {
                drawingFormHelper.DrawFriendUserSendedCards();
            }
            if (position == 4)
            {
                drawingFormHelper.DrawPreviousUserSendedCards();
            }
            ThisPlayer.CurrentTrickState.ShowedCards[result.PlayerId].Clear();
        }

        private void ThisPlayer_HostIsOnlineEventHandler(bool success)
        {
            string result = success ? "Host online!" : "Host offline";
            Invoke(new Action(() =>
            {
                this.lblSouthStarter.Text = result;
                this.progressBarPingHost.Value = this.progressBarPingHost.Maximum;
            }));
        }

        #endregion

        private void btnReady_Click(object sender, EventArgs e)
        {
            if (ThisPlayer.isObserver) return;
            //为防止以外连续点两下就绪按钮，造成重复发牌，点完一下就立即disable就绪按钮
            this.btnReady.Enabled = false;
            ThisPlayer.ReadyToStart();
        }

        private void SettingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSettings formSettings = new FormSettings();
            formSettings.SettingsUpdatedEvent += Mainform_SettingsUpdatedEventHandler;
            formSettings.SettingsSoundVolumeUpdatedEvent += Mainform_SettingsSoundVolumeUpdatedEventHandler;
            formSettings.ShowDialog();
        }

        private void RebootToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void ResumeGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ThisPlayer.isObserver) return;
            if (AllOnline() && !ThisPlayer.isObserver && ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing)
            {
                this.ThisPlayer_NotifyMessageEventHandler(new string[] { "游戏中途不允许继续牌局", "请完成此盘游戏后重试" });
                return;
            }

            ThisPlayer.ResumeGameFromFile(ThisPlayer.MyOwnId);
        }

        private void theTimer_Tick(object sender, EventArgs e)
        {
            if (this.timerCountDown > 0)
            {
                this.timerCountDown--;
                this.drawingFormHelper.DrawCountDown(true);
                if (this.timerCountDown > 0) return;
            }
            Thread.Sleep(200);
            this.drawingFormHelper.DrawCountDown(false);
            this.theTimer.Stop();
            if (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.BeforeDistributingCards)
            {
                this.drawingFormHelper.DrawCenterImage();
            }
        }

        private void AutoUpdaterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AutoUpdater.ReportErrors = true;
            AutoUpdater.Start("https://raw.githubusercontent.com/iceburgy/Tractor_LAN/master/SourceCode/TractorWinformClient/AutoUpdater.xml");
        }

        private void FeatureOverviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/iceburgy/Tractor_LAN/blob/master/README.md");
        }

        private void TeamUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ThisPlayer.isObserver) return;
            if (AllOnline() && !ThisPlayer.isObserver && ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing)
            {
                this.ThisPlayer_NotifyMessageEventHandler(new string[] { "游戏中途不允许随机组队", "请完成此盘游戏后重试" });
                return;
            }

            ThisPlayer.TeamUp(ThisPlayer.MyOwnId);
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (this.updateOnLoad)
            {
                AutoUpdater.ReportErrors = false;
                AutoUpdater.Start("https://raw.githubusercontent.com/iceburgy/Tractor_LAN/master/SourceCode/TractorWinformClient/AutoUpdater.xml");
            }
        }

        private void ToolStripMenuItemShowVersion_Click(object sender, EventArgs e)
        {
            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            MessageBox.Show(string.Format("当前版本：{0}", assemblyVersion));
        }

        private void ToolStripMenuItemGetReady_Click(object sender, EventArgs e)
        {
            this.btnReady.PerformClick();
        }

        private void btnEnterRoom_Click(object sender, EventArgs e)
        {
            int roomID;
            int posID = -1;
            string[] nameParts = ((Button)sender).Name.Split('_');
            string roomIDString = nameParts[2];
            if (int.TryParse(roomIDString, out roomID))
            {
                if (nameParts.Length >= 4)
                {
                    string posIDString = nameParts[3];
                    int.TryParse(posIDString, out posID);
                }
                ThisPlayer.PlayerEnterRoom(ThisPlayer.MyOwnId, roomID, posID);
            }
            else
            {
                MessageBox.Show(string.Format("failed to click button with a bad ID: {0}", roomIDString));
            }
        }

        private void btnJoinVideoCall_Click(object sender, EventArgs e)
        {
            Process.Start(videoCallUrl);
        }

        private void ToolStripMenuItemEnterHall_Click(object sender, EventArgs e)
        {
            this.btnEnterHall.PerformClick();
        }

        private void btnEnterHall_Click(object sender, EventArgs e)
        {
            ThisPlayer.PlayerEnterHall(ThisPlayer.MyOwnId);
        }

        private void ToolStripMenuItemRobot_Click(object sender, EventArgs e)
        {
            this.btnRobot.PerformClick();
        }

        private void btnRobot_Click(object sender, EventArgs e)
        {
            if (ThisPlayer.isObserver) return;
            ThisPlayer.ToggleIsRobot();
        }

        private void ToolStripMenuItemObserverNextPlayer_Click(object sender, EventArgs e)
        {
            this.btnObserveNext.PerformClick();
        }

        private void btnObserveNext_Click(object sender, EventArgs e)
        {
            if (ThisPlayer.isObserver)
            {
                ThisPlayer.ObservePlayerById(PositionPlayer[2], ThisPlayer.MyOwnId);
            }
        }

        private void btnExitRoom_Click(object sender, EventArgs e)
        {
            if (playerIsOutHall())
            {
                this.Close();
                return;
            }
            if (ThisPlayer.isReplay)
            {
                this.ClearRoom();
                this.btnEnterHall.Show();
                this.btnReplay.Show();
                this.btnReplayAngle.Hide();
                this.btnFirstTrick.Hide();
                this.btnPreviousTrick.Hide();
                this.btnNextTrick.Hide();
                this.btnLastTrick.Hide();
                this.lblReplayDate.Hide();
                this.lblReplayFile.Hide();
                this.cbbReplayDate.Hide();
                this.cbbReplayFile.Hide();
                this.btnLoadReplay.Hide();
                ThisPlayer.isReplay = false;
                ThisPlayer.replayEntity = null;
                return;
            }
            if (playerIsInHall())
            {
                try
                {
                    ThisPlayer.Quit();
                }
                catch (Exception)
                {
                }
                this.ToolStripMenuItemEnterHall.Enabled = true;
                this.btnEnterHall.Show();
                this.btnReplay.Show();
                HideRoomControls();
                return;
            }
            if (AllOnline() && !ThisPlayer.isObserver && !ThisPlayer.isReplay && ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing)
            {
                DialogResult dialogResult = MessageBox.Show("游戏正在进行中，是否确定退出？", "是否确定退出", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.No)
                {
                    return;
                }
            }

            ThisPlayer.ExitRoom(ThisPlayer.MyOwnId);
        }

        private bool playerIsOutHall()
        {
            return this.btnEnterHall.Visible;
        }

        private bool playerIsInHall()
        {
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl.Name.StartsWith(this.roomControlPrefix))
                {
                    return true;
                }
            }
            return false;
        }

        private bool AllOnline()
        {
            bool allOnline = true;
            foreach (PlayerEntity player in ThisPlayer.CurrentGameState.Players)
            {
            	if (player ==  null) continue;
                if (player.IsOffline)
                {
                    allOnline = false;
                    break;
                }
            }
            return allOnline;
        }

        private void btnSurrender_Click(object sender, EventArgs e)
        {
            this.btnSurrender.Visible = false;
            ThisPlayer.SpecialEndGameRequest(ThisPlayer.MyOwnId);
        }

        private void ThisPlayer_SpecialEndGameShouldAgreeEventHandler()
        {
            this.btnSurrender.Visible = false;
            DialogResult dialogResult = MessageBox.Show("队友提出投降，是否同意？", "是否同意投降", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                ThisPlayer.SpecialEndGame(ThisPlayer.MyOwnId, SpecialEndingType.Surrender);
            }
            else if (dialogResult == DialogResult.No)
            {
                ThisPlayer.SpecialEndGameDeclined(ThisPlayer.MyOwnId);
            }
        }

        private void btnRiot_Click(object sender, EventArgs e)
        {
            SpecialEndingType endType = SpecialEndingType.RiotByScore;
            //如果允许革命，则判断是哪种革命
            int riotScoreCap = ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewScoreCards;
            if (ThisPlayer.CurrentPoker.GetTotalScore() <= riotScoreCap)
            {
                endType = SpecialEndingType.RiotByScore;
            }
            else
            {
                endType = SpecialEndingType.RiotByTrump;
            }

            ThisPlayer.SpecialEndGame(ThisPlayer.MyOwnId, endType);
            this.btnSurrender.Visible = false;
            this.btnRiot.Visible = false;
        }

        private void ToolStripMenuItemUserManual_Click(object sender, EventArgs e)
        {
            string userManual = "";
            userManual = "【基本规则】";
            userManual += "\n- 如果无人亮主，则打无主";
            userManual += "\n- 甩牌失败，将会以每张10分进行罚分";
            userManual += "\n\n【隐藏技】";
            userManual += "\n- 右键选牌：自动向左选择所有合法张数的牌（适用于出牌、埋底）";
            userManual += "\n- 右键单击空白处查看：上轮出牌、谁亮过什么牌";
            userManual += "\n- 八卦图标代表一圈中的大牌，【杀】图标代表主毙牌";
            userManual += "\n\n【快捷键】";
            userManual += "\n- 出牌：按键S（Show cards）";
            userManual += "\n- 录像回放上一轮：左箭头";
            userManual += "\n- 录像回放下一轮：右箭头";
            userManual += "\n- 进入大厅：F1";
            userManual += "\n- 进入第一个房间：F2";
            userManual += "\n- 就绪：F3、按键Z（Zhunbei准备）";
            userManual += "\n- 托管：F4、按键R（Robot）";
            userManual += "\n- 旁观下家：F5（仅限旁观模式下）";
            userManual += "\n\n此帮助信息可在菜单【帮助】【使用说明】中再次查看";
            userManual += "\n\n不再提示？";

            DialogResult dialogResult = MessageBox.Show(userManual, "使用说明", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                FormSettings.SetSetting(FormSettings.KeyIsHelpSeen, "true");
            }
            else if (dialogResult == DialogResult.No)
            {
                FormSettings.SetSetting(FormSettings.KeyIsHelpSeen, "false");
            }
        }

        private void ToolStripMenuItemEnterRoom0_Click(object sender, EventArgs e)
        {
            Control[] controls = this.Controls.Find(string.Format("{0}_btnEnterRoom_0", roomControlPrefix), false);
            if (controls != null && controls.Length > 0)
            {
                Button enterRoom0 = ((Button)controls[0]);
                if (enterRoom0.Visible)
                {
                    enterRoom0.PerformClick();
                }
            }
        }

        private void tmrGeneral_Tick(object sender, EventArgs e)
        {
            if (this.progressBarPingHost.Visible == true && this.progressBarPingHost.Value < this.progressBarPingHost.Maximum)
            {
                int step = (this.progressBarPingHost.Maximum - this.progressBarPingHost.Value) / 8;
                this.progressBarPingHost.Increment(step);
            }
            else
            {
                this.tmrGeneral.Stop();
                progressBarPingHost.Value = this.progressBarPingHost.Maximum;
                this.progressBarPingHost.Hide();
                Refresh();
            }
        }

        private void btnPig_Click(object sender, EventArgs e)
        {
            ToDiscard8Cards();
            ToShowCards();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.R:
                    if (this.btnRobot.Visible && this.btnRobot.Enabled)
                    {
                        this.btnRobot.PerformClick();
                    }
                    return true;
                case Keys.S:
                    if (this.btnPig.Visible)
                    {
                        this.btnPig.PerformClick();
                    }
                    return true;
                case Keys.Z:
                    if (this.btnReady.Visible && this.btnReady.Enabled)
                    {
                        this.btnReady.PerformClick();
                    }
                    return true;
                case Keys.Left:
                    if (ThisPlayer.isReplay)
                    {
                        this.btnPreviousTrick.PerformClick();
                    }
                    return true;
                case Keys.Right:
                    if (ThisPlayer.isReplay)
                    {
                        this.btnNextTrick.PerformClick();
                    }
                    return true;
                case Keys.Up:
                    if (ThisPlayer.isReplay)
                    {
                        this.btnFirstTrick.PerformClick();
                    }
                    return true;
                case Keys.Down:
                    if (ThisPlayer.isReplay)
                    {
                        this.btnLastTrick.PerformClick();
                    }
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        private void btnRoomSetting_Click(object sender, EventArgs e)
        {
            FormRoomSetting roomSetting = new FormRoomSetting(this);
            roomSetting.RoomSettingChangedByClientEvent += Mainform_RoomSettingChangedByClientEventHandler;
            roomSetting.ShowDialog();
        }

        private void btnReplay_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(this.rootReplayFolderPath) || !Directory.EnumerateFileSystemEntries(this.rootReplayFolderPath).Any())
            {
                MessageBox.Show("未找到录像文件");
                return;
            }

            btnEnterHall.Hide();
            this.btnReplay.Hide();
            this.lblReplayDate.Show();
            this.lblReplayFile.Show();
            this.cbbReplayDate.Show();
            this.cbbReplayFile.Show();
            this.btnLoadReplay.Show();
            ThisPlayer.isReplay = true;

            DirectoryInfo dirRoot = new DirectoryInfo(this.rootReplayFolderPath);
            DirectoryInfo[] dirs = dirRoot.GetDirectories();
            for (int i = 0; i < dirs.Length; i++)
            {
                cbbReplayDate.Items.Add(dirs[i].Name);
            }

            if (cbbReplayDate.Items.Count > 0)
            {
                cbbReplayDate.SelectedIndex = cbbReplayDate.Items.Count - 1;
            }
        }

        private void btnLoadReplay_Click(object sender, EventArgs e)
        {
            LoadReplay(true);
            this.btnReplayAngle.Show();
            this.btnFirstTrick.Show();
            this.btnPreviousTrick.Show();
            this.btnNextTrick.Show();
            this.btnLastTrick.Show();
        }

        private void LoadReplay(bool shouldDraw)
        {
            string replayFile = string.Format("{0}\\{1}\\{2}", this.rootReplayFolderPath, (string)cbbReplayDate.SelectedItem, (string)cbbReplayFile.SelectedItem);
            ReplayEntity replayEntity = CommonMethods.ReadObjectFromFile<ReplayEntity>(replayFile);

            ThisPlayer.replayEntity = replayEntity;
            ThisPlayer.replayEntity.CurrentTrickStates.Add(null); // use null to indicate end of tricks, so that to show ending scores
            ThisPlayer.replayedTricks = new Stack<CurrentTrickState>();
            CommonMethods.RotateArray(ThisPlayer.replayEntity.Players, ThisPlayer.replayAngle);
            if (ThisPlayer.replayEntity.PlayerRanks != null)
            {
                CommonMethods.RotateArray(ThisPlayer.replayEntity.PlayerRanks, ThisPlayer.replayAngle);
            }

            StartReplay(shouldDraw);
        }

        private void StartReplay(bool shouldDraw)
        {
            drawingFormHelper.DrawCenterImage();
            string[] players = ThisPlayer.replayEntity.Players;
            int[] playerRanks = new int[4];
            if (ThisPlayer.replayEntity.PlayerRanks != null)
            {
                playerRanks = ThisPlayer.replayEntity.PlayerRanks;
            }
            else
            {
                int tempRank = ThisPlayer.replayEntity.CurrentHandState.Rank;
                playerRanks = new int[] { tempRank, tempRank, tempRank, tempRank };
            }
            ThisPlayer.PlayerId = players[0];
            lblSouthNickName.Text = players[0];
            lblEastNickName.Text = players[1];
            lblNorthNickName.Text = players[2];
            lblWestNickName.Text = players[3];
            lblWestNickName.Show();

            lblSouthStarter.Text = players[0] == ThisPlayer.replayEntity.CurrentHandState.Starter ? "庄家" : "1";
            lblEastStarter.Text = players[1] == ThisPlayer.replayEntity.CurrentHandState.Starter ? "庄家" : "2";
            lblNorthStarter.Text = players[2] == ThisPlayer.replayEntity.CurrentHandState.Starter ? "庄家" : "3";
            lblWestStarter.Text = players[3] == ThisPlayer.replayEntity.CurrentHandState.Starter ? "庄家" : "4";
            lblWestStarter.Show();

            ThisPlayer.CurrentGameState = new GameState();
            ThisPlayer.CurrentGameState.Players[0] = new PlayerEntity { PlayerId = players[0], Rank = playerRanks[0], Team = GameTeam.VerticalTeam, Observers = new HashSet<string>() };
            ThisPlayer.CurrentGameState.Players[1] = new PlayerEntity { PlayerId = players[1], Rank = playerRanks[1], Team = GameTeam.HorizonTeam, Observers = new HashSet<string>() };
            ThisPlayer.CurrentGameState.Players[2] = new PlayerEntity { PlayerId = players[2], Rank = playerRanks[2], Team = GameTeam.VerticalTeam, Observers = new HashSet<string>() };
            ThisPlayer.CurrentGameState.Players[3] = new PlayerEntity { PlayerId = players[3], Rank = playerRanks[3], Team = GameTeam.HorizonTeam, Observers = new HashSet<string>() };
            //set player position
            PlayerPosition.Clear();
            PositionPlayer.Clear();
            string nextPlayer = players[0];
            int postion = 1;
            PlayerPosition.Add(nextPlayer, postion);
            PositionPlayer.Add(postion, nextPlayer);
            nextPlayer = ThisPlayer.CurrentGameState.GetNextPlayerAfterThePlayer(nextPlayer).PlayerId;
            while (nextPlayer != players[0])
            {
                postion++;
                PlayerPosition.Add(nextPlayer, postion);
                PositionPlayer.Add(postion, nextPlayer);
                nextPlayer = ThisPlayer.CurrentGameState.GetNextPlayerAfterThePlayer(nextPlayer).PlayerId;
            }

            ThisPlayer.CurrentHandState = CommonMethods.DeepClone<CurrentHandState>(ThisPlayer.replayEntity.CurrentHandState);
            ThisPlayer.CurrentHandState.Score = 0;
            ThisPlayer.CurrentHandState.ScoreCards.Clear();
            ThisPlayer.CurrentPoker = ThisPlayer.replayEntity.CurrentHandState.PlayerHoldingCards[players[0]];

            Graphics g = Graphics.FromImage(bmp);
            drawingFormHelper.DrawSidebar(g);
            drawingFormHelper.Starter();
            drawingFormHelper.Rank();
            drawingFormHelper.Trump();

            drawingFormHelper.DrawDiscardedCards();

            if (shouldDraw)
            {
                drawingFormHelper.DrawMyHandCards();
                drawingFormHelper.LastTrumpMadeCardsShow();
                drawAllOtherHandCards();
            }
        }

        private void replayNextTrick()
        {
            if (ThisPlayer.replayEntity.CurrentTrickStates.Count == 0)
            {
                return;
            }
            CurrentTrickState trick = ThisPlayer.replayEntity.CurrentTrickStates[0];
            ThisPlayer.replayEntity.CurrentTrickStates.RemoveAt(0);
            ThisPlayer.replayedTricks.Push(trick);
            if (trick == null)
            {
                ThisPlayer.CurrentHandState.ScoreCards = new List<int>(ThisPlayer.replayEntity.CurrentHandState.ScoreCards);
                ThisPlayer.CurrentHandState.Score = ThisPlayer.replayEntity.CurrentHandState.Score;
                drawingFormHelper.DrawFinishedSendedCards();
                return;
            }
            drawingFormHelper.DrawCenterImage();

            ThisPlayer.CurrentTrickState = trick;
            string curPlayer = trick.Learder;
            for (int i = 0; i < 4; i++)
            {
                ArrayList cardsList = new ArrayList(trick.ShowedCards[curPlayer]);
                int position = PlayerPosition[curPlayer];
                if (position == 1)
                {
                    drawingFormHelper.DrawMySendedCardsAction(cardsList);
                    foreach (int card in trick.ShowedCards[curPlayer])
                    {
                        ThisPlayer.CurrentPoker.RemoveCard(card);
                    }
                    drawingFormHelper.DrawMyHandCards();
                }
                else if (position == 2)
                {
                    drawingFormHelper.DrawNextUserSendedCardsAction(cardsList);
                    foreach (int card in trick.ShowedCards[curPlayer])
                    {
                        ThisPlayer.replayEntity.CurrentHandState.PlayerHoldingCards[PositionPlayer[2]].RemoveCard(card);
                    }
                }
                else if (position == 3)
                {
                    drawingFormHelper.DrawFriendUserSendedCardsAction(cardsList);
                    foreach (int card in trick.ShowedCards[curPlayer])
                    {
                        ThisPlayer.replayEntity.CurrentHandState.PlayerHoldingCards[PositionPlayer[3]].RemoveCard(card);
                    }
                }
                else if (position == 4)
                {
                    drawingFormHelper.DrawPreviousUserSendedCardsAction(cardsList);
                    foreach (int card in trick.ShowedCards[curPlayer])
                    {
                        ThisPlayer.replayEntity.CurrentHandState.PlayerHoldingCards[PositionPlayer[4]].RemoveCard(card);
                    }
                }
                Refresh();
                curPlayer = ThisPlayer.CurrentGameState.GetNextPlayerAfterThePlayer(curPlayer).PlayerId;
            }

            drawAllOtherHandCards();

            if (!string.IsNullOrEmpty(trick.Winner))
            {
                if (
                    !ThisPlayer.CurrentGameState.ArePlayersInSameTeam(ThisPlayer.CurrentHandState.Starter,
                                                                trick.Winner))
                {
                    ThisPlayer.CurrentHandState.Score += trick.Points;
                    //收集得分牌
                    ThisPlayer.CurrentHandState.ScoreCards.AddRange(trick.ScoreCards);
                }
            }
            drawingFormHelper.DrawScoreImageAndCards();

            Refresh();

        }

        private void btnFirstTrick_Click(object sender, EventArgs e)
        {
            if (ThisPlayer.replayedTricks.Count > 0) LoadReplay(true);
            else
            {
                if (replayPreviousFile()) btnLastTrick_Click(sender, e);
            }
        }

        private void btnPreviousTrick_Click(object sender, EventArgs e)
        {
            if (ThisPlayer.replayedTricks.Count > 1)
            {
                drawingFormHelper.DrawCenterImage();
                revertReplayTrick();
                revertReplayTrick();
                replayNextTrick();
            }
            else if (ThisPlayer.replayedTricks.Count == 1)
            {
                btnFirstTrick_Click(sender, e);
            }
            else
            {
                if (replayPreviousFile()) btnLastTrick_Click(sender, e);
            }
        }

        private void btnNextTrick_Click(object sender, EventArgs e)
        {
            if (ThisPlayer.replayEntity.CurrentTrickStates.Count == 0)
            {
                replayNextFile();
                return;
            }
            replayNextTrick();
        }

        private void btnLastTrick_Click(object sender, EventArgs e)
        {
            if (ThisPlayer.replayEntity.CurrentTrickStates.Count > 0)
            {
                while (ThisPlayer.replayEntity.CurrentTrickStates.Count > 1)
                {
                    CurrentTrickState trick = ThisPlayer.replayEntity.CurrentTrickStates[0];
                    ThisPlayer.replayedTricks.Push(trick);
                    ThisPlayer.replayEntity.CurrentTrickStates.RemoveAt(0);

                    string curPlayer = trick.Learder;
                    for (int i = 0; i < 4; i++)
                    {
                        ArrayList cardsList = new ArrayList(trick.ShowedCards[curPlayer]);
                        int position = PlayerPosition[curPlayer];
                        if (position == 1)
                        {
                            foreach (int card in trick.ShowedCards[curPlayer])
                            {
                                ThisPlayer.CurrentPoker.RemoveCard(card);
                            }
                        }
                        else if (position == 2)
                        {
                            foreach (int card in trick.ShowedCards[curPlayer])
                            {
                                ThisPlayer.replayEntity.CurrentHandState.PlayerHoldingCards[PositionPlayer[2]].RemoveCard(card);
                            }
                        }
                        else if (position == 3)
                        {
                            foreach (int card in trick.ShowedCards[curPlayer])
                            {
                                ThisPlayer.replayEntity.CurrentHandState.PlayerHoldingCards[PositionPlayer[3]].RemoveCard(card);
                            }
                        }
                        else if (position == 4)
                        {
                            foreach (int card in trick.ShowedCards[curPlayer])
                            {
                                ThisPlayer.replayEntity.CurrentHandState.PlayerHoldingCards[PositionPlayer[4]].RemoveCard(card);
                            }
                        }
                        curPlayer = ThisPlayer.CurrentGameState.GetNextPlayerAfterThePlayer(curPlayer).PlayerId;
                    }
                }
                drawingFormHelper.DrawMyHandCards();
                replayNextTrick();
            }
            else replayNextFile();
        }

        private bool replayPreviousFile()
        {
            if (cbbReplayFile.SelectedIndex > 0)
            {
                cbbReplayFile.SelectedIndex = cbbReplayFile.SelectedIndex - 1;
                LoadReplay(false);
                return true;
            }
            else if (cbbReplayDate.SelectedIndex > 0)
            {
                cbbReplayDate.SelectedIndex = cbbReplayDate.SelectedIndex - 1;
                if (cbbReplayFile.Items != null && cbbReplayFile.Items.Count > 0)
                {
                    cbbReplayFile.SelectedIndex = cbbReplayFile.Items.Count - 1;
                    LoadReplay(false);
                    return true;
                }
            }
            return false;
        }

        private void replayNextFile()
        {
            if (cbbReplayFile.SelectedIndex < cbbReplayFile.Items.Count - 1)
            {
                cbbReplayFile.SelectedIndex = cbbReplayFile.SelectedIndex + 1;
                LoadReplay(true);
            }
            else if (cbbReplayDate.SelectedIndex < cbbReplayDate.Items.Count - 1)
            {
                cbbReplayDate.SelectedIndex = cbbReplayDate.SelectedIndex + 1;
                if (cbbReplayFile.Items != null && cbbReplayFile.Items.Count > 0) LoadReplay(true);
            }
        }

        private void revertReplayTrick()
        {
            CurrentTrickState trick = ThisPlayer.replayedTricks.Pop();
            ThisPlayer.replayEntity.CurrentTrickStates.Insert(0, trick);
            if (trick == null)
            {
                ThisPlayer.CurrentHandState.Score -= ThisPlayer.CurrentHandState.ScorePunishment + ThisPlayer.CurrentHandState.ScoreLast8CardsBase * ThisPlayer.CurrentHandState.ScoreLast8CardsMultiplier;
            }
            else
            {
                foreach (var entry in trick.ShowedCards)
                {
                    foreach (int card in entry.Value)
                    {
                        ThisPlayer.replayEntity.CurrentHandState.PlayerHoldingCards[entry.Key].AddCard(card);
                    }
                }

                if (!string.IsNullOrEmpty(trick.Winner))
                {
                    if (
                        !ThisPlayer.CurrentGameState.ArePlayersInSameTeam(ThisPlayer.CurrentHandState.Starter,
                                                                    trick.Winner))
                    {
                        ThisPlayer.CurrentHandState.Score -= trick.Points;
                        //收集得分牌
                        foreach (var sc in trick.ScoreCards)
                        {
                            ThisPlayer.CurrentHandState.ScoreCards.Remove(sc);
                        }
                    }
                }
                drawingFormHelper.DrawScoreImageAndCards();

            }
        }

        private void btnReplayAngle_Click(object sender, EventArgs e)
        {
            ThisPlayer.replayAngle = (ThisPlayer.replayAngle + 1) % 4;
            CommonMethods.RotateArray(ThisPlayer.replayEntity.Players, 1);
            if (ThisPlayer.replayEntity.PlayerRanks != null)
            {
                CommonMethods.RotateArray(ThisPlayer.replayEntity.PlayerRanks, 1);
            }
            StartReplay(true);
        }

        private void cbbReplayDate_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbbReplayFile.Items.Clear();
            DirectoryInfo selectedDate = new DirectoryInfo(string.Format("{0}\\{1}", this.rootReplayFolderPath, cbbReplayDate.SelectedItem));
            FileInfo[] files = selectedDate.GetFiles();
            for (int j = 0; j < files.Length; j++)
            {
                cbbReplayFile.Items.Add(files[j].Name);
            }

            if (cbbReplayFile.Items.Count > 0)
            {
                cbbReplayFile.SelectedIndex = 0;
            }
        }

        private void drawAllOtherHandCards()
        {
            drawingFormHelper.DrawOtherSortedCards(ThisPlayer.replayEntity.CurrentHandState.PlayerHoldingCards[PositionPlayer[2]], 2, false);
            drawingFormHelper.DrawOtherSortedCards(ThisPlayer.replayEntity.CurrentHandState.PlayerHoldingCards[PositionPlayer[3]], 3, false);
            drawingFormHelper.DrawOtherSortedCards(ThisPlayer.replayEntity.CurrentHandState.PlayerHoldingCards[PositionPlayer[4]], 4, false);
        }
    }
}