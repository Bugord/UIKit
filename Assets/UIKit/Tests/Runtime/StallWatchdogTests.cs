using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UIKit.Tests.PlayMode
{
    // The watchdog measures real time, so these tests wait for it - bounded, and with a
    // threshold far below the wait.
    public class StallWatchdogTests : PanelTestBase
    {
        private const float Threshold = 0.05f;
        private const float WaitSeconds = 2f;

        private static readonly Regex StallMessage = new("navigation has been blocked .* by 'open SlowViewModel'");

        private static readonly Regex AnyStallMessage = new("navigation has been blocked");

        private AwaitableCompletionSource slowShow;
        private AwaitableCompletionSource stubbornShow;
        private int stallErrors;

        private protected override void RegisterScreens()
        {
            // NUnit reuses one fixture instance for every test in it.
            stallErrors = 0;
            slowShow = new AwaitableCompletionSource();
            stubbornShow = new AwaitableCompletionSource();
            RegisterHeld<SlowViewModel>(slowShow);
            Context.Register(() => new StubbornViewModel(),
                () => new PlayView<StubbornViewModel>(stubbornShow, ignoreCancellation: true));
            Register<HomeViewModel>();
            Application.logMessageReceived += CountStallErrors;
        }

        [TearDown]
        public void ReleaseSlowShow()
        {
            Application.logMessageReceived -= CountStallErrors;

            // Before the base tear-down disposes the context, so the queued open runs through.
            slowShow.TrySetResult();
        }

        [UnityTest]
        public IEnumerator Stall_QueuedBehindALongShow_LogsAnError()
        {
            Context.StallWarningSeconds = Threshold;
            LogAssert.Expect(LogType.Error, StallMessage);

            Navigator.OpenAsync<SlowViewModel>();
            Navigator.OpenAsync<HomeViewModel>();
            yield return UntilTime(() => stallErrors > 0);

            Assert.That(stallErrors, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Stall_StillBlocked_LogsTheErrorOnlyOnce()
        {
            Context.StallWarningSeconds = Threshold;
            LogAssert.Expect(LogType.Error, StallMessage);
            Navigator.OpenAsync<SlowViewModel>();
            Navigator.OpenAsync<HomeViewModel>();
            yield return UntilTime(() => stallErrors > 0);

            yield return UntilTime(() => stallErrors > 1, Threshold * 10);

            Assert.That(stallErrors, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Stall_NothingQueued_LogsNothing()
        {
            Context.StallWarningSeconds = Threshold;

            Navigator.OpenAsync<SlowViewModel>();
            yield return UntilTime(() => stallErrors > 0, Threshold * 10);

            Assert.That(stallErrors, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Stall_WarningDisabled_LogsNothing()
        {
            Context.StallWarningSeconds = 0;

            Navigator.OpenAsync<SlowViewModel>();
            Navigator.OpenAsync<HomeViewModel>();
            yield return UntilTime(() => stallErrors > 0, Threshold * 10);

            Assert.That(stallErrors, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Stall_ShowEndsInTime_LogsNothing()
        {
            Context.StallWarningSeconds = 1f;
            Navigator.OpenAsync<SlowViewModel>();
            var home = Navigator.OpenAsync<HomeViewModel>();

            slowShow.SetResult();
            yield return UntilTime(() => stallErrors > 0, Threshold * 10);

            Assert.That(home.GetAwaiter().IsCompleted, Is.True);
            Assert.That(stallErrors, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Stall_ContextDisposedWhileAShowIgnoresCancellation_LogsNothing()
        {
            Context.StallWarningSeconds = Threshold;
            Navigator.OpenAsync<StubbornViewModel>();
            Navigator.OpenAsync<HomeViewModel>();

            // The show ignores the cancellation, so the gate stays held with the open queued.
            Context.Dispose();
            yield return UntilTime(() => stallErrors > 0, Threshold * 10);

            Assert.That(stallErrors, Is.Zero);
        }

        private void CountStallErrors(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error && AnyStallMessage.IsMatch(condition)) {
                stallErrors++;
            }
        }

        private static IEnumerator UntilTime(System.Func<bool> condition, float seconds = WaitSeconds)
        {
            var deadline = Time.realtimeSinceStartupAsDouble + seconds;

            while (!condition() && Time.realtimeSinceStartupAsDouble < deadline) {
                yield return null;
            }
        }
    }
}
