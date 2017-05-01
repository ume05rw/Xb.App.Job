using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace Xb.App
{
    public partial class Job
    {
        /// <summary>
        /// Job Manager Class
        /// Job情報管理クラス
        /// </summary>
        public sealed class InfoStore : IDisposable
        {
            //Singleton実装
            private static Job.InfoStore _instance;

            /// <summary>
            /// Job.InfoStore Instance
            /// </summary>
            public static Job.InfoStore Instance => Job.InfoStore._instance;

            /// <summary>
            /// Create Instance(Singleton)
            /// </summary>
            /// <param name="isWorkingJobOnly"></param>
            /// <returns></returns>
            public static Job.InfoStore Create(bool isWorkingJobOnly = true)
            {
                try
                {
                    return Job.InfoStore._instance 
                           ?? (Job.InfoStore._instance = new Job.InfoStore(isWorkingJobOnly));
                }
                catch (Exception ex)
                {
                    Xb.Util.Out(ex);
                    throw;
                }
            }

            /// <summary>
            /// Dispose Instance
            /// </summary>
            public static void DisposeInstance()
            {
                Job.InfoStore._instance?.Dispose();
                Job.InfoStore._instance = null;
            }


            /// <summary>
            /// Whether it is currently in operation or not.
            /// 現在稼働中か否か
            /// </summary>
            public static bool IsWorking
            {
                get { return (Job.InfoStore.Instance != null); }
                set
                {
                    if (value && Job.InfoStore.Instance == null)
                    {
                        Job.InfoStore.Create();
                    }
                    else if (!value && Job.InfoStore.Instance != null)
                    {
                        Job.InfoStore.DisposeInstance();
                    }
                }
            }


            /// <summary>
            /// Class for exclusive control
            /// lock構文用排他制御クラス
            /// </summary>
            private class Locker
            {
                public bool IsLocked { get; set; } = false;
            }


            #region "インスタンス実装"

            /// <summary>
            /// lock-object for exclusive control
            /// ジョブ追加／削除時等のロックオブジェクト
            /// </summary>
            private Job.InfoStore.Locker _locker = new Job.InfoStore.Locker();

            /// <summary>
            /// Job.Info Array
            /// ジョブ開始番号に対するInfoクラスの連想配列
            /// </summary>
            private Dictionary<long, Job.Info> _jobs = new Dictionary<long, Job.Info>();

            /// <summary>
            /// Number of started jobs
            /// 開始したジョブ数
            /// </summary>
            public long Started { get; private set; } = 0;

            /// <summary>
            /// Number of jobs ended(Include abnormally terminated jobs)
            /// 終了したジョブ数(異常終了したジョブも含まれる)
            /// </summary>
            public long Ended { get; private set; } = 0;

            /// <summary>
            /// Number of active jobs
            /// 稼働中のジョブ数
            /// </summary>
            public long OnWork => this.Started - this.Ended;

            /// <summary>
            /// Whether to not manage finished jobs.
            /// 稼働中ジョブ以外は監視しないか否か
            /// </summary>
            /// <remarks>
            /// true  :Discard the Info object of the finished job.(default)
            ///       :終了したジョブのInfoオブジェクトを破棄する。
            /// false :Holds the Info object of the finished job information. When you want to see the transition of all job information.
            ///       :終了したジョブ情報のInfoオブジェクトを保持する。ジョブ情報の推移を見たいときなどに。
            /// </remarks>
            public bool IsWorkingJobOnly { get; private set; }



            /// <summary>
            /// Constructor
            /// コンストラクタ
            /// </summary>
            /// <param name="isWorkingJobOnly"></param>
            private InfoStore(bool isWorkingJobOnly = true)
            {
                this.IsWorkingJobOnly = isWorkingJobOnly;
            }

            /// <summary>
            /// Generate job information and place it under control.
            /// ジョブ情報を生成し、管理下に置く。(Returns the job start number)
            /// </summary>
            /// <param name="name"></param>
            /// <param name="calledClassName"></param>
            /// <returns>ジョブ開始番号</returns>
            public long Start(string name, string calledClassName)
            {
                lock (this._locker)
                {
                    try
                    {
                        this._locker.IsLocked = true;
                        this.Started++;
                        var info = new Job.Info(name ?? "", calledClassName ?? "");

                        lock (this._jobs)
                            this._jobs.Add(info.JobId, info);

                        Xb.Util.Out($"Job.Run Created - {info.State}");

                        this._locker.IsLocked = false;
                        return info.JobId;
                    }
                    catch (Exception ex)
                    {
                        this._locker.IsLocked = false;
                        Xb.Util.Out(ex);
                        throw;
                    }
                }
            }


            /// <summary>
            /// Set the thread ID to the job
            /// ジョブにスレッドIDをセットする。
            /// </summary>
            /// <param name="jobId"></param>
            /// <remarks>execute it after thread generated. 必ず生成後のスレッド内で実行すること。</remarks>
            public void SetThreadId(long? jobId)
            {
                try
                {
                    if (jobId == null)
                        return;

                    var idx = (long)jobId;

                    lock (this._jobs)
                    {
                        if (this._jobs.ContainsKey(idx))
                        {
                            var info = this._jobs[idx];
                            info.SetThreadId(Thread.CurrentThread.ManagedThreadId);

                            Xb.Util.Out($"Job.Run Started - {info.State}");
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
            /// Add an end record to job information.
            /// 渡し値ジョブ監視番号のジョブ情報に終了記録を付ける。
            /// </summary>
            /// <param name="jobId"></param>
            public void End(long? jobId)
            {
                try
                {
                    this.Ended++;

                    if (jobId == null)
                    {
                        Xb.Util.Out($"Job.Run Ended, BUT JOB_ID IS NULL...?");
                        return;
                    }

                    var idx = (long)jobId;

                    lock (this._jobs)
                    {
                        if (this._jobs.ContainsKey(idx))
                        {
                            var info = this._jobs[idx];
                            Xb.Util.Out($"Job.Run Ended   - {info.State}");

                            if (!this.IsWorkingJobOnly)
                            {
                                this._jobs.Remove(idx);
                                info.Dispose();
                            }
                        }
                        else
                        {
                            Xb.Util.Out($"Job.Run Ended, BUT JOB_ID NOT FOUND...? [{idx}]");
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
            /// Add an end record to job information.(Abnormal termination)
            /// 渡し値ジョブ監視番号のジョブ情報に終了記録を付ける。(異常終了時)
            /// </summary>
            /// <param name="jobId"></param>
            public void ErrorEnd(long? jobId)
            {
                try
                {
                    this.Ended++;

                    if (jobId == null)
                    {
                        Xb.Util.Out($"Job.Run ### ERROR-END ###, BUT JOB_ID IS NULL...?");
                        return;
                    }

                    var idx = (long)jobId;

                    lock (this._jobs)
                    {
                        if (this._jobs.ContainsKey(idx))
                        {
                            var info = this._jobs[idx];
                            Xb.Util.Out($"Job.Run ### ERROR-END ### {info.State}");

                            if (!this.IsWorkingJobOnly)
                            {
                                this._jobs.Remove(idx);
                                info.Dispose();
                            }
                        }
                        else
                        {
                            Xb.Util.Out($"Job.Run ### ERROR-END ###, BUT JOB_ID NOT FOUND...? [{idx}]");
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
            /// Generates string array for job state.
            /// ジョブ状態ダンプ用の文字列配列を生成する。
            /// </summary>
            /// <param name="isOnWorkOnly">Filter it ended jobs. 稼働中ジョブのみに絞る</param>
            /// <returns></returns>
            public string[] GetInfoDump(bool isOnWorkOnly = true)
            {
                try
                {
                    var dumpLines = new List<string>
                    {
                        $"------------------------------------------------------------",
                        $"- Job Infos ",
                        $"OnWork: {this.OnWork},  Started: {this.Started},  Ended: {this.Ended}"
                    };

                    IOrderedEnumerable<KeyValuePair<long, Job.Info>> jobStateLines;
                    lock (this._jobs)
                    {
                        //条件メモ
                        // 前者：終了ジョブを破棄する設定のときか、もしくは稼働中ジョブ絞り込みフラグが渡された場合
                        // 後者：ジョブ履歴を保持する設定か、もしくは全ジョブ情報取得を指定されているとき
                        jobStateLines = (this.IsWorkingJobOnly || isOnWorkOnly)
                            ? this._jobs.Where(p => !p.Value.IsEnded)
                                        .OrderBy(p => p.Value.JobId)
                            : this._jobs.OrderBy(p => p.Value.JobId);
                    }

                    if (jobStateLines.Any())
                    {
                        dumpLines.Add("");
                        dumpLines.AddRange(jobStateLines.Select(p => $"{p.Value.State}"));
                    }
                    return dumpLines.ToArray();
                }
                catch (Exception ex)
                {
                    Xb.Util.Out(ex);
                    throw;
                }
            }


            /// <summary>
            /// Generate string array of task verification.
            /// タスク検証結果の文字列配列を生成する。
            /// </summary>
            /// <returns></returns>
            public string[] GetSuspiciousDump()
            {
                try
                {
                    //稼働中のタスクを取得
                    IEnumerable<Job.Info> jobs;
                    lock (this._jobs)
                    {
                        jobs = this._jobs.Where(p => !p.Value.IsEnded)
                                         .Select(p => p.Value);
                    }

                    var baseTime = DateTime.Now;

                    //--デッドロック疑いのタスク抽出処理--
                    //同一スレッドで実行中であり、開始から30秒以上経過しているものを列挙する。

                    //閾値時刻は、現時刻の30秒前
                    var deadlockLimitTime = baseTime.AddSeconds(-30);

                    //スレッドID取得済み、かつ閾値時刻より前に開始したタスクを取得
                    var deadlockTargets = jobs.Where(j => j.ThreadId != -1
                                                     && j.StartTime <= deadlockLimitTime);

                    //タスクのスレッドIDをキー配列とする。
                    var keys = deadlockTargets.GroupBy(j => j.ThreadId).Select(group => group.Key);

                    //デッドロックと思しきタスク情報を文字列配列化する。
                    var deadlockLines = new List<string>();
                    foreach (var key in keys)
                    {
                        var infos = deadlockTargets.Where(j => j.ThreadId == key)
                                                      .OrderBy(j => j.StartTime);
                        if (infos.Count() <= 1)
                            continue;

                        deadlockLines.AddRange(new string[]
                        {
                            $"",
                            $"- Thread {key.ToString().PadLeft(5)} Seems DeadLock?"
                        });
                        deadlockLines.AddRange(infos.Select(j => $"     {j.State}"));
                    }

                    //出力データが存在するとき、ヘッダを先頭に付ける。
                    if (deadlockLines.Any())
                    {
                        deadlockLines.InsertRange(0, new[]
                        {
                            "",
                            $"------------------------------------------------------------",
                            $"- Deadlock Tasks ",
                        });
                    }


                    //--時間超過タスク抽出処理--
                    //開始から1分以上経過しているものを列挙する。

                    //閾値時刻は、現時刻の1分前
                    var heavyLimitTime = baseTime.AddSeconds(-60);

                    //デッドロック候補のタスクからさらに、閾値時刻以上経過しているものを取得
                    var heavyTargets = deadlockTargets.Where(j => j.StartTime <= heavyLimitTime)
                                                      .OrderBy(j => j.StartTime);

                    //開始から1分以上経過している情報を文字列配列化する。
                    var heavyLines = new List<string>() { };
                    if (heavyTargets.Any())
                    {
                        heavyLines.AddRange(new[]
                        {
                            $"",
                            $"------------------------------------------------------------",
                            $"- Heavy Tasks",
                        });
                        heavyLines.AddRange(
                            heavyTargets.Select(j => $"     { (baseTime - j.StartTime).TotalSeconds.ToString("#").PadRight(5)  } sec  - {j.State}")
                        );
                    }


                    //--最終出力--
                    //デッドロック疑い、もしくは長時間タスク情報があるときのみ、文字列を書き出す。
                    var dumpLines = new List<string>();
                    if (deadlockLines.Count > 0
                        || heavyLines.Count > 0)
                    {
                        dumpLines.AddRange(deadlockLines);
                        dumpLines.AddRange(heavyLines);
                    }

                    return dumpLines.ToArray();
                }
                catch (Exception ex)
                {
                    Xb.Util.Out(ex);
                    throw;
                }
            }

            #region IDisposable Support
            private bool disposedValue = false; // 重複する呼び出しを検出するには

            private void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        try
                        {
                            lock (this._jobs)
                            {
                                foreach (var pair in this._jobs)
                                    pair.Value.Dispose();

                                this._jobs.Clear();
                                this._jobs = null;
                            }

                            this._locker = null;
                        }
                        catch (Exception ex)
                        {
                            Xb.Util.Out(ex);
                            throw;
                        }
                    }
                    disposedValue = true;
                }
            }

            /// <summary>
            /// Dispose
            /// 破棄する。
            /// </summary>
            public void Dispose()
            {
                this.Dispose(true);
            }
            #endregion

            #endregion
        }
    }
}