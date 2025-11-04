using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2.Models
{
    internal class Log
    {
        public MessageType MessageType { get; }
        public string Message { get; }
        public DateTime DateTime { get; }

        public Log(MessageType messageType, string message)
        {
            this.MessageType = messageType;
            this.Message = message;
            this.DateTime = DateTime.Now;
        }

        public string GetLogMessage()
        {
            string rtn = "";
            switch (MessageType)
            {
                case MessageType.System:
                    rtn += "시스템메시지: ";
                    break;
            }
            rtn += Message;
            return rtn; 
        }
    }

    internal enum MessageType
    {
        System
    }
}
