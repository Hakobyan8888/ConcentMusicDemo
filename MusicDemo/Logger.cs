using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MusicDemo
{
    //Done 
    class Logger
    {
        public static void Error(string logMessage)
        {
            WrileLog("ERR: " + logMessage);
        }

        public static void Warn(string logMessage)
        {
            WrileLog("WARN: " + logMessage);
        }

        public static void Information(string logMessage)
        {
            WrileLog("INFO: " + logMessage);
        }

        public static void Debug(string logMessage)
        {
            WrileLog("DEBUG: " + logMessage);
        }

        public static void Trace(string logMessage)
        {
            WrileLog("TRACE: " + logMessage);
        }

        private static void WrileLog(string typedLogMessage)
        {
            string _logLine = $"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}:\t{typedLogMessage}";

            using (StreamWriter logWriter = File.AppendText(ApplicationSettings.LogsDirectory + "log.txt"))
            {
                logWriter.WriteLine(_logLine);
            }
        }

        public static void Init()
        {
            CreateLogDirectory();
        }

        private static void CreateLogDirectory()
        {
            try
            {
                Directory.CreateDirectory(ApplicationSettings.LogsDirectory);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message} Can't create log directory.");
            }
        }
    }
}