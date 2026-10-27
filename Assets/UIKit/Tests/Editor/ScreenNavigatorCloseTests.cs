using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UIKit.Tests
{
    public class ScreenNavigatorCloseTests : NavigatorTestBase
    {
        [SetUp]
        public void RegisterScreens()
        {
            ui.Register<ScreenAViewModel>();
            ui.Register<ScreenBViewModel>();
            ui.Register<ScreenCViewModel>();
            ui.Register<PopupAViewModel>();
            ui.Register<PopupBViewModel>();
            ui.Register<CancelableViewModel>();
        }

        [Test]
        public void CloseAsync_TopScreen_DisposesItAndShowsTheScreenBelow()
        {
            var a = ui.Open<ScreenAViewModel>();
            var b = ui.Open<ScreenBViewModel>();
            var bView = ui.ViewOf(b);

            AwaitableAssert.Completes(ui.Navigator.CloseAsync(b));

            Assert.That(ui.Navigator.IsOpen(b), Is.False);
            Assert.That(b.IsDisposed, Is.True);
            Assert.That(bView.IsDisposed, Is.True);
            Assert.That(ui.ScreenLayer.Contains(bView.Root), Is.False);
            Assert.That(ui.ViewOf(a).IsShown, Is.True);
            Assert.That(ui.Navigator.Top, Is.SameAs(a));
        }

        [Test]
        public void CloseAsync_TopScreen_RaisesOnClosedThenHidesThenDisposesThenShowsTheScreenBelow()
        {
            ui.Open<ScreenAViewModel>();
            var b = ui.Open<ScreenBViewModel>();
            ui.Log.Clear();

            AwaitableAssert.Completes(ui.Navigator.CloseAsync(b));

            Assert.That(ui.Log.Entries, Is.EqualTo(new[] {
                "ScreenB.OnClosed", "ScreenB.hide", "ScreenB.view.dispose", "ScreenB.OnDispose", "ScreenA.show"
            }));
        }

        [Test]
        public void CloseAsync_CoveredScreen_RemovesItWithoutTouchingTheTop()
        {
            var a = ui.Open<ScreenAViewModel>();
            var b = ui.Open<ScreenBViewModel>();
            var c = ui.Open<ScreenCViewModel>();
            ui.Log.Clear();

            AwaitableAssert.Completes(ui.Navigator.CloseAsync(b));

            Assert.That(b.IsDisposed, Is.True);
            Assert.That(ui.Log.Contains("ScreenB.hide"), Is.False, "an already hidden screen is not hidden again");
            Assert.That(ui.Navigator.Top, Is.SameAs(c));
            Assert.That(ui.ViewOf(c).IsShown, Is.True);
            Assert.That(ui.ViewOf(a).IsShown, Is.False);
        }

        [Test]
        public void CloseAsync_ThenGoBack_ReturnsToTheScreenBelowTheClosedOne()
        {
            var a = ui.Open<ScreenAViewModel>();
            var b = ui.Open<ScreenBViewModel>();
            ui.Open<ScreenCViewModel>();
            AwaitableAssert.Completes(ui.Navigator.CloseAsync(b));

            AwaitableAssert.Completes(ui.Navigator.GoBackAsync());

            Assert.That(ui.Navigator.Top, Is.SameAs(a));
            Assert.That(ui.ViewOf(a).IsShown, Is.True);
        }

        [Test]
        public void CloseAsync_LastPopup_StopsThePopupLayerBlockingPointerInput()
        {
            ui.Open<ScreenAViewModel>();
            var popup = ui.Open<PopupAViewModel>();

            AwaitableAssert.Completes(ui.Navigator.CloseAsync(popup));

            Assert.That(ui.PopupLayer.pickingMode, Is.EqualTo(PickingMode.Ignore));
        }

        [Test]
        public void CloseAsync_NotOpen_DoesNothing()
        {
            var a = ui.Open<ScreenAViewModel>();
            var stranger = new ScreenBViewModel();

            AwaitableAssert.Completes(ui.Navigator.CloseAsync(stranger));

            Assert.That(stranger.IsDisposed, Is.False);
            Assert.That(ui.Navigator.Top, Is.SameAs(a));
        }

        [Test]
        public void CloseAsync_Null_Faults()
        {
            AwaitableAssert.Faults<ArgumentNullException>(ui.Navigator.CloseAsync(null));
        }

        [Test]
        public void CloseAsync_CalledAgainWhileTheHideAnimationRuns_ClosesOnce()
        {
            ui.Open<ScreenAViewModel>();
            var b = ui.Open<ScreenBViewModel>();
            var hide = ui.ViewOf(b).HoldNextHide();
            var first = ui.Navigator.CloseAsync(b);
            var second = ui.Navigator.CloseAsync(b);

            hide.SetResult();

            AwaitableAssert.Completes(first);
            AwaitableAssert.Completes(second);
            Assert.That(b.OnClosedCount, Is.EqualTo(1));
            Assert.That(ui.ViewOf(b).DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void CloseAsync_HideCancelled_StillDisposesTheScreenAndShowsTheOneBelow()
        {
            var a = ui.Open<ScreenAViewModel>();
            var b = ui.Open<ScreenBViewModel>();
            ui.ViewOf(b).HoldNextHide();
            using var cancellation = new CancellationTokenSource();
            var close = ui.Navigator.CloseAsync(b, cancellation.Token);

            cancellation.Cancel();

            AwaitableAssert.Faults<OperationCanceledException>(close);
            Assert.That(b.IsDisposed, Is.True);
            Assert.That(ui.ViewOf(a).IsShown, Is.True);
            Assert.That(ui.Navigator.Top, Is.SameAs(a));
        }

        [Test]
        public void CloseAsync_Cancelable_CancelsItBeforeOnClosed()
        {
            var cancelable = ui.Open<CancelableViewModel>();

            AwaitableAssert.Completes(ui.Navigator.CloseAsync(cancelable));

            Assert.That(ui.Log.IndexOf("Cancelable.Cancel"), Is.GreaterThanOrEqualTo(0));
            Assert.That(ui.Log.IndexOf("Cancelable.Cancel"), Is.LessThan(ui.Log.IndexOf("Cancelable.OnClosed")));
        }

        [Test]
        public void CloseAsync_OnClosedThrows_LogsItAndStillClosesTheScreen()
        {
            ui.Open<ScreenAViewModel>();
            ui.RegisterWith(() => new PayloadViewModel {
                OnClosedAction = () => throw new InvalidOperationException("closed failed")
            });
            var closing = ui.Open<PayloadViewModel>();
            LogAssert.Expect(LogType.Exception, new Regex("closed failed"));

            AwaitableAssert.Completes(ui.Navigator.CloseAsync(closing));

            Assert.That(closing.IsDisposed, Is.True);
            Assert.That(ui.Navigator.IsOpen(closing), Is.False);
        }

        [Test]
        public void CloseAsync_ViaHandle_ClosesTheScreen()
        {
            ui.Open<ScreenAViewModel>();
            var handle = AwaitableAssert.Completes(ui.Navigator.OpenAsync<ScreenBViewModel>());

            AwaitableAssert.Completes(handle.CloseAsync());

            Assert.That(handle.IsOpen, Is.False);
        }

        [Test]
        public void GoBackAsync_WithAPopupOpen_ClosesOnlyTheTopPopup()
        {
            ui.Open<ScreenAViewModel>();
            var b = ui.Open<ScreenBViewModel>();
            var lower = ui.Open<PopupAViewModel>();
            var upper = ui.Open<PopupBViewModel>();

            var wentBack = AwaitableAssert.Completes(ui.Navigator.GoBackAsync());

            Assert.That(wentBack, Is.True);
            Assert.That(ui.Navigator.IsOpen(upper), Is.False);
            Assert.That(ui.Navigator.IsOpen(lower), Is.True);
            Assert.That(ui.Navigator.IsOpen(b), Is.True);
        }

        [Test]
        public void GoBackAsync_TwoScreens_ClosesTheTopOne()
        {
            var a = ui.Open<ScreenAViewModel>();
            var b = ui.Open<ScreenBViewModel>();

            var wentBack = AwaitableAssert.Completes(ui.Navigator.GoBackAsync());

            Assert.That(wentBack, Is.True);
            Assert.That(ui.Navigator.IsOpen(b), Is.False);
            Assert.That(ui.Navigator.Top, Is.SameAs(a));
        }

        [Test]
        public void GoBackAsync_OnlyTheRootScreen_ReturnsFalseAndKeepsIt()
        {
            var a = ui.Open<ScreenAViewModel>();

            var wentBack = AwaitableAssert.Completes(ui.Navigator.GoBackAsync());

            Assert.That(wentBack, Is.False);
            Assert.That(ui.Navigator.IsOpen(a), Is.True);
            Assert.That(ui.ViewOf(a).IsShown, Is.True);
        }

        [Test]
        public void GoBackAsync_NothingOpen_ReturnsFalse()
        {
            Assert.That(AwaitableAssert.Completes(ui.Navigator.GoBackAsync()), Is.False);
        }

        [Test]
        public void GoBackAsync_OnlyAPopupOverNoScreen_ClosesThePopup()
        {
            var popup = ui.Open<PopupAViewModel>();

            var wentBack = AwaitableAssert.Completes(ui.Navigator.GoBackAsync());

            Assert.That(wentBack, Is.True);
            Assert.That(ui.Navigator.IsOpen(popup), Is.False);
        }

        [Test]
        public void PopToRootAsync_ClosesEverythingButTheBottomScreenAndShowsIt()
        {
            var a = ui.Open<ScreenAViewModel>();
            var b = ui.Open<ScreenBViewModel>();
            var c = ui.Open<ScreenCViewModel>();
            var popup = ui.Open<PopupAViewModel>();

            AwaitableAssert.Completes(ui.Navigator.PopToRootAsync());

            Assert.That(new ViewModel[] { b, c, popup }.Select(vm => vm.IsDisposed), Is.All.True);
            Assert.That(ui.Navigator.Top, Is.SameAs(a));
            Assert.That(ui.ViewOf(a).IsShown, Is.True);
            Assert.That(ui.PopupLayer.childCount, Is.Zero);
        }

        [Test]
        public void PopToRootAsync_PlaysAHideAnimationOnlyForTheVisibleScreen()
        {
            ui.Open<ScreenAViewModel>();
            ui.Open<ScreenBViewModel>();
            ui.Open<ScreenCViewModel>();
            ui.Log.Clear();

            AwaitableAssert.Completes(ui.Navigator.PopToRootAsync());

            Assert.That(ui.Log.Entries.Where(entry => entry.EndsWith(".hide")), Is.EqualTo(new[] { "ScreenC.hide" }));
            Assert.That(ui.Log.Entries.Count(entry => entry == "ScreenA.show"), Is.EqualTo(1));
        }

        [Test]
        public void CloseAllAsync_ClosesEveryScreenAndPopup()
        {
            var a = ui.Open<ScreenAViewModel>();
            var b = ui.Open<ScreenBViewModel>();
            var popup = ui.Open<PopupAViewModel>();

            AwaitableAssert.Completes(ui.Navigator.CloseAllAsync());

            Assert.That(new ViewModel[] { a, b, popup }.Select(vm => vm.IsDisposed), Is.All.True);
            Assert.That(ui.Navigator.Top, Is.Null);
            Assert.That(ui.ScreenLayer.childCount + ui.PopupLayer.childCount, Is.Zero);
        }
    }
}
