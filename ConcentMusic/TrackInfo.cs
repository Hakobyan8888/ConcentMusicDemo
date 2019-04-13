
namespace ConcentMusic
{
    public enum TrackState
    {
        NotDownloaded,
        Downloading,
        Downloaded,
        Playing
    }

    public class TrackInfo
    {
        public readonly string url;
        public readonly int trackId;
        public readonly string user;
        public TrackState trackState;

        public TrackInfo(string url, string user)
        {
            this.url = url;
            this.user = user;
            trackId = TelegramBot.trackId;
            trackState = TrackState.NotDownloaded;

            TelegramBot.trackId++;
        }
    }
}