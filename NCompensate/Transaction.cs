using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NCompensate
{
    public class Transaction
    {
        private TransactionConfiguration Configuration { get; }

        private List<(Func<bool> shouldRun,ICompensable compensable)> _actions = new(5);

        private IEnumerable<ICompensable> _compensables => _actions.Where(pair => pair.shouldRun.Invoke()).Select(pair => pair.compensable);

        private Transaction(TransactionConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static Transaction New(TransactionConfiguration config = null)
        {
            return new Transaction(config ?? new TransactionConfiguration());
        }

        public Transaction Add(ICompensable compensable)
        {
            _actions.Add((() => true, compensable));
            return this;
        }
        
        /// <summary>
        /// Evaluated immediately. deferred evaluation can be provided with <see cref="AddConditionalStep(Func´bool´,Transaction.ICompensable)"/>
        /// </summary>
        /// <returns></returns>
        public Transaction AddConditionally(bool condition, ICompensable compensable)
        {
            if (condition)
            {
                Add(compensable);
            }

            return this;
        }

        /// <summary>
        /// Waits with evaluating condition until <see cref="InvokeAllAsync"/> is called
        /// </summary>
        public Transaction AddConditionally(Func<bool> condition, ICompensable compensable)
        {
            _actions.Add((condition,compensable));

            return this;
        }
        
        /// <summary>
        /// Invokes all <see cref="ICompensable"/><see cref="ICompensable.InvokeAsync"/> 
        /// </summary>
        public async Task InvokeAllAsync()
        {
            await Invocation(_compensables.Select(r => new Func<Task>(async () => await r.InvokeAsync())));
        }

        /// <summary>
        /// Invokes all <see cref="ICompensable"/><see cref="ICompensable.CompensateAsync"/> In Reverse Order
        /// </summary>
        public async Task CompensateAllAsync()
        {
            await Invocation(_compensables.Select(r => new Func<Task>(async () => await r.CompensateAsync())).Reverse());
        }

        /// <summary>
        /// <inheritdoc cref="InvokeAllAsync"/> and
        /// <inheritdoc cref="CompensateAllAsync"/>
        /// <exception cref="Exception">Any exception thrown in InvokeAsync if <see cref="ErrorStrategy"/> not <see cref="ErrorStrategy.Ignore"/></exception>
        /// <exception cref="AggregateException">Any exception thrown in <see cref="ICompensable.CompensateAsync"/> when <see cref="ErrorStrategy"/> is <see cref="ErrorStrategy.Throw"/></exception>
        /// </summary>
        public async Task InvokeWithCompensationAsync()
        {
            try
            {
                await InvokeAllAsync();
            }
            catch (Exception invokeException) when (invokeException is not OperationCanceledException)
            {
                try
                {
                    await CompensateAllAsync();
                }
                catch (Exception compensationException) when (compensationException is not OperationCanceledException)
                { 
                    Throw(new AggregateException(compensationException, invokeException));
                }
                Throw(invokeException);
            }
        }

        private void Throw(Exception e)
        {
            if (Configuration.ErrorStrategy == ErrorStrategy.Throw)
            {
                throw e;
            }
        }

        private async Task Invocation(IEnumerable<Func<Task>> compensables)
        {
            if (Configuration.InvokeOrder is InvocationOrder.Ordered)
            {
                await compensables.ToAsyncEnumerable()
                    .ForEachAwaitAsync(async c => await c.Invoke(),Configuration.CanellationToken);
            }
            else
            {
                await Task.WhenAll(compensables.Select( async t => await t.Invoke()));
            }
        }
    }
}
