using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xb.App;

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
            this.textBox1.Text = "";
            Assert.Init(this.textBox1);

            this.ExecTest();
        }

        private async Task ExecTest()
        {
            this.InitTest();

            await this.IsUIThreadTest();

            this.IsMonitorEnabledTest();

            this.IsDumpAnyTest();

            this.TimerIntervalMsecTest();

            await this.RunTestTest();

            await this.RunTestCancelTest();

            await this.RunTestTTest();

            await this.RunTestTCancelTest();

            await this.RunSerialTest();

            await this.RunSerialCancelTest();

            await this.RunSerialTTest();

            await this.RunSerialTCancelTest();

            MessageBox.Show("おわりやで");
        }



        private void InitTest()
        {
            try
            {
                Assert.IsFalse(Job.IsMonitorEnabled);
                Assert.IsFalse(Job.IsDumpStatus);
                Assert.IsFalse(Job.IsDumpTaskValidation);
                Assert.AreEqual(Job.TimerIntervalMsec, -1);
                Assert.IsNull(Job.Dumper.Instance);
                Assert.IsFalse(Job.Dumper.IsWorking);
                Assert.IsFalse(Job.Dumper.IsDumpStatus);
                Assert.IsFalse(Job.Dumper.IsDumpTaskValidation);
                Assert.IsNull(Job.Monitor.Instance);
                Assert.IsFalse(Job.Monitor.IsWorking);

                Job.Init();

                Assert.IsTrue(Job.IsMonitorEnabled);
                Assert.IsFalse(Job.IsDumpStatus);
                Assert.IsTrue(Job.IsDumpTaskValidation);
                Assert.AreEqual(Job.TimerIntervalMsec, 30000);
                Assert.IsNotNull(Job.Dumper.Instance);
                Assert.IsTrue(Job.Dumper.IsWorking);
                Assert.IsFalse(Job.Dumper.IsDumpStatus);
                Assert.IsTrue(Job.Dumper.IsDumpTaskValidation);
                Assert.IsNotNull(Job.Monitor.Instance);
                Assert.IsTrue(Job.Monitor.IsWorking);

                Assert.IsTrue(Job.Monitor.Instance.IsWorkingJobOnly);
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
                Assert.IsTrue(Job.IsUIThread);

                for (var i = 0; i < 200; i++)
                {
                    await Job.Run(() =>
                    {
                        Assert.IsFalse(Job.IsUIThread, "IsUIThreadTest");
                    }, false).ConfigureAwait(false);
                }
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
                Assert.IsTrue(Job.IsMonitorEnabled);
                Assert.IsTrue(Job.Monitor.IsWorking);
                Assert.IsNotNull(Job.Monitor.Instance);

                Job.IsMonitorEnabled = false;

                Assert.IsFalse(Job.IsMonitorEnabled);
                Assert.IsFalse(Job.Monitor.IsWorking);
                Assert.IsNull(Job.Monitor.Instance);

                Job.IsMonitorEnabled = true;

                Assert.IsTrue(Job.IsMonitorEnabled);
                Assert.IsTrue(Job.Monitor.IsWorking);
                Assert.IsNotNull(Job.Monitor.Instance);
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
                Assert.IsFalse(Job.IsDumpStatus);
                Assert.IsTrue(Job.IsDumpTaskValidation);
                Assert.IsNotNull(Job.Dumper.Instance);
                Assert.IsTrue(Job.Dumper.IsWorking);
                Assert.IsFalse(Job.Dumper.IsDumpStatus);
                Assert.IsTrue(Job.Dumper.IsDumpTaskValidation);

                Job.IsDumpStatus = true;

                Assert.IsTrue(Job.IsDumpStatus);
                Assert.IsTrue(Job.IsDumpTaskValidation);
                Assert.IsNotNull(Job.Dumper.Instance);
                Assert.IsTrue(Job.Dumper.IsWorking);
                Assert.IsTrue(Job.Dumper.IsDumpStatus);
                Assert.IsTrue(Job.Dumper.IsDumpTaskValidation);

                Job.IsDumpStatus = false;

                Assert.IsFalse(Job.IsDumpStatus);
                Assert.IsTrue(Job.IsDumpTaskValidation);
                Assert.IsNotNull(Job.Dumper.Instance);
                Assert.IsTrue(Job.Dumper.IsWorking);
                Assert.IsFalse(Job.Dumper.IsDumpStatus);
                Assert.IsTrue(Job.Dumper.IsDumpTaskValidation);

                Job.IsDumpTaskValidation = false;

                Assert.IsFalse(Job.IsDumpStatus);
                Assert.IsFalse(Job.IsDumpTaskValidation);
                Assert.IsNull(Job.Dumper.Instance);
                Assert.IsFalse(Job.Dumper.IsWorking);
                Assert.IsFalse(Job.Dumper.IsDumpStatus);
                Assert.IsFalse(Job.Dumper.IsDumpTaskValidation);

                Job.IsDumpTaskValidation = true;

                Assert.IsFalse(Job.IsDumpStatus);
                Assert.IsTrue(Job.IsDumpTaskValidation);
                Assert.IsNotNull(Job.Dumper.Instance);
                Assert.IsTrue(Job.Dumper.IsWorking);
                Assert.IsFalse(Job.Dumper.IsDumpStatus);
                Assert.IsTrue(Job.Dumper.IsDumpTaskValidation);

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

                Assert.IsFalse(Job.IsDumpStatus);
                Assert.IsFalse(Job.IsDumpTaskValidation);
                Assert.IsNull(Job.Dumper.Instance);
                Assert.IsFalse(Job.Dumper.IsWorking);
                Assert.IsFalse(Job.Dumper.IsDumpStatus);
                Assert.IsFalse(Job.Dumper.IsDumpTaskValidation);

                Assert.AreEqual(Job.TimerIntervalMsec, -1);

                try
                {
                    var msec = Job.TimerIntervalMsec;
                }
                catch (InvalidOperationException)
                {
                    Assert.IsTrue(true);
                }
                catch (Exception)
                {
                    throw;
                }

                Job.IsDumpTaskValidation = true;

                Assert.AreEqual(Job.TimerIntervalMsec, 30000);
                Job.TimerIntervalMsec = 1000;
                Assert.AreEqual(Job.TimerIntervalMsec, 1000);

                try
                {
                    Job.TimerIntervalMsec = 999;
                }
                catch (ArgumentException)
                {
                    Assert.IsTrue(true);
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


        private async Task RunTestTest()
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
                        Assert.IsFalse(Job.IsUIThread, "RunTestTest");
                    }, false);
                }

                for (var i = 0; i < 100; i++)
                {
                    await Job.Run(() =>
                    {
                        execCountUi++;
                        Assert.IsTrue(Job.IsUIThread, "RunTestTest");
                    }, true);
                }

                Assert.AreEqual(execCountNonUi, 100, "RunTestTest");
                Assert.AreEqual(execCountUi, 100, "RunTestTest");
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private async Task RunTestCancelTest()
        {
            try
            {
                var execCountNonUi = 0;
                var cancellerNonUi = new CancellationTokenSource();
                for (var i = 0; i < 100; i++)
                {
                    try
                    {
                        await Job.Run(() =>
                        {
                            if (execCountNonUi >= 50)
                            {
                                cancellerNonUi.Cancel(true);
                            }

                            execCountNonUi++;
                            Assert.IsFalse(Job.IsUIThread, "RunTestCancelTest");
                        }, false, "RunTestCancelTest", cancellerNonUi);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                var execCountUi = 0;
                var cancellerUi = new CancellationTokenSource();
                for (var i = 0; i < 100; i++)
                {
                    try
                    {
                        await Job.Run(() =>
                        {
                            if (execCountUi >= 50)
                            {
                                cancellerUi.Cancel(true);
                            }

                            execCountUi++;
                            Assert.IsTrue(Job.IsUIThread, "RunTestCancelTest");
                        }, true, "RunTestCancelTest", cancellerUi);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                Assert.AreEqual(execCountNonUi, 51, "RunTestCancelTest");
                Assert.AreEqual(execCountUi, 51, "RunTestCancelTest");
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private async Task RunTestTTest()
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
                        Assert.IsFalse(Job.IsUIThread, "RunTestTTest");

                        return execCountNonUi;
                    }, false);

                    Assert.AreEqual(res, execCountNonUi, "RunTestTTest");
                }

                for (var i = 0; i < 100; i++)
                {
                    var res = await Job.Run<int>(() =>
                    {
                        execCountUi++;
                        Assert.IsTrue(Job.IsUIThread, "RunTestTTest");

                        return execCountUi;
                    }, true);

                    Assert.AreEqual(res, execCountUi, "RunTestTTest");
                }

                Assert.AreEqual(execCountNonUi, 100, "RunTestTTest");
                Assert.AreEqual(execCountUi, 100, "RunTestTTest");
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private async Task RunTestTCancelTest()
        {
            try
            {
                var execCountNonUi = 0;
                var cancellerNonUi = new CancellationTokenSource();
                for (var i = 0; i < 100; i++)
                {
                    try
                    {
                        var res = await Job.Run<int>(() =>
                        {
                            if (execCountNonUi >= 50)
                            {
                                cancellerNonUi.Cancel(true);
                            }

                            execCountNonUi++;
                            Assert.IsFalse(Job.IsUIThread, "RunTestTCancelTest");

                            return execCountNonUi;

                        }, false, "RunTestTCancelTest", cancellerNonUi);

                        Assert.AreEqual(res, execCountNonUi, "RunTestTCancelTest");
                    }
                    catch (OperationCanceledException)
                    {
                    }

                }

                var execCountUi = 0;
                var cancellerUi = new CancellationTokenSource();
                for (var i = 0; i < 100; i++)
                {
                    try
                    {
                        var res = await Job.Run<int>(() =>
                        {
                            if (execCountUi >= 50)
                            {
                                cancellerUi.Cancel(true);
                            }

                            execCountUi++;
                            Assert.IsTrue(Job.IsUIThread, "RunTestTCancelTest");

                            return execCountUi;

                        }, true, "RunTestTCancelTest", cancellerUi);

                        Assert.AreEqual(res, execCountUi, "RunTestTCancelTest");
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                Assert.AreEqual(execCountNonUi, 51, "RunTestTCancelTest");
                Assert.AreEqual(execCountUi, 51, "RunTestTCancelTest");
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
                            Assert.AreEqual(jobCount, 1);
                            Assert.IsFalse(Job.IsUIThread, "RunSerialTest");
                            doTask1 = true;

                        }, false, "RunSerialTest"),
                        Job.CreateJob(() =>
                        {
                            jobCount++;
                            Assert.AreEqual(jobCount, 2);
                            Assert.IsTrue(Job.IsUIThread, "RunSerialTest");
                            doTask2 = true;

                        }, true, "RunSerialTest"),
                        Job.CreateDelay(500),
                        Job.CreateJob(() =>
                        {
                            jobCount++;
                            Assert.AreEqual(jobCount, 3);
                            Assert.IsFalse(Job.IsUIThread, "RunSerialTest");
                            doTask3 = true;

                        }, false, "RunSerialTest")
                    );
                    Assert.AreEqual(jobCount, 3, "RunSerialTest");
                    Assert.IsTimeOver(startTime, 500, "RunSerialTest");

                    Assert.IsTrue(doTask1, "RunSerialTest");
                    Assert.IsTrue(doTask2, "RunSerialTest");
                    Assert.IsTrue(doTask3, "RunSerialTest");
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

                    try
                    {
                        await Job.RunSerial(
                            canceller,
                            Job.CreateJob(() =>
                            {
                                canceller.Cancel(true);

                                jobCount++;
                                Assert.AreEqual(jobCount, 1);
                                Assert.IsFalse(Job.IsUIThread, "RunSerialCancelTest");
                                doTask1 = true;

                            }, false, "RunSerialCancelTest"),
                            Job.CreateJob(() =>
                            {
                                jobCount++;
                                Assert.AreEqual(jobCount, 2);
                                Assert.IsTrue(Job.IsUIThread, "RunSerialCancelTest");
                                doTask2 = true;

                            }, true, "RunSerialCancelTest"),
                            Job.CreateDelay(500),
                            Job.CreateJob(() =>
                            {
                                jobCount++;
                                Assert.AreEqual(jobCount, 3);
                                Assert.IsFalse(Job.IsUIThread, "RunSerialCancelTest");
                                doTask3 = true;

                            }, false, "RunSerialCancelTest")
                        );
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    Assert.AreEqual(jobCount, 1, "RunSerialCancelTest");
                    Assert.IsTrue((DateTime.Now - startTime).TotalMilliseconds < 400, "RunSerialCancelTest");

                    Assert.IsTrue(doTask1, "RunSerialCancelTest");
                    Assert.IsFalse(doTask2, "RunSerialCancelTest");
                    Assert.IsFalse(doTask3, "RunSerialCancelTest");
                }
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private async Task RunSerialTTest()
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
                            Assert.AreEqual(jobCount, 4);
                            Assert.IsTrue(Job.IsUIThread, "RunSerialTTest");
                            doTask4 = true;

                            return true;
                        },
                        true,
                        null,
                        Job.CreateJob(() =>
                        {
                            jobCount++;
                            Assert.AreEqual(jobCount, 1);
                            Assert.IsFalse(Job.IsUIThread, "RunSerialTTest");
                            doTask1 = true;

                        }, false, "RunSerialTTest"),
                        Job.CreateJob(() =>
                        {
                            jobCount++;
                            Assert.AreEqual(jobCount, 2);
                            Assert.IsTrue(Job.IsUIThread, "RunSerialTTest");
                            doTask2 = true;

                        }, true, "RunSerialTTest"),
                        Job.CreateDelay(500),
                        Job.CreateJob(() =>
                        {
                            jobCount++;
                            Assert.AreEqual(jobCount, 3);
                            Assert.IsFalse(Job.IsUIThread, "RunSerialTTest");
                            doTask3 = true;

                        }, false, "RunSerialTTest")
                    );

                    Assert.AreEqual(jobCount, 4, "RunSerialTTest");
                    Assert.IsTimeOver(startTime, 500, "RunSerialTTest");

                    Assert.IsTrue(res);
                    Assert.IsTrue(doTask1, "RunSerialTTest");
                    Assert.IsTrue(doTask2, "RunSerialTTest");
                    Assert.IsTrue(doTask3, "RunSerialTTest");
                    Assert.IsTrue(doTask4, "RunSerialTTest");
                }
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private async Task RunSerialTCancelTest()
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
                    var lastResult = false;
                    try
                    {
                        lastResult = await Job.RunSerial<bool>(
                            () =>
                            {
                                jobCount++;
                                Assert.AreEqual(jobCount, 4);
                                Assert.IsTrue(Job.IsUIThread, "RunSerialTCancelTest");
                                doTask4 = true;

                                return true;
                            },
                            true,
                            canceller,
                            Job.CreateJob(() =>
                            {
                                jobCount++;
                                Assert.AreEqual(jobCount, 1);
                                Assert.IsFalse(Job.IsUIThread, "RunSerialTCancelTest");
                                doTask1 = true;

                            }, false, "RunSerialTCancelTest"),
                            Job.CreateJob(() =>
                            {
                                canceller.Cancel(true);

                                jobCount++;
                                Assert.AreEqual(jobCount, 2);
                                Assert.IsTrue(Job.IsUIThread, "RunSerialTCancelTest");
                                doTask2 = true;

                            }, true, "RunSerialTCancelTest"),
                            Job.CreateDelay(500),
                            Job.CreateJob(() =>
                            {
                                jobCount++;
                                Assert.AreEqual(jobCount, 3);
                                Assert.IsFalse(Job.IsUIThread, "RunSerialTCancelTest");
                                doTask3 = true;

                            }, false, "RunSerialTCancelTest")
                        );
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    
                    Assert.AreEqual(jobCount, 2, "RunSerialTCancelTest");
                    Assert.IsTrue((DateTime.Now - startTime).TotalMilliseconds < 400, "RunSerialTCancelTest");

                    Assert.IsFalse(lastResult);
                    Assert.IsTrue(doTask1, "RunSerialTCancelTest");
                    Assert.IsTrue(doTask2, "RunSerialTCancelTest");
                    Assert.IsFalse(doTask3, "RunSerialTCancelTest");
                    Assert.IsFalse(doTask4, "RunSerialTCancelTest");
                }
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }

        private void Template()
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                Xb.Util.Out(ex);
                throw;
            }
        }
    }
}
