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
    public class TelegramBot
    {
        private TelegramBotClient _telegramBotClient;
        private MusicDownloader _musicDownloader;
        private MusicPlayer _musicPlayer;
        private Dictionary<string, bool> _allowedUsers = new Dictionary<string, bool>();
        public static Dictionary<string, bool> _alterVote = new Dictionary<string, bool>();
        public static Dictionary<string, bool> _clearListVote = new Dictionary<string, bool>();
        public static int? _clearListVotersCount;
        public static int? _alterVotersCount;
        public static List<TrackInfo> t_racksList;
        public static int _trackId;


        public TelegramBot()
        {
            Logger.Init();
            Logger.Info("Starting server");

            _allowedUsers.Add(AppSettings.DefaultUser.ToLower(), true);
            RestoreUsers();
            _trackId = 0;
            t_racksList = new List<TrackInfo>();
            _musicPlayer = new MusicPlayer();
            _musicPlayer.StartPlayingVlc();
            _musicDownloader = new MusicDownloader();
            _telegramBotClient = new TelegramBotClient(AppSettings.TelegramApiKey);

            _telegramBotClient.OnMessage += MessageHandler;
            _telegramBotClient.OnMessage += AddToQueue;

            Logger.Info("Start receiving");

            _telegramBotClient.StartReceiving();
            while (Console.ReadKey().KeyChar != 'q') { }
            _telegramBotClient.StopReceiving();
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

        private void RestoreUsers()
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
                            _allowedUsers.Add(line, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.Message}: Key exists");
            }
        }

        private void WriteUsersToFile()
        {
            StringBuilder usersList = new StringBuilder();
            using (StreamWriter sr = new StreamWriter(AppSettings.AllowedUsersDirectory + "users.txt"))
            {
                foreach (string user in _allowedUsers.Keys)
                {
                    usersList.AppendLine(user);
                }
                sr.Write(usersList);
            }
        }

        private void MessageHandler(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            Logger.Info($"Got message \"{ev.Message.Text}\"");

            if (string.IsNullOrEmpty(ev.Message.Chat.Username))
            {
                _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.SpecifyUsername).Wait();
                return;
            }

            if (!_allowedUsers.ContainsKey(ev.Message.Chat.Username.ToLower()))
            {
                _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.NoPermission);
                return;
            }

            switch (ev.Message.Text)
            {
                case "/start":
                    _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.Start);
                    break;
                case "/skip":
                    Alter(sender, ev);
                    break;
                case "/volumeup":
                    VolumeUp(sender, ev);
                    break;
                case "/volumedown":
                    VolumeDown(sender, ev);
                    break;
                case "/pause":
                    PauseTrack(sender, ev);
                    break;
                case "/play":
                    ResumeTrack(sender, ev);
                    break;
                case "/getlist":
                    GetTitles(sender, ev);
                    break;
                case "/getusers":
                    GetUsers(sender, ev);
                    break;
                case "/clearlist":
                    ClearList(sender, ev);
                    break;
            }

            if (ev.Message.Text.StartsWith("/adduser"))
            {
                AddUser(sender, ev);
            }

            if (ev.Message.Text.StartsWith("/removeuser"))
            {
                RemoveUser(sender, ev);
            }
        }

        private void AddToQueue(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            long _chatId = ev.Message.Chat.Id;

            if (string.IsNullOrEmpty(ev.Message.Chat.Username))
                return;

            if (!_allowedUsers.ContainsKey(ev.Message.Chat.Username.ToLower()))
                return;

            if (ev.Message.Text.StartsWith("/") || ev.Message.Text.StartsWith("@"))
                return;

            if (!CheckLink(ev.Message.Text) || ev.Message.Text == null || ev.Message.Text.Contains('\n') == true)
            {
                _telegramBotClient.SendTextMessageAsync(_chatId, ResponseMessages.InvalidLink);
                return;
            }

            Logger.Info("Valid youtube link detected.");
            _telegramBotClient.SendTextMessageAsync(_chatId, ResponseMessages.QueuedSuccessful);
            t_racksList.Add(new TrackInfo(ev.Message.Text, ev.Message.Chat.Username));
            if (t_racksList.Count != 0)
            {
                _musicDownloader.DownloadAudio();
            }
        }

        private bool CheckLink(string link)
        {
            Regex YoutubeVideoRegex = new Regex(@"^(http(s)?:\/\/)?((w){3}.)?youtu(be|.be)?(\.com)?\/.+", RegexOptions.IgnoreCase);
            return YoutubeVideoRegex.Match(link).Success;
        }

        private void AddUser(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            var _chatId = ev.Message.Chat.Id;
            var user = ev.Message.Text.Split(' ').ElementAtOrDefault(1);

            if (string.IsNullOrEmpty(user))
            {
                _telegramBotClient.SendTextMessageAsync(_chatId, ResponseMessages.AddUserExample).Wait();
                return;
            }

            if (user.StartsWith("@") && !_allowedUsers.ContainsKey(user.Substring(1).ToLower()))
            {
                _allowedUsers.Add(user.Substring(1).ToLower(), true);
                _telegramBotClient.SendTextMessageAsync(_chatId, ResponseMessages.UserAddedSuccessful);
                WriteUsersToFile();
            }
            else if (_allowedUsers.ContainsKey(user.Substring(1).ToLower()))
                _telegramBotClient.SendTextMessageAsync(_chatId, ResponseMessages.UserAlreadyExists);
            else
                _telegramBotClient.SendTextMessageAsync(_chatId, ResponseMessages.AddUserExample);
        }

        private void RemoveUser(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            long _chatId = ev.Message.Chat.Id;
            string _user = ev.Message.Text.Split(' ')[1];

            if (!_allowedUsers.ContainsKey(_user.Substring(1).ToLower()))
            {
                _telegramBotClient.SendTextMessageAsync(_chatId, ResponseMessages.UserNotExists);
                return;
            }

            if (ev.Message.Chat.Username.ToLower() == _user.Substring(1).ToLower())
            {
                _telegramBotClient.SendTextMessageAsync(_chatId, ResponseMessages.CannotRemoveYourself);
                return;
            }

            if (_user.StartsWith("@"))
            {
                _allowedUsers.Remove(_user.Substring(1).ToLower());
                _telegramBotClient.SendTextMessageAsync(_chatId, ResponseMessages.UserRemovedSuccessful);
                WriteUsersToFile();
            }
            else
            {
                _telegramBotClient.SendTextMessageAsync(_chatId, ResponseMessages.InvalidUsername);
            }
        }

        private void Alter(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            long _chatId = ev.Message.Chat.Id;

            if (_alterVotersCount == null)
            {
                _telegramBotClient.SendTextMessageAsync(_chatId, ResponseMessages.TrackNotStarted);
                return;
            }

            if (_alterVote.ContainsKey(ev.Message.Chat.Username.ToLower()))
            {
                _telegramBotClient.SendTextMessageAsync(_chatId, ResponseMessages.AlreadyVoted);
                return;
            }

            int _members = _allowedUsers.Count;
            int _halfMembers = _members / 2;

            _alterVotersCount++;
            _alterVote.Add(ev.Message.Chat.Username.ToLower(), true);

            if (_alterVotersCount > _halfMembers)
            {
                _musicPlayer.Skip();
                _telegramBotClient.SendTextMessageAsync(_chatId, ResponseMessages.TrackSkiped);
            }
            else
            {
                _telegramBotClient.SendTextMessageAsync(_chatId, $"{_halfMembers + 1 - _alterVotersCount} {ResponseMessages.VotesNotEnough}");
            }
        }

        public void VolumeUp(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = "amixer";
            psi.Arguments = "set 'Master' 10%+";
            Process amixerProcess = Process.Start(psi);
            amixerProcess.WaitForExit();
            amixerProcess.Close();
        }

        public void VolumeDown(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = "amixer";
            psi.Arguments = "set 'Master' 10%-";
            Process amixerProcess = Process.Start(psi);
            amixerProcess.WaitForExit();
            amixerProcess.Close();
        }

        private void PauseTrack(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            if (_musicPlayer.IsPlaying() == null)
            {
                _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackNotStarted);
                return;
            }

            if (_musicPlayer.IsPlaying() == false)
            {
                _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackAlreadyPaused);
                return;
            }

            _musicPlayer.PauseTrack();
            _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackPaused);
        }

        private void ResumeTrack(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            if (_musicPlayer.IsPlaying() == null)
            {
                _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackNotStarted);
                return;
            }

            if (_musicPlayer.IsPlaying() == true)
            {
                _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackAlreadyPlaying);
                return;
            }

            _musicPlayer.PauseTrack();
            _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackStarted);
            _musicPlayer.ResumeTrack();
        }

        private void GetUsers(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            StringBuilder usersBuilder = new StringBuilder();

            foreach (string user in _allowedUsers.Keys)
                usersBuilder.AppendLine("@" + user);
            _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, usersBuilder.ToString());
        }

        private void ClearList(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            if (!t_racksList.Any())
            {
                _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackListIsEmpty);
                return;
            }

            if (_clearListVote.ContainsKey(ev.Message.Chat.Username.ToLower()))
            {
                _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.AlreadyVoted);
                return;
            }

            int _members = _allowedUsers.Count;
            int _halfMembers = _members / 2;

            _clearListVotersCount++;
            _clearListVote.Add(ev.Message.Chat.Username.ToLower(), true);

            if (_clearListVotersCount > _halfMembers)
            {
                Directory.Delete(AppSettings.MusicDirectory, true);
                t_racksList.Clear();
                _musicDownloader.CreateMusicDirectory();
                _clearListVotersCount = 0;
                _clearListVote.Clear();
                _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackListCleared);
            }
            else
            {
                _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, $"{_halfMembers + 1 - _clearListVotersCount} {ResponseMessages.ClearVotesNotEnough}");
            }
        }

        public void GetTitles(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            StringBuilder titlesBuilder = new StringBuilder();
            int _trackNumber = 1;

            if (!t_racksList.Any() || t_racksList.Count() == 0)
            {
                _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackListIsEmpty);
                return;
            }

            foreach (TrackInfo track in t_racksList)
            {
                string _title = GetTitle(track._url);

                if (track._trackState == TrackState.Playing)
                    titlesBuilder.Append("->");

                titlesBuilder.AppendLine(_trackNumber++ + ") " + _title + $" | User @{track._user}");
            }
            _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, titlesBuilder.ToString());
        }

        private string GetTitle(string url)
        {
            string _api = $"http://youtube.com/get_video_info?video_id={GetArgs(url, "v", '?')}";
            string _title = GetArgs(new WebClient().DownloadString(_api), "title", '&');

            return _title;
        }

        private string GetArgs(string args, string key, char query)
        {
            var _iqs = args.IndexOf(query);
            return _iqs == -1
                ? string.Empty
                : HttpUtility.ParseQueryString(_iqs < args.Length - 1
                    ? args.Substring(_iqs + 1) : string.Empty)[key];
        }
    }
}