using System;
using System.Threading;
using NUnit.Framework;

namespace UIKit.Tests
{
    public class ScreenNavigatorRunTests : NavigatorTestBase
    {
        private int? answerOnOpen;

        [SetUp]
        public void RegisterScreens()
        {
            answerOnOpen = null;
            ui.Register<ScreenAViewModel>();
            ui.RegisterWith(() => new AnswerViewModel { AnswerOnOpen = answerOnOpen });
            ui.RegisterWith(() => new ConfirmViewModel());
            ui.RegisterWith(() => new GreetingViewModel());
        }

        [Test]
        public void RunAsync_ResultSet_ReturnsItAndClosesTheScreen()
        {
            ui.Open<ScreenAViewModel>();
            var run = ui.Navigator.RunAsync<AnswerViewModel, int>();
            var answer = ui.LastCreated<AnswerViewModel>();
            Assert.That(run.GetAwaiter().IsCompleted, Is.False);

            answer.SetResult(42);

            Assert.That(AwaitableAssert.Completes(run), Is.EqualTo(42));
            Assert.That(ui.Navigator.IsOpen(answer), Is.False);
            Assert.That(answer.IsDisposed, Is.True);
        }

        [Test]
        public void RunAsync_ResultSetDuringOnOpened_ReturnsIt()
        {
            answerOnOpen = 7;

            var run = ui.Navigator.RunAsync<AnswerViewModel, int>();

            Assert.That(AwaitableAssert.Completes(run), Is.EqualTo(7));
        }

        [Test]
        public void RunAsync_WithPayload_DeliversItBeforeWaiting()
        {
            var run = ui.Navigator.RunAsync<GreetingViewModel, string, string>("Ann");
            var greeting = ui.LastCreated<GreetingViewModel>();

            greeting.SetResult($"Hi {greeting.Name}");

            Assert.That(AwaitableAssert.Completes(run), Is.EqualTo("Hi Ann"));
        }

        [Test]
        public void RunAsync_PopupDismissedWithGoBack_ReturnsTheCancelResult()
        {
            var run = ui.Navigator.RunAsync<ConfirmViewModel, bool>();

            AwaitableAssert.Completes(ui.Navigator.GoBackAsync());

            Assert.That(AwaitableAssert.Completes(run), Is.False);
            Assert.That(ui.LastCreated<ConfirmViewModel>().CancelCount, Is.EqualTo(1));
        }

        [Test]
        public void RunAsync_PopupClosedFromElsewhere_ReturnsTheCancelResult()
        {
            var run = ui.Navigator.RunAsync<ConfirmViewModel, bool>();

            AwaitableAssert.Completes(ui.Navigator.CloseAsync(ui.LastCreated<ConfirmViewModel>()));

            Assert.That(AwaitableAssert.Completes(run), Is.False);
        }

        [Test]
        public void RunAsync_PopupAnswered_DoesNotCancelItWhileClosing()
        {
            var run = ui.Navigator.RunAsync<ConfirmViewModel, bool>();
            var confirm = ui.LastCreated<ConfirmViewModel>();

            confirm.SetResult(true);

            Assert.That(AwaitableAssert.Completes(run), Is.True);
            Assert.That(confirm.CancelCount, Is.Zero);
        }

        [Test]
        public void RunAsync_NonCancelableClosedFromElsewhere_FaultsWithOperationCanceled()
        {
            var run = ui.Navigator.RunAsync<AnswerViewModel, int>();

            AwaitableAssert.Completes(ui.Navigator.CloseAsync(ui.LastCreated<AnswerViewModel>()));

            AwaitableAssert.Faults<OperationCanceledException, int>(run);
        }

        [Test]
        public void RunAsync_TokenCancelledWhileWaiting_FaultsAndClosesTheScreen()
        {
            using var cancellation = new CancellationTokenSource();
            var run = ui.Navigator.RunAsync<AnswerViewModel, int>(cancellation.Token);
            var answer = ui.LastCreated<AnswerViewModel>();

            cancellation.Cancel();

            AwaitableAssert.Faults<OperationCanceledException, int>(run);
            Assert.That(ui.Navigator.IsOpen(answer), Is.False);
            Assert.That(answer.IsDisposed, Is.True);
        }

        [Test]
        public void RunAsync_TokenCancelledWhileWaiting_StillPlaysTheHideAnimation()
        {
            using var cancellation = new CancellationTokenSource();
            var run = ui.Navigator.RunAsync<AnswerViewModel, int>(cancellation.Token);
            var view = ui.LastView<AnswerViewModel>();

            cancellation.Cancel();

            AwaitableAssert.Faults<OperationCanceledException, int>(run);
            Assert.That(view.HideCount, Is.EqualTo(1));
        }

        [Test]
        public void RunAsync_ChildViewOnly_Faults()
        {
            ui.Context.RegisterView<BadgeResultViewModel, TestView<BadgeResultViewModel>>(
                () => new TestView<BadgeResultViewModel>());

            var thrown = AwaitableAssert.Faults<InvalidOperationException, int>(
                ui.Navigator.RunAsync<BadgeResultViewModel, int>());

            StringAssert.Contains("child view only", thrown.Message);
        }

        [Test]
        public void RunAsync_ContextDisposedWhileWaiting_FaultsWithOperationCanceled()
        {
            var run = ui.Navigator.RunAsync<AnswerViewModel, int>();

            ui.Context.Dispose();

            AwaitableAssert.Faults<OperationCanceledException, int>(run);
        }
    }
}
