using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;

namespace RawReminder
{
    /// <summary>
    /// Main class where Reminders are stored
    /// </summary>
    /// 
    #region Reminders class
    class Reminders
    {
        [Key]
        public int ReminderId { get; set; }
        public string ReminderContent { get; set; }
        public DateTime DateToRemind { get; set; }
        public DateTime DateReminderIsSet { get; set; }
        public string Notiz { get; set; }

        public ICollection<HistoryReminders> HistoryReminders { get; set; }

    }
    #endregion
    
    #region Reminders Context - Connection string for SQLite and Model init
    class RemindersContext : DbContext
    {
        public RemindersContext() : base(new SQLiteConnection()
        {
            ConnectionString = new SQLiteConnectionStringBuilder()
            {
                DataSource = DbOperations.DbName,
                ForeignKeys = false
            }.ConnectionString
        }, true)
        {
            // disable db init
            Database.SetInitializer<RemindersContext>(null);
        }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            // i decided not to use fluentAPI
            //modelBuilder.Types().Configure(t=>t.MapToStoredProcedures());

            // configure primary key
            //modelBuilder.Entity<Reminders>().HasKey<int>(s => s.ReminderId);
            //modelBuilder.Entity<HistoryReminders>().HasKey<int>(s => s.HistoryId);

            base.OnModelCreating(modelBuilder);
        }
        public DbSet<Reminders> DbSetReminders { get; set; }
        public DbSet<HistoryReminders> DbSetHistoryReminders { get; set; }
    }
    #endregion
}
