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

                for (var i = 0; i < 1000; i++)
                {
                    await Job.Run(() =>
                    {
                        execCountNonUi++;
                        Assert.IsFalse(Job.IsUIThread, "RunTestTest");
                    }, false);
                }

                for (var i = 0; i < 1000; i++)
                {
                    await Job.Run(() =>
                    {
                        execCountUi++;
                        Assert.IsTrue(Job.IsUIThread, "RunTestTest");
                    }, true);
                }

                Assert.AreEqual(execCountNonUi, 1000, "RunTestTest");
                Assert.AreEqual(execCountUi, 1000, "RunTestTest");
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
                var execCountUi = 0;
                var cancellerNonUi = new CancellationTokenSource();
                var cancellerUi = new CancellationTokenSource();

                for (var i = 0; i < 1000; i++)
                {
                    try
                    {
                        await Job.Run(() =>
                        {
                            if (execCountNonUi >= 500)
                            {
                                cancellerNonUi.Cancel(true);
                            }

                            execCountNonUi++;
                            Assert.IsFalse(Job.IsUIThread, "RunTestCancelTest");
                        }, false, "RunTestCancelTest", cancellerNonUi);
                    }
                    catch (Exception)
                    {
                    }

                }

                for (var i = 0; i < 1000; i++)
                {
                    try
                    {
                        await Job.Run(() =>
                        {
                            if (execCountUi >= 500)
                            {
                                cancellerUi.Cancel(true);
                            }

                            execCountUi++;
                            Assert.IsTrue(Job.IsUIThread, "RunTestCancelTest");
                        }, true, "RunTestCancelTest", cancellerUi);
                    }
                    catch (Exception)
                    {
                    }
                }

                Assert.AreEqual(execCountNonUi, 501, "RunTestCancelTest");
                Assert.AreEqual(execCountUi, 501, "RunTestCancelTest");
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
