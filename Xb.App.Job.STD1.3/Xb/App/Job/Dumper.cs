using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Xb.App
{
    public partial class Job
    {
        /// <summary>
        /// Dump output class of Resource / Job information
        /// リソース情報／ジョブ情報のダンプ出力クラス
        /// </summary>
        public sealed class Dumper : IDisposable
        {
            //Singleton実装
            private static Job.Dumper _instance = null;

            /// <summary>
            /// Job.Dumper Instance
            /// </summary>
            public static Job.Dumper Instance => Job.Dumper._instance;

            /// <summary>
            /// Create Instance(Singleton)
            /// </summary>
            /// <returns></returns>
            private static Job.Dumper Create()
            {
                try
                {
                    return Job.Dumper._instance
                           ?? (Job.Dumper._instance = new Job.Dumper());
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
            private static void DisposeInstance()
            {
                Job.Dumper._instance?.Dispose();
                Job.Dumper._instance = null;
            }


            /// <summary>
            /// Default dump timer interval
            /// デフォルトのダンプタイマー間隔
            /// </summary>
            private const int DefaultTimerIntervalMsec = 30000;


            /// <summary>
            /// Whether it is currently in operation or not.
            /// 現在稼働中か否か
            /// </summary>
            public static bool IsWorking => (Job.Dumper._instance != null);


            /// <summary>
            /// Whether console-dump output of [Periodic Status Info] is active.
            /// リソース情報のダンプ出力が稼働中か否か
            /// </summary>
            public static bool IsDumpStatus
            {
                get { return Job.Dumper._isDumpStatus; }
                set
                {
                    Job.Dumper._isDumpStatus = value;
                    Job.Dumper.ApplyTimer();
                }
            }
            private static bool _isDumpStatus = false;


            /// <summary>
            /// Whether console-dump output of [Task Verification Info] is active.
            /// タスク検証情報のダンプ出力が稼働中か否か
            /// </summary>
            public static bool IsDumpTaskValidation
            {
                get { return Job.Dumper._isDumpTaskValidation; }
                set
                {
                    Job.Dumper._isDumpTaskValidation = value;
                    Job.Dumper.ApplyTimer();
                }
            }
            private static bool _isDumpTaskValidation = false;


            /// <summary>
            /// Control the operation of the timer from the state of the flag.
            /// タイマータスクフラグの状態から、タイマー稼働を制御する。
            /// </summary>
            private static void ApplyTimer()
            {
                if (
                    (Job.Dumper._isDumpStatus
                     || Job.Dumper._isDumpTaskValidation)
                    && !Job.Dumper.IsWorking
                )
                {
                    Job.Dumper.Create();
                }
                else if (
                    (!Job.Dumper._isDumpStatus
                     && !Job.Dumper._isDumpTaskValidation)
                    && Job.Dumper.IsWorking
                )
                {
                    Job.Dumper.DisposeInstance();
                }
            }


            #region "インスタンス実装"

            /// <summary>
            /// Time interval of dump output
            /// ダンプ出力の時間間隔
            /// </summary>
            public int TimerIntervalMsec { get; set; }


            /// <summary>
            /// Current Process instance
            /// カレントプロセスのProcessインスタンス
            /// </summary>
            /// <remarks>
            /// For acquiring memory / thread information
            /// メモリ／スレッド情報取得用
            /// </remarks>
            private System.Diagnostics.Process Process
            {
                get
                {
                    try
                    {
                        return this._process
                               ?? (this._process = System.Diagnostics.Process.GetCurrentProcess());
                    }
                    catch (Exception ex)
                    {
                        Xb.Util.Out(ex);
                        throw;
                    }
                }
            }

            private System.Diagnostics.Process _process = null;


            /// <summary>
            /// Constructor
            /// コンストラクタ
            /// </summary>
            private Dumper(int timerSpanMsec = Job.Dumper.DefaultTimerIntervalMsec)
            {
                this.SetTimerInterval(timerSpanMsec);

                this.TimerExec();
            }

            /// <summary>
            /// Set the execution interval of periodic staus dump / task verification processing.
            /// ステータス情報定期ダンプ／タスク検証処理の実行間隔をセットする。
            /// </summary>
            /// <param name="msec"></param>
            public void SetTimerInterval(int msec)
            {
                this.TimerIntervalMsec = (msec <= 0)
                    ? Job.Dumper.DefaultTimerIntervalMsec
                    : msec;
            }

            /// <summary>
            /// Perform timer processing for monitoring.
            /// 監視用のタイマー処理を実行する。
            /// </summary>
            private void TimerExec()
            {
                ////定期監視ジョブ自体が、終了しないジョブとしてリストアップされてしまうため
                ////Job.RunでなくTaskを直接使う。
                //↑見えた方がいいと思うので、これをやめる。
                Job.Run(() =>
                {
                    while (Job.Dumper.IsWorking)
                    {
                        try
                        {
                            //ステータス出力
                            this.Dump();
                            Job.WaitSynced(this.TimerIntervalMsec);
                        }
                        catch (Exception)
                        {
                            //throw;
                            //何が有っても動じない！！
                        }
                    }
                });
            }


            /// <summary>
            /// Perform status output / task verification processing.
            /// ステータス出力／タスク検証処理を行う。
            /// </summary>
            /// <returns></returns>
            private void Dump()
            {
                try
                {
                    //定期出力とタスク検証が、どちらも出力内容が無いとき、
                    //なにもしないで終わる。
                    if (!Job.Dumper.IsDumpStatus
                        && (Job.Monitor.Instance == null
                            || !Job.Dumper.IsDumpTaskValidation))
                        return;


                    var infoDump = new string[] { };
                    var suspiciousDump = new string[] { };

                    //稼働中タスク情報フラグがONのとき、取得する。
                    if (Job.Dumper.IsDumpStatus && Job.Monitor.Instance != null)
                        infoDump = Job.Monitor.Instance.GetStatus();

                    //タスク検証情報フラグがONのとき、取得する。
                    if (Job.Dumper.IsDumpTaskValidation && Job.Monitor.Instance != null)
                        suspiciousDump = Job.Monitor.Instance.GetValidation();

                    //出力対象が無い場合、なにもしないで終わる。
                    if (!Job.Dumper.IsDumpStatus 
                        && suspiciousDump.Length <= 0)
                        return;

                    //状態ダンプを開始
                    var dumpLines = new List<string>();

                    //システム情報を取得してセット
                    dumpLines.AddRange(this.GetSystemStatusDump());

                    //稼働中タスク情報をセット
                    if (infoDump.Length > 0)
                        dumpLines.AddRange(infoDump);

                    //タスク検証情報をセット
                    if (suspiciousDump.Length > 0)
                        dumpLines.AddRange(suspiciousDump);

                    //出力する。
                    Xb.Util.Out(Xb.Util.GetHighlighted(dumpLines.ToArray()));
                }
                catch (Exception ex)
                {
                    Xb.Util.Out(ex);
                    throw;
                }
            }


            /// <summary>
            /// Get system status string array.
            /// システム状態状況を取得し、文字列配列で返す。
            /// </summary>
            /// <returns></returns>
            /// <remarks>
            /// </remarks>
            private string[] GetSystemStatusDump()
            {
                try
                {
                    var proc = this.Process;
                    var phyMem = (double)proc.WorkingSet64 / 1024 / 1024; //(MB)
                    var virMem = (double)proc.VirtualMemorySize64 / 1024 / 1024; //(MB)
                    var ppgMem = (double)proc.PeakPagedMemorySize64 / 1024 / 1024; //(MB)


                    //ThreadPool.GetAvailableThreads は、.Net Standard 2.0で実装予定らしい。
                    //2.0移行後に復旧したい。
                    //https://apisof.net/catalog/System.Threading.ThreadPool.GetAvailableThreads(Int32,Int32)


                    //var availableWorkerThreads = 0;
                    //var availableCompletionPortThreads = 0;
                    //var maxWorkerThreads = 0;
                    //var maxCompletionPortThreads = 0;
                    //ThreadPool.GetAvailableThreads(
                    //    out availableWorkerThreads
                    //    , out availableCompletionPortThreads
                    //);
                    //ThreadPool.GetMaxThreads(
                    //    out maxWorkerThreads
                    //    , out maxCompletionPortThreads
                    //);

                    var dumpLines = new List<string>
                    {
                        $"------------------------------------------------------------",
                        $"- System Status",
                        $"PhysicalMemory: {phyMem.ToString("F2").PadLeft(7)} MB / VirtualMemory: {virMem.ToString("F2").PadLeft(7)} MB / PeakPagedmemory: {ppgMem.ToString("F2").PadLeft(7)} MB",
                        $"UI Thread ID  : {Job._uiThreadId.ToString().PadLeft(4)} "
                    };

                    // AvailableWTs  :利用可能なワーカースレッド数
                    // MaxWTs        :最大ワーカースレッド数
                    // AvailableCPTs :利用可能な非同期IOスレッド数
                    // MaxCPTs       :最大非同期IOスレッド数
                    //$"AvailableWTs  : {availableWorkerThreads.ToString().PadLeft(4)}       /       MaxWTs : {maxWorkerThreads.ToString().PadLeft(4)}",
                    //$"AvailableCPTs : {availableCompletionPortThreads.ToString().PadLeft(4)}       /       MaxCPTs: {maxCompletionPortThreads.ToString().PadLeft(4)}",

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
                        this._process = null;
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