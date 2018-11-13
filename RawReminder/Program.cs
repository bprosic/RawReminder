using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Terminal = System.Console;

using System.Windows.Forms;

namespace RawReminder
{
    class Program
    {
        /// <summary>
        /// version 0e 7.10.2018
        /// </summary>
        /// <param name="args"></param>
        // thread init EF
        static Thread threadInitDbInitEF;
        // thread for rmeinder tasks
        static Thread threadControlReminders;
        static DbOperations db;
        // check if EF init is finished
        static bool isThreadInitDbFinished { get; set; }
        // qt adding reminders to thread pool
        static QueueTask qt;

        static void Main(string[] args)
        {
            var msgToUser = string.Empty;
            // show terminal window in specific width and height
            HelpFunctions.SetWindowWidth(120, 41);

            if (args == null || args.Length == 0)
            {
                // Start engine.
                // This thread checks if db exists, tables and this is also EF initialization thread.
                threadInitDbInitEF = new Thread(ThreadCheckDb);
                threadInitDbInitEF.Start();
                // Thread that loops all data in reminders table and sets tasks to execute reminders.
                // Also this thread is in function for clearing/cleaning data in reminders table 
                // when reminders "date to execute" is gone/expired.
                threadControlReminders = new Thread(ThreadControlReminders);
                threadControlReminders.Start();

                // this is terminal for user input and control
                InputFromUser();
            }
            else
            {
                // This was first version of this program. later I added engine above code and leave this code
                // this is just crud operation using parameters, but no thread will be working if you run it!
                // to run: 
                // reminder.exe -parameter

                // Set all args into array
                var arg = Array.ConvertAll(args, i => i);

                if (arg.Length == 0)
                    return;

                if (arg[0].ToLower().Equals("-set"))
                {
                    if (arg.Length < 2)
                    {
                        Terminal.WriteLine("\nEnter date time ...\n");
                        return;
                    }
                    DateTime dateTime;
                    if (!DateTime.TryParse(arg[1], out dateTime))
                    {
                        Terminal.WriteLine("\nUse correct datetime format: 13:45, 1:45, 21.12.1998 1:45, 21-12-1998 1:43 ... \n");
                        return;
                    }
                    if (arg.Length < 3)
                    {
                        Terminal.WriteLine("\nEnter message to remind ... \n");
                        return;
                    }
                    if (string.IsNullOrEmpty(arg[2]) || string.IsNullOrWhiteSpace(arg[2]))
                    {
                        Terminal.Write("\nEnter proper reminder message, e.g. \"Empty trash!\"\n");
                        return;
                    }
                    else
                        msgToUser = "Used parameters: ";
                    foreach (var c in arg)
                        msgToUser += c + ",";
                    Terminal.Write("\nExecuting ..." + msgToUser);
                    DbOperations.AddDataToReminders(dateTime, arg[2], "Added with args");
                }
                else
                {
                    Terminal.WriteLine("Use -set parameter!");
                    return;
                }
            } //end else
        } // end main
        #region Input from user

        static void InputFromUser()
        {
            // FIXME: wait for thread 1 to finish
            while (true)
            {
                if (isThreadInitDbFinished)
                    break;
            }
            var inputArgs = string.Empty;
            var msg = "Write exit to exit app or help for more help";
            var spliter = new string('-', msg.Length);
            Terminal.WriteLine("Ready\n" + spliter + "\n" + msg);

            while (true)
            {
                Terminal.WriteLine();
                Terminal.Write("ReminderConsole>");
                inputArgs = Terminal.ReadLine();
                inputArgs = inputArgs.ToLower();
                // update data
                if (inputArgs.Equals("update"))
                    UpdateData();

                // insert new data
                if (inputArgs.Equals("set"))
                    InsertReminder();

                // delete row id
                if (inputArgs.Equals("delete"))
                    DeleteReminder();

                // exit app
                if (inputArgs.Equals("exit"))
                {
                    StopAllThreads();
                    break;
                }

                // show table reminders
                if (inputArgs.Equals("showr"))
                {
                    Terminal.WriteLine("Showing reminders table\n");
                    DbOperations.ShowAllReminders(true);
                }

                // show table history reminders
                if (inputArgs.Equals("showh"))
                {
                    Terminal.WriteLine("Showing history table\n");
                    DbOperations.ShowAllHistoryReminders();
                }

                // show help
                if (inputArgs.Equals("help"))
                    HelpFunctions.ShowHelp();

                //clear screen
                if (inputArgs.Equals("cls") || inputArgs.Equals("clear"))
                    Terminal.Clear();
            }
        } // end inputfromuser() 
        #endregion

        #region Thread check DB
        static void ThreadCheckDb()
        {
            db = new DbOperations();
            // DB Creation and DB Integrity Check + Init for EF
            // First init is always very slow, but when EF is initialised, then it functions in normal speed.
            // Thats why I have put it right here, and false variable is just to not show the result to the user.
            // There are better methods than this, but I couldn't find
            DbOperations.ShowAllReminders(false);
            isThreadInitDbFinished = true;
        }
        #endregion

        #region Thread control reminders
        static void ThreadControlReminders()
        {
            // Check all reminders in reminders table and cleanup of expired reminders 
            // Wait for thread threadInitDbInitEF to finish first
            // EF needs to be initialized first.
            while (true)
            {
                if (isThreadInitDbFinished)
                    break;
            }
            // continue
            StartAllTasks();
            // TODO: cleanup
            // TODO: remove a reminder when it is finished


        }
        #endregion

        #region Stop All Threads
        static void StopAllThreads()
        {
            Terminal.WriteLine("Stopping all threads");
            try
            {
                try
                {
                    if (threadInitDbInitEF != null)
                        threadInitDbInitEF.Abort();
                    if (threadControlReminders != null)
                        threadControlReminders.Abort();
                }
                catch (Exception e)
                {
                    HelpFunctions.log.Info("Err aborting thread: " + e);
                }
                // notifier.ForceExit();
            }
            catch (Exception e)
            {
                HelpFunctions.log.Info("Err in StopAllThreads: " + e);
                throw;
            }
        }
        #endregion

        #region Insert, delete or update reminder
        #region Insert new data to reminders table
        static void InsertReminder()
        {
            DateTime dateToRemind;
            var exitFromLoop = 1;
            Terminal.WriteLine("\nInserting new reminder:");
            Terminal.Write("Text to remind: ");
            var textToRemind = Terminal.ReadLine();
            do
            {
                if (textToRemind.Length == 0)
                {
                    Terminal.WriteLine("Text is empty, try again");
                    Terminal.Write("Text to remind (Write null to exit): ");
                    textToRemind = Terminal.ReadLine();
                }
                else
                    exitFromLoop = 2;

            } while (exitFromLoop == 1);
            if (textToRemind.ToLower().Equals("null"))
            {
                // exit set
                textToRemind = string.Empty;
            }
            else
            {
                Terminal.Write("\nEnter date/time to remind (e.g. 13:45 or 18.9.2018 13:45): ");
                var textDate = Terminal.ReadLine();
                var isDateValid = DateTime.TryParse(textDate, out dateToRemind);
                do
                {
                    if (!isDateValid)
                    {
                        Terminal.Write("\nEnter correct date/time to remind (e.g. 13:45 or 18.9.2018 13:45): ");
                        textDate = Terminal.ReadLine();
                        isDateValid = DateTime.TryParse(textDate, out dateToRemind);
                    }
                    else
                        exitFromLoop = 1;

                } while (exitFromLoop == 2);
                Terminal.WriteLine("\nAdd an extra note for this reminder, like the reason why to remind?");
                Terminal.Write("If U dont need extra note, just press enter: ");
                var notes = Terminal.ReadLine();
                DbOperations.AddDataToReminders(dateToRemind, textToRemind, notes);
                RestartAllTasks();
                Terminal.WriteLine("New Reminder is set:\n" + textToRemind + "\nfor date: " + dateToRemind);
                Terminal.WriteLine("Data inserted into DB Reminders tbl");
            }
        }
        #endregion
        #region Start all tasks in threadpool
        static void StartAllTasks()
        {
            // store every reminder to list
            var reminderExamples = DbOperations.AllRemindersToList();

            // Then execute those reminders
            if (reminderExamples.Count != 0)
            {
                qt = new QueueTask();
                qt.Produce(reminderExamples);
            }
        }
        #endregion
        
        #region Restart all tasks in threadpool
        static void RestartAllTasks()
        {
            qt.StopAllTasks();
            Thread.Sleep(100);
            StartAllTasks();
        }
        #endregion

        #region Update existing data based on row id
        static void UpdateData()
        {
            DbOperations.ShowAllReminders(true);
            var msg = "Write null to exit. If old data stays the same, leave empty.";
            var spliter = new string('*', msg.Length);
            Terminal.WriteLine("\n" + spliter + "\n" + msg + "\n" + spliter);
            Terminal.Write("\nEnter reminder ID to update: ");
            string strReminderId = Terminal.ReadLine();
            var reminderId = 0;
            // if number is ok, continue with input from user
            if (int.TryParse(strReminderId, out reminderId))
            {
                Terminal.Write("Insert new date/time to remind (e.g. 13:45 or 18.9.2018 13:45): ");
                var dateWhen = Terminal.ReadLine();
                if (dateWhen.Equals("null"))
                    return;
                Terminal.Write("Insert new reminder content: ");
                var msgToRemind = Terminal.ReadLine();
                if (msgToRemind.Equals("null"))
                    return;
                Terminal.Write("Insert new note: ");
                var notes = Terminal.ReadLine();
                if (notes.Equals("null"))
                    return;
                DbOperations.UpdateDataToReminders(dateWhen, msgToRemind, notes, reminderId);
            }
            // if null then exit from input
            else if (strReminderId.ToLower().Equals("null"))
                return;
            else
                Terminal.WriteLine("Only numbers!");

        }
        #endregion

        #region Delete reminder based on Row id
        static void DeleteReminder()
        {
            DbOperations.ShowAllReminders(true);
            //var s = new stringbuilder();
            var msg = "Write null to exit. Write all to delete all records.";
            var spliter = new string('*', msg.Length);
            Terminal.WriteLine("\n" + spliter + "\n" + msg + "\n" + spliter);

            Terminal.Write("\nEnter reminder ID to delete: ");
            var strReminderId = Terminal.ReadLine();
            var reminderId = 0;
            if (int.TryParse(strReminderId, out reminderId))
                DbOperations.DeleteReminderById(reminderId);
            else if (strReminderId.ToLower().Equals("null"))
                return;
            else if (strReminderId.ToLower().Equals("all"))
                DbOperations.DeleteReminderById("all");
            else
                Terminal.WriteLine("Only numbers!");
        }
        #endregion
        #endregion








    }
}
