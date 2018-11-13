using System;
using System.ComponentModel.DataAnnotations;

namespace RawReminder
{
    /// <summary>
    /// To store old reminders information.
    /// </summary>
    #region Class HistroyReminders
    class HistoryReminders 
    {
        [Key]
        public int HistoryId { get; set; }
        public int ReminderId { get; set; }
        public string ReminderHistoryContent { get; set; }
        public DateTime DateReminderExecuted { get; set; }

        public Reminders Reminders { get; set; }

    }
    #endregion


}
