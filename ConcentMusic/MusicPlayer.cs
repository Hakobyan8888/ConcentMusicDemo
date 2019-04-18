using System;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace ConcentMusic
{
    class MusicPlayer
    {
        private Process _vlcProcess;
        private bool? _isPlaying;

        ~MusicPlayer()
        {
            try
            {
                _vlcProcess.Kill();
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}: Can't close cvlc process.");
            }

        }

        public void PlayMusic(int id)
        {
            int _minTrackId;
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = @"C:\Program Files\VideoLAN\VLC\vlc.exe";
            psi.Arguments = "--play-and-exit " + AppSettings.MusicDirectory + id;
            _minTrackId = TelegramBot._tracksList.Where(x => x._trackState == TrackState.Downloaded).Min(x => x._trackId);
            TelegramBot._tracksList.Where(x => x._trackId == _minTrackId).First()._trackState = TrackState.Playing;
            _vlcProcess = Process.Start(psi);
            _vlcProcess.WaitForExit();

            try
            {
                _vlcProcess.Close();
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.Message}: Can't close the process");
            }
            RemoveTrack(id);
        }

        private void RemoveTrack(int id)
        {
            try
            {
                File.Delete(AppSettings.MusicDirectory + id);
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.Message}: Can't delete track file.");
            }
        }

        public void StartPlayingVlc()
        {
            Thread cvlc = new Thread(StartPlaying);
            cvlc.Start();
        }

        private void StartPlaying()
        {
            int _minTrackId;
            while (true)
            {
                if (TelegramBot._tracksList.Where(x => x._trackState == TrackState.Downloaded).Count() != 0)
                {
                    TelegramBot._alterVotersCount = 0;
                    TelegramBot._alterVote.Clear();
                    _isPlaying = true;
                    _minTrackId = TelegramBot._tracksList.Where(x => x._trackState == TrackState.Downloaded).Min(x => x._trackId);
                    PlayMusic(_minTrackId);
                    TelegramBot._tracksList.RemoveAll(x => x._trackId == _minTrackId);
                }
                else
                {
                    _isPlaying = null;
                    TelegramBot._alterVotersCount = null;
                    Thread.Sleep(100);
                }
            }
        }

        public void StopTrack()
        {
            Process _stopvlc;
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = @"C:\Program Files\VideoLAN\VLC\vlc.exe";
            psi.Arguments = "--global-key-stop " + _vlcProcess.Id;
            _stopvlc = Process.Start(psi);
            _stopvlc.WaitForExit();
            _stopvlc.Close();
            _isPlaying = false;
        }

        public void PauseTrack()
        {
            Process _pausevlc;
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = @"C:\Users\Arthur\Downloads\PSTools\pssuspend.exe";
            psi.Arguments = _vlcProcess.Id.ToString();
            _pausevlc = Process.Start(psi);
            _pausevlc.WaitForExit();
            _pausevlc.Close();
            _isPlaying = false;
        }

        public void ResumeTrack()
        {
            Process _resumeCvlc;
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = @"C:\Users\Arthur\Downloads\PSTools\pssuspend.exe";
            psi.Arguments = "-r " + _vlcProcess.Id;
            _resumeCvlc = Process.Start(psi);
            _resumeCvlc.WaitForExit();
            _resumeCvlc.Close();
            _isPlaying = true;
        }

        public bool? IsPlaying()
        {
            return _isPlaying;
        }

        public void Skip()
        {
            try
            {
                _vlcProcess.Kill();
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.Message}: Can't kill vlc process.");
            }
        }
    }
}