Xb.App.Job
====

Xamarin & .NET Core Ready, Thread.Task Replacer Implementation. Dump task lyfecycle, Detect Deadlock suspicious thread.

## Description

It's a Thread.Task Replacer Implementation, to Visualize the state of Thread.Task.  
Dump Task-Info to the console at creation / start / completion.

Task-Info includes information such as the name of the class in which the task was generated, the thread ID for executing the task, the time when the task was started, and so on.

Supports .NET4.5.1, .NET Standard1.3

## Requirement
[System.Diagnostics.Process](https://www.nuget.org/packages/System.Diagnostics.Process/)  
[System.Threading.Thread](https://www.nuget.org/packages/System.Threading.Thread/)  
[Xb.Core](https://www.nuget.org/packages/Xb.Core/)  

## Usage
1. ~~[Add NuGet Package](https://www.nuget.org/packages/Xb.App.Job/) to your project.~~ -> Not Ready.  
  Clone this project, and add a reference this to your project.
2. Call "Xb.App.Job.Init()" with UI-THREAD, to Initialize.
3. Execute Xb.App.Job.Run () instead of Task.Run ().
4. See your Console, Dumped a Task Info.
5. Call Static Methods / Properties Xb.App.Job.Any().
  
ex) Exec Task on Non-UI Thread.  

    Xb.App.Job.Run(() => 
    {
        //Action on Non-UI Thread.
        var value = 1;
    }, false, "StoreBase.DelayedUpdated");
    // ^non-ui    ^jobName

ex) Task Info on the Console.  

    09:20:26.721:  [ThID:   8]  Job.Run Start  ThID:     8,  StartTime: 09:20:25.760,  ProcTime:     0.960 sec,  ActiveThreads:   9, JobName: StoreBase.DelayedUpdated  CalledClass: StorePurchase
  
  　  
Namespace and Methods are...

    ・Xb.App
          |
          +- Job(static)
          |   |
          |   +- .Init()
          |   |   Initialize - ** MAKE SURE to execute this with UI-THREAD. **
          |   |
          |   |
          |   | ### settings and parameters ###
          |   |
          |   +- .IsUIThread { get; }
          |   |   Whether the current thread is a UI thread
          |   |
          |   +- .IsWorkingJobManager { get; set; }
          |   |   Whether Job-Info Manager is active.
          |   |
          |   +- .IsDumpStatus { get; set; }
          |   |   Whether console-dump output of [Periodic Status Info] is active.
          |   |
          |   +- .IsDumpTaskValidation { get; set; }
          |   |   Whether console-dump output of [Task Verification Info] is active.
          |   |
          |   +- .SetDumpTimerInterval(int msec)
          |   |   Set the execution interval of periodic staus dump / task verification processing.
          |   |
          |   |
          |   |
          |   | ### for single task processing ###
          |   |
          |   +- .Run(Action action,
          |   |       bool isExecUiThread,
          |   |       string jobName = null
          |   |       CancellationTokenSource cancellation = null)
          |   |   Execute a job without return value.
          |   |
          |   +- .Run<T>(Func<T> action,
          |   |          bool isExecUiThread,
          |   |          string jobName = null,
          |   |          CancellationTokenSource cancellation = null)
          |   |   Execute a job with return value.
          |   |
          |   +- .Run(Action action,
          |   |       string jobName = null)
          |   |   Execute the passed Action asynchronously in Non-UI-Thread.
          |   |
          |   +- .RunUI(Action action,
          |   |         string jobName = null)
          |   |   Execute the passed Action asynchronously in UI-Thread.
          |   |
          |   +- .DelayedRun(Action action,
          |   |              int msec,
          |   |              string jobName = null)
          |   |   Delayed Execute the passed Action asynchronously in Non-UI-Thread.
          |   |
          |   +- .DelayedRunUI(Action action,
          |   |                int msec,
          |   |                string jobName = null)
          |   |   Delayed Execute the passed Action asynchronously in UI-Thread.
          |   |
          |   +- .RunSynced(Action action
          |   |             string jobName = null)
          |   |   Synchronously Execute the passed Action in Non-UI-Thread.
          |   |
          |   +- .RunUISynced(Action action
          |   |               string jobName = null)
          |   |   Synchronously Execute the passed Action in Non-UI-Thread.
          |   |
          |   +- .Wait(int msec)
          |   |   Wait for specified milliseconds.
          |   |
          |   +- .WaitSynced(int msec)
          |   |   Synchronously Wait for specified milliseconds.
          |   |
          |   |
          |   |
          |   | ### for serial task processing ###
          |   |
          |   +- .CreateJob(Action action,
          |   |             bool isExecUiThread = false,
          |   |             string jobName = null)
          |   |   Generate Job-Instance for serial processing.
          |   |
          |   +- .CreateDelay(int delayMsec = 300)
          |   |   Generate Delay-Job-Instance for serial processing.
          |   |
          |   +- .RunSerial(params Job[] jobs)
          |   |   Execute the Job-Instance array sequentially.
          |   |
          |   +- .RunSerial(params Action[] actions)
          |   |   Execute the Action array sequentially with non-UI-Threads.
          |   |
          |   +- .RunSerial<T>(Func<T> lastJob,
          |                    bool isUiThreadLastJob = false,
          |                    params Job[] jobs)
          |       Execute a continuous job with return value.
          |
          +- Job(instance) - for serial task processing
              |
              +- Action { get; }
              |  Job Logic
              |
              +- IsExecUIThread { get; }
              |  Whether it is necessary to run in the UI-Thread.
              |
              +- DelayMSec { get; }
              |  Start Delay
              |
              +- JobName { get; }
                 Name of Info in Job-Manager.
           


## Licence

[MIT Licence](https://github.com/ume05rw/Xb.App.Job/blob/master/LICENSE)

## Author

[Do-Be's](http://dobes.jp)
