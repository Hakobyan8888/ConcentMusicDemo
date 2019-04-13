using System;
using System.IO;

namespace ConcentMusic
{
    class Logger
    {
        public static void Error(string logMessage)
        {
            wrileLog("ERR: " + logMessage);
        }

        public static void Warn(string logMessage)
        {
            wrileLog("WARN: " + logMessage);
        }

        public static void Info(string logMessage)
        {
            wrileLog("INFO: " + logMessage);
        }

        public static void Debug(string logMessage)
        {
            wrileLog("DEBUG: " + logMessage);
        }

        public static void Trace(string logMessage)
        {
            wrileLog("TRACE: " + logMessage);
        }

        private static void wrileLog(string typedLogMessage)
        {
            string logLine = $"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}:\t{typedLogMessage}";

            using (StreamWriter logWriter = File.AppendText(AppSettings.LogsDirectory + "log.txt"))
            {
                logWriter.WriteLine(logLine);
            }
        }

        public static void Init()
        {
            createLogDirectory();
        }

        private static void createLogDirectory()
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