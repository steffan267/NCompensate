using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace NCompensate.Tests
{
    [Parallelizable(ParallelScope.All)]
    public class Tests
    {
        [Test]
        public void Called_When_InvokeAsync_Not_Invoked_Should_Be_False()
        {

            //arrange
            var compensable = new Compensable<bool>(null);
            
            //act

            //assert
            compensable.Called.Should().BeFalse();
        }
        
        [Test]
        public async Task Called_When_InvokeAsync_Invoked_Should_Be_true()
        {
            //arrange
            var compensable = new Compensable<bool>(GetTrueReturningAction());
            
            //act
            await compensable.InvokeAsync();

            //assert
            compensable.Called.Should().BeTrue();
        }
        
        [Test]
        public async Task Result_When_InvokeAsync_Invoked_Should_Be_true()
        {
            //arrange
            var compensable = new Compensable<bool>(GetTrueReturningAction());
            
            //act
            await compensable.InvokeAsync();

            //assert
            compensable.Result.Should().BeTrue();
        }
        
        [Test]
        public void Result_When_InvokeAsync_Not_Invoked_Should_Throw_InvalidOperationException()
        {
            //arrange
            var compensable = new Compensable<bool>(GetTrueReturningAction());
            
            //act
            Func<bool> variable = () => compensable.Result;

            //assert
            variable.Should().Throw<InvalidOperationException>();
        }

        private static Func<Task<bool>> GetTrueReturningAction()
        {
            var action = new Func<Task<bool>>(async () =>
            {
                await Task.Delay(0);
                return true;
            });
            return action;
        }

        [Test]
        public async Task CompensateAsync_With_WhenCalled_Compensation_Strategy_And_Failed_InvokeAsync_Should_Not_Compensate()
        {
            //arrange
            var compensated = false;
            var compensable = new Compensable<bool>(ExceptionAction(), (_) =>
            {
                compensated = true;
                return Task.Delay(0);
            });

            //act
            await TestHelp.SuppressException(async () => await compensable.InvokeAsync());
            await compensable.CompensateAsync();
            
            //assert
            compensated.Should().BeFalse();
        }
        
        [Test]
        public async Task CompensateAsync_With_Always_Compensate_Strategy_And_Failed_InvokeAsync_Should_Compensate()
        {
            //arrange
            var compensated = false;
            var compensable = new Compensable<bool>(ExceptionAction(), (_) =>
            {
                compensated = true;
                return Task.Delay(0);
            },CompensationStrategy.Always);

            //act
            await TestHelp.SuppressException(async () => await compensable.InvokeAsync());
            await compensable.CompensateAsync();
            
            //assert
            compensated.Should().BeTrue();
        }

        private static Func<Task<bool>> ExceptionAction()
        {
            return () => throw new Exception();
        }
    }
}