using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace DroidServiceTest.Core.StoreAndForward.Model
{
    internal class DbMessageParameter
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [ForeignKey(typeof(DbMessage))]
        public int MessageId { get; set; }

        [MaxLength(250)]
        public string Type { get; set; }

        [MaxLength(8000)]
        public string Text { get; set; }

        public static implicit operator DbMessageParameter(MessageParameter item)
        {
            if (item == null) return null;

            return new DbMessageParameter
            {
                MessageId = item.MessageId,
                Id = item.Sequence,
                Text = item.Text,
                Type = item.Type,
            };
        }

        public static implicit operator MessageParameter(DbMessageParameter item)
        {
            if (item == null) return null;
            return new MessageParameter
            {
                Sequence = item.Id,
                MessageId = item.MessageId,
                Type = item.Type,
                Text = item.Text
            };
        }
    }
}