using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCompensate
{
    public class TransactionConfiguration
    {
        public TransactionConfiguration(
            CancellationToken cancellationToken = default,
            InvocationOrder invokeOrder = InvocationOrder.Ordered,
            ErrorStrategy errorStrategy = ErrorStrategy.Throw)
        {
            CanellationToken = cancellationToken;
            InvokeOrder = invokeOrder;
            ErrorStrategy = errorStrategy;
        }

        public CancellationToken CanellationToken { get; set; }
        public InvocationOrder InvokeOrder { get; set; }
        
        public ErrorStrategy ErrorStrategy { get; set;  }
    }
}