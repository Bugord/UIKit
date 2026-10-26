using System;
using System.Threading;
using NUnit.Framework;

namespace UIKit.Tests
{
    public class ResultViewModelTests
    {
        private AnswerViewModel answer;

        [SetUp]
        public void SetUp()
        {
            answer = new AnswerViewModel();
        }

        [Test]
        public void WaitForResultAsync_ThenSetResult_CompletesWithIt()
        {
            var wait = answer.WaitForResultAsync();
            Assert.That(wait.GetAwaiter().IsCompleted, Is.False);

            answer.SetResult(3);

            Assert.That(AwaitableAssert.Completes(wait), Is.EqualTo(3));
        }

        [Test]
        public void WaitForResultAsync_AfterSetResult_ReturnsTheKeptResult()
        {
            answer.SetResult(3);

            Assert.That(AwaitableAssert.Completes(answer.WaitForResultAsync()), Is.EqualTo(3));
        }

        [Test]
        public void SetResult_Twice_KeepsTheFirst()
        {
            answer.SetResult(1);
            answer.SetResult(2);

            Assert.That(AwaitableAssert.Completes(answer.WaitForResultAsync()), Is.EqualTo(1));
        }

        [Test]
        public void SetResult_CompletesEveryPendingWait()
        {
            var first = answer.WaitForResultAsync();
            var second = answer.WaitForResultAsync();

            answer.SetResult(4);

            Assert.That(AwaitableAssert.Completes(first), Is.EqualTo(4));
            Assert.That(AwaitableAssert.Completes(second), Is.EqualTo(4));
        }

        [Test]
        public void WaitForResultAsync_TokenCancelled_CancelsOnlyThatWait()
        {
            using var cancellation = new CancellationTokenSource();
            var cancelled = answer.WaitForResultAsync(cancellation.Token);
            var other = answer.WaitForResultAsync();

            cancellation.Cancel();
            answer.SetResult(5);

            AwaitableAssert.Faults<OperationCanceledException, int>(cancelled);
            Assert.That(AwaitableAssert.Completes(other), Is.EqualTo(5));
        }

        [Test]
        public void WaitForResultAsync_TokenAlreadyCancelled_IsCancelled()
        {
            AwaitableAssert.Faults<OperationCanceledException, int>(answer.WaitForResultAsync(new CancellationToken(true)));
        }

        [Test]
        public void WaitForResultAsync_TokenAlreadyCancelledButResultSet_ReturnsTheResult()
        {
            answer.SetResult(6);

            Assert.That(AwaitableAssert.Completes(answer.WaitForResultAsync(new CancellationToken(true))), Is.EqualTo(6));
        }

        [Test]
        public void WaitForResultAsync_TokenCancelledAfterTheResult_DoesNotThrow()
        {
            using var cancellation = new CancellationTokenSource();
            var wait = answer.WaitForResultAsync(cancellation.Token);
            answer.SetResult(7);

            Assert.DoesNotThrow(() => cancellation.Cancel());
            Assert.That(AwaitableAssert.Completes(wait), Is.EqualTo(7));
        }

        [Test]
        public void Dispose_WithoutAResult_CancelsPendingWaits()
        {
            var wait = answer.WaitForResultAsync();

            answer.Dispose();

            AwaitableAssert.Faults<OperationCanceledException, int>(wait);
        }

        [Test]
        public void WaitForResultAsync_AfterDisposeWithoutAResult_IsCancelled()
        {
            answer.Dispose();

            AwaitableAssert.Faults<OperationCanceledException, int>(answer.WaitForResultAsync());
        }

        [Test]
        public void WaitForResultAsync_AfterDisposeWithAResult_ReturnsTheResult()
        {
            answer.SetResult(8);
            answer.Dispose();

            Assert.That(AwaitableAssert.Completes(answer.WaitForResultAsync()), Is.EqualTo(8));
        }

        [Test]
        public void SetResult_AfterDispose_IsIgnored()
        {
            answer.Dispose();

            answer.SetResult(9);

            Assert.That(answer.HasResult, Is.False);
        }

        [Test]
        public void PopupCancel_SetsTheCancelResult()
        {
            var confirm = new ConfirmViewModel();

            confirm.Cancel();

            Assert.That(AwaitableAssert.Completes(confirm.WaitForResultAsync()), Is.False);
        }

        [Test]
        public void PopupCancel_AfterAResult_KeepsTheResult()
        {
            var confirm = new ConfirmViewModel();
            confirm.SetResult(true);

            confirm.Cancel();

            Assert.That(AwaitableAssert.Completes(confirm.WaitForResultAsync()), Is.True);
        }
    }
}
