using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonoGameGumFromFile.Managers
{
    public class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<Action> _messagesToProcess = new Queue<Action>();
        private readonly object _syncHandle = new object();

        public SingleThreadSynchronizationContext()
        {
            SynchronizationContext.SetSynchronizationContext(this);
        }

        public override void Send(SendOrPostCallback codeToRun, object state)
        {
            throw new NotImplementedException();
        }

        public override void Post(SendOrPostCallback codeToRun, object state)
        {
            lock (_syncHandle)
            {
                _messagesToProcess.Enqueue(() =>
                {
                    try
                    {
                        codeToRun(state);
                    }
                    catch (TaskCanceledException) { }
                });
                SignalContinue();
            }
        }


        private void SignalContinue()
        {
            Monitor.Pulse(_syncHandle);
        }

        Queue<Action> thisFrameQueue = new Queue<Action>();
        public void Update()
        {
            // Normally with async methods, it's all time-based. For example,
            // you do an await Task.Delay(100); and you expect that it waits 100 
            // milliseconds, then continues. FRB is a little different - it needs
            // to do time based, but the time we're interested in specifically is game
            // time. We use TimeManager.DelaySeconds to make sure that everything respects
            // game time and time factor (slow-mo, speed-up, and pausing). Therefore, we need
            // to check tasks every time Update is called because there could be any amount of 
            // time passed between frames. Frames could speed up considerably if the user drags
            // the window or performs some other kind of logic that freezes the window temporarily 
            // and then MonoGame calls Update a bunch of times to play "catch-up". Therefore, the
            // TimeManager.DelaySeconds will not internally wait anything, it calls Task.Yield which
            // immediately puts the task back on the message to process. Before running any code we want
            // to empty the _messagesToProcess into thisFrameQueue, then run all tasks on this frame. Next
            // frame everything will repeat.

            lock (_syncHandle)
            {
                while (_messagesToProcess.Count > 0)
                {
                    thisFrameQueue.Enqueue(_messagesToProcess.Dequeue());
                }
            }

            while (thisFrameQueue.Count > 0)
            {
                var actionToRun = thisFrameQueue.Dequeue();
                try
                {
                    actionToRun();
                }
                catch (TaskCanceledException)
                {
                    // Most likely the task was cancelled due to moving screens.
                    // Nothing is to be done for cancelled tasks.
                }
            }
        }

        public void UpdateDependencies()
        {

        }

        public void Clear()
        {
            lock (_syncHandle)
            {
                _messagesToProcess.Clear();
            }

        }
    }
}
