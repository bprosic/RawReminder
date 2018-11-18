using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mischel.Synchronization;
using Terminal = System.Console;
using System.Windows.Forms;
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
        public void Produce(DateTime dateWhen, string message, int reminderId)
        {
            // here are all reminders in list
            //ExecutionTask = Task.Factory.StartNew(() => RunSched(reminder), CancelToken);
            ExecutionTask = Task.Factory.StartNew(() => RunSched(dateWhen, message, reminderId), CancelToken);
            IsTaskAborted = false;

        }
        #endregion

        #region Run scheduler
        public void RunSched(DateTime dateWhen, string message, int reminderId)
        {
            // calculate time when to remind and run waitable time based upon diff variable
            long diff = 0;
            if (dateWhen < DateTime.Now)
                return;

            if (dateWhen > DateTime.Now)
                diff = Convert.ToInt64((dateWhen - DateTime.Now).TotalSeconds);
            // using Waitable Timer class from Jim Mischel
            WaitTimer = new WaitableTimer(true, TimeSpan.FromSeconds(diff), 0);
            WaitTimerList.Add(WaitTimer);

            while (!CancelToken.IsCancellationRequested)
            {
                try
                {
                    if (CancelToken.IsCancellationRequested)
                        Console.Write("Cancel requested");

                    // timer here is waiting ...
                    WaitTimer.WaitOne();
                    // When time is over, execute that reminder, show reminder to a user: 
                    // when scheduling using the same date-time, then two threads want to enter to the same object
                    // MessageWindow ms = new MessageWindow(message + " => for date: " + dateWhen);
                    MessageBox.Show(message + " => for date: " + dateWhen);

                    // TODO: Delete/Move reminder from reminder -> to history table
                    // remove a reminder when it is finished
                    DbOperations.MoveDataFromRemindersToHistory(reminderId);

                }
                catch (OperationCanceledException e)
                {
                    // I will supress exception because a lot of errors are shown in console..
                    Terminal.WriteLine(e);
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
            Console.WriteLine(WaitTimerList.Count);
            try
            {
                // If there are no active tasks, then exit this method
                if (WaitTimerList.Count == 0)
                    return;
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
