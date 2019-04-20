using System;
using System.Collections.Generic;
using System.Text;

namespace MusicDemo
{
    internal class TrackInfo
    {
        public readonly string url;
        public readonly int trackId;
        public readonly string user;
        public TrackState trackState;

        public TrackInfo(string url,string user)
        {
            this.url = url;
            this.user = user;
            trackState = TrackState.NotDownloaded;
            trackId = BotSettings.TrackId;
            BotSettings.TrackId++;
        }
    }

    public enum TrackState
    {
        NotDownloaded,
        Downloading,
        Downloaded,
        Playing
    }
}