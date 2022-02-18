using System;
using System.Threading.Tasks;

namespace NCompensate
{
    public class Compensable<TResult> : ICompensable
    {
        private readonly Func<Task<TResult>> _action;
        private CompensationStrategy _strategy;
        private Func<TResult, Task> _compensation;
        private TResult _result;
        public bool Called;
        public TResult Result => 
            Called ? _result : throw new InvalidOperationException("Result is only available when Compensable.Called is true");

        public Compensable(Func<Task<TResult>> action, Func<TResult, Task> compensation = null, CompensationStrategy strategy = CompensationStrategy.WhenCalled)
        {
            _action = action;
            _compensation = compensation ?? (async (_) => await Task.CompletedTask);
            _strategy = strategy;
        }

        public async Task InvokeAsync()
        {
            _result = await _action.Invoke();
            Called = true;
        }
	
        public async Task CompensateAsync()
        {
            if (Called)
            {
                await _compensation.Invoke(Result);
            }
            else if (_strategy == CompensationStrategy.Always)
            {
                await _compensation.Invoke(default);
            }
        }

        public Compensable<TResult> WithCompensation(Func<TResult, Task> compensation = null, CompensationStrategy strategy = CompensationStrategy.WhenCalled)
        {
            _compensation = compensation ?? throw new ArgumentNullException("Compensation can't be null");
            _strategy = strategy;
            return this;
        }
    }
}
