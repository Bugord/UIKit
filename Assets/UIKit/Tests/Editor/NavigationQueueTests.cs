using NUnit.Framework;

namespace UIKit.Tests
{
    // Navigator operations are serialised: one started while another runs waits for it.
    public class NavigationQueueTests : NavigatorTestBase
    {
        [Test]
        public void OpenAsync_WhileAnotherOpenIsShowing_StartsOnlyOnceThatOneFinishes()
        {
            ui.Register<ScreenAViewModel>(view => view.HoldNextShow());
            ui.Register<ScreenBViewModel>();
            var openA = ui.Navigator.OpenAsync<ScreenAViewModel>();
            var showA = ui.LastView<ScreenAViewModel>();

            var openB = ui.Navigator.OpenAsync<ScreenBViewModel>();

            Assert.That(ui.Created, Has.None.InstanceOf<ScreenBViewModel>());
            Assert.That(openB.GetAwaiter().IsCompleted, Is.False);
            showA.ReleaseHeldShow();
            AwaitableAssert.Completes(openA);
            Assert.That(ui.Navigator.Top, Is.SameAs(AwaitableAssert.Completes(openB).ViewModel));
        }

        [Test]
        public void Operations_QueuedBehindARunningOne_ApplyInCallOrder()
        {
            ui.Register<ScreenAViewModel>(view => view.HoldNextShow());
            ui.Register<ScreenBViewModel>();
            ui.Register<ScreenCViewModel>();
            var openA = ui.Navigator.OpenAsync<ScreenAViewModel>();

            var openB = ui.Navigator.OpenAsync<ScreenBViewModel>();
            var goBack = ui.Navigator.GoBackAsync();
            var openC = ui.Navigator.OpenAsync<ScreenCViewModel>();
            ui.LastView<ScreenAViewModel>().ReleaseHeldShow();

            var a = AwaitableAssert.Completes(openA).ViewModel;
            var b = AwaitableAssert.Completes(openB).ViewModel;
            Assert.That(AwaitableAssert.Completes(goBack), Is.True);
            var c = AwaitableAssert.Completes(openC).ViewModel;
            Assert.That(b.IsDisposed, Is.True);
            Assert.That(ui.Navigator.IsOpen(a), Is.True);
            Assert.That(ui.Navigator.Top, Is.SameAs(c));
        }

        [Test]
        public void CloseAsync_WhileTheScreenIsStillOpening_ClosesItOnceItIsOpen()
        {
            ui.Register<ScreenAViewModel>();
            ui.Register<ScreenBViewModel>(view => view.HoldNextShow());
            var a = ui.Open<ScreenAViewModel>();
            var openB = ui.Navigator.OpenAsync<ScreenBViewModel>();
            var b = ui.LastCreated<ScreenBViewModel>();

            var close = ui.Navigator.CloseAsync(b);
            ui.ViewOf(b).ReleaseHeldShow();

            Assert.That(AwaitableAssert.Completes(openB).ViewModel, Is.SameAs(b));
            AwaitableAssert.Completes(close);
            Assert.That(b.OnOpenedCount, Is.EqualTo(1));
            Assert.That(b.IsDisposed, Is.True);
            Assert.That(ui.Navigator.Top, Is.SameAs(a));
            Assert.That(ui.ViewOf(a).IsShown, Is.True);
        }

        [Test]
        public void GoBackAsync_WhileAPopupIsOpening_ClosesThatPopup()
        {
            ui.Register<ScreenAViewModel>();
            ui.Register<PopupAViewModel>(view => view.HoldNextShow());
            var screen = ui.Open<ScreenAViewModel>();
            var openPopup = ui.Navigator.OpenAsync<PopupAViewModel>();

            var goBack = ui.Navigator.GoBackAsync();
            ui.LastView<PopupAViewModel>().ReleaseHeldShow();

            AwaitableAssert.Completes(openPopup);
            Assert.That(AwaitableAssert.Completes(goBack), Is.True);
            Assert.That(ui.Navigator.Top, Is.SameAs(screen));
        }
    }
}
