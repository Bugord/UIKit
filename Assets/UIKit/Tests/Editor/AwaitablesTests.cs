using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UIKit.Tests
{
    public class AwaitablesTests
    {
        [Test]
        public void Completed_IsCompleted()
        {
            AwaitableAssert.Completes(Awaitables.Completed());
        }

        [Test]
        public void Completed_ReturnsASeparateInstanceEachCall()
        {
            // Awaitables are pooled and single-use, so a shared instance would break its second awaiter.
            var first = Awaitables.Completed();
            var second = Awaitables.Completed();

            Assert.That(second, Is.Not.SameAs(first));
            AwaitableAssert.Completes(first);
            AwaitableAssert.Completes(second);
        }

        [Test]
        public void FromResult_CompletesWithTheValue()
        {
            Assert.That(AwaitableAssert.Completes(Awaitables.FromResult("done")), Is.EqualTo("done"));
        }

        [Test]
        public void Forget_FaultedAwaitable_LogsTheException()
        {
            var source = new AwaitableCompletionSource();
            source.SetException(new InvalidOperationException("forgotten failure"));
            LogAssert.Expect(LogType.Exception, new Regex("forgotten failure"));

            source.Awaitable.Forget();
        }

        [Test]
        public void Forget_FaultedAwaitableOfT_LogsTheException()
        {
            var source = new AwaitableCompletionSource<int>();
            source.SetException(new InvalidOperationException("forgotten failure"));
            LogAssert.Expect(LogType.Exception, new Regex("forgotten failure"));

            source.Awaitable.Forget();
        }

        [Test]
        public void Forget_CancelledAwaitable_LogsNothing()
        {
            var source = new AwaitableCompletionSource();
            source.SetCanceled();

            source.Awaitable.Forget();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Forget_AwaitableFaultingLater_LogsWhenItFaults()
        {
            var source = new AwaitableCompletionSource();
            source.Awaitable.Forget();
            LogAssert.Expect(LogType.Exception, new Regex("late failure"));

            source.SetException(new InvalidOperationException("late failure"));
        }
    }
}
