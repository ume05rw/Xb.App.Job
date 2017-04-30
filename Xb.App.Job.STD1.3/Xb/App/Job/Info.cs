using System;

namespace Xb.App
{
    public partial class Job
    {
        /// <summary>
        /// Job Infomation Class
        /// Job情報保持クラス
        /// </summary>
        /// <remarks>
        /// 長期パフォーマンス計測したい場合にDisposeせず保持出来るように、
        /// 文字列、数値、時刻しか持たせない。
        /// </remarks>
        private class Info : IDisposable
        {
            /// <summary>
            /// Current maximum job ID(=Number of jobs generated so far)
            /// 現在の最大ジョブID(=現在までに生成されたジョブの数)
            /// </summary>
            private static long _maxJobId = 0;

            /// <summary>
            /// job ID
            /// ジョブ番号
            /// </summary>
            /// <remarks>
            /// Indicates "Which Job was generated"
            /// 「何番目に生成されたJobか」を示す。
            /// </remarks>
            public long JobId { get; private set; }

            /// <summary>
            /// job Started DateTime
            /// ジョブ開始日時
            /// </summary>
            public DateTime StartTime { get; private set; }

            /// <summary>
            /// Generator class name
            /// Action/FuncTの生成元クラス名
            /// </summary>
            public string CalledClassName { get; private set; }

            /// <summary>
            /// job name
            /// ジョブ名称
            /// </summary>
            public string JobName { get; private set; }

            /// <summary>
            /// Whether the job has ended.
            /// ジョブが終了したか否か
            /// </summary>
            public bool IsEnded { get; private set; }

            /// <summary>
            /// job Ended DateTime
            /// ジョブ終了日時
            /// </summary>
            public DateTime EndTime { get; private set; }

            /// <summary>
            /// Thread ID
            /// スレッドID
            /// </summary>
            public int ThreadId { get; private set; } = -1;

            /// <summary>
            /// Status String
            /// 状態情報文字列
            /// </summary>
            public string State
                => $"ThID: {this.ThreadId.ToString().PadLeft(5)},  StartTime: {this.StartTime:HH:mm:ss.fff},  ProcTime:{(DateTime.Now - this.StartTime).TotalSeconds.ToString("F3").PadLeft(10)} sec,  ActiveThreads: {Job.InfoStore.Instance?.OnWork.ToString().PadLeft(3)}, JobName: {this.JobName.PadRight(25)} CalledClass: {this.CalledClassName}";



            /// <summary>
            /// Constructor
            /// コンストラクタ
            /// </summary>
            /// <param name="name"></param>
            /// <param name="calledClassName"></param>
            public Info(string name, string calledClassName)
            {
                Job.Info._maxJobId++;
                this.JobId = Job.Info._maxJobId;
                this.StartTime = DateTime.Now;
                this.JobName = name;
                this.CalledClassName = calledClassName;
                this.IsEnded = false;
                this.EndTime = DateTime.MaxValue;
            }


            /// <summary>
            /// Set Thread-ID
            /// スレッドIDをセットする。
            /// </summary>
            /// <param name="threadId"></param>
            public void SetThreadId(int threadId)
            {
                this.ThreadId = threadId;
            }


            /// <summary>
            /// Set Ended Info.
            /// ジョブ終了を記録する。
            /// </summary>
            public void End()
            {
                this.EndTime = DateTime.Now;
                this.IsEnded = true;
            }


            #region IDisposable Support
            private bool disposedValue = false; // 重複する呼び出しを検出するには

            private void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        this.CalledClassName = null;
                        this.JobName = null;
                    }
                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                this.Dispose(true);
            }
            #endregion
        }
    }
}
