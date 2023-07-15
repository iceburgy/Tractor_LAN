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
        #region ��������

        //������ͼ��
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
        internal string shengjiwebUrl = FormSettings.DefaultShengjiwebUrl;
        internal MciSoundPlayer[] soundPlayersShowCard;
        internal MciSoundPlayer soundPlayerTrumpUpdated;
        internal MciSoundPlayer soundPlayerDiscardingLast8CardsFinished;
        internal MciSoundPlayer soundPlayerGameOver;
        internal MciSoundPlayer soundPlayerDraw;
        internal MciSoundPlayer soundPlayerDrawx;
        internal MciSoundPlayer soundPlayerDumpFailure;
        internal MciSoundPlayer soundPlayerRecover;

        internal CurrentPoker[] currentAllSendPokers =
        {
            new CurrentPoker(), new CurrentPoker(), new CurrentPoker(),
            new CurrentPoker()
        };

        private log4net.ILog log;

        //ԭʼ����ͼƬ

        //��ͼ�Ĵ��������ڷ���ʱʹ�ã�
        internal int currentCount = 0;

        internal CurrentPoker[] currentPokers =
        {
            new CurrentPoker(), new CurrentPoker(), new CurrentPoker(),
            new CurrentPoker()
        };

        internal int currentRank = 0;

        //��ǰһ�ָ��ҵĳ������
        internal ArrayList[] currentSendCards = new ArrayList[4];
        internal CurrentState currentState;
        internal DrawingFormHelper drawingFormHelper = null;
        //Ӧ��˭����
        //һ�γ�����˭���ȿ�ʼ������
        internal int firstSend = 0;
        internal GameConfig gameConfig = new GameConfig();
        internal Bitmap image = null;
        internal bool isNew = true;
        internal ArrayList myCardIsReady = new ArrayList();

        //*��������
        //��ǰ�����Ƶ�����
        internal ArrayList myCardsLocation = new ArrayList();
        //��ǰ�����Ƶ���ֵ
        internal ArrayList myCardsNumber = new ArrayList();
        internal ArrayList[] pokerList = null;
        //��ǰ�����Ƶ��Ƿ񱻵��
        //��ǰ�۵׵���
        internal ArrayList send8Cards = new ArrayList();
        internal int showSuits = 0;

        //*���ҵ��Ƶĸ�������
        //����˳��
        internal long sleepMaxTime = 2000;
        internal long sleepTime;
        internal CardCommands wakeupCardCommands;

        //*�滭������
        //DrawingForm����

        //����ʱĿǰ��������һ��
        internal int whoIsBigger = 0;
        internal int whoShowRank = 0;
        internal int whoseOrder = 0; //0δ��,1�ң�2�Լң�3����,4����

        internal int timerCountDown = 0;

        //�����ļ�

        #endregion // ��������

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


            //��ȡ��������
            InitAppSetting();

            notifyIcon.Text = Text;
            BackgroundImage = image;

            //������ʼ��
            bmp = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
            ThisPlayer = new TractorPlayer();

            LoadSoundResources();

            //���ص�ǰ����
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

            //�������Ʊ��labels
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
            ThisPlayer.ReenterOrResumeEvent += ThisPlayer_ReenterOrResumeEventHandler;            
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
            ThisPlayer.ObservePlayerByIDEvent += ThisPlayer_ObservePlayerByIDEventHandler;
            ThisPlayer.Last8Discarded += ThisPlayer_Last8Discarded;
            ThisPlayer.DistributingLast8Cards += ThisPlayer_DistributingLast8Cards;
            ThisPlayer.DiscardingLast8 += ThisPlayer_DiscardingLast8;
            ThisPlayer.DumpingFail += ThisPlayer_DumpingFail;
            ThisPlayer.NotifyEmojiEvent += ThisPlayer_NotifyEmojiEventHandler;
            ThisPlayer.NotifyTryToDumpResultEvent += ThisPlayer_NotifyTryToDumpResultEventHandler;
            SelectedCards = new List<int>();
            PlayerPosition = new Dictionary<string, int>();
            PositionPlayer = new Dictionary<int, string>();
            drawingFormHelper = new DrawingFormHelper(this);
            calculateRegionHelper = new CalculateRegionHelper(this);

            for (int i = 0; i < 54; i++)
            {
                cardsImages[i] = null; //��ʼ��
            }

            int shortCutKey = 1;
            foreach (string name in Enum.GetNames(typeof(EmojiType)))
            {
                this.cbbEmoji.Items.Add(string.Format("{0}-{1}", shortCutKey++, name));
            }
            this.cbbEmoji.SelectedIndex = 0;
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
            MessageBox.Show(string.Format("��Ϸ�����볢��������Ϸ\n\n��־�ļ���\n{0}", fullLogFilePath));
        }

        private void LoadSoundResources()
        {
            //������Ч
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
            soundPlayerRecover = new MciSoundPlayer(Path.Combine(fullFolder, "music\\recover.mp3"), "recover");

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
            soundPlayerRecover.LoadMediaFiles();
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
            soundPlayerRecover.SetVolume(Int32.Parse(soundVolume));
        }

        private void CreateOverridingLabels()
        {
            //�Լ�
            this.imbOverridingFlag_1.AutoSize = false;
            this.imbOverridingFlag_1.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.imbOverridingFlag_1);

            //�¼�
            this.imbOverridingFlag_2.AutoSize = false;
            this.imbOverridingFlag_2.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.imbOverridingFlag_2);

            //�Լ�
            this.imbOverridingFlag_3.AutoSize = false;
            this.imbOverridingFlag_3.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.imbOverridingFlag_3);

            //�ϼ�
            this.imbOverridingFlag_4.AutoSize = false;
            this.imbOverridingFlag_4.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.imbOverridingFlag_4);
        }


        private void InitAppSetting()
        {
            //û�������ļ������config�ļ��ж�ȡ
            if (!File.Exists("gameConfig"))
            {
                var reader = new AppSettingsReader();
                try
                {
                    Text = (String) reader.GetValue("title", typeof (String));
                }
                catch (Exception ex)
                {
                    Text = "��������ս";
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
                //ʵ�ʴ�gameConfig�ļ��ж�ȡ
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

            //δ���л���ֵ
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
                DialogResult dialogResult = MessageBox.Show("��Ϸ���ڽ����У��Ƿ�ȷ���˳���", "�Ƿ�ȷ���˳�", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    try
                    {
                        ThisPlayer.MarkPlayerOffline(ThisPlayer.MyOwnId);
                    }
                    catch (Exception)
                    {
                    }
                }
                return;
            }
            try
            {
                ThisPlayer.Quit();
            }
            catch (Exception)
            {
            }
        }

        //������ͣ�����ʱ�䣬�Լ���ͣ�������ִ������
        internal void SetPauseSet(int max, CardCommands wakeup)
        {
            sleepMaxTime = max;
            sleepTime = DateTime.Now.Ticks;
            wakeupCardCommands = wakeup;
            currentState.CurrentCardCommands = CardCommands.Pause;
        }

        #region �����¼��������

        private void init()
        {
            //ÿ�γ�ʼ�����ػ汳��
            Graphics g = Graphics.FromImage(bmp);
            drawingFormHelper.DrawBackground(g);

            //Ŀǰ�����Է���
            showSuits = 0;
            whoShowRank = 0;

            //�÷�����
            Scores = 0;

            DrawSidebarFull(g);

            send8Cards = new ArrayList();
            //������ɫ
            if (currentRank == 53)
            {
                currentState.Suit = 5;
            }

            Refresh();
        }

        private void MainForm_MouseClick(object sender, MouseEventArgs e)
        {
            //¼��طŲ�����������Ч��
            if (ThisPlayer.isReplay) return;

            //�Թ۲��ܴ���ѡ��Ч��
            //���ƻ������ʱ��Ӧ����¼�ѡ��
            if (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing ||
                ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8Cards)
            {
                if (!ThisPlayer.isObserver && e.Button == MouseButtons.Left) //���
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
                else if (e.Button == MouseButtons.Right) //�Ҽ�
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
                            //��Ӧ�Ҽ���3�������
                            //1. �׳���Ĭ�ϣ�
                            int selectMoreCount = 0;
                            for (int left = i - 1; left >= 0; left--)
                            {
                                if ((int)myCardsLocation[left] == (x - (i - left) * 12 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor)) selectMoreCount++;
                                else break;
                            }

                            bool isLeader = ThisPlayer.CurrentTrickState.Learder == ThisPlayer.PlayerId;
                            if (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8Cards)
                            {
                                //2. �����
                                selectMoreCount = Math.Min(selectMoreCount, 8 - 1 - readyCount);
                            }
                            else if (ThisPlayer.CurrentTrickState.LeadingCards != null && ThisPlayer.CurrentTrickState.LeadingCards.Count > 0)
                            {
                                //3. ����
                                selectMoreCount = Math.Min(selectMoreCount, ThisPlayer.CurrentTrickState.LeadingCards.Count - 1 - readyCount);
                            }
                            if (b)
                            {
                                var showingCardsCp = new CurrentPoker();
                                showingCardsCp.TrumpInt = (int)ThisPlayer.CurrentHandState.Trump;
                                showingCardsCp.Rank = ThisPlayer.CurrentHandState.Rank;

                                List<int> cardsToDump = new List<int>();
                                List<int> cardsToDumpCardNumber = new List<int>();

                                bool isClickedTrump = PokerHelper.IsTrump(clickedCardNumber, (Suit)showingCardsCp.TrumpInt, showingCardsCp.Rank);
                                int maxCard = showingCardsCp.Rank == 12 ? 11 : 12;
                                bool selectTopToDump = !isClickedTrump && (int)myCardsNumber[i] % 13 == maxCard || isClickedTrump && (int)myCardsNumber[i] == 53; //����Ҽ����A���ߴ�����������˦���ŵ�������������ѡ�����б��ź����˦����
                                if (selectTopToDump)
                                {
                                    bool singleCardFound = false;
                                    for (int j = 1; j <= selectMoreCount; j++)
                                    {
                                        int toAddCardNumber = (int)myCardsNumber[i - j];
                                        int toAddCardNumberOnRight = (int)myCardsNumber[i - j + 1];
                                        //�����ѡ����ͬһ��ɫ
                                        if (PokerHelper.GetSuit(toAddCardNumber) == PokerHelper.GetSuit(clickedCardNumber) ||
                                            PokerHelper.IsTrump(toAddCardNumber, (Suit)showingCardsCp.TrumpInt, showingCardsCp.Rank) && isClickedTrump)
                                        {
                                            if (isLeader)
                                            {
                                                bool isSingleCard = toAddCardNumber != toAddCardNumberOnRight;
                                                if (isSingleCard)
                                                {
                                                    if (singleCardFound)
                                                    {
                                                        showingCardsCp.Clear();
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        singleCardFound = true;
                                                    }
                                                }
                                                showingCardsCp.AddCard(toAddCardNumberOnRight);
                                                cardsToDump.Add(i - j + 1);
                                                cardsToDumpCardNumber.Add(toAddCardNumberOnRight);
                                                showingCardsCp.AddCard(toAddCardNumberOnRight);

                                                if (!isSingleCard)
                                                {
                                                    cardsToDump.Add(i - j);
                                                    cardsToDumpCardNumber.Add(toAddCardNumberOnRight);
                                                }

                                                if (j > 1)
                                                {
                                                    int tractorCount = showingCardsCp.GetTractorOfAnySuit().Count;
                                                    bool needToBreak = false;
                                                    while (cardsToDumpCardNumber.Count > 0 && !(tractorCount > 1 && tractorCount * 2 == showingCardsCp.Count))
                                                    {
                                                        needToBreak = true;
                                                        int totalCount = cardsToDumpCardNumber.Count;
                                                        int cardNumToDel = cardsToDumpCardNumber[cardsToDumpCardNumber.Count - 1];
                                                        showingCardsCp.RemoveCard(cardNumToDel);
                                                        showingCardsCp.RemoveCard(cardNumToDel);

                                                        cardsToDumpCardNumber.RemoveAt(totalCount - 1);
                                                        cardsToDump.RemoveAt(totalCount - 1);

                                                        if (cardsToDumpCardNumber.Count > 0 && cardsToDumpCardNumber[totalCount - 2] == cardNumToDel)
                                                        {
                                                            cardsToDumpCardNumber.RemoveAt(totalCount - 2);
                                                            cardsToDump.RemoveAt(totalCount - 2);
                                                        }
                                                    }
                                                    if (needToBreak)
                                                    {
                                                        break;
                                                    }
                                                }

                                                if (!isSingleCard)
                                                {
                                                    j++;
                                                }

                                                //��������������һ�����Ŷ��Ž������¸�ѭ������������ѭ������
                                                if (j == selectMoreCount && !singleCardFound)
                                                {
                                                    toAddCardNumberOnRight = (int)myCardsNumber[i - j];
                                                    showingCardsCp.AddCard(toAddCardNumberOnRight);
                                                    showingCardsCp.AddCard(toAddCardNumberOnRight);
                                                    int tractorCount = showingCardsCp.GetTractorOfAnySuit().Count;
                                                    if (tractorCount > 1 && tractorCount * 2 == showingCardsCp.Count)
                                                    {
                                                        cardsToDump.Add(i - j);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }

                                if (cardsToDump.Count >= 2)
                                {
                                    foreach (int c in cardsToDump)
                                    {
                                        myCardIsReady[c] = b;
                                    }
                                }
                                else
                                {
                                    showingCardsCp.Clear();
                                    bool selectAll = false; //����Ҽ����ɢ�ƣ�������ѡ�����б��Ż�ɫ����
                                    for (int j = 1; j <= selectMoreCount; j++)
                                    {
                                        //�����ѡ����ͬһ��ɫ
                                        if ((int)myCardsLocation[i - j] == (x - 12 * drawingFormHelper.scaleDividend / drawingFormHelper.scaleDivisor))
                                        {
                                            if (isLeader)
                                            {
                                                //��һ��������ѡ��Ϊ���ӣ�������
                                                if (!selectAll)
                                                {
                                                    int toAddCardNumber = (int)myCardsNumber[i - j];
                                                    int toAddCardNumberOnRight = (int)myCardsNumber[i - j + 1];
                                                    showingCardsCp.AddCard(toAddCardNumberOnRight);
                                                    showingCardsCp.AddCard(toAddCardNumber);
                                                }

                                                if (showingCardsCp.Count == 2 && (showingCardsCp.GetPairs().Count == 1) || //�����һ��
                                                    ((showingCardsCp.GetTractorOfAnySuit().Count > 1) &&
                                                    showingCardsCp.Count == showingCardsCp.GetTractorOfAnySuit().Count * 2))  //�����������
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
                                                //��׻��߸���
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
            //�Թ۲��ܴ�������Ч��
            //����ʱ��Ӧ����¼�����
            else if (!ThisPlayer.isObserver &&
                (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DistributingCards ||
                ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DistributingCardsFinished))
            {
                ExposeTrump(e);
            }
            //һ�ֽ���ʱ�Ҽ��鿴���һ�ָ����������ƣ���С��һ�룬�������½�
            else if (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Ending && e.Button == MouseButtons.Right) //�Ҽ�
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
            //������һ��
            drawingFormHelper.DrawCenterImage();
            drawingFormHelper.DrawScoreImageAndCards();

            this.drawingFormHelper.DrawMessages(new string[] { "�ؿ����ֳ��ơ�˭������" });
            //�鿴˭����ʲô��
            drawingFormHelper.LastTrumpMadeCardsShow();
            //������һ�ָ����������ƣ���С��һ�룬�������½ǣ������ػ���ǰ�ָ�����������
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
            //�ж��Ƿ��ڿ��ƽ׶�
            if (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8Cards &&
                ThisPlayer.CurrentHandState.Last8Holder == ThisPlayer.PlayerId) //������ҿ���
            {
                if (SelectedCards.Count == 8)
                {
                    //����,���Բ�ȥС��
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
                //�����׼�������ƺϷ�
                if (showingCardsValidationResult.ResultType == ShowingCardsValidationResultType.Valid)
                {
                    //��ȥС��
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
                    //��ȥС��
                    this.btnPig.Visible = false;

                    ThisPlayer.ValidateDumpingCards(ThisPlayer.MyOwnId, SelectedCards);
                }
            }
        }

        private void ThisPlayer_NotifyTryToDumpResultEventHandler(ShowingCardsValidationResult result)
        {
            if (result.ResultType == ShowingCardsValidationResultType.DumpingSuccess) //˦�Ƴɹ�.
            {
                foreach (int card in SelectedCards)
                {
                    ThisPlayer.CurrentPoker.RemoveCard(card);
                }
                ThisPlayer.ShowCards(ThisPlayer.MyOwnId, SelectedCards);
                drawingFormHelper.DrawMyHandCards();
                SelectedCards.Clear();
            }
            //˦��ʧ��
            else
            {
                this.drawingFormHelper.DrawMessages(new string[] { string.Format("˦��{0}��ʧ��", SelectedCards.Count), string.Format("���֣�{0}", SelectedCards.Count * 10) });
                //˦��ʧ�ܲ�����ʾ��
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

        //���ڻ滭����,��������ͼ�񻭵�������
        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            //��bmp����������
            g.DrawImage(bmp, 0, 0);
        }

        #endregion

        #region �˵��¼�����

        internal void MenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem) sender;
            if (menuItem.Text.Equals("�˳�"))
            {
                Close();
            }
            else if (menuItem.Name.StartsWith("toolStripMenuItemBeginRank") && !ThisPlayer.isObserver)
            {
                if (AllOnline() && !ThisPlayer.isObserver && ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing)
                {
                    this.ThisPlayer_NotifyMessageEventHandler(new string[] { "��Ϸ��;�������޸ĴӼ�����", "����ɴ�����Ϸ������" });
                    return;
                }

                string beginRankString = menuItem.Name.Substring("toolStripMenuItemBeginRank".Length, 1);
                ThisPlayer.SetBeginRank(ThisPlayer.MyOwnId, beginRankString);
            }
        }

        //�����¼�����
        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            Show();
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            Activate();
        }
    

        #endregion // �˵��¼�����

        #region handle player event

        private void StartGame()
        {
            //��Ϸ��ʼǰ���ø��ֱ���
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
            //���Ʋ�����ʾ��
            if (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DistributingCards)
            {
                soundPlayerDraw.Play(this.enableSound);
            }

            drawingFormHelper.IGetCard(cardNumber);

            //�йܴ�������
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

        //�����������ػ����ƺͳ�����
        private void ThisPlayer_ReenterOrResumeEventHandler()
        {
            Graphics g = Graphics.FromImage(bmp);
            DrawSidebarFull(g);
            Refresh();
            g.Dispose();
            this.ThisPlayer.playerLocalCache.ShowedCardsInCurrentTrick = ThisPlayer.CurrentTrickState.ShowedCards.ToDictionary(entry => entry.Key, entry => entry.Value.ToList());
            this.ThisPlayer_PlayerCurrentTrickShowedCards();
            drawingFormHelper.DrawMyPlayingCards(ThisPlayer.CurrentPoker);

            DrawDiscardedCardsCaller();
        }

        private void DrawDiscardedCardsCaller()
        {
            if (ThisPlayer.CurrentPoker != null && ThisPlayer.CurrentPoker.Count > 0 &&
                ThisPlayer.CurrentHandState.Last8Holder == ThisPlayer.PlayerId &&
                ThisPlayer.CurrentHandState.DiscardedCards != null &&
                ThisPlayer.CurrentHandState.DiscardedCards.Length == 8)
            {
                drawingFormHelper.DrawDiscardedCards();
            }
        }

        //��鵱ǰ�����ߵ����Ƿ�Ϊ���ƣ�0 - ��1 - �ǣ�2 - ����Ϊ������3 - ����Ϊ������
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

            //����µ�һ�ֿ�ʼ�����û�����Ϣ
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
            //������Ʊ�������»��������Ϣ
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

            //������ڻؿ����ֳ��ƣ����ػ��ոճ�����
            if (!this.ThisPlayer.ShowLastTrickCards)
            {
                //������һ��
                if (ThisPlayer.CurrentTrickState.CountOfPlayerShowedCards() == 1)
                {
                    drawingFormHelper.DrawCenterImage();
                    drawingFormHelper.DrawScoreImageAndCards();
                }

                //���ų�����Ч
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

            //������ڻؿ������Լ��ոճ����ƣ������ûؿ������»���
            if (this.ThisPlayer.ShowLastTrickCards && latestPlayer == ThisPlayer.PlayerId)
            {
                this.ThisPlayer.ShowLastTrickCards = false;
                ThisPlayer_PlayerCurrentTrickShowedCards();
            }

            if (!gameConfig.IsDebug && ThisPlayer.CurrentTrickState.NextPlayer() == ThisPlayer.PlayerId)
                drawingFormHelper.DrawMyPlayingCards(ThisPlayer.CurrentPoker);

            //��ʱ�����Թ�����
            if (ThisPlayer.isObserver && ThisPlayer.PlayerId == latestPlayer)
            {
                ThisPlayer.CurrentPoker = ThisPlayer.CurrentHandState.PlayerHoldingCards[ThisPlayer.PlayerId];
                ResortMyCards();
            }

            if (winResult > 0) drawingFormHelper.DrawOverridingFlag(this.PlayerPosition[this.ThisPlayer.playerLocalCache.WinnderID], this.ThisPlayer.playerLocalCache.WinResult - 1, 1);

            RobotPlayFollowing();
        }

        //������һ�ָ����������ƣ���С��һ�룬�������½�
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

        //���Ƶ�ǰ�ָ����������ƣ��������л��ӽǻ�ǰ�غϴ��Ʊ��ʱ��
        private void ThisPlayer_PlayerCurrentTrickShowedCards()
        {
            //����������
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
            //�ػ���������
            if (this.ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8Cards)
            {
                drawingFormHelper.TrumpMadeCardsShow();
            }

            //�ػ����Ʊ��
            if (!string.IsNullOrEmpty(this.ThisPlayer.playerLocalCache.WinnderID))
            {
                drawingFormHelper.DrawOverridingFlag(this.PlayerPosition[this.ThisPlayer.playerLocalCache.WinnderID], this.ThisPlayer.playerLocalCache.WinResult - 1, 1);
            }
            Refresh();
        }

        //���Ƶ�ǰ�������棨�������л��ӽ�ʱ��
        private void ThisPlayer_ShowEnding()
        {
            //����������
            drawingFormHelper.DrawCenterImage();

            ThisPlayer_HandEnding();
        }

        private void ThisPlayer_ShowingCardBegan()
        {
            ThisPlayer_DiscardingLast8();
            drawingFormHelper.RemoveToolbar();
            drawingFormHelper.DrawCenterImage();
            drawingFormHelper.DrawScoreImageAndCards();

            //���ƿ�ʼǰ��ȥ������Ҫ��controls
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

        private void ThisPlayer_NewPlayerJoined(bool meJoined)
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
                this.btnSendEmoji.Show();
                this.cbbEmoji.Show();
                this.ToolStripMenuItemInRoom.Visible = true;
            }
            else
            {
                this.btnObserveNext.Show();
                this.ToolStripMenuItemObserve.Visible = true;
            }
            this.btnRoomSetting.Show();

            //�ڴ������ߵ�label��ס��online������֣�ֻ�������أ����˷�������ʾ
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
                    //set player position
                    PlayerPosition[curPlayer.PlayerId] = i + 1;
                    PositionPlayer[i + 1] = curPlayer.PlayerId;
                    
                    nickNameLabels[i].Text = curPlayer.PlayerId;
                    foreach (string ob in curPlayer.Observers)
                    {
                        string newLine = i == 0 ? "" : "\n";
                        nickNameLabels[i].Text += string.Format("{0}��{1}��", newLine, ob);
                    }
                }
                else
                {
                    nickNameLabels[i].Text = "";
                }
                curIndex = (curIndex + 1) % 4;
            }

            bool isHelpSeen = FormSettings.GetSettingBool(FormSettings.KeyIsHelpSeen);
            if (!isHelpSeen && meJoined)
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
                this.drawingFormHelper.DrawMessages(new string[] { "¼���ļ�������", replayState.ReplayId });
            }
        }

        private void DisplayRoomSetting(bool isRoomSettingModified)
        {
            List<string> msgs = new List<string>();
            if (this.ThisPlayer.CurrentRoomSetting.DisplaySignalCardInfo)
            {
                msgs.AddRange(new string[] { "�ź��ƻ���������", "8��9��J��Q�������н�����", "���Ƶ�������Ѱ��ԼҰ�æ����", "" });
            }
            if (isRoomSettingModified)
            {
                msgs.Add("���������Ѹ��ģ�");
            }
            else
            {
                msgs.Add("�������ã�");
            }
            msgs.Add(string.Format("����Ͷ����{0}", this.ThisPlayer.CurrentRoomSetting.AllowSurrender ? "��" : "��"));
            msgs.Add(string.Format("����J���ף�{0}", this.ThisPlayer.CurrentRoomSetting.AllowJToBottom ? "��" : "��"));
            msgs.Add(string.Format("�����й��Զ����ƣ�{0}", this.ThisPlayer.CurrentRoomSetting.AllowRobotMakeTrump ? "��" : "��"));
            msgs.Add(string.Format("�������С�ڵ���Xʱ������{0}", this.ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewScoreCards >= 0 ? this.ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewScoreCards.ToString() : "��"));
            msgs.Add(string.Format("��������С�ڵ���X��ʱ������{0}", this.ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewTrumpCards >= 0 ? this.ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewTrumpCards.ToString() : "��"));
            msgs.Add(string.Format("���������ȴ�ʱ����{0}��", this.ThisPlayer.CurrentRoomSetting.secondsToWaitForReenter));
            msgs.Add(string.Format("����ʱ�ޣ�{0}��", this.ThisPlayer.CurrentRoomSetting.secondsToShowCards));
            msgs.Add(string.Format("���ʱ�ޣ�{0}��", this.ThisPlayer.CurrentRoomSetting.secondsToDiscardCards));

            List<int> mandRanks = this.ThisPlayer.CurrentRoomSetting.GetManditoryRanks();
            string[] mandRanksStr = mandRanks.Select(x => CommonMethods.cardNumToValue[x].ToString()).ToArray();
            msgs.Add(mandRanks.Count > 0 ? string.Format("�ش�{0}", string.Join(",", mandRanksStr)) : "û�бش���");

            ThisPlayer_NotifyMessageEventHandler(msgs.ToArray());
        }

        private void ThisPlayer_NewPlayerReadyToStart(bool readyToStart, bool anyBecomesReady)
        {
            this.btnReady.Enabled = ThisPlayer.CurrentGameState.Players.Where(p => p != null && p.IsReadyToStart).Count() < 4;
            this.btnReady.Text = readyToStart ? "ȡ��" : "����";
            this.ToolStripMenuItemGetReady.Checked = readyToStart;

            //����˭�������
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
                    readyLabels[i].Text = "������";
                }
                else if (curPlayer != null && curPlayer.IsRobot)
                {
                    readyLabels[i].Text = "�й���";
                }
                else if (curPlayer != null && !curPlayer.IsReadyToStart)
                {
                    readyLabels[i].Text = "˼����";
                }
                else if (curPlayer != null && !string.IsNullOrEmpty(ThisPlayer.CurrentHandState.Starter) && curPlayer.PlayerId == ThisPlayer.CurrentHandState.Starter)
                {
                    readyLabels[i].Text = "ׯ��";
                }
                else
                {
                    readyLabels[i].Text = (curIndex + 1).ToString();
                }
                curIndex = (curIndex + 1) % 4;
            }
            if (anyBecomesReady &&
                (ThisPlayer.CurrentHandState.CurrentHandStep <= HandStep.BeforeDistributingCards || ThisPlayer.CurrentHandState.CurrentHandStep >= HandStep.SpecialEnding))
            {
                if (CommonMethods.AllReady(ThisPlayer.CurrentGameState.Players)) soundPlayerDiscardingLast8CardsFinished.Play(this.enableSound);
                else soundPlayerRecover.Play(this.enableSound);
            }
        }

        private void ThisPlayer_PlayerToggleIsRobot(bool isRobot)
        {
            bool shouldTrigger = isRobot && isRobot != gameConfig.IsDebug;
            this.ToolStripMenuItemRobot.Checked = isRobot;
            gameConfig.IsDebug = isRobot;
            this.btnRobot.Text = isRobot ? "ȡ��" : "�й�";

            //����˭���й���
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
                    readyLabels[i].Text = "������";
                }
                else if (curPlayer != null && curPlayer.IsRobot)
                {
                    readyLabels[i].Text = "�й���";
                }
                else if (curPlayer != null && !curPlayer.IsReadyToStart)
                {
                    readyLabels[i].Text = "˼����";
                }
                else if (curPlayer != null && !string.IsNullOrEmpty(ThisPlayer.CurrentHandState.Starter) && curPlayer.PlayerId == ThisPlayer.CurrentHandState.Starter)
                {
                    readyLabels[i].Text = "ׯ��";
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

        private void ThisPlayer_RoomSettingUpdatedEventHandler(RoomSetting roomSetting, bool showMessage)
        {
            bool isRoomSettingModified = this.ThisPlayer.CurrentRoomSetting == null || !this.ThisPlayer.CurrentRoomSetting.Equals(roomSetting);
            this.ThisPlayer.CurrentRoomSetting = roomSetting;
            if (this.ThisPlayer.CurrentRoomSetting.RoomOwner == this.ThisPlayer.MyOwnId)
            {
                //��ʾ�������ɼ��Ĳ˵�
                this.BeginRankToolStripMenuItem.Visible = true;
                this.ResumeGameToolStripMenuItem.Visible = true;
                this.TeamUpToolStripMenuItem.Visible = true;
                this.SwapSeatToolStripMenuItem.Visible = true;
            }
            this.lblRoomName.Text = this.ThisPlayer.CurrentRoomSetting.RoomName;
            if (showMessage)
            {
                this.DisplayRoomSetting(isRoomSettingModified);
            }
        }

        private void ThisPlayer_ShowAllHandCardsEventHandler()
        {
            //����������
            drawingFormHelper.DrawCenterImage();
            drawingFormHelper.DrawScoreImageAndCards();
            //�ػ����ƣ��Ӷ��ѱ�ѡ�е��ƷŻ�ȥ
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
            this.btnShengjiweb.Hide();
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
            this.btnSendEmoji.Hide();
            this.cbbEmoji.Hide();
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

            //���ؽ������ɼ��Ĳ˵�
            this.BeginRankToolStripMenuItem.Visible = false;
            this.ResumeGameToolStripMenuItem.Visible = false;
            this.TeamUpToolStripMenuItem.Visible = false;
            this.SwapSeatToolStripMenuItem.Visible = false;

            //����״̬
            this.ThisPlayer.CurrentGameState = new GameState();
            this.ThisPlayer.CurrentHandState = new CurrentHandState(this.ThisPlayer.CurrentGameState);

            //ֹͣ����ʱ
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
            btnJoinVideoCall.Text = "��������";
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
            labelOnline.Text = "����";
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

                        if (j == 2)
                        {
                            labelOffsetX = -seatSize / 2;
                            labelRoomByPos.Location = new System.Drawing.Point(offsetXSeat + labelOffsetX, offsetYSeat + labelOffsetY);
                        }
                        
                        if (j == 0)
                        {
                            labelRoomByPos.Location = new System.Drawing.Point(0, 0);
                            labelRoomByPos.Size = new System.Drawing.Size(0, seatSize);

                            labelRoomByPos.Dock = System.Windows.Forms.DockStyle.Bottom;
                            labelRoomByPos.TextAlign = System.Drawing.ContentAlignment.BottomLeft;

                            TableLayoutPanel tlpRoomByPos = new TableLayoutPanel();
                            tlpRoomByPos.SuspendLayout();

                            tlpRoomByPos.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
                            tlpRoomByPos.Size = new System.Drawing.Size(0, seatSize);
                            tlpRoomByPos.AutoSize = true;
                            tlpRoomByPos.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
                            tlpRoomByPos.BackColor = System.Drawing.Color.Transparent;
                            tlpRoomByPos.ColumnCount = 1;
                            tlpRoomByPos.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
                            tlpRoomByPos.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
                            tlpRoomByPos.Controls.Add(labelRoomByPos, 0, 0);

                            labelOffsetX = -seatSize / 2;
                            tlpRoomByPos.Location = new System.Drawing.Point(offsetXSeat + labelOffsetX, offsetYSeat + labelOffsetY);

                            tlpRoomByPos.Name = string.Format("{0}_lblRoom_{1}_{2}_tlp", roomControlPrefix, room.RoomID, j);
                            tlpRoomByPos.RowCount = 1;
                            tlpRoomByPos.RowStyles.Add(new System.Windows.Forms.RowStyle());
                            tlpRoomByPos.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));

                            this.Controls.Add(tlpRoomByPos);

                            tlpRoomByPos.ResumeLayout(false);
                            tlpRoomByPos.PerformLayout();
                        }
                        else if (j == 1)
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
                        foreach (string ob in players[j].Observers)
                        {
                            labelRoomByPos.Text += string.Format("\n��{0}��", ob);
                        }
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

        //�йܴ���
        private void RobotPlayFollowing()
        {
            //����
            if ((this.ThisPlayer.playerLocalCache.isLastTrick || gameConfig.IsDebug) && !ThisPlayer.isObserver &&
                ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing &&
                ThisPlayer.CurrentTrickState.NextPlayer() == ThisPlayer.PlayerId &&
                ThisPlayer.CurrentTrickState.IsStarted())
            {
                List<int> tempSelectedCards = new List<int>();
                Algorithm.MustSelectedCards(tempSelectedCards, this.ThisPlayer.CurrentTrickState, this.ThisPlayer.CurrentPoker);

                SelectedCards.Clear();
                for (int i = 0; i < myCardsNumber.Count; i++)
                {
                    if (tempSelectedCards.Contains((int)myCardsNumber[i]))
                    {
                        SelectedCards.Add((int)myCardsNumber[i]);
                        tempSelectedCards.Remove((int)myCardsNumber[i]);
                    }
                }

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

            //��ѡ��������û�������ֶ�ѡ�ƣ����б�ѡ�Ƶ�������Զ�ѡ���ѡ�ƣ�������ҿ�ݳ���
            if (SelectedCards.Count == 0 &&
                !ThisPlayer.isObserver &&
                ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing &&
                ThisPlayer.CurrentTrickState.NextPlayer() == ThisPlayer.PlayerId &&
                ThisPlayer.CurrentTrickState.IsStarted())
            {
                //���ѡ���ƣ����ػ����ƣ�����ֱ�ӵ�ȷ������
                List<int> tempSelectedCards = new List<int>();
                Algorithm.MustSelectedCardsNoShow(tempSelectedCards, this.ThisPlayer.CurrentTrickState, this.ThisPlayer.CurrentPoker);
                if (tempSelectedCards.Count > 0)
                {
                    SelectedCards.Clear();
                    for (int i = 0; i < myCardsNumber.Count; i++)
                    {
                        if (tempSelectedCards.Contains((int)myCardsNumber[i]))
                        {
                            //��ѡ�������������� via myCardIsReady
                            myCardIsReady[i] = true;
                            SelectedCards.Add((int)myCardsNumber[i]);
                            tempSelectedCards.Remove((int)myCardsNumber[i]);
                        }
                    }

                    drawingFormHelper.DrawMyPlayingCards(ThisPlayer.CurrentPoker);
                    Refresh();

                    ThisPlayer.CardsReady(ThisPlayer.PlayerId, myCardIsReady);
                }
            }
        }

        //�йܴ�������
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

            DrawSidebarFull(g);

            ResortMyCards();

            drawingFormHelper.ReDrawToolbar();

            Refresh();
            g.Dispose();
        }

        private void DrawSidebarFull(Graphics g)
        {
            //����Sidebar
            drawingFormHelper.DrawSidebar(g);
            //���ƶ�������

            drawingFormHelper.Starter();

            //����Rank
            drawingFormHelper.Rank();

            //���ƻ�ɫ
            drawingFormHelper.Trump();
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
            //����˭��ׯ��
            for (int i = 0; i < 4; i++)
            {
                var curPlayer = ThisPlayer.CurrentGameState.Players[curIndex];
                if (curPlayer != null && curPlayer.IsOffline)
                {
                    starterLabels[i].Text = "������";
                }
                else if (curPlayer != null && curPlayer.IsRobot)
                {
                    starterLabels[i].Text = "�й���";
                }
                else if (curPlayer != null && !curPlayer.IsReadyToStart)
                {
                    starterLabels[i].Text = "˼����";
                }
                else if (curPlayer != null && !string.IsNullOrEmpty(ThisPlayer.CurrentHandState.Starter) &&
                    curPlayer.PlayerId == ThisPlayer.CurrentHandState.Starter &&
                    ThisPlayer.CurrentHandState.CurrentHandStep != HandStep.Ending)
                {
                    starterLabels[i].Text = "ׯ��";
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
                if (m.Contains("��ʤ��"))
                {
                    soundPlayerGameOver.Play(this.enableSound);

                    // �����̻�
                    if (this.ThisPlayer.CurrentRoomSetting.RoomOwner == this.ThisPlayer.MyOwnId)
                    {
                        Bitmap[] emojiList = this.drawingFormHelper.emojiDict[EmojiType.Fireworks];
                        int emojiListSize = emojiList.Length;
                        int emojiRandomIndex = CommonMethods.RandomNext(emojiListSize);
                        ThisPlayer.SendEmoji((int)EmojiType.Fireworks, emojiRandomIndex, true);
                    }
                }
                else if (m.Equals(CommonMethods.reenterRoomSignal))
                {
                    if (!ThisPlayer.isObserver)
                    {
                        ThisPlayer.IsTryingReenter = true;
                        this.btnEnterHall.Hide();
                        this.btnShengjiweb.Hide();
                        this.btnReplay.Hide();
                    }
                }
                else if (m.Equals(CommonMethods.resumeGameSignal))
                {
                    if (!ThisPlayer.isObserver)
                    {
                        ThisPlayer.IsTryingResumeGame = true;
                    }
                }
                else if (m.Contains("����Ϸ������ʼ"))
                {
                    //����Ϸ��ʼǰ������ʾ�����������Ҫ��ׯ
                    soundPlayerGameOver.Play(this.enableSound);
                }
                else if (m.Contains("����"))
                {
                    //˦��ʧ�ܲ�����ʾ��
                    soundPlayerDumpFailure.Play(this.enableSound);
                }
            }
            this.drawingFormHelper.DrawMessages(msgs);
        }

        private void ThisPlayer_NotifyEmojiEventHandler(string playerID, int emojiType, int emojiIndex, bool isCenter, string msgString)
        {
            if (!isCenter && !string.IsNullOrEmpty(msgString))
            {
                this.drawingFormHelper.DrawMessages(new string[] { string.Format("��{0}��˵��", playerID), msgString });
            }
            if (emojiType < 0 || emojiType >= this.drawingFormHelper.emojiDict.Count || !PlayerPosition.ContainsKey(playerID)) return;
            var threadDrawEmoji = new Thread(() =>
            {
                PictureBox pic;
                int displayDuration;
                if (isCenter)
                {
                    pic = this.fireworksPic;
                    displayDuration = 5000;
                }
                else
                {
                    int position = PlayerPosition[playerID];
                    pic = this.drawingFormHelper.emojiPictureBoxes[position - 1];
                    displayDuration = 3000;
                }
                EmojiType emojiEnumType = (EmojiType)emojiType;
                Bitmap[] emojiList = this.drawingFormHelper.emojiDict[emojiEnumType];

                Invoke(new Action(() =>
                {
                    pic.Show();
                    pic.BringToFront();
                    pic.Image = emojiList[emojiIndex];
                }));

                Thread.Sleep(displayDuration);
                if (this.IsDisposed) return;
                Invoke(new Action(() =>
                {
                    pic.Hide();
                    if (!isCenter && !ThisPlayer.isObserver && playerID == ThisPlayer.MyOwnId)
                    {
                        this.btnSendEmoji.Enabled = true;
                    }
                }));
            });
            threadDrawEmoji.Start();
        }

        private void ThisPlayer_NotifyStartTimerEventHandler(int timerLength)
        {
            this.timerCountDown = timerLength;
            if (timerLength > 0)
            {
                this.drawingFormHelper.DrawCountDown(true);
                this.theTimer.Start();
            }
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

        private void ThisPlayer_CutCardShoeCardsEventHandler()
        {
            string cutInfo = string.Empty;
            if (!gameConfig.IsDebug && enableCutCards)
            {
                FormCutCards frmCutCards = new FormCutCards();
                frmCutCards.StartPosition = FormStartPosition.CenterParent;
                frmCutCards.TopMost = true;
                frmCutCards.ShowDialog();
                if (frmCutCards.noMoreCut)
                {
                    FormSettings.SetSetting(FormSettings.KeyEnableCutCards, "false");
                    this.LoadSettings();
                }
                cutInfo = string.Format("{0},{1}", frmCutCards.cutType, frmCutCards.cutPoint);
            }
            ThisPlayer.PlayerHasCutCards(ThisPlayer.PlayerId, cutInfo);
        }

        private void ThisPlayer_ObservePlayerByIDEventHandler()
        {
            ThisPlayer.CurrentPoker = ThisPlayer.CurrentHandState.PlayerHoldingCards[ThisPlayer.PlayerId];
            if (ThisPlayer.isObserverChanged)
            {
                ResortMyCards();
                DrawDiscardedCardsCaller();

                Graphics g = Graphics.FromImage(bmp);
                DrawSidebarFull(g);

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
            DrawDiscardedCardsCaller();
            Refresh();
            g.Dispose();
        }

        private void ThisPlayer_DistributingLast8Cards()
        {
            //��ȥ�����ư�ť���ٷŷ����ƶ���
            drawingFormHelper.ReDrawToolbar();
            //�ػ����ƣ��Ӷ��ѱ��������Լ������ƷŻ�ȥ
            ResortMyCards();

            int position = PlayerPosition[ThisPlayer.CurrentHandState.Last8Holder];
            //�Լ����ײ��û�
            if (position > 1)
            {
                drawingFormHelper.DrawDistributingLast8Cards(position);
            }
            else
            {
                //����������Ч
                soundPlayerDrawx.Play(this.enableSound);
            }

            if (ThisPlayer.isObserver)
            {
                return;
            }

            //���ƽ�������������й�״̬����ȡ���й�
            if (gameConfig.IsDebug && !ThisPlayer.CurrentRoomSetting.IsFullDebug)
            {
                this.btnRobot.PerformClick();
            }

            //���ƽ������������Ͷ��������ʾͶ����ť
            if (ThisPlayer.CurrentRoomSetting.AllowSurrender)
            {
                this.btnSurrender.Visible = true;
            }

            //������̨�µ���ҿ��Ը���
            if (!this.ThisPlayer.CurrentGameState.ArePlayersInSameTeam(this.ThisPlayer.CurrentHandState.Starter, this.ThisPlayer.PlayerId))
            {
                //���ƽ������������������������ж��Ƿ����ʾ������ť
                int riotScoreCap = ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewScoreCards;
                if (ThisPlayer.CurrentPoker.GetTotalScore() <= riotScoreCap)
                {
                    this.btnRiot.Visible = true;
                }

                //���ƽ���������������Ƹ��������ж��Ƿ����ʾ������ť
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

            //�йܴ������
            if (ThisPlayer.CurrentRoomSetting.IsFullDebug && gameConfig.IsDebug && !ThisPlayer.isObserver)
            {
                if (ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8Cards &&
                    ThisPlayer.CurrentHandState.Last8Holder == ThisPlayer.PlayerId) //������ҿ���
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
            //������һ��
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
            //Ϊ��ֹ�������������¾�����ť������ظ����ƣ�����һ�¾�����disable������ť
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
                this.ThisPlayer_NotifyMessageEventHandler(new string[] { "��Ϸ��;����������ƾ�", "����ɴ�����Ϸ������" });
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
                this.ThisPlayer_NotifyMessageEventHandler(new string[] { "��Ϸ��;������������", "����ɴ�����Ϸ������" });
                return;
            }

            ThisPlayer.TeamUp(ThisPlayer.MyOwnId);
        }

        private void SwapSeatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ThisPlayer.isObserver) return;
            if (AllOnline() && !ThisPlayer.isObserver && ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing)
            {
                this.ThisPlayer_NotifyMessageEventHandler(new string[] { "��Ϸ��;���������һ���", "����ɴ�����Ϸ������" });
                return;
            }

            int offset = -1;
            var menuItem = (ToolStripMenuItem)sender;

            switch (menuItem.Text)
            {
                case "�¼�":
                    offset = 1;
                    break;
                case "�Լ�":
                    offset = 2;
                    break;
                case "�ϼ�":
                    offset = 3;
                    break;
                default:
                    break;
            }

            ThisPlayer.SwapSeat(ThisPlayer.MyOwnId, offset);
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
            MessageBox.Show(string.Format("��ǰ�汾��{0}", assemblyVersion));
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
            ThisPlayer.ToggleIsRobot();
        }

        private void btnSendEmoji_Click(object sender, EventArgs e)
        {
            this.btnSendEmoji.Enabled = false;

            EmojiType emojiEnumType = (EmojiType)this.cbbEmoji.SelectedIndex;
            Bitmap[] emojiList = this.drawingFormHelper.emojiDict[emojiEnumType];
            int emojiListSize = emojiList.Length;
            int emojiRandomIndex = CommonMethods.RandomNext(emojiListSize);

            ThisPlayer.SendEmoji(this.cbbEmoji.SelectedIndex, emojiRandomIndex, false);
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
                this.btnShengjiweb.Show();
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
                this.btnShengjiweb.Show();
                this.btnReplay.Show();
                HideRoomControls();
                return;
            }
            if (AllOnline() && !ThisPlayer.isObserver && !ThisPlayer.isReplay && ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.Playing)
            {
                Application.Restart();
                return;
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
            foreach (PlayerEntity player in ThisPlayer.CurrentGameState.Players)
            {
                if (player == null || player.IsOffline) return false;
            }
            return true;
        }

        private void btnSurrender_Click(object sender, EventArgs e)
        {
            this.btnSurrender.Visible = false;
            ThisPlayer.SpecialEndGameRequest(ThisPlayer.MyOwnId);
        }

        private void ThisPlayer_SpecialEndGameShouldAgreeEventHandler()
        {
            this.btnSurrender.Visible = false;
            DialogResult dialogResult = MessageBox.Show("�������Ͷ�����Ƿ�ͬ�⣿", "�Ƿ�ͬ��Ͷ��", MessageBoxButtons.YesNo);
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
            //���������������ж������ָ���
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
            userManual = "����������";
            userManual += "\n- ��������������������";
            userManual += "\n- ˦��ʧ�ܣ�������ÿ��10�ֽ��з���";
            userManual += "\n\n�����ؼ���";
            userManual += "\n- �Ҽ�ѡ�ƣ��Զ�����ѡ�����кϷ��������ƣ������ڳ��ơ���ף�";
            userManual += "\n- �Ҽ������հ״��鿴�����ֳ��ơ�˭����ʲô��";
            userManual += "\n- ����ͼ�����һȦ�еĴ��ƣ���ɱ��ͼ�����������";
            userManual += "\n\n����ݼ���";
            userManual += "\n- ���ƣ�����S��Show cards��";
            userManual += "\n- �����������������˵�������Ӧ������";
            userManual += "\n- ¼��ط���һ�֣����ͷ";
            userManual += "\n- ¼��ط���һ�֣��Ҽ�ͷ";
            userManual += "\n- ���������F1";
            userManual += "\n- �����һ�����䣺F2";
            userManual += "\n- ������F3������Z��Zhunbei׼����";
            userManual += "\n- �йܣ�F4������R��Robot��";
            userManual += "\n- �Թ��¼ң�F5�������Թ�ģʽ�£�";
            userManual += "\n\n�˰�����Ϣ���ڲ˵�����������ʹ��˵�������ٴβ鿴";
            userManual += "\n\n������ʾ��";

            DialogResult dialogResult = MessageBox.Show(userManual, "ʹ��˵��", MessageBoxButtons.YesNo);
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
                    if (ThisPlayer.isReplay && !this.cbbReplayDate.Focused && !this.cbbReplayFile.Focused)
                    {
                        this.btnPreviousTrick.PerformClick();
                        return true;
                    }
                    break;
                case Keys.Right:
                    if (ThisPlayer.isReplay && !this.cbbReplayDate.Focused && !this.cbbReplayFile.Focused)
                    {
                        this.btnNextTrick.PerformClick();
                        return true;
                    }
                    break;
                case Keys.Up:
                    if (ThisPlayer.isReplay && !this.cbbReplayDate.Focused && !this.cbbReplayFile.Focused)
                    {
                        this.btnFirstTrick.PerformClick();
                        return true;
                    }
                    break;
                case Keys.Down:
                    if (ThisPlayer.isReplay && !this.cbbReplayDate.Focused && !this.cbbReplayFile.Focused)
                    {
                        this.btnLastTrick.PerformClick();
                        return true;
                    }
                    break;
                case Keys.F5:
                    if (ThisPlayer.isReplay)
                    {
                        this.btnReplayAngle.PerformClick();
                        return true;
                    }
                    break;
                case Keys.D1:
                case Keys.D2:
                case Keys.D3:
                case Keys.D4:
                case Keys.D5:
                case Keys.D6:
                case Keys.D7:
                case Keys.D8:
                case Keys.D9:
                    if (this.cbbEmoji.Visible && this.btnSendEmoji.Visible && this.btnSendEmoji.Enabled)
                    {
                        int emojiIndex = (int)keyData - (int)Keys.D1;
                        if (0 <= emojiIndex && emojiIndex < this.drawingFormHelper.emojiDict.Count)
                        {
                            this.cbbEmoji.SelectedIndex = emojiIndex;
                            this.btnSendEmoji.PerformClick();
                            return true;
                        }
                    }
                    break;
                default:
                    break;
            }
            return base.ProcessCmdKey(ref msg, keyData);
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
                MessageBox.Show("δ�ҵ�¼���ļ�");
                return;
            }

            btnEnterHall.Hide();
            btnShengjiweb.Hide();
            this.btnReplay.Hide();
            this.lblReplayDate.Show();
            this.lblReplayFile.Show();
            this.cbbReplayDate.Show();
            this.cbbReplayFile.Show();
            this.btnLoadReplay.Show();
            ThisPlayer.isReplay = true;

            cbbReplayDate.Items.Clear();
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

            lblSouthStarter.Text = players[0] == ThisPlayer.replayEntity.CurrentHandState.Starter ? "ׯ��" : "1";
            lblEastStarter.Text = players[1] == ThisPlayer.replayEntity.CurrentHandState.Starter ? "ׯ��" : "2";
            lblNorthStarter.Text = players[2] == ThisPlayer.replayEntity.CurrentHandState.Starter ? "ׯ��" : "3";
            lblWestStarter.Text = players[3] == ThisPlayer.replayEntity.CurrentHandState.Starter ? "ׯ��" : "4";
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
            DrawSidebarFull(g);

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

            if (trick.ShowedCards.Count == 1 && PlayerPosition[trick.Learder] == 1)
            {
                DrawDumpFailureMessage(trick);
            }

            ThisPlayer.CurrentTrickState = trick;
            string curPlayer = trick.Learder;
            for (int i = 0; i < trick.ShowedCards.Count; i++)
            {
                int position = PlayerPosition[curPlayer];
                if (trick.ShowedCards.Count == 4)
                {
                    foreach (int card in trick.ShowedCards[curPlayer])
                    {
                        ThisPlayer.replayEntity.CurrentHandState.PlayerHoldingCards[PositionPlayer[position]].RemoveCard(card);
                    }
                }

                ArrayList cardsList = new ArrayList(trick.ShowedCards[curPlayer]);
                if (position == 1)
                {
                    drawingFormHelper.DrawMySendedCardsAction(cardsList);
                    drawingFormHelper.DrawMyHandCards();
                }
                else if (position == 2)
                {
                    drawingFormHelper.DrawNextUserSendedCardsAction(cardsList);
                }
                else if (position == 3)
                {
                    drawingFormHelper.DrawFriendUserSendedCardsAction(cardsList);
                }
                else if (position == 4)
                {
                    drawingFormHelper.DrawPreviousUserSendedCardsAction(cardsList);
                }
                Refresh();
                curPlayer = ThisPlayer.CurrentGameState.GetNextPlayerAfterThePlayer(curPlayer).PlayerId;
            }

            if (trick.ShowedCards.Count == 1 && PlayerPosition[trick.Learder] != 1)
            {
                DrawDumpFailureMessage(trick);
            }

            drawAllOtherHandCards();

            if (!string.IsNullOrEmpty(trick.Winner))
            {
                if (
                    !ThisPlayer.CurrentGameState.ArePlayersInSameTeam(ThisPlayer.CurrentHandState.Starter,
                                                                trick.Winner))
                {
                    ThisPlayer.CurrentHandState.Score += trick.Points;
                    //�ռ��÷���
                    ThisPlayer.CurrentHandState.ScoreCards.AddRange(trick.ScoreCards);
                }
            }
            drawingFormHelper.DrawScoreImageAndCards();

            Refresh();

        }

        private void DrawDumpFailureMessage(CurrentTrickState trick)
        {
            this.drawingFormHelper.DrawMessages(new string[] { 
                    string.Format("��ҡ�{0}��", trick.Learder), 
                    string.Format("˦��{0}��ʧ��", trick.ShowedCards[trick.Learder].Count), 
                    string.Format("���֣�{0}", trick.ShowedCards[trick.Learder].Count * 10),
                    "",
                    "",
                    "",
                    ""
                });
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

                    // ˦��ʧ��
                    if (trick.ShowedCards.Count == 1) continue;

                    string curPlayer = trick.Learder;
                    for (int i = 0; i < trick.ShowedCards.Count; i++)
                    {
                        int position = PlayerPosition[curPlayer];
                        foreach (int card in trick.ShowedCards[curPlayer])
                        {
                            ThisPlayer.replayEntity.CurrentHandState.PlayerHoldingCards[PositionPlayer[position]].RemoveCard(card);
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
            else if (trick.ShowedCards.Count == 4)
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
                    if (!ThisPlayer.CurrentGameState.ArePlayersInSameTeam(ThisPlayer.CurrentHandState.Starter, trick.Winner))
                    {
                        ThisPlayer.CurrentHandState.Score -= trick.Points;
                        //�ռ��÷���
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

        private void btnShengjiweb_Click(object sender, EventArgs e)
        {
            Process.Start(shengjiwebUrl);
        }
    }
}