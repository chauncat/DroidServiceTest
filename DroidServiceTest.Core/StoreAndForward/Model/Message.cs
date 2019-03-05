using System;
using System.Collections.Generic;

namespace DroidServiceTest.Core.StoreAndForward.Model
{
    public class Message
    {
        public Message()
        {
            Status = MessageStatus.Incomplete;
            Parameters = new List<MessageParameter>();
        }

        public int Id { get; set; }
        public Guid RecurringId { get; set; }
        public string Type { get; set; }
        public string Text { get; set; }
        public MessageStatus Status { get; set; }
        public int Retries { get; set; }
        public bool WifiOnly { get; set; }

        public List<MessageParameter> Parameters { get; set; }
    }
}
