using System;
using NUnit.Framework;
using UIKit.Zenject;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace UIKit.Tests.ZenjectIntegration
{
    public sealed class HubViewModel : ViewModel
    {
    }

    public sealed class ArmoryService
    {
    }

    public sealed class ArmoryViewModel : ViewModel
    {
        public ArmoryViewModel(ArmoryService service)
        {
            Service = service;
        }

        public ArmoryService Service { get; }
    }

    public sealed class RankBadgeViewModel : ViewModel
    {
    }

    public sealed class HubView : View<HubViewModel>
    {
        protected override VisualElement CreateRoot() => new() { name = "Hub" };
    }

    public sealed class ArmoryView : View<ArmoryViewModel>
    {
        protected override VisualElement CreateRoot() => new() { name = "Armory" };
    }

    public sealed class RankBadgeView : View<RankBadgeViewModel>
    {
        protected override VisualElement CreateRoot() => new() { name = "RankBadge" };
    }

    public class ZenjectIntegrationTests
    {
        private DiContainer container;
        private DiContainer armoryContainer;
        private bool containerDisposed;

        [SetUp]
        public void SetUp()
        {
            containerDisposed = false;
            armoryContainer = null;

            container = new DiContainer();
            ZenjectManagersInstaller.Install(container);
            container.BindUIKit(new VisualElementRootProvider(new VisualElement()));
            container.RegisterScreen<HubViewModel, HubView>();
            container.RegisterView<RankBadgeViewModel, RankBadgeView>();
            container.ResolveRoots();

            Context.StallWarningSeconds = 0;
        }

        [TearDown]
        public void TearDown()
        {
            DisposeArmoryContainer();

            if (!containerDisposed) {
                container.Resolve<DisposableManager>().Dispose();
            }
        }

        private UIKitContext Context => container.Resolve<UIKitContext>();
        private IScreenNavigator Navigator => container.Resolve<IScreenNavigator>();

        [Test]
        public void BindUIKit_ContextResolvedTwice_IsTheSameInstance()
        {
            Assert.That(container.Resolve<UIKitContext>(), Is.SameAs(container.Resolve<UIKitContext>()));
        }

        [Test]
        public void BindUIKit_Navigator_IsTheContextsNavigator()
        {
            Assert.That(container.Resolve<IScreenNavigator>(), Is.SameAs(Context.Navigator));
        }

        [Test]
        public void BindUIKit_ViewFactory_IsTheContextsViewFactory()
        {
            Assert.That(container.Resolve<IViewFactory>(), Is.SameAs(Context.ViewFactory));
        }

        [Test]
        public void BindUIKit_NullPanelSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DiContainer().BindUIKit((PanelSettings)null));
        }

        [Test]
        public void BindUIKit_NullRootProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DiContainer().BindUIKit((IUIRootProvider)null));
        }

        [Test]
        public void RegisterScreen_InTheRootContainer_OpensTheScreen()
        {
            var hub = Sync.Result(Navigator.OpenAsync<HubViewModel>()).ViewModel;

            Assert.That(Navigator.Top, Is.SameAs(hub));
        }

        [Test]
        public void RegisterScreen_OpenedTwice_ResolvesANewViewModelEachTime()
        {
            var first = Sync.Result(Navigator.OpenAsync<HubViewModel>()).ViewModel;

            var second = Sync.Result(Navigator.OpenAsync<HubViewModel>()).ViewModel;

            Assert.That(second, Is.Not.SameAs(first));
        }

        [Test]
        public void RegisterScreen_InASubContainer_ResolvesTheViewModelFromThere()
        {
            var service = new ArmoryService();
            CreateArmoryContainer(service);

            var armory = Sync.Result(Navigator.OpenAsync<ArmoryViewModel>()).ViewModel;

            Assert.That(armory.Service, Is.SameAs(service));
        }

        [Test]
        public void RegisterScreen_InASubContainer_DoesNotRegisterTheParentsScreensAgain()
        {
            // Registering the parent's screens again would throw: a screen is registered in
            // one live group only.
            Assert.DoesNotThrow(() => CreateArmoryContainer(new ArmoryService()));
        }

        [Test]
        public void SubContainer_Disposed_ClosesAndDisposesItsOpenScreens()
        {
            var hub = Sync.Result(Navigator.OpenAsync<HubViewModel>()).ViewModel;
            CreateArmoryContainer(new ArmoryService());
            var armory = Sync.Result(Navigator.OpenAsync<ArmoryViewModel>()).ViewModel;

            DisposeArmoryContainer();

            Assert.That(Navigator.IsOpen(armory), Is.False);
            Assert.That(armory.IsDisposed, Is.True);
            Assert.That(Navigator.Top, Is.SameAs(hub));
        }

        [Test]
        public void SubContainer_Disposed_UnregistersItsScreens()
        {
            CreateArmoryContainer(new ArmoryService());

            DisposeArmoryContainer();

            Assert.Throws<InvalidOperationException>(() => Sync.Result(Navigator.OpenAsync<ArmoryViewModel>()));
        }

        [Test]
        public void SubContainer_Disposed_KeepsTheParentsScreensRegistered()
        {
            CreateArmoryContainer(new ArmoryService());

            DisposeArmoryContainer();

            Assert.DoesNotThrow(() => Sync.Result(Navigator.OpenAsync<HubViewModel>()));
        }

        [Test]
        public void SubContainer_CreatedAgainAfterDispose_RegistersItsScreensAgain()
        {
            CreateArmoryContainer(new ArmoryService());
            DisposeArmoryContainer();

            var service = new ArmoryService();
            CreateArmoryContainer(service);

            Assert.That(Sync.Result(Navigator.OpenAsync<ArmoryViewModel>()).ViewModel.Service, Is.SameAs(service));
        }

        [Test]
        public void RegisterView_View_IsCreatedByTheViewFactory()
        {
            var badge = new RankBadgeViewModel();
            var parent = new VisualElement();

            var view = container.Resolve<IViewFactory>().Create<RankBadgeView, RankBadgeViewModel>(badge, parent);

            Assert.That(view.ViewModel, Is.SameAs(badge));
            Assert.That(view.Root.parent, Is.SameAs(parent));
            view.Dispose();
        }

        [Test]
        public void RegisterView_OpenedAsAScreen_FaultsAsAChildViewOnly()
        {
            var open = Navigator.OpenAsync<RankBadgeViewModel>();

            var thrown = Assert.Throws<InvalidOperationException>(() => Sync.Result(open));
            StringAssert.Contains("child view only", thrown.Message);
        }

        [Test]
        public void RootContainer_Disposed_DisposesTheContext()
        {
            var context = Context;

            DisposeContainer();

            Assert.That(context.IsDisposed, Is.True);
        }

        [Test]
        public void RootContainer_Disposed_DisposesOpenScreens()
        {
            var hub = Sync.Result(Navigator.OpenAsync<HubViewModel>()).ViewModel;

            DisposeContainer();

            Assert.That(hub.IsDisposed, Is.True);
        }

        private void CreateArmoryContainer(ArmoryService service)
        {
            armoryContainer = container.CreateSubContainer();
            armoryContainer.BindInstance(service);
            armoryContainer.RegisterScreen<ArmoryViewModel, ArmoryView>();
            armoryContainer.ResolveRoots();
        }

        // A subcontainer gets its own DisposableManager (the managers are copied into every
        // subcontainer), as a GameObjectContext's would.
        private void DisposeArmoryContainer()
        {
            armoryContainer?.Resolve<DisposableManager>().Dispose();
            armoryContainer = null;
        }

        private void DisposeContainer()
        {
            containerDisposed = true;
            container.Resolve<DisposableManager>().Dispose();
        }
    }

    internal static class Sync
    {
        public static T Result<T>(Awaitable<T> awaitable)
        {
            var awaiter = awaitable.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "Expected the operation to complete synchronously.");
            return awaiter.GetResult();
        }
    }
}
