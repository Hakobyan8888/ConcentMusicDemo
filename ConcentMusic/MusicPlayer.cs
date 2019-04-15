using System;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace ConcentMusic
{
    class MusicPlayer
    {
        private Process _cvlcProcess;
        private bool? _isPlaying;

        ~MusicPlayer()
        {
            try
            {
                _cvlcProcess.Kill();
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}: can't close cvlc process.");
            }

        }

        public void PlayMusic(int id)
        {
            int _minTrackId;
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = @"C:\Program Files\VideoLAN\VLC\vlc.exe";
            psi.Arguments = "--play-and-exit " + AppSettings.MusicDirectory + id;
            _minTrackId = TelegramBot.t_racksList.Where(x => x.trackState == TrackState.Downloaded).Min(x => x._trackId);
            TelegramBot.t_racksList.Where(x => x._trackId == _minTrackId).First().trackState = TrackState.Playing;
            _cvlcProcess = Process.Start(psi);
            _cvlcProcess.WaitForExit();

            try
            {
                _cvlcProcess.Close();
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
                if (TelegramBot.t_racksList.Where(x => x.trackState == TrackState.Downloaded).Count() != 0)
                {
                    TelegramBot._alterVotersCount = 0;
                    TelegramBot._alterVote.Clear();
                    _isPlaying = true;
                    _minTrackId = TelegramBot.t_racksList.Where(x => x.trackState == TrackState.Downloaded).Min(x => x._trackId);
                    PlayMusic(_minTrackId);
                    TelegramBot.t_racksList.RemoveAll(x => x._trackId == _minTrackId);
                }
                else
                {
                    _isPlaying = null;
                    TelegramBot._alterVotersCount = null;
                    Thread.Sleep(100);
                }
            }
        }

        public void PauseTrack()
        {
            Process _stopCvlc;
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = "kill";
            psi.Arguments = "-STOP " + _cvlcProcess.Id;
            _stopCvlc = Process.Start(psi);
            _stopCvlc.WaitForExit();
            _stopCvlc.Close();
            _isPlaying = false;
        }

        public void ResumeTrack()
        {
            Process _resumeCvlc;
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = "kill";
            psi.Arguments = "-CONT " + _cvlcProcess.Id;
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
                _cvlcProcess.Kill();
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.Message}: Can't kill cvlc process.");
            }
        }
    }
}