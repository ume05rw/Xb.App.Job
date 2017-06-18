using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xb.App
{
    public partial class Job
    {
        /// <summary>
        /// Delayed-Job execution manager class
        /// </summary>
        /// <remarks>
        /// accepts multiple execution requests in delay time, and execute only one.
        /// each time an execution request is made, 
        /// the scheduled execution time is delayed and reset.
        /// </remarks>
        public class DelayedOnceJobManager : IDisposable
        {
            private const int DefaultDelayMsec = 3000;
            private const int DefaultSleepMsec = 500;
            private Action _delayedAction = null;

            /// <summary>
            /// Job execution scheduled time
            /// </summary>
            public DateTime ScheduledTime { get; private set; } = DateTime.MinValue;

            /// <summary>
            /// Whether job execution scheduled or not.
            /// </summary>
            public bool IsScheduled { get; private set; } = false;

            /// <summary>
            /// Job-Execution delay time
            /// </summary>
            public int DelayMsec { get; set; } = DefaultDelayMsec;

            /// <summary>
            /// Waiting-Sleep span
            /// </summary>
            public int SleepMsec { get; set; } = DefaultSleepMsec;


            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="delayedAction"></param>
            /// <param name="delayMsec"></param>
            public DelayedOnceJobManager(Action delayedAction
                                       , int delayMsec = DefaultDelayMsec)
            {
                this._delayedAction = delayedAction;
                this.DelayMsec = delayMsec;
            }


            /// <summary>
            /// Request execution
            /// </summary>
            public void Run()
            {
                this.ScheduledTime = DateTime.Now.AddMilliseconds(this.DelayMsec);

                if (this.IsScheduled)
                    return;

                this.IsScheduled = true;

                Job.Run(() =>
                {
                    while (this.ScheduledTime > DateTime.Now)
                        Job.WaitSynced(this.SleepMsec);

                    try
                    {
                        this._delayedAction.Invoke();
                    }
                    catch (Exception ex)
                    {
                        this.IsScheduled = false;
                        Xb.Util.Out(ex);
                        throw;
                    }

                    this.IsScheduled = false;

                }, false, "DelayedJobManager.Run").ConfigureAwait(false);
            }

            #region IDisposable Support
            private bool disposedValue = false; // 重複する呼び出しを検出するには

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        this._delayedAction = null;
                        // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                    }
                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
            #endregion
        }
    }
}
