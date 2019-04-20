using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace MusicDemo
{
    internal class MusicDownloader
    {
        private Task _downloader;

        //Done
        public MusicDownloader()
        {
            CreateMusicDirectory();
        }

        //Done
        public void DownloadAudio()
        {
            var notDownloadedTracks = BotSettings.tracksList.Where(x => x.trackState == TrackState.NotDownloaded);

            _downloader = Task.Factory.StartNew(() =>
            {
                foreach (var track in notDownloadedTracks.ToList())
                {
                    DownloadAudio(track.trackId);
                }
            });
        }

        //Done
        public void CreateMusicDirectory()
        {
            try
            {
                Directory.CreateDirectory(ApplicationSettings.MusicDirectory);
            }
            catch (Exception ex)
            {
                Logger.Information($"{ex.Message} Can't create music directory.");
            }
        }

        //Done
        private void DownloadAudio(int trackId)
        {
            Logger.Information("Downloading track");

            string _url = BotSettings.tracksList.Where(x => x.trackId == trackId).First().url;
            ProcessStartInfo psi = new ProcessStartInfo();

            BotSettings.tracksList.Where(x => x.trackId == trackId).First().trackState = TrackState.Downloading;
            psi.FileName = @"C:\Program Files (x86)\youtube-dl.exe";
            psi.Arguments = "-o " + "\"" + ApplicationSettings.MusicDirectory + trackId + "\"" + " -f 140 " + "\"" + _url + "\"";
            Process youtubeDlProcess = Process.Start(psi);
            youtubeDlProcess.WaitForExit();
            BotSettings.tracksList.Where(x => x.trackId == trackId).First().trackState = TrackState.Downloaded;
            youtubeDlProcess.Close();
            Logger.Information("Track downloaded");
        }
    }
}