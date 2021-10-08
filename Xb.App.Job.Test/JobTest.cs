using System;
using Xunit;
using Xb.App;
using System.Threading;
using System.Threading.Tasks;

namespace XbAppJob.Test
{
    public class JobTest
    {
        [Fact]
        public async Task RunTest()
        {
            Job.Init();

            var execCountNonUi = 0;
            var execCountUi = 0;

            for (var i = 0; i < 100; i++)
            {
                await Job.Run(() =>
                {
                    execCountNonUi++;
                    Assert.False(Job.IsUIThread);
                }, false);
            }

            for (var i = 0; i < 100; i++)
            {
                await Job.Run(() =>
                {
                    execCountUi++;

                    // UIスレッドの概念が存在しないプラットフォームのときは、
                    // TaskScheduler.FromCurrentSynchronizationContext() で取得した
                    // スケジューラで実行しても、実行スレッドに揺らぎが発生する。
                    //Assert.True(Job.IsUIThread);
                }, true);
            }

            Assert.Equal(100, execCountNonUi);
            Assert.Equal(100, execCountUi);
        }

        [Fact]
        public async Task RunCancelTest()
        {
            Job.Init();

            var execCountNonUi = 0;
            var cancellerNonUi = new CancellationTokenSource();
            for (var i = 0; i < 100; i++)
            {
                await Job.Run(() =>
                {
                    execCountNonUi++;
                    Assert.False(Job.IsUIThread);

                    if (50 <= execCountNonUi)
                        cancellerNonUi.Cancel(true);
                }, false, "RunTestCancelTest", cancellerNonUi.Token);
            }

            var execCountUi = 0;
            var cancellerUi = new CancellationTokenSource();
            for (var i = 0; i < 100; i++)
            {
                await Job.Run(() =>
                {
                    execCountUi++;

                    // UIスレッドの概念が存在しないプラットフォームのときは、
                    // TaskScheduler.FromCurrentSynchronizationContext() で取得した
                    // スケジューラで実行しても、実行スレッドに揺らぎが発生する。
                    //Assert.True(Job.IsUIThread);

                    if (50 <= execCountUi)
                        cancellerUi.Cancel(true);
                }, true, "RunTestCancelTest", cancellerUi.Token);
            }

            Assert.Equal(50, execCountNonUi);
            Assert.Equal(50, execCountUi);
        }

        [Fact]
        public async Task RunWithResultTest()
        {
            Job.Init();

            var execCountNonUi = 0;
            var execCountUi = 0;

            for (var i = 0; i < 100; i++)
            {
                var res = await Job.Run<int>(() =>
                {
                    execCountNonUi++;
                    Assert.False(Job.IsUIThread);

                    return execCountNonUi;
                }, false);

                Assert.Equal(res, execCountNonUi);
            }

            for (var i = 0; i < 100; i++)
            {
                var res = await Job.Run<int>(() =>
                {
                    execCountUi++;

                    // UIスレッドの概念が存在しないプラットフォームのときは、
                    // TaskScheduler.FromCurrentSynchronizationContext() で取得した
                    // スケジューラで実行しても、実行スレッドに揺らぎが発生する。
                    //Assert.True(Job.IsUIThread);

                    return execCountUi;
                }, true);

                Assert.Equal(res, execCountUi);
            }

            Assert.Equal(100, execCountNonUi);
            Assert.Equal(100, execCountUi);
        }

        [Fact]
        public async Task RunWithResultCancelTest()
        {
            Job.Init();

            var execCountNonUi = 0;
            var cancellerNonUi = new CancellationTokenSource();
            for (var i = 0; i < 100; i++)
            {
                var res = await Job.Run<int>(() =>
                {
                    execCountNonUi++;
                    Assert.False(Job.IsUIThread);

                    if (50 <= execCountNonUi)
                        cancellerNonUi.Cancel(true);

                    return execCountNonUi;

                }, false, "RunTestTCancelTest", cancellerNonUi.Token);

                // キャンセル直後は正常応答が来る。
                if (i < 50)
                    Assert.Equal(execCountNonUi, res);
                else
                    Assert.Equal(default, res);
            }

            var execCountUi = 0;
            var cancellerUi = new CancellationTokenSource();
            for (var i = 0; i < 100; i++)
            {
                var res = await Job.Run<int>(() =>
                {
                    execCountUi++;

                    // UIスレッドの概念が存在しないプラットフォームのときは、
                    // TaskScheduler.FromCurrentSynchronizationContext() で取得した
                    // スケジューラで実行しても、実行スレッドに揺らぎが発生する。
                    //Assert.True(Job.IsUIThread);

                    if (50 <= execCountUi)
                        cancellerUi.Cancel(true);

                    return execCountUi;

                }, true, "RunTestTCancelTest", cancellerUi.Token);

                // キャンセル直後は正常応答が来る。
                if (i < 50)
                    Assert.Equal(execCountUi, res);
                else
                    Assert.Equal(default, res);
            }

            Assert.Equal(50, execCountNonUi);
            Assert.Equal(50, execCountUi);
        }

        [Fact]
        public async Task RunSerialTest()
        {
            Job.Init();

            for (var i = 0; i < 10; i++)
            {
                var jobCount = 0;
                var startTime = DateTime.Now;
                var doTask1 = false;
                var doTask2 = false;
                var doTask3 = false;

                await Job.RunSerial(
                    Job.CreateJob(() =>
                    {
                        jobCount++;
                        Assert.Equal(1, jobCount);
                        Assert.False(Job.IsUIThread);
                        doTask1 = true;

                    }, false, "RunSerialTest"),
                    Job.CreateJob(() =>
                    {
                        jobCount++;
                        Assert.Equal(2, jobCount);

                        // UIスレッドの概念が存在しないプラットフォームのときは、
                        // TaskScheduler.FromCurrentSynchronizationContext() で取得した
                        // スケジューラで実行しても、実行スレッドに揺らぎが発生する。
                        //Assert.True(Job.IsUIThread);
                        doTask2 = true;

                    }, true, "RunSerialTest"),
                    Job.CreateDelay(500),
                    Job.CreateJob(() =>
                    {
                        jobCount++;
                        Assert.Equal(3, jobCount);
                        Assert.False(Job.IsUIThread);
                        doTask3 = true;

                    }, false, "RunSerialTest")
                );
                Assert.Equal(3, jobCount);
                Assert.True(500 < (DateTime.Now - startTime).TotalMilliseconds);

                Assert.True(doTask1);
                Assert.True(doTask2);
                Assert.True(doTask3);
            }
        }

        [Fact]
        public async Task RunSerialCancelTest()
        {
            Job.Init();

            for (var i = 0; i < 10; i++)
            {
                var jobCount = 0;
                var startTime = DateTime.Now;
                var canceller = new CancellationTokenSource();
                var doTask1 = false;
                var doTask2 = false;
                var doTask3 = false;

                await Job.RunSerial(
                    canceller.Token,
                    Job.CreateJob(() =>
                    {
                        canceller.Cancel(true);

                        jobCount++;
                        Assert.Equal(1, jobCount);
                        Assert.False(Job.IsUIThread);
                        doTask1 = true;

                    }, false, "RunSerialCancelTest"),
                    Job.CreateJob(() =>
                    {
                        jobCount++;
                        Assert.Equal(2, jobCount);
                        Assert.True(Job.IsUIThread);
                        doTask2 = true;

                    }, true, "RunSerialCancelTest"),
                    Job.CreateDelay(500),
                    Job.CreateJob(() =>
                    {
                        jobCount++;
                        Assert.Equal(3, jobCount);
                        Assert.False(Job.IsUIThread);
                        doTask3 = true;

                    }, false, "RunSerialCancelTest")
                );

                Assert.Equal(1, jobCount);
                Assert.True((DateTime.Now - startTime).TotalMilliseconds < 400);

                Assert.True(doTask1);
                Assert.False(doTask2);
                Assert.False(doTask3);
            }
        }

        [Fact]
        public async Task RunSerialWithResultTest()
        {
            Job.Init();

            for (var i = 0; i < 10; i++)
            {
                var jobCount = 0;
                var startTime = DateTime.Now;
                var doTask1 = false;
                var doTask2 = false;
                var doTask3 = false;
                var doTask4 = false;

                var res = await Job.RunSerial<bool>(
                    () =>
                    {
                        jobCount++;
                        Assert.Equal(4, jobCount);

                        // UIスレッドの概念が存在しないプラットフォームのときは、
                        // TaskScheduler.FromCurrentSynchronizationContext() で取得した
                        // スケジューラで実行しても、実行スレッドに揺らぎが発生する。
                        //Assert.True(Job.IsUIThread);
                        doTask4 = true;

                        return true;
                    },
                    true,
                    default,
                    Job.CreateJob(() =>
                    {
                        jobCount++;
                        Assert.Equal(1, jobCount);
                        Assert.False(Job.IsUIThread);
                        doTask1 = true;

                    }, false, "RunSerialTTest"),
                    Job.CreateJob(() =>
                    {
                        jobCount++;
                        Assert.Equal(2, jobCount);

                        // UIスレッドの概念が存在しないプラットフォームのときは、
                        // TaskScheduler.FromCurrentSynchronizationContext() で取得した
                        // スケジューラで実行しても、実行スレッドに揺らぎが発生する。
                        //Assert.True(Job.IsUIThread);
                        doTask2 = true;

                    }, true, "RunSerialTTest"),
                    Job.CreateDelay(500),
                    Job.CreateJob(() =>
                    {
                        jobCount++;
                        Assert.Equal(3, jobCount);
                        Assert.False(Job.IsUIThread);
                        doTask3 = true;

                    }, false, "RunSerialTTest")
                );

                Assert.Equal(4, jobCount);
                Assert.True(500 < (DateTime.Now - startTime).TotalMilliseconds);

                Assert.True(res);
                Assert.True(doTask1);
                Assert.True(doTask2);
                Assert.True(doTask3);
                Assert.True(doTask4);
            }
        }

        [Fact]
        public async Task RunSerialWithResultCancelTest()
        {
            Job.Init();

            for (var i = 0; i < 10; i++)
            {
                var jobCount = 0;
                var startTime = DateTime.Now;
                var canceller = new CancellationTokenSource();
                var doTask1 = false;
                var doTask2 = false;
                var doTask3 = false;
                var doTask4 = false;

                var lastResult = await Job.RunSerial<bool>(
                    () =>
                    {
                        jobCount++;
                        Assert.Equal(4, jobCount);
                        Assert.True(Job.IsUIThread);
                        doTask4 = true;

                        return true;
                    },
                    true,
                    canceller.Token,
                    Job.CreateJob(() =>
                    {
                        jobCount++;
                        Assert.Equal(1, jobCount);
                        Assert.False(Job.IsUIThread);
                        doTask1 = true;

                    }, false, "RunSerialTCancelTest"),
                    Job.CreateJob(() =>
                    {
                        canceller.Cancel(true);

                        jobCount++;
                        Assert.Equal(2, jobCount);

                        // UIスレッドの概念が存在しないプラットフォームのときは、
                        // TaskScheduler.FromCurrentSynchronizationContext() で取得した
                        // スケジューラで実行しても、実行スレッドに揺らぎが発生する。
                        //Assert.True(Job.IsUIThread);
                        doTask2 = true;

                    }, true, "RunSerialTCancelTest"),
                    Job.CreateDelay(500),
                    Job.CreateJob(() =>
                    {
                        jobCount++;
                        Assert.Equal(3, jobCount);
                        Assert.False(Job.IsUIThread);
                        doTask3 = true;

                    }, false, "RunSerialTCancelTest")
                );


                Assert.Equal(2, jobCount);
                Assert.True((DateTime.Now - startTime).TotalMilliseconds < 400);

                Assert.Equal(default, lastResult);
                Assert.True(doTask1);
                Assert.True(doTask2);
                Assert.False(doTask3);
                Assert.False(doTask4);
            }
        }
    }
}
