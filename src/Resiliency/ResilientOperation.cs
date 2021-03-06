﻿using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
{
    public partial class ResilientOperation
    {
        /// <summary>
        /// Create a <see cref="ResilientActionBuilder"/> from an existing async action with cancellation support. 
        /// </summary>
        /// <param name="action">An async action, that supports cancellation, that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientActionBuilder From(
            Func<CancellationToken, Task> action,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "")
        {
            return new ResilientActionBuilder(async (cancellationToken) =>
            {
                await action(cancellationToken).ConfigureAwait(false);

                return default;
            }, sourceLineNumber, sourceFilePath, memberName);
        }

        /// <summary>
        /// Create a <see cref="ResilientActionBuilder"/> from an existing async action. 
        /// </summary>
        /// <param name="action">An async action that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientActionBuilder From(
            Func<Task> action, 
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "")
        {
            return new ResilientActionBuilder(async (cancellationToken) => 
            {
                await action().ConfigureAwait(false);

                return default;
            }, sourceLineNumber, sourceFilePath, memberName);
        }

        /// <summary>
        /// Create a <see cref="ResilientActionBuilder"/> from an existing synchronous action. 
        /// </summary>
        /// <param name="action">A synchronous action that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientActionBuilder From(
            Action action,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "")
        {
            return new ResilientActionBuilder((cancellationToken) =>
            {
                action();

                return Task.FromResult(default(Unit));
            }, sourceLineNumber, sourceFilePath, memberName);
        }

        /// <summary>
        /// Provide an implementation of IWaiter that can use mechanisms other than Task.Delay, such as for Unit tests with mocks.
        /// </summary>
        /// <returns>A function that given a <see cref="CancellationToken"/>, will get a waiter object.</returns>
        public static Func<CancellationToken, IWaiter> WaiterFactory { internal get; set; } =
            (cancellationToken) => new TaskDelayWaiter(cancellationToken);

        /// <summary>
        /// Create a <see cref="ResilientFuncBuilder{TFunction, TResult}"/> from an existing async function with cancellation support. 
        /// </summary>
        /// <param name="function">An async function, that supports cancellation, that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientFuncBuilder<Func<CancellationToken, Task<TResult>>, TResult> From<TResult>(
            Func<CancellationToken, Task<TResult>> function,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "")
        {
            return new ResilientFuncBuilder<Func<CancellationToken, Task<TResult>>, TResult>(function, sourceLineNumber, sourceFilePath, memberName);
        }

        /// <summary>
        /// Create a <see cref="ResilientFuncBuilder{TFunction, TResult}"/> from an existing async function. 
        /// </summary>
        /// <param name="function">An async function that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientFuncBuilder<Func<Task<TResult>>, TResult> From<TResult>(
            Func<Task<TResult>> function,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "")
        {
            return new ResilientFuncBuilder<Func<Task<TResult>>, TResult>(function, sourceLineNumber, sourceFilePath, memberName);
        }

        /// <summary>
        /// Create a <see cref="ResilientFuncBuilder{TFunction, TResult}"/> from an existing synchronous function. 
        /// </summary>
        /// <param name="function">A synchronous function that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientFuncBuilder<Func<TResult>, TResult> From<TResult>(
            Func<TResult> function,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "")
        {
            return new ResilientFuncBuilder<Func<TResult>, TResult>(function, sourceLineNumber, sourceFilePath, memberName);
        }
    }

    public partial class ResilientOperation<TResult>
        : IResilientOperation<TResult>
        , IResilientOperationInfo
    {
        public ResilientOperation(
            string implicitOperationKey,
            ResilientOperationHandlerInfo handler,
            ResilientOperationTotalInfo total,
            CancellationToken cancellationToken)
        {
            ImplicitOperationKey = implicitOperationKey;
            Handler = handler;
            Total = total;
            CancellationToken = cancellationToken;
            DefaultCircuitBreaker = CircuitBreaker.GetCircuitBreaker(implicitOperationKey);
        }

        public string ImplicitOperationKey { get; }

        public virtual ResilientOperationHandlerInfo Handler { get; }

        public virtual ResilientOperationTotalInfo Total { get; }

        public virtual CancellationToken CancellationToken { get; }

        public virtual CircuitBreaker DefaultCircuitBreaker { get; }

        public virtual HandlerResult HandlerResult { get; set; }

        public virtual TResult Result { get; set; }

        public virtual int CurrentAttempt => Total.CurrentAttempt;

        /// <summary>
        /// Informs the operation to retry once the handler finishes execution.
        /// </summary>
        public virtual void Retry()
        {
            HandlerResult = HandlerResult.Retry;
        }

        /// <summary>
        /// Informs the operation to stop without trying again; no value will be returned.
        /// </summary>
        public virtual void Break()
        {
            HandlerResult = HandlerResult.Break;
        }

        /// <summary>
        /// Informs the operation to stop without trying again and provides a value to be returned.
        /// </summary>
        public virtual void Return(TResult result)
        {
            Result = result;

            HandlerResult = HandlerResult.Return;
        }
    }
}
