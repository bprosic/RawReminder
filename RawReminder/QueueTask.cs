using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mischel.Synchronization;
namespace RawReminder
{
    /// <summary>
    /// This class has everything with threading and tasks ;) 
    /// </summary>
    class QueueTask
    {
        // vars for task mngmt
        // If I have more than one task, only one cancel token source is enough to cancel all tasks.
        public CancellationTokenSource CancelTokenSource = null;
        public CancellationToken CancelToken;
        public Task ExecutionTask = null;
        // I will put all tasks in a list
        public List<Task> TaskList;

        // for better control of time when reminder will be executed
        public WaitableTimer WaitTimer = null;
        // I will put all the times into 
        public List<WaitableTimer> WaitTimerList;

        // for disposing a task
        bool IsTaskAborted { get; set; }

        #region Set Task Token, Cancellation Token and init Timer and Tasks list
        public QueueTask()
        {
            CancelTokenSource = new CancellationTokenSource();
            CancelToken = CancelTokenSource.Token;

            WaitTimerList = new List<WaitableTimer>();
            TaskList = new List<Task>();
        }
        #endregion

        #region Send new Task to Task Factory
        public void Produce(List<Reminders> reminder)
        {
            ExecutionTask = Task.Factory.StartNew(() => RunSched(reminder), CancelToken);
            IsTaskAborted = false;
        }
        #endregion

        #region Run action -> schedule next reminder, delete reminder from db after showing to user
        public void RunSched(List<Reminders> reminder)
        {
            if (reminder.Count == 0)
                return;

            var dateWhen = reminder.FirstOrDefault().DateToRemind;
            var txtToRmnd = reminder.FirstOrDefault().ReminderContent;

            // calculate time when to remind and run waitable time based upon diff variable
            long diff = 0;
            if (dateWhen < DateTime.Now)
                return;

            if (dateWhen > DateTime.Now)
                diff = Convert.ToInt64((dateWhen - DateTime.Now).TotalSeconds);

            // using Waitable Timer class from Jim Mischel
            // https://stackoverflow.com/questions/18611226/c-how-to-start-a-thread-at-a-specific-time		
            // http://www.mischel.com/pubs/waitabletimer.zip

            WaitTimer = new WaitableTimer(true, TimeSpan.FromSeconds(diff), 0);
            WaitTimerList.Add(WaitTimer);

            while (!CancelToken.IsCancellationRequested)
            {
                try
                {
                    if (CancelToken.IsCancellationRequested)
                        HelpFunctions.log.Info("Cancel requested");
                    // timer here is waiting ...
                    WaitTimer.WaitOne();
                    // When time is over, execute that reminder, show reminder to a user: 
                    MessageWindow ms = new MessageWindow(txtToRmnd + " => for date: " + dateWhen);
                    HelpFunctions.log.Info(txtToRmnd + ". Reminder shown to the user on thread: " + Thread.CurrentThread.GetHashCode());
                    // Delete/Move reminder from reminder -> to history table
                    DbOperations.MoveDataFromRemindersToHistory(reminder.FirstOrDefault().ReminderId);
                }
                catch (OperationCanceledException e)
                {
                    // I will supress exception because a lot of errors are shown in console..
                    // HelpFunctions.log.Info(e);
                    // break;
                }
                finally
                {
                    // When task is executed, then dispose a task 
                    DisposeTask(true);
                }
            }
        }
        #endregion

        #region Dispose current task
        public void DisposeTask(bool dispose)
        {
            //Terminal.WriteLine("Dispose triggered...");
            try
            {
                if (!IsTaskAborted)
                {
                    if (dispose)
                    {
                        CancelTokenSource.Cancel();
                        ExecutionTask.Wait();
                        CancelTokenSource.Dispose();
                        ExecutionTask.Dispose();
                        WaitTimerList.Remove(WaitTimer);

                    }
                    IsTaskAborted = true;
                }
            }
            catch (Exception ae)
            {
                // supress task
                //Terminal.WriteLine(ae);
            }
            finally
            {
                CancelTokenSource = null;
                ExecutionTask = null;
            }
        }
        #endregion

        #region Execute STOP of all tasks!!
        public void StopAllTasks()
        {
            try
            {
                // If I want to cancel a task, there is no way until first cancel a timer.

                // Cancel first the timer
                foreach (var time in WaitTimerList)
                {
                    time.Cancel();
                }
                // Then cancel task(s).
                CancelTokenSource.Cancel();
                foreach (var task in TaskList)
                {
                    // just to be sure to dispose a task..
                    task.Dispose();
                }
            }
            catch (Exception e)
            {
                // supress except
                //Terminal.WriteLine("Exception occured" + e);
            }
        }
        #endregion

    }
}
