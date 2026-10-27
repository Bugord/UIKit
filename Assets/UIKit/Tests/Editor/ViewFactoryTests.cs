using System;
using System.Threading;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UIKit.Tests
{
    public class ViewFactoryTests : NavigatorTestBase
    {
        private VisualElement container;

        private IViewFactory Factory => ui.Context.ViewFactory;

        [SetUp]
        public void CreateContainer()
        {
            container = new VisualElement();
        }

        [Test]
        public void Create_AddsTheRootToTheContainerAndBindsTheViewModel()
        {
            ui.Context.RegisterView<BadgeViewModel, TestView<BadgeViewModel>>(() => new TestView<BadgeViewModel>());
            var badge = new BadgeViewModel();

            var view = Factory.Create<TestView<BadgeViewModel>, BadgeViewModel>(badge, container);

            Assert.That(view.Root.parent, Is.SameAs(container));
            Assert.That(view.Root.dataSource, Is.SameAs(badge));
            Assert.That(view.ViewModel, Is.SameAs(badge));
            Assert.That(view.AttachCount, Is.EqualTo(1));
        }

        [Test]
        public void Create_ByRuntimeType_CreatesTheViewRegisteredForIt()
        {
            ui.Context.RegisterView<BadgeViewModel, TestView<BadgeViewModel>>(() => new TestView<BadgeViewModel>());
            ViewModel badge = new BadgeViewModel();

            var view = Factory.Create(badge, container);

            Assert.That(view, Is.InstanceOf<TestView<BadgeViewModel>>());
            Assert.That(view.ViewModel, Is.SameAs(badge));
        }

        [Test]
        public void Create_ForAScreenRegisteredWithRegister_Works()
        {
            ui.Register<ScreenAViewModel>();

            var view = Factory.Create<TestView<ScreenAViewModel>, ScreenAViewModel>(new ScreenAViewModel(), container);

            Assert.That(view.Root.parent, Is.SameAs(container));
        }

        [Test]
        public void Create_RequestedViewTypeDiffersFromTheRegisteredOne_ThrowsAndLeavesTheContainerEmpty()
        {
            ui.Context.RegisterView<BadgeViewModel, TestView<BadgeViewModel>>(() => new TestView<BadgeViewModel>());

            Assert.Throws<InvalidOperationException>(
                () => Factory.Create<CodeView<BadgeViewModel>, BadgeViewModel>(new BadgeViewModel(), container));

            Assert.That(container.childCount, Is.Zero);
        }

        [Test]
        public void Create_Unregistered_Throws()
        {
            var thrown = Assert.Throws<InvalidOperationException>(() => Factory.Create(new BadgeViewModel(), container));

            StringAssert.Contains("not registered", thrown.Message);
        }

        [Test]
        public void Create_NullViewModel_Throws()
        {
            ui.Context.RegisterView<BadgeViewModel, CodeView<BadgeViewModel>>();

            Assert.Throws<ArgumentNullException>(() => Factory.Create(null, container));
            Assert.Throws<ArgumentNullException>(
                () => Factory.Create<CodeView<BadgeViewModel>, BadgeViewModel>(null, container));
        }

        [Test]
        public void Create_NullContainer_Throws()
        {
            ui.Context.RegisterView<BadgeViewModel, CodeView<BadgeViewModel>>();

            Assert.Throws<ArgumentNullException>(() => Factory.Create(new BadgeViewModel(), null));
        }

        [Test]
        public void Create_OnAttachThrows_RemovesTheRootAndDisposesTheViewModel()
        {
            ui.Context.RegisterView<BadgeViewModel, TestView<BadgeViewModel>>(() => new TestView<BadgeViewModel> {
                Attached = _ => throw new InvalidOperationException("attach failed")
            });
            var badge = new BadgeViewModel();

            Assert.Throws<InvalidOperationException>(() => Factory.Create(badge, container));

            Assert.That(container.childCount, Is.Zero);
            Assert.That(badge.IsDisposed, Is.True);
        }

        [Test]
        public void Dispose_RemovesTheRootAndDisposesTheViewModel()
        {
            ui.Context.RegisterView<BadgeViewModel, TestView<BadgeViewModel>>(() => new TestView<BadgeViewModel>());
            var badge = new BadgeViewModel();
            var view = Factory.Create<TestView<BadgeViewModel>, BadgeViewModel>(badge, container);

            view.Dispose();

            Assert.That(container.childCount, Is.Zero);
            Assert.That(view.Root, Is.Null);
            Assert.That(view.IsDisposed, Is.True);
            Assert.That(badge.IsDisposed, Is.True);
        }

        [Test]
        public void Dispose_CreatedWithDisposeViewModelFalse_LeavesTheViewModelAlive()
        {
            ui.Context.RegisterView<BadgeViewModel, CodeView<BadgeViewModel>>();
            var badge = new BadgeViewModel();
            var view = Factory.Create(badge, container, disposeViewModel: false);

            view.Dispose();

            Assert.That(badge.IsDisposed, Is.False);
        }

        [Test]
        public void Dispose_Twice_RunsOnDisposeOnce()
        {
            ui.Context.RegisterView<BadgeViewModel, TestView<BadgeViewModel>>(() => new TestView<BadgeViewModel>());
            var view = Factory.Create<TestView<BadgeViewModel>, BadgeViewModel>(new BadgeViewModel(), container);

            view.Dispose();
            view.Dispose();

            Assert.That(view.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void ViewModelPropertyChange_ReachesOnPropertyChanged()
        {
            ui.Context.RegisterView<ScoreViewModel, TestView<ScoreViewModel>>(() => new TestView<ScoreViewModel>());
            var score = new ScoreViewModel();
            var view = Factory.Create<TestView<ScoreViewModel>, ScoreViewModel>(score, container);

            score.Score = 3;

            Assert.That(view.ChangedProperties, Is.EqualTo(new[] { nameof(ScoreViewModel.Score) }));
        }

        [Test]
        public void ViewModelPropertyChange_AfterTheViewIsDisposed_NoLongerReachesIt()
        {
            ui.Context.RegisterView<ScoreViewModel, TestView<ScoreViewModel>>(() => new TestView<ScoreViewModel>());
            var score = new ScoreViewModel();
            var view = Factory.Create<TestView<ScoreViewModel>, ScoreViewModel>(score, container, disposeViewModel: false);
            view.Dispose();

            score.Score = 3;

            Assert.That(view.ChangedProperties, Is.Empty);
        }

        [Test]
        public void ChildView_IsDisposedWithItsParent()
        {
            ui.Context.RegisterView<ScoreViewModel, TestView<ScoreViewModel>>(() => new TestView<ScoreViewModel>());
            ui.Context.RegisterView<BadgeViewModel, TestView<BadgeViewModel>>(() => new TestView<BadgeViewModel>());
            var parent = Factory.Create<TestView<ScoreViewModel>, ScoreViewModel>(new ScoreViewModel(), container);
            var badge = new BadgeViewModel();
            var child = parent.Children.Create<TestView<BadgeViewModel>, BadgeViewModel>(badge, parent.Root);

            parent.Dispose();

            Assert.That(child.IsDisposed, Is.True);
            Assert.That(badge.IsDisposed, Is.True);
        }

        [Test]
        public void ChildView_CreatedWithDisposeViewModelFalse_LeavesItsViewModelToTheParent()
        {
            ui.Context.RegisterView<ScoreViewModel, TestView<ScoreViewModel>>(() => new TestView<ScoreViewModel>());
            ui.Context.RegisterView<BadgeViewModel, TestView<BadgeViewModel>>(() => new TestView<BadgeViewModel>());
            var parent = Factory.Create<TestView<ScoreViewModel>, ScoreViewModel>(new ScoreViewModel(), container);
            var badge = new BadgeViewModel();
            var child = parent.Children.Create<TestView<BadgeViewModel>, BadgeViewModel>(badge, parent.Root, false);

            parent.Dispose();

            Assert.That(child.IsDisposed, Is.True);
            Assert.That(badge.IsDisposed, Is.False);
        }

        [Test]
        public void ChildViewFactory_AfterTheParentIsDisposed_Throws()
        {
            ui.Context.RegisterView<ScoreViewModel, TestView<ScoreViewModel>>(() => new TestView<ScoreViewModel>());
            ui.Context.RegisterView<BadgeViewModel, CodeView<BadgeViewModel>>();
            var parent = Factory.Create<TestView<ScoreViewModel>, ScoreViewModel>(new ScoreViewModel(), container);
            var children = parent.Children;
            parent.Dispose();

            Assert.Throws<ObjectDisposedException>(() => children.Create(new BadgeViewModel(), container));
        }

        [Test]
        public void HideAsync_HidesTheRoot_ShowAsyncShowsItAgain()
        {
            ui.Context.RegisterView<BadgeViewModel, CodeView<BadgeViewModel>>();
            var view = Factory.Create(new BadgeViewModel(), container);

            AwaitableAssert.Completes(view.HideAsync(CancellationToken.None));
            Assert.That(view.Root.style.display.value, Is.EqualTo(DisplayStyle.None));

            AwaitableAssert.Completes(view.ShowAsync(CancellationToken.None));
            Assert.That(view.Root.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void ShowAsync_CancelledToken_Throws()
        {
            ui.Context.RegisterView<BadgeViewModel, CodeView<BadgeViewModel>>();
            var view = Factory.Create(new BadgeViewModel(), container);

            Assert.Throws<OperationCanceledException>(() => view.ShowAsync(new CancellationToken(true)));
        }
    }

    // Views whose root is cloned from the UXML their catalog lists.
    public class UxmlViewTests
    {
        private ScreenCatalog catalog;
        private VisualTreeAsset emptyUxml;
        private UIHarness ui;
        private VisualElement container;

        [SetUp]
        public void SetUp()
        {
            emptyUxml = ScriptableObject.CreateInstance<VisualTreeAsset>();
            catalog = Catalogs.Create("TestCatalog",
                ("ScreenA", LoadFixture("UIKitTestSingleRoot")),
                ("ScreenB", LoadFixture("UIKitTestTwoRoots")),
                ("ScreenC", emptyUxml),
                ("Badge", null));
            ui = new UIHarness(catalog);
            container = new VisualElement();
        }

        [TearDown]
        public void TearDown()
        {
            ui.Dispose();
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(emptyUxml);
        }

        [Test]
        public void Create_SingleRootUxml_UsesThatRootAsTheViewRoot()
        {
            ui.Context.RegisterView<ScreenAViewModel, UxmlView<ScreenAViewModel>>(() => new UxmlView<ScreenAViewModel>());

            var view = ui.Context.ViewFactory.Create(new ScreenAViewModel(), container);

            Assert.That(view.Root.name, Is.EqualTo("single-root"));
            Assert.That(container.childCount, Is.EqualTo(1));
            Assert.That(view.Root.parent, Is.SameAs(container));
        }

        [Test]
        public void Create_UxmlWithTwoRoots_ThrowsAndLeavesTheContainerEmpty()
        {
            ui.Context.RegisterView<ScreenBViewModel, UxmlView<ScreenBViewModel>>(() => new UxmlView<ScreenBViewModel>());
            var screen = new ScreenBViewModel();

            var thrown = Assert.Throws<InvalidOperationException>(() => ui.Context.ViewFactory.Create(screen, container));

            StringAssert.Contains("produced 2 root elements", thrown.Message);
            Assert.That(container.childCount, Is.Zero);
            Assert.That(screen.IsDisposed, Is.True);
        }

        [Test]
        public void Create_EmptyUxml_Throws()
        {
            ui.Context.RegisterView<ScreenCViewModel, UxmlView<ScreenCViewModel>>(() => new UxmlView<ScreenCViewModel>());

            var thrown = Assert.Throws<InvalidOperationException>(
                () => ui.Context.ViewFactory.Create(new ScreenCViewModel(), container));

            StringAssert.Contains("produced 0 root elements", thrown.Message);
        }

        [Test]
        public void Create_CatalogEntryWithoutUxml_ThrowsNamingTheCatalog()
        {
            ui.Context.RegisterView<BadgeViewModel, UxmlView<BadgeViewModel>>(() => new UxmlView<BadgeViewModel>());

            var thrown = Assert.Throws<InvalidOperationException>(
                () => ui.Context.ViewFactory.Create(new BadgeViewModel(), container));

            StringAssert.Contains("no UXML assigned for screen 'Badge' in 'TestCatalog'", thrown.Message);
        }

        [Test]
        public void OpenAsync_UxmlScreen_AddsTheClonedRootToTheScreenLayer()
        {
            ui.Context.Register<ScreenAViewModel, UxmlView<ScreenAViewModel>>(
                () => new ScreenAViewModel(), () => new UxmlView<ScreenAViewModel>());

            ui.Open<ScreenAViewModel>();

            Assert.That(ui.ScreenLayer.Q("single-root"), Is.Not.Null);
        }

        private static VisualTreeAsset LoadFixture(string name)
        {
            var guids = AssetDatabase.FindAssets($"{name} t:VisualTreeAsset");
            Assert.That(guids, Has.Length.EqualTo(1), $"Fixture '{name}.uxml' not found.");
            return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
