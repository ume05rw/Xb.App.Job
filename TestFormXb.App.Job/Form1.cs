using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xb.App;
using Xunit;

namespace TestFormXb
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
        }

        private void TestForm_Load(object sender, EventArgs e)
        {
            this.InitTest();

            _ = Task.Run(() =>
            {
                _ = this.ExecTest();
            });
        }

        private async Task ExecTest()
        {
            this.BackgroundJobManagerTest();

            this.DelayedOnceJobManagerTest();

            await this.IsUIThreadTest();

            this.IsMonitorEnabledTest();

            this.IsDumpAnyTest();

            this.TimerIntervalMsecTest();

            await this.RunTest();

            await this.RunCancelTest();

            await this.RunWithResultTest();

            await this.RunWithResultCancelTest();

            await this.RunSerialTest();

            await this.RunSerialCancelTest();

            await this.RunSerialWithResultTest();

            await this.RunSerialWithResultCancelTest();

            MessageBox.Show("All OK!");
        }



        private void InitTest()
        {
            try
            {
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
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        private async Task IsUIThreadTest()
        {
            try
            {
                var tasks = new List<Task>();

                for (var i = 0; i < 200; i++)
                {
                    tasks.Add(Job.Run(() =>
                    {
                        Assert.False(Job.IsUIThread);
                    }, false));

                    tasks.Add(Job.Run(() =>
                    {
                        Assert.True(Job.IsUIThread);
                    }, true));
                }

                for (var i = 0; i < 200; i++)
                {
                    await Job.Run(() =>
                    {
                        Assert.False(Job.IsUIThread);
                    }, false).ConfigureAwait(false);

                    await Job.Run(() =>
                    {
                        Xb.Util.Out($"Binded: ThreadID = {Environment.CurrentManagedThreadId}");
                        Assert.True(Job.IsUIThread);
                    }, true).ConfigureAwait(false);
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private void IsMonitorEnabledTest()
        {
            try
            {
                Job.IsMonitorEnabled = false;

                Assert.False(Job.IsMonitorEnabled);
                Assert.False(Job.Monitor.IsWorking);
                Assert.Null(Job.Monitor.Instance);

                Job.IsMonitorEnabled = true;

                Assert.True(Job.IsMonitorEnabled);
                Assert.True(Job.Monitor.IsWorking);
                Assert.NotNull(Job.Monitor.Instance);
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        private void IsDumpAnyTest()
        {
            try
            {
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
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        private void TimerIntervalMsecTest()
        {
            try
            {
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
                    throw;
                }

                Job.TimerIntervalMsec = 30000;

            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }


        private async Task RunTest()
        {
            try
            {
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
                        Assert.True(Job.IsUIThread);
                    }, true);
                }

                Assert.Equal(100, execCountNonUi);
                Assert.Equal(100, execCountUi);
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private async Task RunCancelTest()
        {
            try
            {
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
                        Assert.True(Job.IsUIThread);

                        if (50 <= execCountUi)
                            cancellerUi.Cancel(true);
                    }, true, "RunTestCancelTest", cancellerUi.Token);
                }

                Assert.Equal(50, execCountNonUi);
                Assert.Equal(50, execCountUi);
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private async Task RunWithResultTest()
        {
            try
            {
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
                        Assert.True(Job.IsUIThread);

                        return execCountUi;
                    }, true);

                    Assert.Equal(res, execCountUi);
                }

                Assert.Equal(100, execCountNonUi);
                Assert.Equal(100, execCountUi);
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private async Task RunWithResultCancelTest()
        {
            try
            {
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
                Assert.Equal(50, execCountNonUi);

                var execCountUi = 0;
                var cancellerUi = new CancellationTokenSource();
                for (var i = 0; i < 100; i++)
                {
                    var res = await Job.Run<int>(() =>
                    {
                        execCountUi++;
                        Assert.True(Job.IsUIThread);

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
                Assert.Equal(50, execCountUi);
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private async Task RunSerialTest()
        {
            try
            {
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
                            Assert.True(Job.IsUIThread);
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
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private async Task RunSerialCancelTest()
        {
            try
            {
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
                            doTask2 = true;
                            Assert.True(false);
                        }, true, "RunSerialCancelTest"),
                        Job.CreateDelay(500),
                        Job.CreateJob(() =>
                        {
                            jobCount++;
                            doTask3 = true;
                            Assert.True(false);
                        }, false, "RunSerialCancelTest")
                    );

                    Assert.Equal(1, jobCount);
                    Assert.True((DateTime.Now - startTime).TotalMilliseconds < 400);

                    Assert.True(doTask1);
                    Assert.False(doTask2);
                    Assert.False(doTask3);
                }
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private async Task RunSerialWithResultTest()
        {
            try
            {
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
                            Assert.True(Job.IsUIThread);
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
                            Assert.True(Job.IsUIThread);
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
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private async Task RunSerialWithResultCancelTest()
        {
            try
            {
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
                            Assert.True(Job.IsUIThread);
                            doTask2 = true;

                        }, true, "RunSerialTCancelTest"),
                        Job.CreateDelay(500),
                        Job.CreateJob(() =>
                        {
                            jobCount++;
                            doTask3 = true;
                            Assert.True(false);
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
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private void DelayedOnceJobManagerTest()
        {
            try
            {
                var execCount = 0;

                var manager = new Job.DelayedOnceJobManager(() =>
                {
                    execCount++;
                    Xb.Util.Out("Job Executed");
                }, 1000);

                for(var i = 0; i < 10; i++)
                {
                    Job.WaitSynced(500);
                    manager.Run();
                    Xb.Util.Out("Run - Unlimitted");
                }

                Job.WaitSynced(1500);

                Assert.Equal(1, execCount);

                execCount = 0;


                manager = new Job.DelayedOnceJobManager(() =>
                {
                    execCount++;
                    Xb.Util.Out("Job Executed");
                }, 1000, 2000);

                for (var i = 0; i < 10; i++)
                {
                    Job.WaitSynced(500);
                    manager.Run();
                    Xb.Util.Out("Run - Limitted");
                }

                Job.WaitSynced(2500);

                Assert.Equal(3, execCount);
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private void BackgroundJobManagerTest()
        {
            try
            {
                var manager = new Job.BackgroundJobManager("TestBGJM");
                Assert.Equal("TestBGJM", manager.Name);
                var execCount = 0;
                var action = new Action(() =>
                {
                    execCount++;
                    Xb.Util.Out("Exec Action");
                });

                Job.Run(() =>
                {
                    manager.Register(action);
                    manager.Register(action);
                    manager.Register(action);
                });


                Job.WaitSynced(1000);

                Assert.Equal(3, execCount);

                execCount = 0;

                manager.Suppress(this, "TestingForm");

                Job.Run(() =>
                {
                    manager.Register(action);
                    manager.Register(action);
                    manager.Register(action);
                });

                Job.WaitSynced(1000);

                Assert.Equal(0, execCount);

                manager.ReleaseSuppress(this);

                Job.WaitSynced(7000);

                Assert.Equal(3, execCount);
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }
    }
}
