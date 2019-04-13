using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Web;
using System.Net;

namespace ConcentMusic
{
    class TelegramBot
    {
        private TelegramBotClient telegramBotClient;
        private MusicDownloader musicDownloader;
        private MusicPlayer musicPlayer;
        private Dictionary<string, bool> allowedUsers = new Dictionary<string, bool>();
        public static Dictionary<string, bool> AlterVote = new Dictionary<string, bool>();
        public static Dictionary<string, bool> ClearListVote = new Dictionary<string, bool>();
        public static int? ClearListVotersCount;
        public static int? AlterVotersCount;
        public static List<TrackInfo> TracksList;
        public static int trackId;


        public TelegramBot()
        {
            Logger.Init();
            Logger.Info("Starting server");

            allowedUsers.Add(AppSettings.DefaultUser.ToLower(), true);
            restoreUsers();
            trackId = 0;
            TracksList = new List<TrackInfo>();
            musicPlayer = new MusicPlayer();
            musicPlayer.StartPlaying();
            musicDownloader = new MusicDownloader();
            telegramBotClient = new TelegramBotClient(AppSettings.TelegramApiKey);

            telegramBotClient.OnMessage += messageHandler;
            telegramBotClient.OnMessage += addToQueue;

            Logger.Info("Start receiving");

            telegramBotClient.StartReceiving();
            while (Console.ReadKey().KeyChar != 'q') { }
            telegramBotClient.StopReceiving();
            Environment.Exit(0);
        }

        ~TelegramBot()
        {
            try
            {
                Directory.Delete(AppSettings.MusicDirectory, true);
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.Message}: Can't delete music directory.");
            }
        }

        private void restoreUsers()
        {
            if (!Directory.Exists(AppSettings.AllowedUsersDirectory))
                Directory.CreateDirectory(AppSettings.AllowedUsersDirectory);

            try
            {
                using (StreamReader sr = new StreamReader(AppSettings.AllowedUsersDirectory + "users.txt"))
                {
                    String line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line != AppSettings.DefaultUser.ToLower())
                            allowedUsers.Add(line, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.Message}: Key exists");
            }
        }

        private void writeUsersToFile()
        {
            StringBuilder usersList = new StringBuilder();
            using (StreamWriter sr = new StreamWriter(AppSettings.AllowedUsersDirectory + "users.txt"))
            {
                foreach (string user in allowedUsers.Keys)
                {
                    usersList.AppendLine(user);
                }
                sr.Write(usersList);
            }
        }

        private void messageHandler(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            Logger.Info($"Got message \"{ev.Message.Text}\"");

            if (!allowedUsers.ContainsKey(ev.Message.Chat.Username.ToLower()))
            {
                telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.NoPermission);
                return;
            }

            switch (ev.Message.Text)
            {
                case "/start":
                    telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.Start);
                    break;
                case "/skip":
                    alter(sender, ev);
                    break;
                case "/volumeup":
                    volumeUp(sender, ev);
                    break;
                case "/volumedown":
                    volumeDown(sender, ev);
                    break;
                case "/pause":
                    pauseTrack(sender, ev);
                    break;
                case "/play":
                    resumeTrack(sender, ev);
                    break;
                case "/getlist":
                    getTitles(sender, ev);
                    break;
                case "/getusers":
                    getUsers(sender, ev);
                    break;
                case "/clearlist":
                    clearList(sender, ev);
                    break;
            }

            if (ev.Message.Text.StartsWith("/adduser"))
            {
                addUser(sender, ev);
            }

            if (ev.Message.Text.StartsWith("/removeuser"))
            {
                removeUser(sender, ev);
            }
        }

        private void addToQueue(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            long chatId = ev.Message.Chat.Id;

            if (!allowedUsers.ContainsKey(ev.Message.Chat.Username.ToLower()))
                return;

            if (ev.Message.Text.StartsWith("/") || ev.Message.Text.StartsWith("@"))
                return;

            if (!checkLink(ev.Message.Text) || ev.Message.Text == null || ev.Message.Text.Contains('\n') == true)
            {
                telegramBotClient.SendTextMessageAsync(chatId, ResponseMessages.InvalidLink);
                return;
            }

            Logger.Info("Valid youtube link detected.");
            telegramBotClient.SendTextMessageAsync(chatId, ResponseMessages.QueuedSuccessful);
            TracksList.Add(new TrackInfo(ev.Message.Text, ev.Message.Chat.Username));
            if (TracksList.Count != 0)
            {
                musicDownloader.DownloadAudio();
            }
        }

        private bool checkLink(string link)
        {
            Regex YoutubeVideoRegex = new Regex(@"^(http(s)?:\/\/)?((w){3}.)?youtu(be|.be)?(\.com)?\/.+", RegexOptions.IgnoreCase);
            return YoutubeVideoRegex.Match(link).Success;
        }

        private void addUser(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            long chatId = ev.Message.Chat.Id;

            if (ev.Message.Text == "/adduser")
                telegramBotClient.SendTextMessageAsync(chatId, ResponseMessages.AddUserExample);

            string user = ev.Message.Text.Split(' ')[1];

            if (user.StartsWith("@") && !allowedUsers.ContainsKey(user.Substring(1).ToLower()))
            {
                allowedUsers.Add(user.Substring(1).ToLower(), true);
                telegramBotClient.SendTextMessageAsync(chatId, ResponseMessages.UserAddedSuccessful);
                writeUsersToFile();
            }
            else if (allowedUsers.ContainsKey(user.Substring(1).ToLower()))
                telegramBotClient.SendTextMessageAsync(chatId, ResponseMessages.UserAlreadyExists);
            else
                telegramBotClient.SendTextMessageAsync(chatId, ResponseMessages.AddUserExample);
        }

        private void removeUser(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            long chatId = ev.Message.Chat.Id;
            string user = ev.Message.Text.Split(' ')[1];

            if (!allowedUsers.ContainsKey(user.Substring(1).ToLower()))
            {
                telegramBotClient.SendTextMessageAsync(chatId, ResponseMessages.UserNotExists);
                return;
            }

            if (ev.Message.Chat.Username.ToLower() == user.Substring(1).ToLower())
            {
                telegramBotClient.SendTextMessageAsync(chatId, ResponseMessages.CannotRemoveYourself);
                return;
            }

            if (user.StartsWith("@"))
            {
                allowedUsers.Remove(user.Substring(1).ToLower());
                telegramBotClient.SendTextMessageAsync(chatId, ResponseMessages.UserRemovedSuccessful);
                writeUsersToFile();
            }
            else
            {
                telegramBotClient.SendTextMessageAsync(chatId, ResponseMessages.InvalidUsername);
            }
        }

        private void alter(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            long chatId = ev.Message.Chat.Id;

            if (AlterVotersCount == null)
            {
                telegramBotClient.SendTextMessageAsync(chatId, ResponseMessages.TrackNotStarted);
                return;
            }

            if (AlterVote.ContainsKey(ev.Message.Chat.Username.ToLower()))
            {
                telegramBotClient.SendTextMessageAsync(chatId, ResponseMessages.AlreadyVoted);
                return;
            }

            int members = allowedUsers.Count;
            int halfMembers = members / 2;

            AlterVotersCount++;
            AlterVote.Add(ev.Message.Chat.Username.ToLower(), true);

            if (AlterVotersCount > halfMembers)
            {
                musicPlayer.Skip();
                telegramBotClient.SendTextMessageAsync(chatId, ResponseMessages.TrackSkiped);
            }
            else
            {
                telegramBotClient.SendTextMessageAsync(chatId, $"{halfMembers + 1 - AlterVotersCount} {ResponseMessages.VotesNotEnough}");
            }
        }

        private void volumeUp(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = "amixer";
            psi.Arguments = "set 'Master' 10%+";
            Process amixerProcess = Process.Start(psi);
            amixerProcess.WaitForExit();
            amixerProcess.Close();
        }

        private void volumeDown(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = "amixer";
            psi.Arguments = "set 'Master' 10%-";
            Process amixerProcess = Process.Start(psi);
            amixerProcess.WaitForExit();
            amixerProcess.Close();
        }

        private void pauseTrack(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            if (musicPlayer.IsPlaying() == null)
            {
                telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackNotStarted);
                return;
            }

            if (musicPlayer.IsPlaying() == false)
            {
                telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackAlreadyPaused);
                return;
            }

            musicPlayer.PauseTrack();
            telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackPaused);
        }

        private void resumeTrack(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            if (musicPlayer.IsPlaying() == null)
            {
                telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackNotStarted);
                return;
            }

            if (musicPlayer.IsPlaying() == true)
            {
                telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackAlreadyPlaying);
                return;
            }

            musicPlayer.PauseTrack();
            telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackStarted);
            musicPlayer.ResumeTrack();
        }

        private void getUsers(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            StringBuilder usersBuilder = new StringBuilder();

            foreach (string user in allowedUsers.Keys)
                usersBuilder.AppendLine("@" + user);
            telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, usersBuilder.ToString());
        }

        private void clearList(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            if (!TracksList.Any())
            {
                telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackListIsEmpty);
                return;
            }

            if (ClearListVote.ContainsKey(ev.Message.Chat.Username.ToLower()))
            {
                telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.AlreadyVoted);
                return;
            }

            int members = allowedUsers.Count;
            int halfMembers = members / 2;

            ClearListVotersCount++;
            ClearListVote.Add(ev.Message.Chat.Username.ToLower(), true);

            if (ClearListVotersCount > halfMembers)
            {
                Directory.Delete(AppSettings.MusicDirectory, true);
                TracksList.Clear();
                musicDownloader.createMusicDirectory();
                ClearListVotersCount = 0;
                ClearListVote.Clear();
                telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackListCleared);
            }
            else
            {
                telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, $"{halfMembers + 1 - ClearListVotersCount} {ResponseMessages.ClearVotesNotEnough}");
            }
        }

        private void getTitles(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            StringBuilder titlesBuilder = new StringBuilder();
            int trackNumber = 1;

            if (!TracksList.Any() || TracksList.Count() == 0)
            {
                telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackListIsEmpty);
                return;
            }

            foreach (TrackInfo track in TracksList)
            {
                string title = getTitle(track.url);

                if (track.trackState == TrackState.Playing)
                    titlesBuilder.Append("->");

                titlesBuilder.AppendLine(trackNumber++ + ") " + title + $" | User @{track.user}");
            }
            telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, titlesBuilder.ToString());
        }

        private string getTitle(string url)
        {
            string api = $"http://youtube.com/get_video_info?video_id={getArgs(url, "v", '?')}";
            string title = getArgs(new WebClient().DownloadString(api), "title", '&');

            return title;
        }

        private string getArgs(string args, string key, char query)
        {
            var iqs = args.IndexOf(query);
            return iqs == -1
                ? string.Empty
                : HttpUtility.ParseQueryString(iqs < args.Length - 1
                    ? args.Substring(iqs + 1) : string.Empty)[key];
        }
    }
}