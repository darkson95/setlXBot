using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace setlXBot.Models
{
    class Logger
    {
        public Logger(LogLevel level, string file)
        {
            LogLevel = level;
            LogFile = file;
        }

        private List<Log> _UnwrittenLogs;

        private List<Log> UnwrittenLogs {
            get
            {
                if (_UnwrittenLogs == null)
                {
                    _UnwrittenLogs = new List<Log>();
                }

                return _UnwrittenLogs;
            }
        }

        public LogLevel LogLevel { get; set; }

        public string LogFile { get; set; }

        public void Debug(string user, params string[] message)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < message.Length; i++)
            {
                sb.Append(message[i]);
                sb.Append(" ");
            }

            Log(new Log(LogLevel.Debug, sb.ToString(), user));
        }

        public void Info(string user, params string[] message)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < message.Length; i++)
            {
                sb.Append(message[i]);
                sb.Append(" ");
            }

            Log(new Log(LogLevel.Info, sb.ToString(), user));
        }

        public void Warning(string user, params string[] message)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < message.Length; i++)
            {
                sb.Append(message[i]);
                sb.Append(" ");
            }

            Log(new Log(LogLevel.Warning, sb.ToString(), user));
        }

        public void Error(string user, params string[] message)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < message.Length; i++)
            {
                sb.Append(message[i]);
                sb.Append(" ");
            }

            Log(new Log(LogLevel.Error, sb.ToString(), user));
        }

        public void Log(Log log)
        {
            if ((int)log.Level >= (int)LogLevel)
            {
                Console.WriteLine(log);
            }

            try
            {
                writeToFile(log);
            }
            catch (Exception)
            {

            }
        }

        private void writeToFile(Log log)
        {
            UnwrittenLogs.Add(log);

            try
            {
                if (UnwrittenLogs != null && UnwrittenLogs.Count > 0)
                {
                    FileInfo fi = new FileInfo(LogFile);
                    if (!fi.Exists)
                    {
                        File.Create(LogFile);
                    }

                    UnwrittenLogs.ForEach(x => {
                        File.AppendAllText(LogFile, x.ToString() + Environment.NewLine);
                        UnwrittenLogs.Remove(x);
                    });
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
