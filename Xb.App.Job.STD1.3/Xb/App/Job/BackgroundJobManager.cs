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
        /// Background task processor class
        /// バックグラウンドタスク処理クラス
        /// </summary>
        public class BackgroundJobManager
        {
            /// <summary>
            /// job-execution event args
            /// </summary>
            public class ExecuteEventArgs : EventArgs
            {
                /// <summary>
                /// executed action
                /// </summary>
                public Action Action { get; private set; }

                /// <summary>
                /// constructor
                /// </summary>
                /// <param name="action"></param>
                public ExecuteEventArgs(Action action)
                {
                    this.Action = action;
                }
            }

            /// <summary>
            /// job execution event handler
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            public delegate void ExecuteEventHandler(object sender, ExecuteEventArgs e);

            /// <summary>
            /// Job-Manager thread started event
            /// </summary>
            public EventHandler Started;

            /// <summary>
            /// Job executed event
            /// </summary>
            public ExecuteEventHandler Executed;

            /// <summary>
            /// Job-Manager thread ended event
            /// </summary>
            public EventHandler Ended;

            /// <summary>
            /// Job-Manager thread go to sleep event
            /// </summary>
            public EventHandler Sleep;

            /// <summary>
            /// Job-Manager thread waked up event
            /// </summary>
            public EventHandler Waked;


            /// <summary>
            /// Job-Manager Name
            /// ジョブマネージャー名称
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Job start delay
            /// ジョブ開始遅延時間(mSec)
            /// </summary>
            public int StartDelayMsec { get; set; } = 500;

            /// <summary>
            /// Job-Detection checking span
            /// ジョブ検出スパン(mSec)
            /// </summary>
            public int JobCheckSpanMsec { get; set; } = 5000;

            /// <summary>
            /// Job-Suppression checking span
            /// ジョブ実行抑止検出スパン(mSec)
            /// </summary>
            public int SuppressCheckSpanMsec { get; set; } = 5000;

            /// <summary>
            /// Whether job-manager thread residentable or not.
            /// ジョブ実行スレッドを常駐するか否か
            /// </summary>
            public bool IsResident { get; set; } = true;

            /// <summary>
            /// Whether job-manager suppressing or not.
            /// 現在ジョブ実行抑止中か否か
            /// </summary>
            public bool IsSuppressing
            {
                get
                {
                    var result = false;

                    lock (this._suppressors)
                        result = (this._suppressors.Count > 0);

                    return result;
                }
            }

            /// <summary>
            /// Whether now on running job or not.
            /// ジョブ実行スレッドが実行中か否か
            /// </summary>
            public bool IsRunning { get; private set; } = false;

            /// <summary>
            /// Job suppressing ordered object count
            /// ジョブ実行抑止指示オブジェクトの個数
            /// </summary>
            private int SuppressorCount
            {
                get
                {
                    var result = 0;

                    lock (this._suppressors)
                        result = this._suppressors.Count;

                    return result;
                }
            }

            private List<Action> _jobs = new List<Action>();
            private Dictionary<object, string> _suppressors = new Dictionary<object, string>();


            /// <summary>
            /// Constructor
            /// コンストラクタ
            /// </summary>
            /// <param name="name"></param>
            public BackgroundJobManager(string name = null)
            {
                this.Name = name ?? "JobManager-" + DateTime.Now.ToString("hhMMssfff");
            }


            /// <summary>
            /// Regist job
            /// ジョブを登録する。
            /// </summary>
            /// <param name="action"></param>
            public void Regist(Action action)
            {
                Xb.Util.Out($"BackgroundJobManager[{this.Name}].Regist - {action}");

                lock (this._jobs)
                    this._jobs.Add(action);

                if (this.IsRunning)
                    return;

                this.IsRunning = true;
                Xb.Util.Out($"BackgroundJobManager[{this.Name}].Regist - Kicked.");

                Job.DelayedRun(() =>
                {
                    try
                    {
                        Xb.Util.Out($"BackgroundJobManager[{this.Name}] - Thread Start.");

                        try { this.Started?.Invoke(this, new EventArgs()); }
                        catch (Exception) { }

                        do
                        {
                            int count = 0;
                            lock (this._jobs)
                                count = this._jobs.Count;

                            while (count > 0)
                            {
                                Xb.Util.Out($"BackgroundJobManager[{this.Name}] - Job Count: {count}");

                                while (this.IsSuppressing)
                                {
                                    Xb.Util.Out($"BackgroundJobManager[{this.Name}] - Waiting Suppress Release, "
                                              + $"Suppressors Count: {this.SuppressorCount}, "
                                              + $"Suppressed By: {this.GetSuppressorNames()}");
                                    Job.WaitSynced(this.SuppressCheckSpanMsec);
                                }

                                Action target = null;

                                lock (this._jobs)
                                    target = this._jobs[0];

                                try
                                {
                                    Xb.Util.Out($"BackgroundJobManager[{this.Name}] - Exec Job {target}");
                                    target.Invoke();
                                }
                                catch (Exception ex)
                                {
                                    Xb.Util.Out(ex);
                                }

                                try { this.Executed?.Invoke(this, new ExecuteEventArgs(target)); }
                                catch (Exception) { }

                                //処理残数を再取得
                                lock (this._jobs)
                                {
                                    this._jobs.Remove(target);
                                    count = this._jobs.Count;
                                }
                            }

                            Xb.Util.Out($"BackgroundJobManager[{this.Name}] - Job Completed.");

                            if (this.IsResident)
                            {
                                Xb.Util.Out($"BackgroundJobManager[{this.Name}] - Sleep.");

                                try { this.Sleep?.Invoke(this, new EventArgs()); }
                                catch (Exception) { }

                                //次のジョブが来るまでスリープを繰り返す。
                                while (count <= 0)
                                {
                                    Job.WaitSynced(this.JobCheckSpanMsec);

                                    lock (this._jobs)
                                        count = this._jobs.Count;
                                }

                                Xb.Util.Out($"BackgroundJobManager[{this.Name}] - Wake.");

                                try { this.Waked?.Invoke(this, new EventArgs()); }
                                catch (Exception) { }
                            }
                        }
                        while (this.IsResident);

                        this.IsRunning = false;
                        Xb.Util.Out($"BackgroundJobManager[{this.Name}] - Thread Close.");

                        try { this.Ended?.Invoke(this, new EventArgs()); }
                        catch (Exception) { }
                    }
                    catch (Exception)
                    {
                        this.IsRunning = false;
                    }
                }, this.StartDelayMsec, $"BackgroundJobManager[{this.Name}]");
            }


            /// <summary>
            /// Suppress job
            /// 処理抑止フラグをセットする。
            /// </summary>
            public void Suppress(object suppressorObject, string suppressorName = null)
            {
                try
                {
                    lock (this._suppressors)
                    {
                        if (!this._suppressors.ContainsKey(suppressorObject))
                        {
                            suppressorName = string.IsNullOrEmpty(suppressorName)
                                ? suppressorObject.GetType().Name
                                : suppressorName;

                            this._suppressors.Add(suppressorObject, suppressorName);

                            Xb.Util.Out($"BackgroundJobManager[{this.Name}].Suppress by {suppressorName}: {suppressorObject.GetType().Name}, "
                                      + $"Suppressors Count: {this.SuppressorCount} / "
                                      + $"{(this.IsSuppressing ? $"Suppressed By: {this.GetSuppressorNames()}" : "Released")}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Xb.Util.Out(ex);
                    throw;
                }
            }


            /// <summary>
            /// Release suppress
            /// 処理抑止フラグを解除する。
            /// </summary>
            public void ReleaseSuppress(object suppressorObject)
            {
                try
                {
                    lock (this._suppressors)
                    {
                        if (this._suppressors.ContainsKey(suppressorObject))
                        {
                            var suppressorName = this._suppressors[suppressorObject];
                            this._suppressors.Remove(suppressorObject);

                            Xb.Util.Out($"BackgroundJobManager[{this.Name}].ReleaseSuppress by {suppressorName}: {suppressorObject.GetType().Name}, "
                                      + $"Suppressors Count: {this.SuppressorCount} / "
                                      + $"{(this.IsSuppressing ? $"Suppressed By: {this.GetSuppressorNames()}" : "Released")}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Xb.Util.Out(ex);
                    throw;
                }
            }


            /// <summary>
            /// Whether passing-object is suppressor or not.
            /// </summary>
            /// <param name="targetObject"></param>
            /// <returns></returns>
            public bool IsSuppressorObject(object targetObject)
            {
                var result = false;

                lock (this._suppressors)
                    result = this._suppressors.ContainsKey(targetObject);

                return result;
            }


            /// <summary>
            /// Get suppressing object's Names
            /// </summary>
            /// <returns></returns>
            public string GetSuppressorNames()
            {
                var result = "";

                lock (this._suppressors)
                {
                    result = string.Join(", ", this._suppressors.Select(p => $"{p.Value}: {p.Key.GetType().Name}").ToArray());
                }

                return result;
            }
        }
    }
}
