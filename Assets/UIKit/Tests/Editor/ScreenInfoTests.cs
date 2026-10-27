using System;
using NUnit.Framework;

namespace UIKit.Tests
{
    public class ScreenInfoTests
    {
        private sealed class InventoryViewModel : UIKit.ViewModel
        {
        }

        private sealed class Inventory : UIKit.ViewModel
        {
        }

        // A type named exactly like the suffix keeps its name rather than becoming "".
        private sealed class ViewModel : UIKit.ViewModel
        {
        }

        private sealed class ListViewModel<T> : UIKit.ViewModel
        {
        }

        [UIScreen("Shop")]
        private sealed class StoreViewModel : UIKit.ViewModel
        {
        }

        private sealed class AlertViewModel : UIKit.ViewModel, IPopup
        {
        }

        [UIScreen(UILayer.Popup)]
        private sealed class ToastViewModel : UIKit.ViewModel
        {
        }

        [UIScreen(UILayer.Screen)]
        private sealed class InlineAlertViewModel : UIKit.ViewModel, IPopup
        {
        }

        [UIScreen(Dismiss = DismissTriggers.None)]
        private sealed class BlockingAlertViewModel : UIKit.ViewModel, IPopup
        {
        }

        [UIScreen("Base", Dismiss = DismissTriggers.None)]
        private class BaseScreenViewModel : UIKit.ViewModel
        {
        }

        private sealed class DerivedScreenViewModel : BaseScreenViewModel
        {
        }

        [TestCase(typeof(InventoryViewModel), "Inventory")]
        [TestCase(typeof(Inventory), "Inventory")]
        [TestCase(typeof(ViewModel), "ViewModel")]
        [TestCase(typeof(ListViewModel<int>), "List")]
        public void Id_ByConvention_IsTheTypeNameWithoutTheViewModelSuffix(Type type, string expected)
        {
            Assert.That(ScreenInfo.Of(type).Id, Is.EqualTo(expected));
        }

        [Test]
        public void Id_WithUIScreenId_UsesIt()
        {
            Assert.That(ScreenInfo.Of<StoreViewModel>().Id, Is.EqualTo("Shop"));
        }

        [Test]
        public void Of_PlainViewModel_IsAScreenDismissedByNavigationCancel()
        {
            var info = ScreenInfo.Of<InventoryViewModel>();

            Assert.That(info.Layer, Is.EqualTo(UILayer.Screen));
            Assert.That(info.Dismiss, Is.EqualTo(DismissTriggers.NavigationCancel));
        }

        [Test]
        public void Of_Popup_IsAPopupDismissedByEverything()
        {
            var info = ScreenInfo.Of<AlertViewModel>();

            Assert.That(info.Layer, Is.EqualTo(UILayer.Popup));
            Assert.That(info.Dismiss, Is.EqualTo(DismissTriggers.All));
        }

        [Test]
        public void Of_LayerOverriddenToPopup_TakesThePopupDismissDefaults()
        {
            var info = ScreenInfo.Of<ToastViewModel>();

            Assert.That(info.Layer, Is.EqualTo(UILayer.Popup));
            Assert.That(info.Dismiss, Is.EqualTo(DismissTriggers.All));
        }

        [Test]
        public void Of_PopupWithLayerOverriddenToScreen_IsAScreen()
        {
            var info = ScreenInfo.Of<InlineAlertViewModel>();

            Assert.That(info.Layer, Is.EqualTo(UILayer.Screen));
            Assert.That(info.Dismiss, Is.EqualTo(DismissTriggers.NavigationCancel));
        }

        [Test]
        public void Of_DismissOverridden_UsesIt()
        {
            Assert.That(ScreenInfo.Of<BlockingAlertViewModel>().Dismiss, Is.EqualTo(DismissTriggers.None));
        }

        [Test]
        public void Of_SubclassOfAnAttributedType_DoesNotInheritTheAttribute()
        {
            var info = ScreenInfo.Of<DerivedScreenViewModel>();

            Assert.That(info.Id, Is.EqualTo("DerivedScreen"));
            Assert.That(info.Dismiss, Is.EqualTo(DismissTriggers.NavigationCancel));
        }

        [Test]
        public void Of_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ScreenInfo.Of(null));
        }
    }
}
