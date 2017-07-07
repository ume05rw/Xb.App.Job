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
            private Action _delayedAction = null;

            /// <summary>
            /// Job execution scheduled time
            /// </summary>
            public DateTime ScheduledTime { get; private set; } = DateTime.MinValue;

            /// <summary>
            /// Maximum delay limit time
            /// </summary>
            /// <remarks>
            /// When MaxDelayMsec is greater than zero, 
            /// the action is forcibly executed when this value is exceeded.
            /// 
            /// When MaxDelayMsec is less than zero(default), this property is disabled.
            /// </remarks>
            public DateTime ScheduleLimitedTime { get; private set; } = DateTime.MinValue;

            /// <summary>
            /// Whether job execution scheduled or not.
            /// </summary>
            public bool IsScheduled { get; private set; } = false;

            /// <summary>
            /// Job-Execution delay time
            /// </summary>
            public int DelayMsec { get; set; } = DefaultDelayMsec;

            /// <summary>
            /// Maximun delay time
            /// </summary>
            /// <remarks>
            /// When this value is greater than zero, 
            /// the action is forcibly executed when ScheduleLimitedTime is exceeded.
            /// 
            /// When this value is less than zero(default), the maximum delay limit is disabled.
            /// At this time, if you continue to run in small increments, 
            /// the action will not be executed.
            /// </remarks>
            public int MaxDelayMsec { get; set; } = 0;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="delayedAction"></param>
            /// <param name="delayMsec"></param>
            /// <param name="maxDelayMsec"></param>
            public DelayedOnceJobManager(Action delayedAction
                                       , int delayMsec = DefaultDelayMsec
                                       , int maxDelayMsec = 0)
            {
                this._delayedAction = delayedAction;
                this.DelayMsec = delayMsec;
                this.MaxDelayMsec = maxDelayMsec;
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

                if (this.MaxDelayMsec > 0)
                    this.ScheduleLimitedTime = DateTime.Now.AddMilliseconds(this.MaxDelayMsec);

                Job.Run(() =>
                {
                    while (true)
                    {
                        //最後にスケジュールされた時刻を過ぎたとき
                        if (this.ScheduledTime <= DateTime.Now)
                            break;

                        //最大遅延時間設定時、かつ最大遅延スケジュール時刻を過ぎたとき
                        if (this.MaxDelayMsec > 0
                            && this.ScheduleLimitedTime <= DateTime.Now)
                            break;

                        //最終スケジュール時刻と最大遅延スケジュール時刻の、小さい方を
                        //次回検証時刻にする。
                        var wakeTime = this.ScheduledTime;
                        if (this.MaxDelayMsec > 0
                            && this.ScheduledTime > this.ScheduleLimitedTime)
                            wakeTime = this.ScheduleLimitedTime;

                        //次回検証時刻までの間の時間をMsecで取得。
                        var sleepMsec = (int)(wakeTime - DateTime.Now).TotalMilliseconds + 10;

                        Job.WaitSynced(sleepMsec);
                    }

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
