using System;
using Terminal = System.Console;
using System.Data.SQLite;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace RawReminder
{
    /// <summary>
    /// This class is for creating DB and connecting to DB and CRUD operations.
    /// </summary>
    class DbOperations
    {

        static SQLiteConnection DbConnection { get; set; }
        static SQLiteCommand DbCommand { get; set; }
        public static string DbName { get; set; }
        static bool _isEnviromentNewLineActivated { get; set; }

        public DbOperations()
        {
            DbName = "TaskList.sqlite";
            DbConnection = new SQLiteConnection("Data Source=" + DbName + ";Version=3");
            InitDb();

            _isEnviromentNewLineActivated = false;
        }

        static string ShowInSingleLine()
        {
            if (_isEnviromentNewLineActivated)
                return "\n";
            else
                return "\r";
        }

        #region InitDb
        // init db (check if db exist, create db, check tables, create tables)
        public void InitDb()
        {
            if (!File.Exists(DbName))
            {
                if (CreateDb())
                    Terminal.Write(ShowInSingleLine() + "Db " + DbName + " created.");
            }
            else
                Terminal.Write(ShowInSingleLine() + "Db " + DbName + " already exists");
            Terminal.Write(ShowInSingleLine() + "Checking tables ...");

            // Get table names based on classes names.
            var tablesForDb = new string[] { nameof(Reminders), nameof(HistoryReminders) };

            try
            {
                DbConnection.Open();
                CheckDbTables(tablesForDb);
            }
            catch (Exception e)
            {
                Terminal.WriteLine("Error in InitDB(): " + e);
                throw;
            }
            finally
            {
                DbConnection.Close();
            }

        }

        #region Create Database file
        bool CreateDb()
        {
            var isDbCreated = false;
            try
            {
                SQLiteConnection.CreateFile(DbName);
                isDbCreated = true;
            }
            catch (Exception e)
            {
                Terminal.Write(ShowInSingleLine() + "Err creating DB: " + e);
                throw;
            }
            return isDbCreated;
        }
        #endregion

        #region Check if Db Tables exsist
        void CheckDbTables(string[] tables)
        {
            foreach (var tbl in tables)
            {
                var msg = string.Empty;
                if (!isTableCreated(tbl))
                    CreateTableInDb(tbl);
                else
                {
                    msg = "Table " + tbl + " consistent.";
                }
                Terminal.Write(ShowInSingleLine() + msg);
            }
        }
        #endregion

        #region bool does Table exists
        static bool isTableCreated(string tableName)
        {
            using (DbCommand = DbConnection.CreateCommand())
            {
                try
                {
                    DbCommand.CommandText = "SELECT name " +
                    "FROM sqlite_master " +
                    "WHERE type = 'table' " +
                    "AND name = @name";
                    DbCommand.Parameters.Add("@name", System.Data.DbType.String).Value = tableName;
                    return (DbCommand.ExecuteScalar() != null);
                }
                catch (Exception e)
                {
                    Terminal.WriteLine("Err (isTableCreated) checking table exists: " + e);
                    throw;
                }
            }
        }
        #endregion

        #region Function to create a table in db
        bool CreateTableInDb(string tblName)
        {
            var a = false;
            var msg = string.Empty;
            var query = string.Empty;

            query = "CREATE TABLE ";
            if (tblName.Equals("Reminders"))
            {
                var rem = new MemberHelper<Reminders>();
                query += tblName + "("
                    + rem.GetName(x => x.ReminderId) + " integer PRIMARY KEY AUTOINCREMENT, "
                    + rem.GetName(x => x.ReminderContent) + " text NOT NULL, "
                    + rem.GetName(x => x.DateToRemind) + " text NOT NULL, "
                    + rem.GetName(x => x.DateReminderIsSet) + " text, "
                    + rem.GetName(x => x.Notiz) + " text);";
                msg = "Creating DB Table: " + tblName + "\nQuery:\n" + query;
                Terminal.WriteLine(msg);
            }
            else if (tblName.Equals("HistoryReminders"))
            {
                var rem = new MemberHelper<HistoryReminders>();
                query += tblName + "("
                    + rem.GetName(x => x.HistoryId) + " integer PRIMARY KEY AUTOINCREMENT, "
                    + rem.GetName(x => x.ReminderId) + " text NOT NULL, "
                    + rem.GetName(x => x.ReminderHistoryContent) + " text NOT NULL, "
                    + rem.GetName(x => x.DateReminderExecuted) + " text);";
                msg = "Creating DB Table: " + tblName + "\nQuery:\n" + query;
                Terminal.WriteLine(msg);
            }

            if (msg.Length != 0)
                Terminal.Write(ShowInSingleLine() + "Creating DB table " + tblName + "...");

            try
            {
                using (SQLiteTransaction tr = DbConnection.BeginTransaction())
                {
                    using (DbCommand = DbConnection.CreateCommand())
                    {
                        DbCommand.Transaction = tr;
                        var cmdText = "DROP TABLE IF EXISTS ";
                        var tbl = (tblName.Equals(nameof(Reminders))) ?
                            nameof(Reminders) :
                            nameof(HistoryReminders);
                        cmdText += tbl + "";
                        DbCommand.CommandText = cmdText;
                        DbCommand.ExecuteNonQuery();

                        DbCommand.CommandText = @query;
                        DbCommand.ExecuteNonQuery();
                    } //end first using
                    tr.Commit();
                    a = true;
                }
            }
            catch (Exception e)
            {
                Terminal.WriteLine("ERR creating table: " + e);
                throw;
            }
            return a;
        }
        #endregion
        #endregion

        #region CRUD operations
        #region Update operation
        public static void UpdateDataToReminders(string dateWhenToRemind, string msgToRemind, string extraNotes, int whatIdToUpdate)
        {
            // First, pull/get data from DB
            var getDataFromDb = GetDataRemindersById(whatIdToUpdate).ToList();
            if (getDataFromDb.Count == 0)
            {
                Terminal.Write(ShowInSingleLine() + "There is no data with id: " + whatIdToUpdate);
                return;
            }
            // Check if parameters are empty. If empty, then we are going to leave old data
            // if dateWhenToRemind is empty, then leave old data
            DateTime dateToRmnd;
            if (!string.IsNullOrEmpty(dateWhenToRemind) && !string.IsNullOrWhiteSpace(dateWhenToRemind))
            {
                var isDateInGoodFormat = DateTime.TryParse(dateWhenToRemind, out dateToRmnd);
                if (isDateInGoodFormat)
                    Terminal.WriteLine("Update of date and time to DB => " + dateToRmnd);
                else
                {
                    Terminal.WriteLine("Date time was in wrong format, we will leave old date.");
                    dateToRmnd = Convert.ToDateTime(getDataFromDb.Select(x => x.DateToRemind).FirstOrDefault().ToString());
                }
            }
            else
                dateToRmnd = Convert.ToDateTime(getDataFromDb.Select(x => x.DateToRemind).FirstOrDefault().ToString());

            // if empty data for reminder content, then leave old data
            if (string.IsNullOrEmpty(msgToRemind) && string.IsNullOrWhiteSpace(msgToRemind))
            {
                msgToRemind = getDataFromDb.Select(x => x.ReminderContent).FirstOrDefault().ToString();
                Terminal.WriteLine("Reminder content is not changed. We are leaving old information => " + msgToRemind);
            }
            // the same is for extra notes field
            if (string.IsNullOrEmpty(extraNotes) && string.IsNullOrWhiteSpace(extraNotes))
            {
                extraNotes = getDataFromDb.Select(x => x.Notiz).FirstOrDefault().ToString();
                Terminal.WriteLine("Reminder notes are not changed. We are leaving old information => " + extraNotes);
            }

            var msg = "\nNew info is updated for rowID " + whatIdToUpdate + "\nReminder Id = " +
                whatIdToUpdate + "\nReminder content = " + msgToRemind + "\nDate when to remind = " + dateToRmnd + "\nReminder notes = " + extraNotes;
            Terminal.WriteLine(msg);
            // bind new data to class members
            var rm = new Reminders
            {
                ReminderContent = msgToRemind,
                DateToRemind = dateToRmnd,
                Notiz = extraNotes,
                ReminderId = whatIdToUpdate,
                DateReminderIsSet = DateTime.UtcNow
            };
            // this is the code for history table.. but not needed right now
            // rm.HistoryReminders.Add(new HistoryReminders { HistoryId = whatIdToUpdate, });

            // execute update db
            using (var ctx = new RemindersContext())
            {
                try
                {
                    var remi = new MemberHelper<Reminders>();
                    ctx.DbSetReminders.Attach(rm);
                    ctx.Entry(rm).State = EntityState.Modified;
                    /* // is there a better method than this??
                    ctx.Entry(rm).Property(remi.GetName(x => x.ReminderContent)).IsModified = true;
                    ctx.Entry(rm).Property(remi.GetName(x => x.DateToRemind)).IsModified = true;
                    ctx.Entry(rm).Property(remi.GetName(x => x.Notiz)).IsModified = true;*/
                    ctx.SaveChanges();
                }
                catch (Exception e)
                {
                    Terminal.WriteLine("ERR in UpdateDataToReminders: " + e);
                    throw;
                }
            }

        }
        #endregion

        #region Move data from 1st table to 2nd table 
        public static bool MoveDataFromRemindersToHistory(int reminderId)
        {
            var isDataMoved = false;
            using (var ctx = new RemindersContext())
            {
                // get data from reminders
                var oldReminderData = ctx.DbSetReminders.Where(g => g.ReminderId == reminderId).FirstOrDefault();
                // copy from table1(reminders) to table2(historyreminders)
                var newHistory = new HistoryReminders
                {
                    ReminderId = oldReminderData.ReminderId,
                    DateReminderExecuted = oldReminderData.DateToRemind,
                    ReminderHistoryContent = oldReminderData.ReminderContent
                };
                ctx.DbSetHistoryReminders.Attach(newHistory);
                ctx.DbSetHistoryReminders.Add(newHistory);
                ctx.DbSetReminders.Remove(oldReminderData);

                try
                {
                    ctx.SaveChanges();
                }
                catch (Exception e)
                {
                    Terminal.WriteLine("Err moving data from table reminder -> history reminders");
                    throw;
                }
            }

            return isDataMoved;
        }
        #endregion

        #region Add operation to reminders
        public static bool AddDataToReminders(DateTime dateWhenToRemind, string someMessageToRemind, string someExtraNotes)
        {
            var isDataAdded = false;
            // add records first in List
            var recordsToAddReminders = new List<Reminders>();
            var reminderInfo = new Reminders()
            {
                ReminderContent = someMessageToRemind,
                DateToRemind = dateWhenToRemind,
                DateReminderIsSet = DateTime.UtcNow,
                Notiz = someExtraNotes
            };
            // save data to second list using add range - faster to update info to db
            recordsToAddReminders.Add(reminderInfo);

            using (var cntx = new RemindersContext())
            {
                try
                {
                    // one more config to improve speed
                    cntx.Configuration.AutoDetectChangesEnabled = false;
                    //cntx.DbSetReminders.Add(reminderInfo);
                    cntx.DbSetReminders.AddRange(recordsToAddReminders);
                    cntx.ChangeTracker.DetectChanges();
                    cntx.SaveChanges();
                    isDataAdded = true;
                }
                catch (Exception ex)
                {
                    Terminal.WriteLine("ERR saving information: " + ex);
                    throw;
                }
            }
            return isDataAdded;
        }
        #endregion

        #region Select and Show Data operation
        #region To get Primary Key ID from Reminders table and store to list
        static public List<Reminders> GetDataRemindersById(int rowId)
        {
            var list = new List<Reminders>();
            using (var ctx = new RemindersContext())
            {
                // for speed up
                try
                {
                    ctx.Configuration.ProxyCreationEnabled = false;
                    list = ctx.DbSetReminders.Where(x => x.ReminderId == rowId).ToList();
                }
                catch (Exception e)
                {
                    Terminal.WriteLine("Err in GetDataRemindersById: " + e);
                    throw;
                }

            }
            return list;
        }
        #endregion

        #region To get Primary Key ID from History Reminders table and store to list
        static List<HistoryReminders> GetDataHistoryRemindersById(int rowId)
        {
            var list = new List<HistoryReminders>();
            using (var ctx = new RemindersContext())
            {
                try
                {
                    ctx.Configuration.ProxyCreationEnabled = false;
                    list = ctx.DbSetHistoryReminders.Where(x => x.HistoryId == rowId).ToList();
                }
                catch (Exception e)
                {
                    Terminal.WriteLine("Err in GetDataHistoryRemindersById: " + e);
                    throw;
                }
            }
            return list;
        }
        #endregion

        #region Show all Reminders table in Terminal
        public static void ShowAllReminders(bool showInTerminal)
        {
            var listRetrievedDataFromDB = new List<Reminders>();
            // read from db
            using (var ctx = new RemindersContext())
            {
                try
                {
                    ctx.Configuration.ProxyCreationEnabled = false;
                    listRetrievedDataFromDB = ctx.DbSetReminders.ToList();
                }
                catch (Exception e)
                {
                    Terminal.WriteLine("ERR in ShowAllReminders: " + e);
                    throw;
                }
            }
            // show data on terminal
            if (showInTerminal)
                Terminal.WriteLine(listRetrievedDataFromDB.ToStringTable(new[] { "Id", "Message", "Date scheduled", "Created", "Notes" },
                    dd => dd.ReminderId, dd => dd.ReminderContent, dd => dd.DateToRemind.ToString("dd/MM/yy HH:mm"), dd => dd.DateReminderIsSet.ToString("dd/MM/yy HH:mm"), dd => dd.Notiz));
        }

        public static List<Reminders> AllRemindersToList()
        {
            var listRetrievedDataFromDB = new List<Reminders>();
            using (var ctx = new RemindersContext())
            {
                try
                {
                    ctx.Configuration.ProxyCreationEnabled = false;
                    listRetrievedDataFromDB = ctx.DbSetReminders.ToList();
                }
                catch (Exception e)
                {
                    Terminal.WriteLine("ERR in ShowAllReminders: " + e);
                    throw;
                }
            }
            return listRetrievedDataFromDB;
        }
        public static List<Reminders> AllRemindersToList(int reminderId)
        {
            var listRetrievedDataFromDB = new List<Reminders>();
            using (var ctx = new RemindersContext())
            {
                try
                {
                    ctx.Configuration.ProxyCreationEnabled = false;
                    listRetrievedDataFromDB = ctx.DbSetReminders.ToList();
                }
                catch (Exception e)
                {
                    Terminal.WriteLine("ERR in ShowAllReminders: " + e);
                    throw;
                }
            }
            return listRetrievedDataFromDB;
        }
        #endregion

        #region Show all History Reminders table in Terminal
        public static void ShowAllHistoryReminders()
        {
            var listRetrievedDataFromDB = new List<HistoryReminders>();
            // read from db
            using (var ctx = new RemindersContext())
            {
                try
                {
                    ctx.Configuration.ProxyCreationEnabled = false;
                    listRetrievedDataFromDB = ctx.DbSetHistoryReminders.ToList();
                }
                catch (Exception e)
                {
                    Terminal.WriteLine("Err in ShowAllHistoryReminders: " + e);
                    throw;
                }
            }
            // show data on terminal
            Terminal.WriteLine(listRetrievedDataFromDB.ToStringTable(new[] { "Id", "ReminderId", "Content", "Date executed" },
                dd => dd.HistoryId, dd => dd.ReminderId, dd => dd.ReminderHistoryContent, dd => dd.DateReminderExecuted));
        }
        #endregion
        #endregion

        #region Delete operation
        static public void DeleteReminderById(int rowId)
        {
            using (var ctx = new RemindersContext())
            {
                ctx.Configuration.ProxyCreationEnabled = false;
                // This is going to be deleted.
                var dataToDelete = new Reminders
                {
                    ReminderId = rowId
                };
                try
                {
                    var checkIfDataExists = GetDataRemindersById(rowId);
                    // If data doesn't exist, then do nothing.
                    // Is this good method for checking data exsistance?
                    if (checkIfDataExists.Count == 0)
                    {
                        Terminal.Write(ShowInSingleLine() + "Row id number " + rowId + " doesn't contain data!");
                        return;
                    }

                    // TODO: move to history table first, then delete?

                    // If data exists, delete it
                    ctx.DbSetReminders.Attach(dataToDelete);
                    ctx.DbSetReminders.Remove(dataToDelete);
                    ctx.SaveChanges();
                    Terminal.Write(ShowInSingleLine() + "Reminder Id " + rowId + " deleted!");
                }
                catch (Exception e)
                {
                    Terminal.WriteLine("Err in DeleteReminderById: " + e);
                    throw;
                }

            }
        }
        static public void DeleteReminderById(string param)
        {
            if (param.ToLower().Equals("all"))
            {
                try
                {
                    using (var ctx = new RemindersContext())
                    {
                        ctx.Database.ExecuteSqlCommand("DELETE FROM [" + nameof(Reminders) + "]");
                        Terminal.WriteLine("All reminders deleted!");
                    }
                }
                catch (Exception e)
                {
                    Terminal.WriteLine("Err while deleteing all reminders: " + e);
                    throw;
                }
                Program.RestartAllTasks(true);
            }
        }
        #endregion

        #endregion


    }






}
