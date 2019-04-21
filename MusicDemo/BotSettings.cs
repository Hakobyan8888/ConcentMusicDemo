using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using System.IO;
using Telegram.Bot.Args;

namespace MusicDemo
{
    internal partial class BotSettings
    {
        private TelegramBotClient _telegramBotClient;
        private MusicDownloader _musicDownloader;
        private MusicPlayer _musicPlayer;
        private Dictionary<string, bool> _allowedUsers = new Dictionary<string, bool>();
        public static Dictionary<string, bool> _alterVote = new Dictionary<string, bool>();
        public static Dictionary<string, bool> _clearListVote = new Dictionary<string, bool>();
        public static int? _alterVotersCount;
        public static List<TrackInfo> tracksList;
        public static int TrackId { get; set; }

        public BotSettings()
        {
            Logger.Init();
            Logger.Information("Starting server");

            _allowedUsers.Add(ApplicationSettings.Admin.ToLower(), true);
            WriteAdmin();
            TrackId = 0;
            tracksList = new List<TrackInfo>();
            _musicPlayer = new MusicPlayer();
            _musicPlayer.StartPlayingVlc();
            _musicDownloader = new MusicDownloader();
            _telegramBotClient = new TelegramBotClient(ApplicationSettings.TelegramAPIKey);

            _telegramBotClient.OnMessage += MessageHandler;
            _telegramBotClient.OnMessage += AddToQueue;

            Logger.Information("Start receiving");

            _telegramBotClient.StartReceiving();
        }

        ~BotSettings()
        {
            try
            {
                Directory.Delete(ApplicationSettings.MusicDirectory, true);
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.Message}: Can't delete music directory.");
            }
        }

        //Done 
        private void WriteAdmin()
        {
            if (!Directory.Exists(ApplicationSettings.AllowedUsersDirectory))
            {
                Directory.CreateDirectory(ApplicationSettings.AllowedUsersDirectory);
            }

            try
            {
                using (var streamReader = new StreamReader(ApplicationSettings.AllowedUsersDirectory + "users.txt"))
                {
                    String line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        if (line != ApplicationSettings.Admin.ToLower())
                        {
                            _allowedUsers.Add(line, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.Message}: Key exists");
            }
        }

        //Done
        private void WriteUsersToFile()
        {
            var usersList = new StringBuilder();
            using (var streamWriter = new StreamWriter(ApplicationSettings.AllowedUsersDirectory + "users.txt"))
            {
                foreach (string user in _allowedUsers.Keys)
                {
                    usersList.AppendLine(user);
                }
                streamWriter.Write(usersList);
            }
        }

        //Done
        private void MessageHandler(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            Logger.Information($"Got message \"{ev.Message.Text}\"");

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
                    SkipVoting(sender, ev);
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
                case "/resume":
                    ResumeTrack(sender, ev);
                    break;
                case "/getlist":
                    GetTitles(sender, ev);
                    break;
                case "/getusers":
                    GetUsers(sender, ev);
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

        

        //Done
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

            Logger.Information("Valid youtube link detected.");
            _telegramBotClient.SendTextMessageAsync(_chatId, ResponseMessages.QueuedSuccessful);
            tracksList.Add(new TrackInfo(ev.Message.Text, ev.Message.Chat.Username));
            if (tracksList.Count != 0)
            {
                _musicDownloader.DownloadAudio();
            }
        }

        //Done 
        private bool CheckLink(string link)
        {
            Regex YoutubeVideoRegex = new Regex(@"^(http(s)?:\/\/)?((w){3}.)?youtu(be|.be)?(\.com)?\/.+", RegexOptions.IgnoreCase);
            return YoutubeVideoRegex.Match(link).Success;
        }

    }
}