using System;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace UIKit.Tests
{
    public class ScreenGroupTests : NavigatorTestBase
    {
        private ScreenGroup group;

        [SetUp]
        public void CreateGroup()
        {
            group = ui.Context.CreateGroup();
        }

        [Test]
        public void Register_SameScreenTwiceInOneGroup_Throws()
        {
            ui.Register<ScreenAViewModel>(group);

            var thrown = Assert.Throws<InvalidOperationException>(() => ui.Register<ScreenAViewModel>(group));

            StringAssert.Contains("in this group", thrown.Message);
        }

        [Test]
        public void Register_SameScreenInTwoLiveGroups_Throws()
        {
            ui.Register<ScreenAViewModel>(group);

            var thrown = Assert.Throws<InvalidOperationException>(
                () => ui.Register<ScreenAViewModel>(ui.Context.CreateGroup()));

            StringAssert.Contains("another screen group", thrown.Message);
        }

        [Test]
        public void RegisterView_ScreenAlreadyRegistered_Throws()
        {
            ui.Register<BadgeViewModel>(group);

            Assert.Throws<InvalidOperationException>(
                () => group.RegisterView<BadgeViewModel, TestView<BadgeViewModel>>(() => new TestView<BadgeViewModel>()));
        }

        [Test]
        public void Register_NullViewModelFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => group.Register<ScreenAViewModel, TestView<ScreenAViewModel>>(null, () => new TestView<ScreenAViewModel>()));
        }

        [Test]
        public void Register_NullViewFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => group.Register<ScreenAViewModel, TestView<ScreenAViewModel>>(() => new ScreenAViewModel(), null));
        }

        [Test]
        public void RegisterView_NullViewFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => group.RegisterView<BadgeViewModel, TestView<BadgeViewModel>>(null));
        }

        [Test]
        public void Register_AfterDispose_Throws()
        {
            group.Dispose();

            Assert.Throws<ObjectDisposedException>(() => ui.Register<ScreenAViewModel>(group));
        }

        [Test]
        public void RegisterView_AfterDispose_Throws()
        {
            group.Dispose();

            Assert.Throws<ObjectDisposedException>(() => group.RegisterView<BadgeViewModel, CodeView<BadgeViewModel>>());
        }

        [Test]
        public void Dispose_UnregistersItsScreens()
        {
            ui.Register<ScreenAViewModel>(group);

            group.Dispose();

            var thrown = AwaitableAssert.Faults<InvalidOperationException, ScreenHandle<ScreenAViewModel>>(
                ui.Navigator.OpenAsync<ScreenAViewModel>());
            StringAssert.Contains("not registered", thrown.Message);
        }

        [Test]
        public void Dispose_UnregistersItsChildViews()
        {
            group.RegisterView<BadgeViewModel, CodeView<BadgeViewModel>>();

            group.Dispose();

            Assert.Throws<InvalidOperationException>(
                () => ui.Context.ViewFactory.Create(new BadgeViewModel(), new VisualElement()));
        }

        [Test]
        public void Dispose_LetsAnotherGroupRegisterTheSameScreen()
        {
            ui.Register<ScreenAViewModel>(group);
            group.Dispose();
            var next = ui.Context.CreateGroup();

            ui.Register<ScreenAViewModel>(next);

            Assert.That(ui.Open<ScreenAViewModel>(), Is.Not.Null);
        }

        [Test]
        public void Dispose_DisposesItsOpenScreensWithoutOnClosedOrAHideAnimation()
        {
            ui.Register<ScreenAViewModel>(group);
            var a = ui.Open<ScreenAViewModel>();
            var view = ui.ViewOf(a);

            group.Dispose();

            Assert.That(a.IsDisposed, Is.True);
            Assert.That(a.OnClosedCount, Is.Zero);
            Assert.That(view.HideCount, Is.Zero);
            Assert.That(ui.ScreenLayer.childCount, Is.Zero);
            Assert.That(ui.Navigator.IsOpen(a), Is.False);
            Assert.That(ui.Navigator.Top, Is.Null);
        }

        [Test]
        public void Dispose_KeepsOtherGroupsScreensAndShowsTheOneItUncovered()
        {
            ui.Register<ScreenAViewModel>();
            ui.Register<ScreenBViewModel>(group);
            var a = ui.Open<ScreenAViewModel>();
            ui.Open<ScreenBViewModel>();

            group.Dispose();

            Assert.That(ui.Navigator.Top, Is.SameAs(a));
            Assert.That(ui.ViewOf(a).IsShown, Is.True);
            Assert.That(a.IsDisposed, Is.False);
        }

        [Test]
        public void Dispose_ClosesItsPopups_SoThePopupLayerStopsBlockingInput()
        {
            ui.Register<ScreenAViewModel>();
            ui.Register<PopupAViewModel>(group);
            ui.Open<ScreenAViewModel>();
            ui.Open<PopupAViewModel>();

            group.Dispose();

            Assert.That(ui.PopupLayer.pickingMode, Is.EqualTo(PickingMode.Ignore));
        }

        [Test]
        public void Dispose_Twice_DisposesItsScreensOnce()
        {
            ui.Register<ScreenAViewModel>(group);
            var a = ui.Open<ScreenAViewModel>();
            group.Dispose();

            group.Dispose();

            Assert.That(a.OnDisposeCount, Is.EqualTo(1));
            Assert.That(group.IsDisposed, Is.True);
        }

        [Test]
        public void Dispose_WhileItsScreenIsShowing_DisposesItRightAway()
        {
            ui.Register<ScreenAViewModel>();
            ui.Register<ScreenBViewModel>(group, view => view.HoldNextShow());
            ui.Open<ScreenAViewModel>();
            ui.Navigator.OpenAsync<ScreenBViewModel>();
            var b = ui.LastCreated<ScreenBViewModel>();

            group.Dispose();

            Assert.That(b.IsDisposed, Is.True);
            Assert.That(ui.Navigator.IsOpen(b), Is.False);
            Assert.That(ui.ViewOf(b).Root, Is.Null);
        }

        [Test]
        public void Dispose_WhileItsScreenIsShowing_CancelsTheShowAndFaultsTheOpen()
        {
            ui.Register<ScreenAViewModel>();
            ui.Register<ScreenBViewModel>(group, view => view.HoldNextShow());
            var a = ui.Open<ScreenAViewModel>();
            var open = ui.Navigator.OpenAsync<ScreenBViewModel>();
            var b = ui.LastCreated<ScreenBViewModel>();
            group.Dispose();

            AwaitableAssert.Faults<OperationCanceledException, ScreenHandle<ScreenBViewModel>>(open);
            Assert.That(b.OnOpenedCount, Is.Zero);
            Assert.That(b.OnDisposeCount, Is.EqualTo(1));
            Assert.That(ui.ScreenLayer.childCount, Is.EqualTo(1));
            Assert.That(ui.Navigator.Top, Is.SameAs(a));
            Assert.That(ui.ViewOf(a).IsShown, Is.True);
        }

        [Test]
        public void Dispose_WhileItsScreenIsHiding_DisposesItRightAway()
        {
            // The group's services are going away, so a closing screen must not keep running
            // view code against them until its hide animation ends.
            ui.Register<ScreenAViewModel>();
            ui.Register<ScreenBViewModel>(group);
            ui.Open<ScreenAViewModel>();
            var b = ui.Open<ScreenBViewModel>();
            ui.ViewOf(b).HoldNextHide();
            ui.Navigator.CloseAsync(b);

            group.Dispose();

            Assert.That(b.IsDisposed, Is.True);
            Assert.That(ui.ViewOf(b).Root, Is.Null);
        }

        [Test]
        public void Dispose_WhileItsScreenIsRunning_FaultsTheRunWithOperationCanceled()
        {
            group.Register<AnswerViewModel, TestView<AnswerViewModel>>(
                () => new AnswerViewModel(), () => new TestView<AnswerViewModel>());
            var run = ui.Navigator.RunAsync<AnswerViewModel, int>();

            group.Dispose();

            AwaitableAssert.Faults<OperationCanceledException, int>(run);
        }
    }
}
