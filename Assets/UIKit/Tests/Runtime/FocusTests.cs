using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UIKit.Tests.PlayMode
{
    public class FocusTests : PanelTestBase
    {
        private protected override void RegisterScreens()
        {
            Register<HomeViewModel>();
            Register<DetailsViewModel>();
            Register<NoticeViewModel>();
        }

        [UnityTest]
        public IEnumerator Open_Screen_FocusesItsRoot()
        {
            var home = Open<HomeViewModel>();

            yield return Until(() => Focused == ViewOf(home).Root);
            Assert.That(Focused, Is.SameAs(ViewOf(home).Root));
        }

        [UnityTest]
        public IEnumerator Open_ScreenOverAnother_FocusesTheNewOne()
        {
            Open<HomeViewModel>();

            var details = Open<DetailsViewModel>();

            yield return Until(() => Focused == ViewOf(details).Root);
            Assert.That(Focused, Is.SameAs(ViewOf(details).Root));
        }

        [UnityTest]
        public IEnumerator Open_PopupOverAScreen_FocusesThePopup()
        {
            Open<HomeViewModel>();

            var notice = Open<NoticeViewModel>();

            yield return Until(() => Focused == ViewOf(notice).Root);
            Assert.That(Focused, Is.SameAs(ViewOf(notice).Root));
        }

        [UnityTest]
        public IEnumerator Close_TopScreen_FocusesTheUncoveredScreen()
        {
            var home = Open<HomeViewModel>();
            var details = Open<DetailsViewModel>();

            Sync.Complete(Navigator.CloseAsync(details));

            yield return Until(() => Focused == ViewOf(home).Root);
            Assert.That(Focused, Is.SameAs(ViewOf(home).Root));
        }

        [UnityTest]
        public IEnumerator Close_Popup_FocusesTheScreenBelow()
        {
            var home = Open<HomeViewModel>();
            var notice = Open<NoticeViewModel>();

            Sync.Complete(Navigator.CloseAsync(notice));

            yield return Until(() => Focused == ViewOf(home).Root);
            Assert.That(Focused, Is.SameAs(ViewOf(home).Root));
        }

        [UnityTest]
        public IEnumerator FocusIn_OnTheScreenUnderAPopup_MovesFocusBackToThePopup()
        {
            var home = Open<HomeViewModel>();
            var notice = Open<NoticeViewModel>();

            ViewOf(home).Content.Focus();
            yield return Until(() => Focused == ViewOf(notice).Root);

            Assert.That(Focused, Is.SameAs(ViewOf(notice).Root));
        }

        [UnityTest]
        public IEnumerator FocusIn_InsideTheTopScreen_IsLeftAlone()
        {
            var home = Open<HomeViewModel>();
            var content = ViewOf(home).Content;

            content.Focus();
            yield return Frames(3);

            Assert.That(Focused, Is.SameAs(content));
        }
    }
}
