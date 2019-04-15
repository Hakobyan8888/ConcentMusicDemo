﻿using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Diagnostics;


namespace ConcentMusic
{
    class MusicDownloader
    {
        private Task _downloader;

        public MusicDownloader()
        {
            CreateMusicDirectory();
        }

        public void DownloadAudio()
        {
            var notDownloadedTracks = TelegramBot.t_racksList.Where(x => x.trackState == TrackState.NotDownloaded);

            _downloader = Task.Factory.StartNew(() =>
            {
                foreach (var track in notDownloadedTracks)
                {
                    DownloadAudio(track._trackId);
                }
            });
        }

        public void CreateMusicDirectory()
        {
            try
            {
                Directory.CreateDirectory(AppSettings.MusicDirectory);
            }
            catch (Exception ex)
            {
                Logger.Info($"{ex.Message} Can't create music directory.");
            }
        }

        private void DownloadAudio(int trackId)
        {
            Logger.Info("Downloading track");

            string _url = TelegramBot.t_racksList.Where(x => x._trackId == trackId).First()._url;
            ProcessStartInfo psi = new ProcessStartInfo();

            TelegramBot.t_racksList.Where(x => x._trackId == trackId).First().trackState = TrackState.Downloading;
            psi.FileName = @"C:\Program Files (x86)\youtube-dl.exe";
            psi.Arguments = "-o " + "\"" + AppSettings.MusicDirectory + trackId + "\"" + " -f 140 " + "\"" + _url + "\"";
            Process youtubeDlProcess = Process.Start(psi);
            youtubeDlProcess.WaitForExit();
            TelegramBot.t_racksList.Where(x => x._trackId == trackId).First().trackState = TrackState.Downloaded;
            youtubeDlProcess.Close();
            Logger.Info("Track downloaded");
        }
    }
}