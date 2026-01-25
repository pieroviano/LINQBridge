#nullable disable
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Expressions;

internal static class StackGuardEx
{
    // Tune this depending on your recursion size
    private const int MaxDepth = 8000;

    // Each thread tracks its own recursion depth
    [ThreadStatic] private static int _depth;

    public static void EnsureSufficientExecutionStack()
    {
        _depth++;

        if (_depth > MaxDepth)
        {
            _depth--;
            throw new InsufficientExecutionStackException(
                "Insufficient stack space to continue execution.");
        }
    }

    public static void Exit()
    {
        _depth--;
    }
}

public class InsufficientExecutionStackException : Exception
{
    public InsufficientExecutionStackException(string message) : base(message)
    {
    }
}

internal sealed class StackGuard
{
    private const int MaxExecutionStackCount = 1024 /*0x0400*/;
    private int _executionStackCount;

    public void RunOnEmptyStack<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
    {
        RunOnEmptyStackCore(s =>
        {
            var tuple = (Tuple<Action<T1, T2>, T1, T2>)s;
            tuple.Item1(tuple.Item2, tuple.Item3);
            return (object)null;
        }, Tuple.Create(action, arg1, arg2));
    }

    public void RunOnEmptyStack<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
    {
        RunOnEmptyStackCore(s =>
        {
            var tuple = (Tuple<Action<T1, T2, T3>, T1, T2, T3>)s;
            tuple.Item1(tuple.Item2, tuple.Item3, tuple.Item4);
            return (object)null;
        }, Tuple.Create(action, arg1, arg2, arg3));
    }

    public R RunOnEmptyStack<T1, T2, R>(Func<T1, T2, R> action, T1 arg1, T2 arg2)
    {
        return RunOnEmptyStackCore(s =>
        {
            var tuple = (Tuple<Func<T1, T2, R>, T1, T2>)s;
            return tuple.Item1(tuple.Item2, tuple.Item3);
        }, Tuple.Create(action, arg1, arg2));
    }

    public R RunOnEmptyStack<T1, T2, T3, R>(Func<T1, T2, T3, R> action, T1 arg1, T2 arg2, T3 arg3)
    {
        return RunOnEmptyStackCore(s =>
        {
            var tuple = (Tuple<Func<T1, T2, T3, R>, T1, T2, T3>)s;
            return tuple.Item1(tuple.Item2, tuple.Item3, tuple.Item4);
        }, Tuple.Create(action, arg1, arg2, arg3));
    }

    public bool TryEnterOnCurrentStack()
    {
        try
        {
            StackGuardEx.EnsureSufficientExecutionStack();
        }
        catch (InsufficientExecutionStackException ex) when (_executionStackCount < 1024 /*0x0400*/)
        {
            return false;
        }

        return true;
    }

    private R RunOnEmptyStackCore<R>(Func<object, R> action, object state)
    {
        ++_executionStackCount;
        try
        {
            var task = Task.Factory.StartNew(action, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
            var awaiter = task.GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                ((IAsyncResult)task).AsyncWaitHandle.WaitOne();
            }

            return awaiter.GetResult();
        }
        finally
        {
            --_executionStackCount;
        }
    }
}