using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DroidServiceTest.Core.Ioc;
using DroidServiceTest.Core.Logging;
using DroidServiceTest.Core.Logging.Logger;
using SQLite.Net;
using SQLiteNetExtensions.Extensions;

namespace DroidServiceTest.Core.StoreAndForward.Model
{
    public class Messages : IDisposable
    {
        public static readonly string StoreAndForwardDb = "snf.db3";
        private static readonly ILogger Logger;
        private static Messages _instance;
        private SQLiteConnection _dbConnection;
        private readonly IPlatformService _platform;
        private const int TIMEOUT = 1000000;
        private static readonly SemaphoreSlim SyncLock = new SemaphoreSlim(1, 1);

        static Messages()
        {
            Logger = LogFactory.Instance.GetLogger<Messages>();
        }

        private Messages()
        {
            Logger.Debug("in Messages constructor");
            _platform = Container.Instance.Resolve<IPlatformService>();
            if (_platform == null)
            {
                Logger.Debug("platform service is null ");
            }
        }

        public static Messages Instance
        {
            get
            {
                var lockTaken = false;
                try
                {
                    lockTaken = SyncLock.Wait(TIMEOUT);
                    if (lockTaken)
                    {
                    if (_instance == null) Create();
                }
                }
                finally
                {
                    if (lockTaken) SyncLock.Release();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Create the static instance of Messages
        /// </summary>
        private static void Create()
        {
            _instance = new Messages();
            _instance.ConfigureDb();
            _instance.DeleteOldMessagesNoLock();
            _instance.ResendStuckMessages();
            Logger.Debug("Exit Create");
        }

        /// <summary>
        /// Save new message into the database
        /// </summary>
        /// <param name="message">Message to save in the database</param>
        public void AddNewMessage(Message message)
        {
            Logger.Debug("Adding a new message");
            // read about Factory.StartNew
            if (_dbConnection != null && message != null)
            {
                var lockTaken = false;
                try
                {
                    lockTaken = SyncLock.Wait(TIMEOUT);
                    if (!lockTaken) return;

                    // creating new Message object so we can update the Message id for the object passed in.
                    // If we try to set message when DbMessage.Add returns it will not work because Add creates
                    // a new object.
                    DbMessage internalMessage = message;
                    internalMessage.CreateTm = DateTime.Now;
                    _dbConnection.InsertWithChildren(internalMessage);
                    Logger.Debug("Writing to DB. Message.Id == " + internalMessage.Id);
                    message.Id = internalMessage.Id;
                    for (int index = 0; index < internalMessage.Parameters.Count; index++)
                    {
                        message.Parameters[index].MessageId = internalMessage.Parameters[index].MessageId;
                        message.Parameters[index].Sequence = internalMessage.Parameters[index].Id;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Error adding Message.  Exception message: {0}", ex.Message), ex);
                }
                finally
                {
                    if (lockTaken) SyncLock.Release();
                }
            }
        }

        /// <summary>
        /// Increment the retries for a message.
        /// </summary>
        /// <param name="message">Message to increase retries</param>
        public bool IncrementRetry(Message message)
        {
            if (_dbConnection != null && message != null)
            {
                var lockTaken = false;
                try
                {
                    lockTaken = SyncLock.Wait(TIMEOUT);
                    if (lockTaken)
                    {
                        var command = _dbConnection.CreateCommand("UPDATE DbMessage SET Retries=Retries+1 WHERE Id=?", message.Id);
                        return (command.ExecuteNonQuery() == 1);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Error increasing retries.  Exception message: {0}", ex.Message), ex);
                }
                finally
                {
                    if (lockTaken) SyncLock.Release();
                }
            }

            return false;
        }

        /// <summary>
        ///  Reset message sent time stamp and status
        /// </summary>
        /// <param name="messageId">Identifier of the message to reset.</param>
        /// <returns>True if successful</returns>
        public bool ResetMessage(int messageId)
        {
            bool result = false;
            if (_dbConnection != null)
            {
                var lockTaken = false;
                try
                {
                    lockTaken = SyncLock.Wait(TIMEOUT);
                    if (!lockTaken) return false;

                    var message = _dbConnection.Find<DbMessage>(messageId);
                    if (message != null)
                    {
                        // change to sql statement having issue with bad dates
                        // and think this is due to sqlite.net object update
                        var command = _dbConnection.CreateCommand("UPDATE DbMessage SET Status = ?, SentTm = NULL WHERE Id = ?", (int)MessageStatus.Incomplete, message.Id);
                        result = (command.ExecuteNonQuery() == 1);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Error reseting message with id = {0}.  Exception message: {1}", messageId, ex.Message), ex);
                }
                finally
                {
                    if (lockTaken) SyncLock.Release();
                }
            }
            return result;
        }

        /// <summary>
        /// Remove recurring message from the database
        /// </summary>
        /// <param name="recurringId">Identifier for the recurring message</param>
        /// <returns>Returns true if successful</returns>
        public bool RemoveRecurringMessage(Guid recurringId)
        {
            var result = false;

            Logger.Debug(string.Format("RemoveRecurringMessage, recurringId passed in:{0}", recurringId));

            if (_dbConnection != null && recurringId != Guid.Empty)
            {
                var lockTaken = false;
                try
                {
                    lockTaken = SyncLock.Wait(TIMEOUT);
                    if (!lockTaken) return false;
                    var message = _dbConnection.Find<DbMessage>(x => x.RecurringId.Equals(recurringId));
                    if (message != null && message.RecurringId.Equals(recurringId))
                    {
                        result = DeleteMessageNoLock(message);
                        Logger.Debug(string.Format("Removed RecurringMessage, message id:{0}", message.RecurringId));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Error removing message with recurring id = {0}. Exception message: {1}", recurringId, ex.Message), ex);
                }
                finally
                {
                    if (lockTaken) SyncLock.Release();
                }
            }
            else
            {
                Logger.Error(" Either dbConnection is null or recurringId is empty");
            }

            return result;
        }

        /// <summary>
        /// Save a change the message status of a message to the database.
        /// </summary>
        /// <param name="message">Message to update</param>
        /// <param name="status">Status change</param>
        public void MarkMessage(Message message, MessageStatus status)
        {
            if (_dbConnection != null && message != null)
            {
                var lockTaken = false;
                try
                {
                    lockTaken = SyncLock.Wait(TIMEOUT);
                    if (!lockTaken) return; 
                    MarkMessageNoLock(message, status);
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Error setting status to {0}.", status.ToString()), ex);
                }
                finally
                {
                    if (lockTaken) SyncLock.Release();
                }
            }
        }


        /// <summary>
        /// Changes status without blocking
        /// </summary>
        /// <param name="message">message to change</param>
        /// <param name="status">status change</param>
        /// <returns>True if successful; Otherwise false</returns>
        private void MarkMessageNoLock(Message message, MessageStatus status)
        {
            if (status == MessageStatus.Sent)
            {
                // not calling DeleteMessage so we avoid nested calls to sync
                DeleteMessageNoLock(message);
            }
            else
            {
                // change to sql statement having issue with bad dates
                // and think this is due to sqlite.net object update
                var command = _dbConnection.CreateCommand(status == MessageStatus.Failed
                    ? "UPDATE DbMessage SET Status = ?, SentTm = datetime('now','localtime') WHERE Id = ?"
                    : "UPDATE DbMessage SET Status = ? WHERE Id = ?", (int)status, message.Id);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Returns a list of pending messages
        /// </summary>
        /// <param name="messageType">If empty will return all pending messages; 
        /// Otherwise will only return pending message that match messageType.</param>
        /// <returns>List of pending messages.</returns>
        public List<Message> GetPendingMessages(string messageType = "")
        {
            var list = new List<Message>();

            if (_dbConnection != null)
            {
                var lockTaken = false;

                try
                {
                    lockTaken = SyncLock.Wait(TIMEOUT);
                    if (lockTaken)
                    {
                    list = GetPendingMessagesNoLock(messageType).ToList();
                }
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Error getting pending messages for type = {0}. Exception Message: {1}", messageType, ex.Message), ex);
                }
                finally
                {
                    if (lockTaken) SyncLock.Release();
                }
            }
            return list;
        }

        /// <summary>
        /// Non-blocking method to retrieve pending messages
        /// </summary>
        /// <param name="messageType">Filter by type.  Empty string returns all.</param>
        /// <returns>List of pending messages</returns>
        private IEnumerable<Message> GetPendingMessagesNoLock(string messageType = "")
        {
            if (messageType == null)
            {
                messageType = string.Empty;
            }
           
            return _dbConnection.GetAllWithChildren<DbMessage>(message =>
                message.SentTm == null && message.Status == MessageStatus.Incomplete &&
                (messageType == "" || message.Type == messageType))
                .OrderBy(message => message.CreateTm)
                .Select(message => (Message) message);

        }

        /// <summary>
        /// Retrieve a message from the database.
        /// </summary>
        /// <param name="messageId">Identifier for the message to retrieve</param>
        /// <returns>If found the message will be return; Otherwise a message with id of zero will be returned.</returns>
        public Message GetMessage(int messageId)
        {
            Message message = null;

            if (_dbConnection != null)
            {
                var lockTaken = false;

                try
                {
                    lockTaken = SyncLock.Wait(TIMEOUT);

                    if (lockTaken)
                    {
                        message = _dbConnection.FindWithChildren<DbMessage>(messageId);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Unable to get message with id = {0}. Exception Message: {1}", messageId, ex.Message), ex);
                }
                finally
                {
                    if (lockTaken) SyncLock.Release();
                }
            }
            return message ?? new Message();
        }

        /// <summary>
        /// Retrieve a list of parameters for message identifier supplied.
        /// </summary>
        /// <param name="messageId">Identifier for the message that has parameter.</param>
        /// <returns>A list of parameters</returns>
        public List<MessageParameter> GetMessageParameters(int messageId)
        {
            var list = new List<MessageParameter>();

            if (_dbConnection != null)
            {
                var lockTaken = false;
                try
                {
                    lockTaken = SyncLock.Wait(TIMEOUT);
                    if (lockTaken)
                    {
                        var message = _dbConnection.FindWithChildren<DbMessage>(messageId);
                        list = message.Parameters.Select(parameter => (MessageParameter) parameter).ToList();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Unable to get parameters with message id = {0}. Exception Message: {1}", messageId, ex.Message), ex);
                }
                finally
                {
                    if (lockTaken) SyncLock.Release();
                }
            }
            return list;
        }


        private bool DeleteMessageNoLock(Message message)
        {
            return DeleteMessageNoLock(message.Id);
        }

        private bool DeleteMessageNoLock(int messageId)
        {
            bool result = false;

            //todo:  why cascade delete not working
            try
            {
                _dbConnection.RunInTransaction(() =>
                {
                    if (_dbConnection.Execute("Delete From DbMessageParameter where MessageId = ?", messageId) >= 0)
                    {
                        result = _dbConnection.Delete<DbMessage>(messageId) > 0;
                    }
                });
            }
            catch (Exception ex)
            {
                result = false;
                Logger.Error(string.Format("Unable to delete Message Id: {0}.", messageId), ex);
            }

            return result;
        }

        /// <summary>
        /// Remove a message from the database
        /// </summary>
        /// <param name="message">Message to be remove.</param>
        public void DeleteMessage(Message message)
        {
            if (_dbConnection != null && message != null)
            {
                var lockTaken = false;

                try
                {
                    lockTaken = SyncLock.Wait(TIMEOUT);
                    if (!lockTaken) return;
                    DeleteMessageNoLock(message);
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Unable to delete message. id = {0}. Exception message: {1}", message.Id, ex.Message), ex);
                }
                finally
                {
                    if (lockTaken) SyncLock.Release();
                }
            }
        }

        /// <summary>
        /// Remove pending messages by message type.
        /// </summary>
        /// <param name="messageType">Message type to remove from database.  Empty string will remove all pending messages.</param>
        public void DeletePendingMessages(string messageType = "")
        {
            if (_dbConnection != null)
            {
                var lockTaken = false;
                try
                {
                    lockTaken = SyncLock.Wait(TIMEOUT);
                    if (!lockTaken) return;
                    // Calling NoLock methods because SemaphoreSlim is not reentrant.
                    var list = GetPendingMessagesNoLock(messageType);
                    foreach (var message in list)
                    {
                        MarkMessageNoLock(message, MessageStatus.Failed);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Unable to mark pending messages as failed for type = {0}.  Exception message: {1}", messageType, ex.Message), ex);
                }
                finally
                {
                    if (lockTaken) SyncLock.Release();
                }
            }
        }

        /// <summary>
        /// Remove messages that are, for whatever reason, invalid.
        /// </summary>
        public void DeleteInvalidMessages()
        {
            if (_dbConnection == null) return;

            var lockTaken = false;
            try
            {
                lockTaken = SyncLock.Wait(TIMEOUT);
                if (!lockTaken) return;
                DeleteMessagesWithInvalidCreateTimeNoLock();
            }
            catch (Exception ex)
            {
                Logger.Error("Unable to delete invalid messages", ex);
            }
            finally
            {
                if (lockTaken) SyncLock.Release();
            }
        }

        private class DbMessageCreateTimeInfo
        {
            public int Id { get; set; }
            public string Text { get; set; }
            public string CreateTmString { get; set; }
        }

        private void DeleteMessagesWithInvalidCreateTimeNoLock()
        {
            var qry =
                _dbConnection.CreateCommand("SELECT Id, Text, CAST(CreateTm AS VARCHAR(50)) AS CreateTmString FROM DbMessage");
            var listDbMessages = qry.ExecuteQuery<DbMessageCreateTimeInfo>();

            // if the CreateTm string is not an actual DateTime, then mark it for deletion.
            DateTime date;
            var invalidMessageIds = (from message in listDbMessages
                                     where !DateTime.TryParse(message.CreateTmString, out date)
                                     select message).ToList();

            foreach (var message in invalidMessageIds)
            {
                Logger.Warn(string.Format("Deleting message {0} with invalid CreateTm, Message Text '{1}'", message.Id, message.Text));
                DeleteMessageNoLock(message.Id);
            }
        }

        /// <summary>
        /// Remove messages older then a day.
        /// </summary>
        public void DeleteOldMessages()
        {
            if (_dbConnection != null)
            {
                var lockTaken = false;

                try
                {
                    lockTaken = SyncLock.Wait(TIMEOUT);
                    if (!lockTaken) return;
                    DeleteOldMessagesNoLock();
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Unable to delete old messages.  Exception message: {0}", ex.Message), ex);
                }
                finally
                {
                    if (lockTaken) SyncLock.Release();
                }
            }
        }

        /// <summary>
        /// Remove messages older then a day. Non-Blocking
        /// </summary>
        private void DeleteOldMessagesNoLock()
        {
            if (_dbConnection != null)
            {
                // cleanup bad timestamps
                DeleteMessagesWithInvalidCreateTimeNoLock();

                // Had issues doing query statement on date time
                // this is not a heavy used method return table first.
                var fullList = _dbConnection.Table<DbMessage>().ToList();
                var list = fullList.Where(x => x.CreateTm <= (DateTime.Now.AddDays(-1)));

                foreach (var message in list)
                {
                    DeleteMessageNoLock(message);
                }
            }
        }

        /// <summary>
        /// Remove messages that are stuck in transmitting status.
        /// There status will be set back to incomplete
        /// </summary>
        private void ResendStuckMessages()
        {
            if (_dbConnection != null)
            {
                try
                {
                    // change to sql statement having issue with bad dates
                    // and think this is due to sqlite.net object update
                    var command = _dbConnection.CreateCommand("UPDATE DbMessage SET Status = ?, SentTm = NULL WHERE Status = ?", (int)MessageStatus.Incomplete, (int)MessageStatus.Transmitting);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Error removing stuck messages. Exception message: {0}", ex.Message), ex);
                }
            }
        }

        /// <summary>
        /// Configure the database
        /// </summary>
        private void ConfigureDb()
        {
            Logger.Debug("entered ConfigureDB");
            if (_platform == null) return;

            _dbConnection = _platform.GetSqlConnection(StoreAndForwardDb);
            
            
            if (_dbConnection != null)
            {
                Logger.Debug("_dbConnection not null");
                _dbConnection.TraceListener = new SqLiteConnectionTraceListener();
              
                _dbConnection.CreateTable<DbMessageParameter>();
                _dbConnection.CreateTable<DbMessage>();
                _dbConnection.Execute(
                    "CREATE TRIGGER IF NOT EXISTS DbMessage_InsertTrigger AFTER INSERT ON DbMessage BEGIN update DbMessage set CreateTm = datetime('now','localtime') where rowid = new.rowid; END;");
            }
            else
            {
                Logger.Debug("_dbConnection is null");
                throw new InvalidOperationException("Failed to establish database connection");
            }
        }

        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            var log = "";

            if (_dbConnection != null)
            {
                _dbConnection.Dispose();
                _dbConnection = null;
                log += "Db connection Disposed,";
            }

            _instance = null;

            log += "instance = null";
            Logger.Debug(log);
        }
    }

    public class SqLiteConnectionTraceListener : ITraceListener
    {
        private static readonly ILogger Logger;

        static SqLiteConnectionTraceListener()
        {
            Logger = LogFactory.Instance.GetLogger<SqLiteConnectionTraceListener>();
        }
        public void Receive(string message)
        {
            Logger.Debug(message);
        }
    }
}
