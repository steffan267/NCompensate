using System;
using System.Threading.Tasks;

namespace NCompensate
{
    public static class CompensableExtensions
    {
        public static Transaction Add<TResult>(this Transaction pipe, Func<Task<TResult>> action)
        {
            pipe.Add(new Compensable<TResult>(action));
            return pipe;
        }

        public static Transaction Add<TResult>(this Transaction pipe, Func<Task<TResult>> action, Func<TResult, Task> compensation,CompensationStrategy strategy = CompensationStrategy.WhenCalled)
        {
            pipe.Add(new Compensable<TResult>(action, compensation, strategy));
            return pipe;
        }

        public static Transaction AddConditionally<TResult>(this Transaction pipe, bool condition, Func<Task<TResult>> action, Func<TResult, Task> compensation = null)
        {
            pipe.AddConditionally(condition, new Compensable<TResult>(action, compensation));
            return pipe;
        }

        public static Transaction AddConditionally<TResult>(this Transaction pipe, Func<bool> deferedCondition, Func<Task<TResult>> action, Func<TResult, Task> compensation, CompensationStrategy strategy = CompensationStrategy.WhenCalled)
        {
            pipe.AddConditionally(deferedCondition, new Compensable<TResult>(action, compensation,strategy));
            return pipe;
        }
    }
}