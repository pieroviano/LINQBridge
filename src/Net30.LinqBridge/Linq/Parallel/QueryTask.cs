#nullable disable
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal abstract class QueryTask
{
    private static readonly Action<object> s_runTaskSynchronouslyDelegate = RunTaskSynchronously;
    private static readonly Action<object> s_baseWorkDelegate = o => ((QueryTask)o).BaseWork(null);
    protected QueryTaskGroupState m_groupState;
    protected int m_taskIndex;

    protected QueryTask(int taskIndex, QueryTaskGroupState groupState)
    {
        m_taskIndex = taskIndex;
        m_groupState = groupState;
    }

    protected abstract void Work();

    internal Task RunAsynchronously(TaskScheduler taskScheduler)
    {
        return Task.Factory.StartNew(s_baseWorkDelegate, this, new CancellationToken(),
            TaskCreationOptions.PreferFairness | TaskCreationOptions.AttachedToParent, taskScheduler);
    }

    internal Task RunSynchronously(TaskScheduler taskScheduler)
    {
        var task = new Task(s_runTaskSynchronouslyDelegate, this, TaskCreationOptions.AttachedToParent);
        task.RunSynchronously(taskScheduler);
        return task;
    }

    private void BaseWork(object unused)
    {
        Work();
    }

    private static void RunTaskSynchronously(object o)
    {
        ((QueryTask)o).BaseWork(null);
    }
}