using System;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace UIKit.Tests
{
    public class UIKitContextTests : NavigatorTestBase
    {
        [Test]
        public void Constructor_NullRootProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new UIKitContext((IUIRootProvider)null));
        }

        [Test]
        public void Register_WithParameterlessView_ConstructsTheViewOnOpen()
        {
            ui.Context.Register<ScreenAViewModel, CodeView<ScreenAViewModel>>(() => new ScreenAViewModel());

            ui.Open<ScreenAViewModel>();

            Assert.That(ui.ScreenLayer.Q("ScreenA"), Is.Not.Null);
        }

        [Test]
        public void RegisterView_MakesTheViewCreatableAsAChild()
        {
            ui.Context.RegisterView<BadgeViewModel, CodeView<BadgeViewModel>>();
            var container = new VisualElement();

            var view = ui.Context.ViewFactory.Create<CodeView<BadgeViewModel>, BadgeViewModel>(new BadgeViewModel(), container);

            Assert.That(view.Root.parent, Is.SameAs(container));
        }

        [Test]
        public void Dispose_DisposesEveryOpenScreenWithoutOnClosedOrAHideAnimation()
        {
            ui.Register<ScreenAViewModel>();
            ui.Register<ScreenBViewModel>();
            ui.Register<PopupAViewModel>();
            var a = ui.Open<ScreenAViewModel>();
            var b = ui.Open<ScreenBViewModel>();
            var popup = ui.Open<PopupAViewModel>();
            ui.Log.Clear();

            ui.Context.Dispose();

            Assert.That(new ViewModel[] { a, b, popup }, Has.All.Property(nameof(ViewModel.IsDisposed)).True);
            Assert.That(ui.Log.Entries, Has.None.EndsWith(".OnClosed"));
            Assert.That(ui.Log.Entries, Has.None.EndsWith(".hide"));
            Assert.That(ui.ScreenLayer.childCount + ui.PopupLayer.childCount, Is.Zero);
            Assert.That(ui.PopupLayer.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(ui.Navigator.Top, Is.Null);
        }

        [Test]
        public void Dispose_DisposesItsGroups()
        {
            var group = ui.Context.CreateGroup();

            ui.Context.Dispose();

            Assert.That(group.IsDisposed, Is.True);
        }

        [Test]
        public void Dispose_Twice_DisposesScreensOnce()
        {
            ui.Register<ScreenAViewModel>();
            var a = ui.Open<ScreenAViewModel>();
            ui.Context.Dispose();

            ui.Context.Dispose();

            Assert.That(a.OnDisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void OpenAsync_AfterDispose_FaultsWithObjectDisposed()
        {
            ui.Register<ScreenAViewModel>();
            ui.Context.Dispose();

            AwaitableAssert.Faults<ObjectDisposedException, ScreenHandle<ScreenAViewModel>>(
                ui.Navigator.OpenAsync<ScreenAViewModel>());
        }

        [Test]
        public void RunAsync_AfterDispose_FaultsWithObjectDisposed()
        {
            ui.RegisterWith(() => new AnswerViewModel());
            ui.Context.Dispose();

            AwaitableAssert.Faults<ObjectDisposedException, int>(ui.Navigator.RunAsync<AnswerViewModel, int>());
        }

        [Test]
        public void ClosingOperations_AfterDispose_DoNothing()
        {
            ui.Register<ScreenAViewModel>();
            var a = ui.Open<ScreenAViewModel>();
            ui.Context.Dispose();

            AwaitableAssert.Completes(ui.Navigator.CloseAsync(a));
            Assert.That(AwaitableAssert.Completes(ui.Navigator.GoBackAsync()), Is.False);
            AwaitableAssert.Completes(ui.Navigator.PopToRootAsync());
            AwaitableAssert.Completes(ui.Navigator.CloseAllAsync());
        }

        [Test]
        public void Register_AfterDispose_Throws()
        {
            ui.Context.Dispose();

            Assert.Throws<ObjectDisposedException>(() => ui.Register<ScreenAViewModel>());
        }

        [Test]
        public void RegisterView_AfterDispose_Throws()
        {
            ui.Context.Dispose();

            Assert.Throws<ObjectDisposedException>(() => ui.Context.RegisterView<BadgeViewModel, CodeView<BadgeViewModel>>());
        }

        [Test]
        public void CreateGroup_AfterDispose_Throws()
        {
            ui.Context.Dispose();

            Assert.Throws<ObjectDisposedException>(() => ui.Context.CreateGroup());
        }

        [Test]
        public void Dispose_WhileAScreenIsShowing_CancelsTheShowAndFaultsTheOpen()
        {
            ui.Register<ScreenAViewModel>(view => view.HoldNextShow());
            var open = ui.Navigator.OpenAsync<ScreenAViewModel>();
            var a = ui.LastCreated<ScreenAViewModel>();

            ui.Context.Dispose();

            AwaitableAssert.Faults<OperationCanceledException, ScreenHandle<ScreenAViewModel>>(open);
            Assert.That(a.OnOpenedCount, Is.Zero);
            Assert.That(a.OnDisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_WithAnOpenQueued_FaultsItWithObjectDisposed()
        {
            ui.Register<ScreenAViewModel>(view => view.HoldNextShow());
            ui.Register<ScreenBViewModel>();
            ui.Navigator.OpenAsync<ScreenAViewModel>();
            var queued = ui.Navigator.OpenAsync<ScreenBViewModel>();

            ui.Context.Dispose();

            AwaitableAssert.Faults<ObjectDisposedException, ScreenHandle<ScreenBViewModel>>(queued);
            Assert.That(ui.Created, Has.None.InstanceOf<ScreenBViewModel>());
        }

        [Test]
        public void Dispose_WhileAScreenIsHiding_DisposesItRightAway()
        {
            ui.Register<ScreenAViewModel>();
            ui.Register<ScreenBViewModel>();
            ui.Open<ScreenAViewModel>();
            var b = ui.Open<ScreenBViewModel>();
            ui.ViewOf(b).HoldNextHide();
            ui.Navigator.CloseAsync(b);

            ui.Context.Dispose();

            Assert.That(b.IsDisposed, Is.True);
            Assert.That(ui.ScreenLayer.childCount, Is.Zero);
        }
    }
}
