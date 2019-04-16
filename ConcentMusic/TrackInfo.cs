
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
        public readonly string _url;
        public readonly int _trackId;
        public readonly string _user;
        public TrackState _trackState;

        public TrackInfo(string url, string user)
        {
            this._url = url;
            this._user = user;
            _trackId = TelegramBot._trackId;
            _trackState = TrackState.NotDownloaded;

            TelegramBot._trackId++;
        }
    }
}