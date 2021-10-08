using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Xb.App
{
    /// <summary>
    /// Thread.Task.Run Replacement method implementation.
    /// Thread.Task.Run 置換メソッド実装
    /// </summary>
    public partial class Job
    {
        #region "Setting & Parameters"

        /// <summary>
        /// UI-Thread TaskScheduler
        /// UIスレッドのタスクスケジューラ
        /// </summary>
        private static TaskScheduler _uiTaskScheduler = null;

        /// <summary>
        /// UI-Thread ID
        /// UIスレッドID
        /// </summary>
        private static int _uiThreadId = -1;


        /// <summary>
        /// Initialize
        /// 初期化処理
        /// </summary>
        /// <param name="isMonitorEnabled"></param>
        /// <param name="isDumpStatus"></param>
        /// <param name="isDumpTaskValidation"></param>
        /// <remarks>
        /// ** MAKE SURE to execute this with UI-THREAD. **
        /// </remarks>
        public static void Init(
            bool isMonitorEnabled = false,
            bool isDumpStatus = false,
            bool isDumpTaskValidation = false
        )
        {
            try
            {
                //Get UI-Thread infomation
                Job._uiThreadId = Environment.CurrentManagedThreadId;

                Job._uiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
                Xb.Util.Out($"UI Thread ID = {Job._uiThreadId}");

                Job.IsMonitorEnabled = isMonitorEnabled;
                Job.IsDumpStatus = isDumpStatus;
                Job.IsDumpTaskValidation = isDumpTaskValidation;
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Whether the current thread is a UI thread.
        /// カレントスレッドがUIスレッドか否かを判定する。
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// ** Before executing this method, MAKE SURE to execute [ Xb.App.Job.Init() ] with UI-THREAD. **
        /// ** NOT SUPPORTED: UWP Multi-ApplicationView **
        /// </remarks>
        public static bool IsUIThread
        {
            get
            {
                try
                {
                    if (Job._uiThreadId == -1)
                        throw new InvalidOperationException("Exec [ Xb.App.Job.Init() ] with UI-Thread first.");

                    return (Environment.CurrentManagedThreadId == Job._uiThreadId);
                }
                catch (Exception ex)
                {
                    Xb.Util.Out(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Whether Job Monitor is active.
        /// ジョブ情報管理が、現在稼働中か否か
        /// </summary>
        public static bool IsMonitorEnabled
        {
            get => Job.Monitor.IsWorking;
            set => Job.Monitor.IsWorking = value;
        }

        /// <summary>
        /// Whether console-dump output of [Periodic Status Info] is active.
        /// ステータス情報の定期ダンプ出力が稼働中か否か
        /// </summary>
        public static bool IsDumpStatus
        {
            get => Job.Dumper.IsDumpStatus;
            set => Job.Dumper.IsDumpStatus = value;
        }

        /// <summary>
        /// Whether console-dump output of [Task Varidation Info] is active.
        /// タスク検証情報のダンプ出力が稼働中か否か
        /// </summary>
        public static bool IsDumpTaskValidation
        {
            get => Job.Dumper.IsDumpTaskValidation;
            set => Job.Dumper.IsDumpTaskValidation = value;
        }

        /// <summary>
        /// execution interval of periodic staus dump / task verification processing.
        /// ステータス情報定期ダンプ／タスク検証処理の実行間隔
        /// </summary>
        public static int TimerIntervalMsec
        {
            get => Job.Dumper.IsWorking
                ? Job.Dumper.Instance.TimerIntervalMsec
                : -1;
            set
            {
                if (!Job.Dumper.IsWorking)
                    throw new InvalidOperationException("Console-Dump Timer is not working.");

                if (value < 1000)
                    throw new ArgumentException("Too Tiny Interval");

                Job.Dumper.Instance.TimerIntervalMsec = value;
            }
        }

        #endregion


        #region "for Single Task Processing"


        /// <summary>
        /// Execute a job without return value.
        /// 戻り値の無いジョブを実行する。
        /// </summary>
        /// <param name="action"></param>
        /// <param name="isExecUiThread"></param>
        /// <param name="jobName"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public static async Task Run(
            Action action,
            bool isExecUiThread,
            string jobName = null,
            CancellationToken? cancelToken = null
        )
        {
            try
            {
                //キャンセル指示済のとき、何も実行せず終了する。
                if (
                    cancelToken != null
                    && ((CancellationToken)cancelToken).IsCancellationRequested
                )
                {
                    return;
                }

                var callerName = action.Target?.GetType().Name ?? string.Empty;
                var startId = Job.Monitor.Instance?.Start(jobName, callerName);

                try
                {
                    if (isExecUiThread && Job.IsUIThread)
                    {
                        //UIスレッド指定で、かつ現在UIスレッドのとき、そのまま実行する。
                        try
                        {
                            Job.Monitor.Instance?.SetThreadId(startId);
                            action.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Xb.Util.Out(ex);
                            Job.Monitor.Instance?.ErrorEnd(startId);
                            throw;
                        }
                        Job.Monitor.Instance?.End(startId);

                        return;
                    }

                    var innerAction = new Action(() =>
                    {
                        //キャンセル指示済のとき、何も実行せず終了する。
                        if (
                            cancelToken != null
                            && ((CancellationToken)cancelToken).IsCancellationRequested
                        )
                        {
                            return;
                        }

                        try
                        {
                            Job.Monitor.Instance?.SetThreadId(startId);
                            action.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Xb.Util.Out(ex);
                            Job.Monitor.Instance?.ErrorEnd(startId);
                            throw;
                        }
                    });

                    //Task.Startは、スケジュールが必要な場合以外は使用しない。
                    //※UIスレッドが開いているとき、UIスレッドを使ってしまう現象があった。
                    //https://blogs.msdn.microsoft.com/pfxteam/2010/06/13/task-factory-startnew-vs-new-task-start/
                    if (isExecUiThread)
                    {
                        var task = (cancelToken == null)
                            ? new Task(innerAction)
                            : new Task(innerAction, (CancellationToken)cancelToken);
                        task.Start(Job._uiTaskScheduler);

                        await task.ConfigureAwait(false);
                    }
                    else
                    {
                        var task = (cancelToken == null)
                            ? Task.Run(innerAction)
                            : Task.Run(innerAction, (CancellationToken)cancelToken);

                        await task.ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Xb.Util.Out(ex);
                    throw;
                }

                Job.Monitor.Instance?.End(startId);
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Execute a job with return value.
        /// 戻り値付きジョブを実行する。
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="isExecUiThread"></param>
        /// <param name="jobName"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public static async Task<TResult> Run<TResult>(
            Func<TResult> func,
            bool isExecUiThread,
            string jobName = null,
            CancellationToken? cancelToken = null
        )
        {
            try
            {
                //キャンセル指示済のとき、何も実行せず終了する。
                if (
                    cancelToken != null
                    && ((CancellationToken)cancelToken).IsCancellationRequested
                )
                {
                    return default;
                }

                var callerName = func.Target?.GetType().Name ?? string.Empty;
                var startId = Job.Monitor.Instance?.Start(jobName, callerName);

                var result = default(TResult);
                try
                {
                    if (isExecUiThread && Job.IsUIThread)
                    {
                        //UIスレッド指定で、かつ現在UIスレッドのとき、そのまま実行する。
                        try
                        {
                            Job.Monitor.Instance?.SetThreadId(startId);
                            result = func.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Xb.Util.Out(ex);
                            Job.Monitor.Instance?.ErrorEnd(startId);
                            throw;
                        }
                        Job.Monitor.Instance?.End(startId);
                        return result;
                    }

                    var innerAction = new Func<TResult>(() =>
                    {
                        //キャンセル指示済のとき、何も実行せず終了する。
                        if (
                            cancelToken != null
                            && ((CancellationToken)cancelToken).IsCancellationRequested
                        )
                        {
                            return default;
                        }

                        try
                        {
                            Job.Monitor.Instance?.SetThreadId(startId);
                            return func.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Xb.Util.Out(ex);
                            Job.Monitor.Instance?.ErrorEnd(startId);
                            throw;
                        }
                    });

                    //Task.Startは、スケジュールが必要な場合以外は使用しない。
                    //※UIスレッドが開いているとき、UIスレッドを使ってしまう現象があった。
                    //https://blogs.msdn.microsoft.com/pfxteam/2010/06/13/task-factory-startnew-vs-new-task-start/
                    if (isExecUiThread)
                    {
                        var task = (cancelToken == null)
                            ? new Task<TResult>(innerAction)
                            : new Task<TResult>(innerAction, (CancellationToken)cancelToken);
                        task.Start(Job._uiTaskScheduler);

                        result = await task.ConfigureAwait(false);
                    }
                    else
                    {
                        var task = (cancelToken == null)
                            ? Task.Run(innerAction)
                            : Task.Run(innerAction, (CancellationToken)cancelToken);

                        result = await task.ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Xb.Util.Out(ex);
                    throw;
                }

                Job.Monitor.Instance?.End(startId);

                return result;
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Execute the passed Action asynchronously on Non-UI-Thread.
        /// 渡し値Actionを非UIスレッドで非同期実行する。
        /// </summary>
        /// <param name="action"></param>
        /// <param name="jobName"></param>
        public static Task Run(Action action, string jobName = null)
        {
            try
            {
                var task = Job.Run(action, false, jobName);
                _ = task.ConfigureAwait(false);

                return task;
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        /// <summary>
        /// Execute the passed Action asynchronously on Non-UI-Thread.
        /// 渡し値Actionを非UIスレッドで非同期実行する。
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="jobName"></param>
        public static Task<TResult> Run<TResult>(Func<TResult> func, string jobName = null)
        {
            try
            {
                var task = Job.Run(func, false, jobName);
                _ = task.ConfigureAwait(false);

                return task;
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        /// <summary>
        /// Execute the passed Action asynchronously on UI-Thread.
        /// 渡し値ActionをUIスレッドで非同期実行する。
        /// </summary>
        /// <param name="action"></param>
        /// <param name="jobName"></param>
        /// <returns></returns>
        public static Task RunUI(Action action, string jobName = null)
        {
            try
            {
                var task = Job.Run(action, true, jobName);
                _ = task.ConfigureAwait(false);

                return task;
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        /// <summary>
        /// Execute the passed Action asynchronously on UI-Thread.
        /// 渡し値ActionをUIスレッドで非同期実行する。
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="jobName"></param>
        /// <returns></returns>
        public static Task<TResult> RunUI<TResult>(Func<TResult> func, string jobName = null)
        {
            try
            {
                var task = Job.Run(func, true, jobName);
                _ = task.ConfigureAwait(false);

                return task;
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Synchronously Execute the passed Action on Non-UI-Thread.
        /// 渡し値Actionを非UIスレッドで同期的に実行する。
        /// </summary>
        /// <param name="action"></param>
        /// <param name="jobName"></param>
        public static void RunSynced(Action action, string jobName = null)
        {
            try
            {
                Job.Run(action, false, jobName)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        /// <summary>
        /// Synchronously Execute the passed Action on Non-UI-Thread.
        /// 渡し値Actionを非UIスレッドで同期的に実行する。
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="jobName"></param>
        public static TResult RunSynced<TResult>(Func<TResult> func, string jobName = null)
        {
            try
            {
                return Job.Run(func, false, jobName)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Synchronously Execute the passed Action on Non-UI-Thread.
        /// 渡し値Actionを、UIスレッドで同期的に実行する。
        /// </summary>
        /// <param name="action"></param>
        /// <param name="jobName"></param>
        public static void RunUISynced(Action action, string jobName = null)
        {
            try
            {
                Job.Run(action, true, jobName)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        /// <summary>
        /// Synchronously Execute the passed Action on Non-UI-Thread.
        /// 渡し値Actionを、UIスレッドで同期的に実行する。
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="jobName"></param>
        public static TResult RunUISynced<TResult>(Func<TResult> func, string jobName = null)
        {
            try
            {
                return Job.Run(func, true, jobName)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Wait for specified milliseconds.
        /// 指定ミリ秒待つ
        /// </summary>
        /// <param name="msec"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public static async Task Wait(
            int msec,
            CancellationToken? cancelToken = null
        )
        {
            try
            {
                //キャンセル指示済のとき、何も実行せず終了する。
                if (
                    cancelToken != null
                    && ((CancellationToken)cancelToken).IsCancellationRequested
                )
                {
                    return;
                }

                var task = (cancelToken == null)
                    ? Task.Delay(msec)
                    : Task.Delay(msec, (CancellationToken)cancelToken);

                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Synchronously Wait for specified milliseconds.
        /// 指定ミリ秒を同期的に待つ
        /// </summary>
        /// <param name="msec"></param>
        /// <param name="cancelToken"></param>
        public static void WaitSynced(
            int msec,
            CancellationToken? cancelToken = null
        )
        {
            try
            {
                Job.Wait(msec, cancelToken)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Delayed Execute the passed Action asynchronously on Non-UI-Thread.
        /// 渡し値Actionを非UIスレッドで遅延つき非同期実行する。
        /// </summary>
        /// <param name="action"></param>
        /// <param name="msec"></param>
        /// <param name="jobName"></param>
        public static Task DelayedRun(
            Action action,
            int msec,
            string jobName = null
        )
        {
            try
            {
                var task = Job.RunSerial(
                    Job.CreateDelay(msec),
                    Job.CreateJob(action, false, jobName)
                );
                _ = task.ConfigureAwait(false);

                return task;
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Delayed Execute the passed Action asynchronously on UI-Thread.
        /// 渡し値ActionをUIスレッドで非同期実行する。
        /// </summary>
        /// <param name="action"></param>
        /// <param name="msec"></param>
        /// <param name="jobName"></param>
        /// <returns></returns>
        public static Task DelayedRunUI(
            Action action,
            int msec,
            string jobName = null
        )
        {
            try
            {
                var task = Job.RunSerial(
                    Job.CreateDelay(msec),
                    Job.CreateJob(action, true, jobName)
                );
                _ = task.ConfigureAwait(false);

                return task;
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        #endregion


        #region "for Serial Tasks Processing"

        /// <summary>
        /// Job Logic
        /// </summary>
        public Action Action { get; private set; }

        /// <summary>
        /// Whether it is necessary to run in the UI-Thread.
        /// </summary>
        public bool IsExecUIThread { get; private set; }

        /// <summary>
        /// Start Delay
        /// </summary>
        public int DelayMSec { get; private set; }

        /// <summary>
        /// Name of Info in Job-Manager.
        /// </summary>
        public string JobName { get; private set; }


        /// <summary>
        /// Constructor
        /// コンストラクタ
        /// </summary>
        private Job()
        {
        }


        /// <summary>
        /// Generate Job-Instance for serial processing.
        /// 連続処理用Job生成
        /// </summary>
        /// <param name="action"></param>
        /// <param name="isExecUiThread"></param>
        /// <param name="jobName"></param>
        /// <returns></returns>
        public static Job CreateJob(
            Action action,
            bool isExecUiThread = false,
            string jobName = null
        )
        {
            try
            {
                if (action == null)
                    throw new ArgumentNullException("action null");

                var result = new Job()
                {
                    IsExecUIThread = isExecUiThread,
                    DelayMSec = 0,
                    Action = action,
                    JobName = jobName
                };

                return result;
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Generate Delay-Job-Instance for serial processing.
        /// 連続処理用の遅延を生成
        /// </summary>
        /// <param name="delayMsec"></param>
        /// <returns></returns>
        public static Job CreateDelay(int delayMsec = 300)
        {
            try
            {
                if (delayMsec <= 0)
                    throw new ArgumentOutOfRangeException($"invalid delayMsec: {delayMsec}");

                var result = new Job()
                {
                    IsExecUIThread = false,
                    DelayMSec = delayMsec,
                    Action = null,
                    JobName = "Job.CreateWait"
                };

                return result;
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Execute the Job instance array sequentially.
        /// Jobインスタンス配列を順次実行する。
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <param name="jobs"></param>
        /// <returns></returns>
        public static async Task RunSerial(
            CancellationToken? cancelToken,
            params Job[] jobs
        )
        {
            try
            {
                if (jobs == null)
                    return;

                foreach (var job in jobs)
                {
                    //キャンセル指示済のとき、何も実行せず終了する。
                    if (
                        cancelToken != null
                        && ((CancellationToken)cancelToken).IsCancellationRequested
                    )
                    {
                        return;
                    }

                    if (job.DelayMSec > 0)
                        await Job.Wait(job.DelayMSec);
                    else
                        await Job.Run(
                            job.Action,
                            job.IsExecUIThread,
                            job.JobName,
                            cancelToken
                        ).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Execute the Job instance array sequentially.
        /// Jobインスタンス配列を順次実行する。
        /// </summary>
        /// <param name="jobs"></param>
        /// <returns></returns>
        public static async Task RunSerial(params Job[] jobs)
        {
            try
            {
                if (jobs == null)
                    return;

                await Job.RunSerial(default, jobs);
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Execute the Action array sequentially with Non-UI-Threads.
        /// Action配列を、非UIスレッドで順次実行する。
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        public static async Task RunSerial(
            CancellationToken? cancelToken,
            params Action[] actions
        )
        {
            try
            {
                if (actions == null)
                    return;

                var jobs = actions
                    .Select(action => Job.CreateJob(action, false, "Job.RunSerial"))
                    .ToArray();

                await Job.RunSerial(cancelToken, jobs);
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Execute the Action array sequentially with Non-UI-Threads.
        /// Action配列を、非UIスレッドで順次実行する。
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        public static async Task RunSerial(params Action[] actions)
        {
            try
            {
                if (actions == null)
                    return;

                await Job.RunSerial(default, actions);
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Execute a continuous job with return value.
        /// Jobインスタンス配列を順次実行したあと、最後に戻り値がある処理を実行する。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lastJob"></param>
        /// <param name="isUiThreadLastJob"></param>
        /// <param name="cancelToken"></param>
        /// <param name="jobs"></param>
        /// <returns></returns>
        public static async Task<T> RunSerial<T>(
            Func<T> lastJob,
            bool isUiThreadLastJob = false,
            CancellationToken? cancelToken = null,
            params Job[] jobs
        )
        {
            try
            {
                await Job.RunSerial(cancelToken, jobs);

                //キャンセル指示済のとき、何も実行せず終了する。
                if (
                    cancelToken != null
                    && ((CancellationToken)cancelToken).IsCancellationRequested
                )
                {
                    return default;
                }

                return await Job.Run<T>(
                    lastJob,
                    isUiThreadLastJob,
                    "Job.RunSerial",
                    cancelToken
                );
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        /// <summary>
        /// Execute a continuous job with return value.
        /// Jobインスタンス配列を順次実行したあと、最後に戻り値がある処理を実行する。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lastJob"></param>
        /// <param name="jobs"></param>
        /// <returns></returns>
        public static async Task<T> RunSerial<T>(
            Func<T> lastJob,
            params Job[] jobs
        )
        {
            try
            {
                await Job.RunSerial(default, jobs);

                return await Job.Run<T>(
                    lastJob,
                    false,
                    "Job.RunSerial"
                );
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        #endregion
    }
}