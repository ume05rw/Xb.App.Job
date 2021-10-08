using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xb.App;
using Xunit;

namespace XbAppJob.Test
{
    public class JobStaticTest
    {
        [Fact]
        public void Init()
        {
            Job.Init();

            Assert.False(Job.IsMonitorEnabled);
            Assert.False(Job.IsDumpStatus);
            Assert.False(Job.IsDumpTaskValidation);
            Assert.Equal(Job.TimerIntervalMsec, -1);
            Assert.Null(Job.Dumper.Instance);
            Assert.False(Job.Dumper.IsWorking);
            Assert.False(Job.Dumper.IsDumpStatus);
            Assert.False(Job.Dumper.IsDumpTaskValidation);
            Assert.Null(Job.Monitor.Instance);
            Assert.False(Job.Monitor.IsWorking);
        }

        [Fact]
        public async Task IsUIThreadTest()
        {
            Job.Init();

            Assert.True(Job.IsUIThread);
            Xb.Util.Out($"Start ThreadID = {Environment.CurrentManagedThreadId}");

            var tasks = new List<Task>();
            for (var i = 0; i < 200; i++)
            {
                tasks.Add(Job.Run(() =>
                {
                    Assert.False(Job.IsUIThread);
                }, false));

                tasks.Add(Job.Run(() =>
                {
                    Xb.Util.Out($"Binded: ThreadID = {Environment.CurrentManagedThreadId}");
                    // UIスレッドの概念が存在しないプラットフォームのときは、
                    // TaskScheduler.FromCurrentSynchronizationContext() で取得した
                    // スケジューラで実行しても、実行スレッドに揺らぎが発生する。
                    //Assert.True(Job.IsUIThread);
                }, true));

                await Job.Run(() =>
                {
                    Assert.False(Job.IsUIThread);
                }, false).ConfigureAwait(false);

                await Job.Run(() =>
                {
                    Xb.Util.Out($"Binded: ThreadID = {Environment.CurrentManagedThreadId}");
                    // UIスレッドの概念が存在しないプラットフォームのときは、
                    // TaskScheduler.FromCurrentSynchronizationContext() で取得した
                    // スケジューラで実行しても、実行スレッドに揺らぎが発生する。
                    //Assert.True(Job.IsUIThread);
                }, true).ConfigureAwait(false);
            }

            await Task.WhenAll(tasks);
        }

        [Fact]
        public void IsMonitorEnabledTest()
        {
            Job.Init();

            Job.IsMonitorEnabled = false;

            Assert.False(Job.IsMonitorEnabled);
            Assert.False(Job.Monitor.IsWorking);
            Assert.Null(Job.Monitor.Instance);

            Job.IsMonitorEnabled = true;

            Assert.True(Job.IsMonitorEnabled);
            Assert.True(Job.Monitor.IsWorking);
            Assert.NotNull(Job.Monitor.Instance);
        }

        [Fact]
        public void IsDumpAnyTest()
        {
            Job.Init();

            Job.IsDumpStatus = true;

            Assert.True(Job.IsDumpStatus);
            Assert.NotNull(Job.Dumper.Instance);
            Assert.True(Job.Dumper.IsWorking);
            Assert.True(Job.Dumper.IsDumpStatus);

            Job.IsDumpStatus = false;

            Assert.False(Job.IsDumpStatus);
            Assert.Null(Job.Dumper.Instance);
            Assert.False(Job.Dumper.IsWorking);
            Assert.False(Job.Dumper.IsDumpStatus);

            Job.IsDumpTaskValidation = false;

            Assert.False(Job.IsDumpStatus);
            Assert.False(Job.IsDumpTaskValidation);
            Assert.Null(Job.Dumper.Instance);
            Assert.False(Job.Dumper.IsWorking);
            Assert.False(Job.Dumper.IsDumpStatus);
            Assert.False(Job.Dumper.IsDumpTaskValidation);

            Job.IsDumpTaskValidation = true;

            Assert.False(Job.IsDumpStatus);
            Assert.True(Job.IsDumpTaskValidation);
            Assert.NotNull(Job.Dumper.Instance);
            Assert.True(Job.Dumper.IsWorking);
            Assert.False(Job.Dumper.IsDumpStatus);
            Assert.True(Job.Dumper.IsDumpTaskValidation);
        }

        [Fact]
        public void TimerIntervalMsecTest()
        {
            Job.Init();

            Job.IsDumpStatus = false;
            Job.IsDumpTaskValidation = false;

            Assert.False(Job.IsDumpStatus);
            Assert.False(Job.IsDumpTaskValidation);
            Assert.Null(Job.Dumper.Instance);
            Assert.False(Job.Dumper.IsWorking);
            Assert.False(Job.Dumper.IsDumpStatus);
            Assert.False(Job.Dumper.IsDumpTaskValidation);

            Assert.Equal(Job.TimerIntervalMsec, -1);

            try
            {
                var msec = Job.TimerIntervalMsec;
            }
            catch (InvalidOperationException)
            {
                Assert.True(true);
            }
            catch (Exception)
            {
                throw;
            }

            Job.IsDumpTaskValidation = true;

            Assert.Equal(30000, Job.TimerIntervalMsec);
            Job.TimerIntervalMsec = 1000;
            Assert.Equal(1000, Job.TimerIntervalMsec);

            try
            {
                Job.TimerIntervalMsec = 999;
            }
            catch (ArgumentException)
            {
                Assert.True(true);
            }
            catch (Exception)
            {
                Assert.True(false);
            }

            Job.TimerIntervalMsec = 30000;
        }
    }
}
