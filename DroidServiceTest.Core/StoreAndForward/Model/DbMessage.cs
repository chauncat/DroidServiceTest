using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace DroidServiceTest.Core.StoreAndForward.Model
{
    internal class DbMessage
    {
        public DbMessage()
        {
            Parameters = new List<DbMessageParameter>();
        }

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public Guid RecurringId { get; set; }

        [MaxLength(250)]
        public string Type { get; set; }

        [MaxLength(8000)]
        public string Text { get; set; }

        public MessageStatus Status { get; set; }
        public int Retries { get; set; }
        public bool WifiOnly { get; set; }
        public DateTime CreateTm { get; set; }
        public DateTime? SentTm { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.All)] 
        public List<DbMessageParameter> Parameters { get; set; }

        public static implicit operator Message(DbMessage message)
        {
            if (message == null) return null;

            var list = message.Parameters.Select(parameter => (MessageParameter) parameter).ToList();


            return new Message
            {
                Id = message.Id,
                Type = message.Type,
                RecurringId = message.RecurringId,
                Retries = message.Retries,
                Status = message.Status,
                Text = message.Text,
                WifiOnly = message.WifiOnly,
                Parameters = list
            };
        }

        public static implicit operator DbMessage(Message message)
        {
            if (message == null) return null;

            return new DbMessage
            {
                Id = message.Id,
                RecurringId = message.RecurringId,
                Type = message.Type,
                Retries = message.Retries,
                Status = message.Status,
                Text = message.Text,
                WifiOnly = message.WifiOnly,
                Parameters = message.Parameters.Select(parameter => (DbMessageParameter)parameter).ToList()
            };

        }
    }
}