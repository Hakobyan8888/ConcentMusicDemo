using System;
using System.IO;

namespace ConcentMusic
{
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

        public static void Info(string logMessage)
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

            using (StreamWriter logWriter = File.AppendText(AppSettings.LogsDirectory + "log.txt"))
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
                Directory.CreateDirectory(AppSettings.LogsDirectory);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message} Can't create log directory.");
            }
        }
    }
}