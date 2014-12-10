using LoLLauncher;
using LoLLauncher.RiotObjects.Platform.Catalog.Champion;
using LoLLauncher.RiotObjects.Platform.Clientfacade.Domain;
using LoLLauncher.RiotObjects.Platform.Game;
using LoLLauncher.RiotObjects.Platform.Game.Message;
using LoLLauncher.RiotObjects.Platform.Matchmaking;
using LoLLauncher.RiotObjects.Platform.Statistics;
using LoLLauncher.RiotObjects;
using LoLLauncher.RiotObjects.Leagues.Pojo;
using LoLLauncher.RiotObjects.Platform.Game.Practice;
using LoLLauncher.RiotObjects.Platform.Harassment;
using LoLLauncher.RiotObjects.Platform.Leagues.Client.Dto;
using LoLLauncher.RiotObjects.Platform.Login;
using LoLLauncher.RiotObjects.Platform.Reroll.Pojo;
using LoLLauncher.RiotObjects.Platform.Statistics.Team;
using LoLLauncher.RiotObjects.Platform.Summoner;
using LoLLauncher.RiotObjects.Platform.Summoner.Boost;
using LoLLauncher.RiotObjects.Platform.Summoner.Masterybook;
using LoLLauncher.RiotObjects.Platform.Summoner.Runes;
using LoLLauncher.RiotObjects.Platform.Summoner.Spellbook;
using LoLLauncher.RiotObjects.Platform.Game.Map;
using LoLLauncher.RiotObjects.Team;
using LoLLauncher.RiotObjects.Team.Dto;
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace RitoBot
{
    enum ShowWindowCommands
    {
        /// <summary>
        /// Hides the window and activates another window.
        /// </summary>
        Hide = 0,
        /// <summary>
        /// Activates and displays a window. If the window is minimized or
        /// maximized, the system restores it to its original size and position.
        /// An application should specify this flag when displaying the window
        /// for the first time.
        /// </summary>
        Normal = 1,
        /// <summary>
        /// Activates the window and displays it as a minimized window.
        /// </summary>
        ShowMinimized = 2,
        /// <summary>
        /// Maximizes the specified window.
        /// </summary>
        Maximize = 3, // is this the right value?
        /// <summary>
        /// Activates the window and displays it as a maximized window.
        /// </summary>      
        ShowMaximized = 3,
        /// <summary>
        /// Displays a window in its most recent size and position. This value
        /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except
        /// the window is not activated.
        /// </summary>
        ShowNoActivate = 4,
        /// <summary>
        /// Activates the window and displays it in its current size and position.
        /// </summary>
        Show = 5,
        /// <summary>
        /// Minimizes the specified window and activates the next top-level
        /// window in the Z order.
        /// </summary>
        Minimize = 6,
        /// <summary>
        /// Displays the window as a minimized window. This value is similar to
        /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the
        /// window is not activated.
        /// </summary>
        ShowMinNoActive = 7,
        /// <summary>
        /// Displays the window in its current size and position. This value is
        /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the
        /// window is not activated.
        /// </summary>
        ShowNA = 8,
        /// <summary>
        /// Activates and displays the window. If the window is minimized or
        /// maximized, the system restores it to its original size and position.
        /// An application should specify this flag when restoring a minimized window.
        /// </summary>
        Restore = 9,
        /// <summary>
        /// Sets the show state based on the SW_* value specified in the
        /// STARTUPINFO structure passed to the CreateProcess function by the
        /// program that started the application.
        /// </summary>
        ShowDefault = 10,
        /// <summary>
        ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread
        /// that owns the window is not responding. This flag should only be
        /// used when minimizing windows from a different thread.
        /// </summary>
        ForceMinimize = 11
    }

    public class UpdateChangedEventArgs : EventArgs
    {
        public string Update { get; set; }
    }

    public class LevelChangedEventArgs : EventArgs
    {
        public int Level { get; set; }
    }

    public class GameLaunchedEventArgs : EventArgs
    {
        public string UserName { get; set; }
    }

    public class ExperienceLaunchedEventArgs : EventArgs
    {
        public int Experience { get; set; }
    }

    public class VoliBot
    {
        private int[] XP_PER_LEVEL =
           new int[] { 0, 90, 188, 293, 406, 854, 1330,
               1834, 2366, 3976, 5076, 6226, 7426, 8676,
               9976, 11326, 12726, 14176, 15676, 17807,
               20007, 22276, 24614, 27020, 29495, 32039,
               34652, 37333, 40084 };

        public Process exeProcess;
        public GameDTO currentGame = new GameDTO();
        public ChampionDTO[] availableChampsArray;
        public LoginDataPacket loginPacket = new LoginDataPacket();
        public LoLConnection connection = new LoLConnection();
        public List<ChampionDTO> availableChamps = new List<ChampionDTO>();

        private double _sumLevel;
        public bool firstTimeInLobby = true;
        public bool firstTimeInQueuePop = true;
        public bool firstTimeInCustom = true;
        public bool HasLaunchedGame = false;

        public string Accountname;
        public string Password;
        public string ipath;
        public string regiona;

        public delegate void LevelChangedEventHandler(LevelChangedEventArgs e);

        public event LevelChangedEventHandler LevelChanged;

        public delegate void ExperienceLaunchedEventHandler(ExperienceLaunchedEventArgs e);

        public event ExperienceLaunchedEventHandler ExperienceLaunched;

        public double sumLevel
        {
            get
            {
                return _sumLevel;
            }
            set
            {
                if(_sumLevel != value)
                {
                    _sumLevel = value;
                    LevelChanged(new LevelChangedEventArgs() { Level = (int)_sumLevel });
                }
            }
        }
        public double archiveSumLevel { get; set; }
        public double rpBalance { get; set; }

        public string Status { get; set; }

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public static string GetWindowText(IntPtr hWnd)
        {
            int size = GetWindowTextLength(hWnd);
            if (size++ > 0)
            {
                var builder = new StringBuilder(size);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }

            return String.Empty;
        }

        public static IEnumerable<IntPtr> FindWindowsWithText(string titleText)
        {
            IntPtr found = IntPtr.Zero;
            List<IntPtr> windows = new List<IntPtr>();

            EnumWindows(delegate(IntPtr wnd, IntPtr param)
            {
                if (GetWindowText(wnd).Contains(titleText))
                {
                    windows.Add(wnd);
                }
                return true;
            },
                        IntPtr.Zero);

            return windows;
        }

        public VoliBot(string username, string password, string region, string path)
        {
            ipath = path; Accountname = username; Password = password;
            regiona = region;
            connection.OnConnect += new LoLConnection.OnConnectHandler(connection_OnConnect);
            connection.OnDisconnect += new LoLConnection.OnDisconnectHandler(connection_OnDisconnect);
            connection.OnError += new LoLConnection.OnErrorHandler(connection_OnError);
            connection.OnLogin += new LoLConnection.OnLoginHandler(connection_OnLogin);
            connection.OnLoginQueueUpdate += new LoLConnection.OnLoginQueueUpdateHandler(connection_OnLoginQueueUpdate);
            connection.OnMessageReceived += new LoLConnection.OnMessageReceivedHandler(connection_OnMessageReceived);
        }

        public void Start()
        {
            connection = new LoLConnection();

            connection.OnConnect += new LoLConnection.OnConnectHandler(connection_OnConnect);
            connection.OnDisconnect += new LoLConnection.OnDisconnectHandler(connection_OnDisconnect);
            connection.OnError += new LoLConnection.OnErrorHandler(connection_OnError);
            connection.OnLogin += new LoLConnection.OnLoginHandler(connection_OnLogin);
            connection.OnLoginQueueUpdate += new LoLConnection.OnLoginQueueUpdateHandler(connection_OnLoginQueueUpdate);
            connection.OnMessageReceived += new LoLConnection.OnMessageReceivedHandler(connection_OnMessageReceived);

            switch (regiona)
            {
                case "EUW":
                    connection.Connect(Accountname, Password, Region.EUW, "4.20.BlazeIt");
                    break;
                case "EUNE":
                    connection.Connect(Accountname, Password, Region.EUN, "4.20.BlazeIt");
                    break;
                case "BR":
                    connection.Connect(Accountname, Password, Region.BR, "4.20.BlazeIt");
                    break;
                case "KR":
                    connection.Connect(Accountname, Password, Region.KR, "4.20.BlazeIt");
                    break;
                case "OCE":
                    connection.Connect(Accountname, Password, Region.OCE, "4.20.BlazeIt");
                    break;
                case "NA":
                    connection.Connect(Accountname, Password, Region.NA, "4.20.BlazeIt");
                    break;
                case "TR":
                    connection.Connect(Accountname, Password, Region.TR, "4.20.BlazeIt");
                    break;
                case "TW":
                    connection.Connect(Accountname, Password, Region.TW, "4.20.BlazeIt");
                    break;
                case "RU":
                    connection.Connect(Accountname, Password, Region.RU, "4.20.BlazeIt");
                    break;
                case "LAN":
                    connection.Connect(Accountname, Password, Region.LAN, "4.20.BlazeIt");
                    break;
                case "LAS":
                    connection.Connect(Accountname, Password, Region.LAS, "4.20.BlazeIt");
                    break;
            }
        }

        public void Stop()
        {
            connection.Disconnect();    
        }
        public async void connection_OnMessageReceived(object sender, object message)
        {
            if (message is GameDTO)
            {
                GameDTO game = message as GameDTO;
                switch (game.GameState)
                {
                    case "CHAMP_SELECT":
                        firstTimeInCustom = true;
                        firstTimeInQueuePop = true;
                        if (firstTimeInLobby)
                        {
                            firstTimeInLobby = false;
                            updateStatus("In Champion Select", Accountname);
                            await connection.SetClientReceivedGameMessage(game.Id, "CHAMP_SELECT_CLIENT");
                            if (Core.championId != "")
                            {
                                await connection.SelectChampion(Enums.championToId(Core.championId));
                                await connection.ChampionSelectCompleted();
                            }
                            else
                            {
                                await connection.SelectChampion(availableChampsArray.First(champ => champ.Owned || champ.FreeToPlay).ChampionId);
                                await connection.ChampionSelectCompleted();
                            }
                            break;
                        }
                        else
                            break;
                    case "POST_CHAMP_SELECT":
                        HasLaunchedGame = false;
                        firstTimeInLobby = false;
                        updateStatus("(Post Champ Select)", Accountname);
                        break;
                    case "PRE_CHAMP_SELECT":
                        updateStatus("(Pre Champ Select)", Accountname);
                        break;
                    case "GAME_START_CLIENT":
                        updateStatus("Game client ran", Accountname);
                        break;
                    case "GameClientConnectedToServer":
                        updateStatus("Client connected to the server", Accountname);
                        break;
                    case "IN_QUEUE":
                        updateStatus("In Queue", Accountname);
                        break;
                    case "TERMINATED":
                        updateStatus("Re-entering queue", Accountname);
                        firstTimeInQueuePop = true;
                        break;
                    case "JOINING_CHAMP_SELECT":
                        if (firstTimeInQueuePop)
                        {
                            updateStatus("Queue popped", Accountname);
                            if (game.StatusOfParticipants.Contains("1"))
                            {
                                updateStatus("Accepted Queue", Accountname);
                                firstTimeInQueuePop = false;
                                firstTimeInLobby = true;
                                await connection.AcceptPoppedGame(true);
                            }
                        }
                        break;
                }
            }
            else if (message is PlayerCredentialsDto)
            {
                PlayerCredentialsDto dto = message as PlayerCredentialsDto;
                if (!HasLaunchedGame)
                {
                    HasLaunchedGame = true;
                    new Thread((ThreadStart)(() =>
                    {
                        LaunchGame(dto);
                        Thread.Sleep(3000);
                    })).Start();
                }
            }
            else if (!(message is GameNotification) && !(message is SearchingForMatchNotification))
            {
                if (message is EndOfGameStats)
                {
                    double experienceEarned = ((EndOfGameStats)message).ExperienceEarned;
                    double experienceTotal = ((EndOfGameStats)message).ExperienceTotal;
                    MatchMakerParams matchParams = new MatchMakerParams();
                    //Set BotParams
                    QueueTypes selectQueueType = QueueTypes.MEDIUM_BOT;

                    if (sumLevel < 5)
                    {
                        matchParams.BotDifficulty = "MEDIUM";
                    }
                    else
                    {
                        selectQueueType = QueueTypes.DOMINION;
                    }
                    matchParams.QueueIds = new Int32[1] { (int)selectQueueType };
                    SearchingForMatchNotification m = await connection.AttachToQueue(matchParams);
                    if (m.PlayerJoinFailures == null)
                    {
                        updateStatus("In Queue: " + selectQueueType.ToString(), Accountname);
                    }
                    else
                    {
                        updateStatus("Couldn't enter Queue! Please contact us @ volibot.com", Accountname);
                    }
                }
                else
                {
                    if (message.ToString().Contains("EndOfGameStats"))
                    {
                        EndOfGameStats eog = new EndOfGameStats();
                        connection_OnMessageReceived(sender, eog);
                        exeProcess.Kill();
                        loginPacket = await this.connection.GetLoginDataPacketForUser();
                        archiveSumLevel = sumLevel;
                        sumLevel = loginPacket.AllSummonerData.SummonerLevel.Level;
                        if (sumLevel != archiveSumLevel)
                        {
                            levelUp();
                        }
                    }
                }
            }
        }



        public delegate void GameLaunchedEventHandler(GameLaunchedEventArgs e);

        public event GameLaunchedEventHandler GameLaunched;

        public bool IsGameLaunchedHandled()
        {
            return GameLaunched != null;
        }

        public void LaunchGame(PlayerCredentialsDto CurrentGame)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.WorkingDirectory = FindLoLExe();
            startInfo.FileName = "League of Legends.exe";
            startInfo.Arguments = "\"8394\" \"LoLLauncher.exe\" \"\" \"" + CurrentGame.ServerIp + " " +
                CurrentGame.ServerPort + " " + CurrentGame.EncryptionKey + " " + CurrentGame.SummonerId + "\"";
            updateStatus("Playing Game", Accountname);
            new Thread(() =>
            {
                exeProcess = Process.Start(startInfo);
                while (exeProcess.MainWindowHandle == IntPtr.Zero) { }
                IntPtr consolehWnd = Process.GetCurrentProcess().MainWindowHandle;
                RECT consoleRect = new RECT();
                GetWindowRect(consolehWnd, ref consoleRect);
                RECT leagueRect = new RECT();
                List<IntPtr> windows = FindWindowsWithText("League of Legends (TM) Client").ToList();

                if (GameLaunched != null)
                {
                    GameLaunched(new GameLaunchedEventArgs() { UserName = Accountname });
                }

                IntPtr leaguehWnd = IntPtr.Zero;
                uint processId;
                foreach (IntPtr hWnd in windows)
                {
                    GetWindowThreadProcessId(hWnd, out processId);
                    if((int)processId == exeProcess.Id)
                    {
                        leaguehWnd = hWnd;
                        GetWindowRect(hWnd, ref leagueRect);
                    }
                }

                Thread.Sleep(1000);
            }).Start();
        }
        private void connection_OnLoginQueueUpdate(object sender, int positionInLine)
        {
            if (positionInLine <= 0)
                return;
            updateStatus("Position to login: " + (object)positionInLine, Accountname);
        }
        private void connection_OnLogin(object sender, string username, string ipAddress)
        {
            new Thread((ThreadStart)(async () =>
            {
                updateStatus("Connecting...", Accountname);
                loginPacket = await connection.GetLoginDataPacketForUser();
                await connection.Subscribe("bc", loginPacket.AllSummonerData.Summoner.AcctId);
                await connection.Subscribe("cn", loginPacket.AllSummonerData.Summoner.AcctId);
                await connection.Subscribe("gn", loginPacket.AllSummonerData.Summoner.AcctId);
                if (loginPacket.AllSummonerData == null)
                {
                    Random rnd = new Random();
                    String summonerName = Accountname;
                    if (summonerName.Length > 16)
                        summonerName = summonerName.Substring(0, 12) + new Random().Next(1000, 9999).ToString();
                    await connection.CreateDefaultSummoner(summonerName);
                    updateStatus("Created Summoner: " + summonerName, Accountname);
                }
                sumLevel = loginPacket.AllSummonerData.SummonerLevel.Level;
                double xp = loginPacket.AllSummonerData.SummonerLevelAndPoints.ExpPoints;

                double totalXP = XP_PER_LEVEL[(int)sumLevel - 1] + xp;

                string sumName = loginPacket.AllSummonerData.Summoner.Name;
                double sumId = loginPacket.AllSummonerData.Summoner.SumId;

                LevelChanged(new LevelChangedEventArgs() { Level = (int)sumLevel });
                ExperienceLaunched(new ExperienceLaunchedEventArgs() { Experience = (int)totalXP });
                rpBalance = loginPacket.RpBalance;
                if (sumLevel > Core.maxLevel || sumLevel == Core.maxLevel)
                {
                    connection.Disconnect();
                    updateStatus("Summoner: " + sumName + " is already max level.", Accountname);
                    updateStatus("Log into new account.", Accountname);
                    Core.lognNewAccount();
                    return;
                }
                if (rpBalance == 400.0 && Core.buyBoost)
                {
                    updateStatus("Buying XP Boost", Accountname);
                    try
                    {
                        Task t = new Task(buyBoost);
                        t.Start();
                    }
                    catch (Exception exception)
                    {
                        updateStatus("Couldn't buy RP Boost.\n" + exception, Accountname);
                    }
                }
                if (loginPacket.AllSummonerData.Summoner.ProfileIconId == -1 || loginPacket.AllSummonerData.Summoner.ProfileIconId == 1)
                {
                    double[] ids = new double[Convert.ToInt32(sumId)];
                    string icons = await connection.GetSummonerIcons(ids);
                    List<int> availableIcons = new List<int> { };
                    var random = new Random();
                    for (int i = 0; i < 29; i++)
                    {
                        availableIcons.Add(i);
                    }
                    foreach (var id in icons)
                    {
                        availableIcons.Add(Convert.ToInt32(id));
                    }
                    int index = random.Next(availableIcons.Count);
                    int randomIcon = availableIcons[index];
                    await connection.UpdateProfileIconId(randomIcon);
                }
                updateStatus("Logged in as " + loginPacket.AllSummonerData.Summoner.Name + " @ level " + loginPacket.AllSummonerData.SummonerLevel.Level, Accountname);
                availableChampsArray = await connection.GetAvailableChampions();
                PlayerDTO player = await connection.CreatePlayer();
                if (loginPacket.ReconnectInfo != null && loginPacket.ReconnectInfo.Game != null)
                {
                    connection_OnMessageReceived(sender, (object)loginPacket.ReconnectInfo.PlayerCredentials);
                }
                else
                    connection_OnMessageReceived(sender, (object)new EndOfGameStats());
            })).Start();
        }
        private void connection_OnError(object sender, LoLLauncher.Error error)
        {
            if (error.Message.Contains("is not owned by summoner"))
            {
                return;
            }
            else if (error.Message.Contains("Your summoner level is too low to select the spell"))
            {
                var random = new Random();
                var spellList = new List<int> { 13, 6, 7, 10, 1, 11, 21, 12, 3, 14, 2, 4 };

                int index = random.Next(spellList.Count);
                int index2 = random.Next(spellList.Count);

                int randomSpell1 = spellList[index];
                int randomSpell2 = spellList[index2];

                if (randomSpell1 == randomSpell2)
                {
                    int index3 = random.Next(spellList.Count);
                    randomSpell2 = spellList[index3];
                }

                int Spell1 = Convert.ToInt32(randomSpell1);
                int Spell2 = Convert.ToInt32(randomSpell2);
                return;
            }
            updateStatus("error received:\n" + error.Message, Accountname);
        }
        private void connection_OnDisconnect(object sender, EventArgs e)
        {
            Core.connectedAccs -= 1;
            //Console.Title = " Current Connected: " + Core.connectedAccs;
            updateStatus("Disconnected", Accountname);
        }
        private void connection_OnConnect(object sender, EventArgs e)
        {
            Core.connectedAccs += 1;
            //Console.Title = " Current Connected: " + Core.connectedAccs;
        }

        public delegate void StatusChangedEventHandler(UpdateChangedEventArgs e);

        public event StatusChangedEventHandler UpdateChanged;

        private void updateStatus(string status, string accname)
        {
            Status = status;
            UpdateChanged(new UpdateChangedEventArgs() { Update = Status });
            //Console.ForegroundColor = ConsoleColor.Cyan;
            //Console.Write(string.Concat(new object[3]
            //  {
            //    (object) "[",
            //    (object) DateTime.Now,
            //    (object) "] "
            //  }));
            //Console.ForegroundColor = ConsoleColor.White;
            //Console.Write(string.Concat(new object[3]
            //  {
            //    (object) "[",
            //    (object) accname,
            //    (object) "] "
            //  }));
            //Console.Write(status + "\n");

        }
        private void levelUp()
        {
            updateStatus("Level Up: " + sumLevel, Accountname);
            rpBalance = loginPacket.RpBalance;
            if (sumLevel >= Core.maxLevel)
            {
                connection.Disconnect();
                if (!connection.IsConnected())
                {
                    Core.lognNewAccount();
                }
            }
            if (rpBalance == 400.0 && Core.buyBoost)
            {
                updateStatus("Buying XP Boost", Accountname);
                try
                {
                    Task t = new Task(buyBoost);
                    t.Start();
                }
                catch (Exception exception)
                {
                    updateStatus("Couldn't buy RP Boost.\n" + exception, Accountname);
                }
            }
        }
        private async void buyBoost()
        {
            try
            {
                string url = await connection.GetStoreUrl();
                HttpClient httpClient = new HttpClient();
                Console.WriteLine(url);
                await httpClient.GetStringAsync(url);

                string storeURL = "https://store." + "NA" + "1.lol.riotgames.com/store/tabs/view/boosts/1";
                await httpClient.GetStringAsync(storeURL);

                string purchaseURL = "https://store." + "NA" + "1.lol.riotgames.com/store/purchase/item";

                List<KeyValuePair<string, string>> storeItemList = new List<KeyValuePair<string, string>>();
                storeItemList.Add(new KeyValuePair<string, string>("item_id", "boosts_2"));
                storeItemList.Add(new KeyValuePair<string, string>("currency_type", "rp"));
                storeItemList.Add(new KeyValuePair<string, string>("quantity", "1"));
                storeItemList.Add(new KeyValuePair<string, string>("rp", "260"));
                storeItemList.Add(new KeyValuePair<string, string>("ip", "null"));
                storeItemList.Add(new KeyValuePair<string, string>("duration_type", "PURCHASED"));
                storeItemList.Add(new KeyValuePair<string, string>("duration", "3"));
                HttpContent httpContent = new FormUrlEncodedContent(storeItemList);
                await httpClient.PostAsync(purchaseURL, httpContent);

                updateStatus("Bought 'XP Boost: 3 Days'!", Accountname);
                httpClient.Dispose();
            }
            catch (Exception e)
            {   
                Console.WriteLine(e);
            }
        }
        private String FindLoLExe()
        {
            String installPath = ipath;
            if (installPath.Contains("notfound"))
                return installPath;
            installPath += @"RADS\solutions\lol_game_client_sln\releases\";
            installPath = Directory.EnumerateDirectories(installPath).OrderBy(f => new DirectoryInfo(f).CreationTime).Last();
            installPath += @"\deploy\";
            return installPath;
        }

        public Thread InjectThread { get; set; }

        public bool IsEventHandlerRegistered(Delegate prospectiveHandler)
        {
            if (this.GameLaunched != null)
            {
                foreach (Delegate existingHandler in this.GameLaunched.GetInvocationList())
                {
                    if (existingHandler == prospectiveHandler)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}