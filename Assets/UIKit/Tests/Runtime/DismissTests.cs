using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UIKit.Tests.PlayMode
{
    public class BackdropClickTests : PanelTestBase
    {
        private protected override void RegisterScreens()
        {
            Register<HomeViewModel>();
            Register<NoticeViewModel>();
            Register<ToastViewModel>();
            Register<LockedNoticeViewModel>();
            Register<QuestionViewModel>();
        }

        [UnityTest]
        public IEnumerator PointerDown_OnThePopupLayer_ClosesThePopup()
        {
            Open<HomeViewModel>();
            var notice = Open<NoticeViewModel>();

            PointerDown(PopupLayer);
            yield return Until(() => !Navigator.IsOpen(notice));

            Assert.That(Navigator.IsOpen(notice), Is.False);
        }

        [UnityTest]
        public IEnumerator PointerDown_OnThePopupRoot_ClosesThePopup()
        {
            var notice = Open<NoticeViewModel>();

            PointerDown(ViewOf(notice).Root);
            yield return Until(() => !Navigator.IsOpen(notice));

            Assert.That(Navigator.IsOpen(notice), Is.False);
        }

        [UnityTest]
        public IEnumerator PointerDown_InsideThePopupContent_KeepsItOpen()
        {
            var notice = Open<NoticeViewModel>();

            PointerDown(ViewOf(notice).Content);
            yield return Frames(3);

            Assert.That(Navigator.IsOpen(notice), Is.True);
        }

        [UnityTest]
        public IEnumerator PointerDown_OnTheBackdrop_ClosesOnlyTheTopPopup()
        {
            var notice = Open<NoticeViewModel>();
            var toast = Open<ToastViewModel>();

            PointerDown(PopupLayer);
            yield return Until(() => !Navigator.IsOpen(toast));

            Assert.That(Navigator.IsOpen(toast), Is.False);
            Assert.That(Navigator.IsOpen(notice), Is.True);
        }

        [UnityTest]
        public IEnumerator PointerDown_InsideALowerPopup_ClosesTheTopOneOnly()
        {
            var notice = Open<NoticeViewModel>();
            var toast = Open<ToastViewModel>();

            PointerDown(ViewOf(notice).Content);
            yield return Until(() => !Navigator.IsOpen(toast));

            Assert.That(Navigator.IsOpen(toast), Is.False);
            Assert.That(Navigator.IsOpen(notice), Is.True);
        }

        [UnityTest]
        public IEnumerator PointerDown_PopupWithoutBackdropDismiss_KeepsItOpen()
        {
            var locked = Open<LockedNoticeViewModel>();

            PointerDown(PopupLayer);
            yield return Frames(3);

            Assert.That(Navigator.IsOpen(locked), Is.True);
        }

        [UnityTest]
        public IEnumerator PointerDown_OnTheBackdropDuringRun_ReturnsTheCancelResult()
        {
            var run = Navigator.RunAsync<QuestionViewModel, bool>();

            PointerDown(PopupLayer);
            yield return Until(() => run.GetAwaiter().IsCompleted);

            Assert.That(run.GetAwaiter().IsCompleted, Is.True);
            Assert.That(run.GetAwaiter().GetResult(), Is.False);
        }
    }

    public class NavigationCancelTests : PanelTestBase
    {
        private protected override void RegisterScreens()
        {
            Register<HomeViewModel>();
            Register<DetailsViewModel>();
            Register<PinnedViewModel>();
            Register<NoticeViewModel>();
            Register<LockedNoticeViewModel>();
            Register<QuestionViewModel>();
        }

        [UnityTest]
        public IEnumerator NavigationCancel_OnTheTopScreen_ClosesIt()
        {
            var home = Open<HomeViewModel>();
            var details = Open<DetailsViewModel>();

            NavigationCancel(ViewOf(details).Root);
            yield return Until(() => !Navigator.IsOpen(details));

            Assert.That(Navigator.IsOpen(details), Is.False);
            Assert.That(Navigator.Top, Is.SameAs(home));
        }

        [UnityTest]
        public IEnumerator NavigationCancel_OnTheOnlyScreen_KeepsItOpen()
        {
            var home = Open<HomeViewModel>();

            NavigationCancel(ViewOf(home).Root);
            yield return Frames(3);

            Assert.That(Navigator.IsOpen(home), Is.True);
        }

        [UnityTest]
        public IEnumerator NavigationCancel_ScreenWithoutCancelDismiss_KeepsItOpen()
        {
            Open<HomeViewModel>();
            var pinned = Open<PinnedViewModel>();

            NavigationCancel(ViewOf(pinned).Root);
            yield return Frames(3);

            Assert.That(Navigator.IsOpen(pinned), Is.True);
        }

        [UnityTest]
        public IEnumerator NavigationCancel_OnTheScreenUnderAPopup_ClosesNothing()
        {
            Open<HomeViewModel>();
            var details = Open<DetailsViewModel>();
            var notice = Open<NoticeViewModel>();

            NavigationCancel(ViewOf(details).Root);
            yield return Frames(3);

            Assert.That(Navigator.IsOpen(details), Is.True);
            Assert.That(Navigator.IsOpen(notice), Is.True);
        }

        [UnityTest]
        public IEnumerator NavigationCancel_OnAPopup_ClosesIt()
        {
            Open<HomeViewModel>();
            var notice = Open<NoticeViewModel>();

            NavigationCancel(ViewOf(notice).Root);
            yield return Until(() => !Navigator.IsOpen(notice));

            Assert.That(Navigator.IsOpen(notice), Is.False);
        }

        [UnityTest]
        public IEnumerator NavigationCancel_OnAPopupWithoutCancelDismiss_KeepsItOpen()
        {
            var locked = Open<LockedNoticeViewModel>();

            NavigationCancel(ViewOf(locked).Root);
            yield return Frames(3);

            Assert.That(Navigator.IsOpen(locked), Is.True);
        }

        [UnityTest]
        public IEnumerator NavigationCancel_OnAPopupDuringRun_ReturnsTheCancelResult()
        {
            var run = Navigator.RunAsync<QuestionViewModel, bool>();
            var question = (QuestionViewModel)Navigator.Top;

            NavigationCancel(ViewOf(question).Root);
            yield return Until(() => run.GetAwaiter().IsCompleted);

            Assert.That(run.GetAwaiter().IsCompleted, Is.True);
            Assert.That(run.GetAwaiter().GetResult(), Is.False);
        }
    }
}
