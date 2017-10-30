using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace setlXBot.Models
{
    class Log
    {
        public LogLevel Level { get; set; }

        public string Message { get; set; }

        public DateTime CreatedAt { get; set; }

        public string User { get; set; }

        public Log(LogLevel level, string message, string user)
        {
            Level = level;
            Message = message;
            User = user;
            CreatedAt = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return String.Format("{0}Z - {1} - {2} - {3}", CreatedAt.ToString("s"), Level, User, Message);
        }
    }
}
