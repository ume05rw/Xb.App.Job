using System;
using Xb.App;
using Xunit;

namespace XbAppJob.Test
{
    public class BackgroundJobManagerTest
    {
        [Fact]
        public void RunTest()
        {
            Job.Init();

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
    }
}
