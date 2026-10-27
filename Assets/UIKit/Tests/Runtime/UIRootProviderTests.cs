using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UIKit.Tests.PlayMode
{
    public class UIRootProviderTests
    {
        private const string RootObjectName = "UIKitRoot";
        private const int MaxFrames = 30;

        private TestPanelSettings panelSettings;
        private UIRootProvider provider;
        private UIKitContext context;

        [SetUp]
        public void SetUp()
        {
            panelSettings = new TestPanelSettings();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            context?.Dispose();
            provider?.Dispose();
            panelSettings.Dispose();
            context = null;
            provider = null;

            // Destroy is deferred in play mode: let it happen before the next test looks.
            yield return null;
        }

        [UnityTest]
        public IEnumerator Constructor_NullPanelSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new UIRootProvider(null));
            yield break;
        }

        [UnityTest]
        public IEnumerator Constructor_CreatesNothingYet()
        {
            provider = new UIRootProvider(panelSettings.Settings);

            Assert.That(GameObject.Find(RootObjectName), Is.Null);
            yield break;
        }

        [UnityTest]
        public IEnumerator GetLayerRoot_FirstCall_CreatesTheRootObject()
        {
            provider = new UIRootProvider(panelSettings.Settings);

            provider.GetLayerRoot(UILayer.Screen);

            Assert.That(GameObject.Find(RootObjectName), Is.Not.Null);
            yield break;
        }

        [UnityTest]
        public IEnumerator GetLayerRoot_CalledAgain_ReturnsTheSameElements()
        {
            provider = new UIRootProvider(panelSettings.Settings);
            var screen = provider.GetLayerRoot(UILayer.Screen);
            var popup = provider.GetLayerRoot(UILayer.Popup);

            Assert.That(provider.GetLayerRoot(UILayer.Screen), Is.SameAs(screen));
            Assert.That(provider.GetLayerRoot(UILayer.Popup), Is.SameAs(popup));
            Assert.That(popup, Is.Not.SameAs(screen));
            yield break;
        }

        [UnityTest]
        public IEnumerator GetLayerRoot_UnknownLayer_Throws()
        {
            provider = new UIRootProvider(panelSettings.Settings);

            Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetLayerRoot((UILayer)42));
            yield break;
        }

        [UnityTest]
        public IEnumerator GetLayerRoot_Layers_EndUpOnTheSamePanel()
        {
            provider = new UIRootProvider(panelSettings.Settings);
            var screen = provider.GetLayerRoot(UILayer.Screen);
            var popup = provider.GetLayerRoot(UILayer.Popup);

            yield return UntilAttached(screen);

            Assert.That(screen.panel, Is.Not.Null, "The screen layer never reached a panel.");
            Assert.That(popup.panel, Is.SameAs(screen.panel));
            Assert.That(popup.parent, Is.SameAs(screen.parent));
        }

        [UnityTest]
        public IEnumerator GetLayerRoot_Layers_PutThePopupLayerAboveTheScreenLayer()
        {
            provider = new UIRootProvider(panelSettings.Settings);
            var screen = provider.GetLayerRoot(UILayer.Screen);
            var popup = provider.GetLayerRoot(UILayer.Popup);

            yield return UntilAttached(screen);

            var parent = screen.parent;
            Assert.That(parent, Is.Not.Null, "The screen layer never reached a panel.");
            Assert.That(parent.IndexOf(popup), Is.GreaterThan(parent.IndexOf(screen)));
        }

        [UnityTest]
        public IEnumerator Dispose_DestroysTheRootObject()
        {
            provider = new UIRootProvider(panelSettings.Settings);
            provider.GetLayerRoot(UILayer.Screen);

            provider.Dispose();
            yield return null;

            Assert.That(GameObject.Find(RootObjectName), Is.Null);
        }

        [UnityTest]
        public IEnumerator Dispose_Twice_IsSafe()
        {
            provider = new UIRootProvider(panelSettings.Settings);
            provider.GetLayerRoot(UILayer.Screen);
            provider.Dispose();

            Assert.DoesNotThrow(() => provider.Dispose());
            yield break;
        }

        [UnityTest]
        public IEnumerator GetLayerRoot_AfterDispose_Throws()
        {
            provider = new UIRootProvider(panelSettings.Settings);
            provider.Dispose();

            Assert.Throws<ObjectDisposedException>(() => provider.GetLayerRoot(UILayer.Screen));
            yield break;
        }

        [UnityTest]
        public IEnumerator ContextWithPanelSettings_Disposed_DestroysItsRootObject()
        {
            context = new UIKitContext(panelSettings.Settings) { StallWarningSeconds = 0 };
            context.Register<HomeViewModel, PlayView<HomeViewModel>>(() => new HomeViewModel());
            Sync.Result(context.Navigator.OpenAsync<HomeViewModel>());
            Assert.That(GameObject.Find(RootObjectName), Is.Not.Null);

            context.Dispose();
            yield return null;

            Assert.That(GameObject.Find(RootObjectName), Is.Null);
        }

        [UnityTest]
        public IEnumerator ContextWithAGivenProvider_Disposed_LeavesTheProviderAlive()
        {
            provider = new UIRootProvider(panelSettings.Settings);
            context = new UIKitContext(provider) { StallWarningSeconds = 0 };
            context.Register<HomeViewModel, PlayView<HomeViewModel>>(() => new HomeViewModel());
            Sync.Result(context.Navigator.OpenAsync<HomeViewModel>());

            context.Dispose();
            yield return null;

            Assert.That(GameObject.Find(RootObjectName), Is.Not.Null);
            Assert.DoesNotThrow(() => provider.GetLayerRoot(UILayer.Screen));
        }

        // The first screen is usually opened right after the context is created, in the same
        // frame as the panel. Once the panel is up, that screen should hold focus so navigation
        // events reach it.
        [UnityTest]
        public IEnumerator Open_InTheFrameThePanelIsCreated_FocusesTheScreenOnceThePanelIsUp()
        {
            context = new UIKitContext(panelSettings.Settings) { StallWarningSeconds = 0 };
            PlayView<HomeViewModel> view = null;
            context.Register(() => new HomeViewModel(), () => view = new PlayView<HomeViewModel>());

            Sync.Result(context.Navigator.OpenAsync<HomeViewModel>());
            yield return UntilAttached(view.Root);
            yield return null;

            Assert.That(view.Root.panel, Is.Not.Null, "The screen never reached a panel.");
            Assert.That(view.Root.panel.focusController.focusedElement, Is.SameAs(view.Root));
        }

        private static IEnumerator UntilAttached(VisualElement element)
        {
            for (var frame = 0; frame < MaxFrames && element.panel == null; frame++) {
                yield return null;
            }
        }
    }
}
