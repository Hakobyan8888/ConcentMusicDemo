using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Diagnostics;


namespace ConcentMusic
{
    class MusicDownloader
    {
        private Task download;

        public MusicDownloader()
        {
            createMusicDirectory();
        }

        public void DownloadAudio()
        {
            var notDownloadedTracks = TelegramBot.TracksList.Where(x => x.trackState == TrackState.NotDownloaded);

            download = Task.Factory.StartNew(() =>
            {
                foreach (var track in notDownloadedTracks)
                {
                    downloadAudio(track.trackId);
                }
            });
        }

        public void createMusicDirectory()
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

        private void downloadAudio(int trackId)
        {
            Logger.Info("Downloading track");

            string URL = TelegramBot.TracksList.Where(x => x.trackId == trackId).First().url;
            ProcessStartInfo psi = new ProcessStartInfo();

            TelegramBot.TracksList.Where(x => x.trackId == trackId).First().trackState = TrackState.Downloading;
            psi.FileName = "youtube-dl";
            psi.Arguments = "-o " + "\"" + AppSettings.MusicDirectory + trackId + "\"" + " -f 140 " + "\"" + URL + "\"";
            Process youtubeDlProcess = Process.Start(psi);
            youtubeDlProcess.WaitForExit();
            TelegramBot.TracksList.Where(x => x.trackId == trackId).First().trackState = TrackState.Downloaded;
            youtubeDlProcess.Close();
            Logger.Info("Track downloaded");
        }
    }
}