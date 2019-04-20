using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Web;
using System.Net;

namespace MusicDemo
{
    internal partial class BotSettings
    {
        //Done
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

        //Done
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

        //Done ?
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

        //Done
        public void VolumeUp(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = @"C:\Users\Arthur\Downloads\nircmd-x64\nircmd.exe";
            psi.Arguments = "changesysvolume 9375 ";
            Process amixerProcess = Process.Start(psi);
            amixerProcess.WaitForExit();
            amixerProcess.Close();
        }

        //Done
        public void VolumeDown(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = @"C:\Users\Arthur\Downloads\nircmd-x64\nircmd.exe";
            psi.Arguments = "changesysvolume -9375 ";
            Process amixerProcess = Process.Start(psi);
            amixerProcess.WaitForExit();
            amixerProcess.Close();
        }

        //Done
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

        //Done
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

            _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackStarted);
            _musicPlayer.ResumeTrack();
        }

        //Done
        private void GetUsers(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            StringBuilder usersBuilder = new StringBuilder();

            foreach (string user in _allowedUsers.Keys)
                usersBuilder.AppendLine("@" + user);
            _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, usersBuilder.ToString());
        }

        //Done
        public void GetTitles(object sender, Telegram.Bot.Args.MessageEventArgs ev)
        {
            StringBuilder titlesBuilder = new StringBuilder();
            int _trackNumber = 1;

            if (!tracksList.Any() || tracksList.Count() == 0)
            {
                _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, ResponseMessages.TrackListIsEmpty);
                return;
            }

            foreach (TrackInfo track in tracksList)
            {
                string _title = GetTitle(track.url);

                if (track.trackState == TrackState.Playing)
                    titlesBuilder.Append("->");

                titlesBuilder.AppendLine(_trackNumber++ + ") " + _title + $" | User @{track.user}");
            }
            _telegramBotClient.SendTextMessageAsync(ev.Message.Chat.Id, titlesBuilder.ToString());
        }

        //Done 
        private string GetTitle(string url)
        {
            string _api = $"http://youtube.com/get_video_info?video_id={GetArgs(url, "v", '?')}";
            string _title = GetArgs(new WebClient().DownloadString(_api), "title", '&');

            return _title;
        }

        //Ask Karen 
        private string GetArgs(string args, string key, char query)
        {
            var iqs = args.IndexOf(query);
            return iqs == -1
                ? string.Empty
                : HttpUtility.ParseQueryString(iqs < args.Length - 1
                    ? args.Substring(iqs + 1) : string.Empty)[key];
        }
    }
}