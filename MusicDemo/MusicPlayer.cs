using System;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace MusicDemo
{
    class MusicPlayer
    {
        private Process _vlcProcess;
        private bool? _isPlaying;
        ProcessStartInfo psi;

        public MusicPlayer()
        {
            psi = new ProcessStartInfo();
        }

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
            psi = new ProcessStartInfo();
            psi.FileName = ApplicationSettings.Plugins + "VLC/vlc.exe";
            psi.Arguments = "--play-and-exit " + ApplicationSettings.MusicDirectory + id;
            _minTrackId = BotSettings.tracksList.Where(x => x.trackState == TrackState.Downloaded).Min(x => x.trackId);
            BotSettings.tracksList.Where(x => x.trackId == _minTrackId).First().trackState = TrackState.Playing;
            _isPlaying = true;
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
                File.Delete(ApplicationSettings.MusicDirectory + id);
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.Message}: Can't delete track file.");
            }
        }

        public void StartPlayingVlc()
        {
            Thread vlc = new Thread(StartPlaying);
            vlc.Start();
        }

        private void StartPlaying()
        {
            int minTrackId;
            while (true)
            {
                if (BotSettings.tracksList.Where(x => x.trackState == TrackState.Downloaded).Count() != 0)
                {
                    BotSettings._alterVotersCount = 0;
                    BotSettings._alterVote.Clear();
                    _isPlaying = true;
                    minTrackId = BotSettings.tracksList.Where(x => x.trackState == TrackState.Downloaded).Min(x => x.trackId);
                    PlayMusic(minTrackId);
                    BotSettings.tracksList.RemoveAll(x => x.trackId == minTrackId);
                }
                else
                {
                    _isPlaying = null;
                    BotSettings._alterVotersCount = null;
                    Thread.Sleep(100);
                }
            }
        }

        public void PauseTrack()
        {
            Process _pausevlc;

            psi.FileName = ApplicationSettings.Plugins + "pssuspend.exe";
            psi.Arguments = _vlcProcess.Id.ToString();
            _pausevlc = Process.Start(psi);
            _pausevlc.WaitForExit();
            _pausevlc.Close();
            _isPlaying = false;
        }

        public void ResumeTrack()
        {
            Process _resumevlc;

            psi.FileName = ApplicationSettings.Plugins + "pssuspend.exe";
            psi.Arguments = "-r " + _vlcProcess.Id;
            _resumevlc = Process.Start(psi);
            _resumevlc.WaitForExit();
            _resumevlc.Close();
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