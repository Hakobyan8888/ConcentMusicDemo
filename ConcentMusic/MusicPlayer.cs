using System;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace ConcentMusic
{
    class MusicPlayer
    {
        private Process cvlcProcess;
        private bool? isPlaying;

        ~MusicPlayer()
        {
            try
            {
                cvlcProcess.Kill();
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}: can't close cvlc process.");
            }

        }

        public void PlayMusic(int id)
        {
            int minTrackId;
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = "cvlc";
            psi.Arguments = "--play-and-exit " + AppSettings.MusicDirectory + id;
            minTrackId = TelegramBot.TracksList.Where(x => x.trackState == TrackState.Downloaded).Min(x => x.trackId);
            TelegramBot.TracksList.Where(x => x.trackId == minTrackId).First().trackState = TrackState.Playing;
            cvlcProcess = Process.Start(psi);
            cvlcProcess.WaitForExit();

            try
            {
                cvlcProcess.Close();
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.Message}: Can't close the process");
            }
            removeTrack(id);
        }

        private void removeTrack(int id)
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

        public void StartPlaying()
        {
            Thread cvlc = new Thread(startPlaying);
            cvlc.Start();
        }

        private void startPlaying()
        {
            int minTrackId;
            while (true)
            {
                if (TelegramBot.TracksList.Where(x => x.trackState == TrackState.Downloaded).Count() != 0)
                {
                    TelegramBot.AlterVotersCount = 0;
                    TelegramBot.AlterVote.Clear();
                    isPlaying = true;
                    minTrackId = TelegramBot.TracksList.Where(x => x.trackState == TrackState.Downloaded).Min(x => x.trackId);
                    PlayMusic(minTrackId);
                    TelegramBot.TracksList.RemoveAll(x => x.trackId == minTrackId);
                }
                else
                {
                    isPlaying = null;
                    TelegramBot.AlterVotersCount = null;
                    Thread.Sleep(1000);
                }
            }
        }

        public void PauseTrack()
        {
            Process stopCvlc;
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = "kill";
            psi.Arguments = "-STOP " + cvlcProcess.Id;
            stopCvlc = Process.Start(psi);
            stopCvlc.WaitForExit();
            stopCvlc.Close();
            isPlaying = false;
        }

        public void ResumeTrack()
        {
            Process resumeCvlc;
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = "kill";
            psi.Arguments = "-CONT " + cvlcProcess.Id;
            resumeCvlc = Process.Start(psi);
            resumeCvlc.WaitForExit();
            resumeCvlc.Close();
            isPlaying = true;
        }

        public bool? IsPlaying()
        {
            return isPlaying;
        }

        public void Skip()
        {
            try
            {
                cvlcProcess.Kill();
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.Message}: Can't kill cvlc process.");
            }
        }
    }
}