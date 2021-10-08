using Xb.App;
using Xunit;

namespace XbAppJob.Test
{
    public class DelayedOnceJobManagerTest
    {
        [Fact]
        public void RunTest()
        {
            Job.Init();

            var execCount = 0;

            var manager = new Job.DelayedOnceJobManager(() =>
            {
                execCount++;
                Xb.Util.Out("Job Executed");
            }, 1000);

            for (var i = 0; i < 10; i++)
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
    }
}
