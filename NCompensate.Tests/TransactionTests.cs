using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace NCompensate.Tests
{
    [Parallelizable(ParallelScope.All)]
    public class TransactionTests
    {
        [Test]
        public async Task InvokeWithCompensationAsync_Should_Invoke_Compensation_On_Parent_Actions_On_Failure()
        {
            //arrange
            var compensated = false;
            var transaction = Transaction.New()
                .Add(
                    async () => await Task.FromResult(true),
                    async (_) =>
                    {
                        compensated = true;
                        await Task.Delay(0);
                    })
                .Add(async () =>
                {
                    throw new Exception();
                    return await Task.FromResult(true);
                });
            
            //act
            await TestHelp.SuppressException( async () => await transaction.InvokeWithCompensationAsync());
            
            //assert
            compensated.Should().BeTrue();
        }

        [Test]
        public async Task InvokeWithCompensationAsync_Should_Not_Invoke_Compensation_On_Parent_Actions_On_Success()
        {
            //arrange
            var compensated = false;
            var transaction = Transaction.New()
                .Add(
                    async () => await Task.FromResult(true),
                async (_) =>
                    {
                        compensated = true;
                        await Task.Delay(0);
                    })
                .Add(async () => await Task.FromResult(true));
            
            //act
            await transaction.InvokeWithCompensationAsync();
            
            //assert
            compensated.Should().BeFalse();
        }
        
        [Test]
        public async Task InvokeWithCompensationAsync_With_Suppress_Exception_Strategy_Should_Ignore_Exceptions()
        {
            //arrange
            var transaction = Transaction.New(new TransactionConfiguration(){ ErrorStrategy = ErrorStrategy.Ignore})
                .Add(
                    async () => await Task.FromResult(true),
                    async (_) =>
                    {
                        await Task.Delay(0);
                        throw new ArgumentException();
                    })
                .Add(async () =>
                {
                    throw new ArgumentException();
                    return await Task.FromResult(true);
                });
            
            //act
            Func<Task> transactionAction = async () => await transaction.InvokeWithCompensationAsync();
            
            //assert
            await transactionAction.Should().NotThrowAsync();
        }
        
        [Test]
        public async Task InvokeWithCompensationAsync_With_Throw_Exception_Strategy_Should_Throw_Exception()
        {
            //arrange
            var transaction = Transaction.New()
                .Add(
                    async () => await Task.FromResult(true),
                    async (_) =>
                    {
                        await Task.Delay(0);
                    })
                .Add(async () =>
                {
                    throw new ArgumentException();
                    return await Task.FromResult(true);
                });
            
            //act
            Func<Task> transactionAction = async () => await transaction.InvokeWithCompensationAsync();
            
            //assert
            await transactionAction.Should().ThrowAsync<ArgumentException>();
        }
        
        [Test]
        public async Task InvokeWithCompensationAsync_With_Throw_Exception_Strategy_And_Compensate_Throws_Exception_Should_Throw_AggregateException()
        {
            //arrange
            var transaction = Transaction.New()
                .Add(
                    async () => await Task.FromResult(true),
                    async (_) =>
                    {
                        await Task.Delay(0);
                        throw new NullReferenceException();
                    })
                .Add(async () =>
                {
                    throw new ArgumentException();
                    return await Task.FromResult(true);
                });
            
            //act
            Func<Task> transactionAction = async () => await transaction.InvokeWithCompensationAsync();
            
            //assert
            await transactionAction.Should().ThrowAsync<AggregateException>();
        }

        [Test]
        public async Task InvokeWithCompensation_When_Compensation_Cancelled_Should_Throw_OperationCancelledException()
        {
            //arrange
            var cancelToken = new CancellationTokenSource();
            async Task LongTask(bool i)
            {
                await Task.Delay(5000, cancelToken.Token);
            }
            var config = new TransactionConfiguration(cancelToken.Token, InvocationOrder.Unordered);
            
            //act
            Func<Task> transaction = async () => await Transaction.New(config).Add(() =>
            {
                throw new ArgumentException();
                return Task.FromResult(true);
            },LongTask,CompensationStrategy.Always).InvokeWithCompensationAsync();
            cancelToken.Cancel();
            
            //assert
            await transaction.Should().ThrowAsync<OperationCanceledException>();
        }
        
        [Test]
        public async Task InvokeWithCompensation_When_Invocation_Cancelled_Should_Throw_OperationCancelledException()
        {
            //arrange
            var cancelToken = new CancellationTokenSource();
            async Task<object> LongTask()
            {
                await Task.Delay(5000, cancelToken.Token);
                return null;
            }
            var config = new TransactionConfiguration(cancellationToken: cancelToken.Token);
            
            //act
            Func<Task> transaction = async () => await Transaction.New(config).Add(LongTask).InvokeWithCompensationAsync();
            cancelToken.Cancel();
            
            //assert
            await transaction.Should().ThrowAsync<OperationCanceledException>();
        }

        
        
        //TODO: Figure out how to reliably test this (without randomization)
        [Test]
        public async Task InvokeAllAsync_With_Unordered_Execution_Should_Return_Unordered_List()
        {
            //arrange
            var random = new Random();
            var list = new ConcurrentQueue<int>();
            var functions = Enumerable.Range(0, 10).Select(i => new Func<Task<int>>(async () =>
            {
                await Task.Delay(random.Next(i, 10));
                list.Enqueue(i);
                return i;
            })).ToList();
            var pipe = Transaction.New(new TransactionConfiguration(){ InvokeOrder = InvocationOrder.Unordered });
            functions.ForEach(p => pipe.Add(p));
            
            //act
            await pipe.InvokeWithCompensationAsync();
            
            //assert
            list.Should().NotBeInAscendingOrder();
        }
    }
}