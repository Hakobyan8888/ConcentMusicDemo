using System;
using System.Collections.Generic;
using System.Text;

namespace MusicDemo
{
    //Done
    internal class ApplicationSettings
    {
        public static string Plugins { get; set; } = "./plugins/";
        public static string MusicDirectory { get; set; } = "./Music/";
        public static string LogsDirectory { get; set; } = "./Logs/";
        public static string Admin { get; set; } = "hastepan";
        public static string TelegramAPIKey { get; set; } = "823949713:AAFCbvVKBWXHKJZNkNOddy_AHRhLnUjbwPg";
        public static string AllowedUsersDirectory { get; set; } = "./AllowedUsers/";
    }
}