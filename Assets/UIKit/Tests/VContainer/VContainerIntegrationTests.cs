using System;
using NUnit.Framework;
using UIKit.VContainer;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace UIKit.Tests.VContainerIntegration
{
    public sealed class LobbyViewModel : ViewModel
    {
    }

    public sealed class ShopService
    {
    }

    public sealed class ShopViewModel : ViewModel
    {
        public ShopViewModel(ShopService service)
        {
            Service = service;
        }

        public ShopService Service { get; }
    }

    public sealed class CoinBadgeViewModel : ViewModel
    {
    }

    public sealed class LobbyView : View<LobbyViewModel>
    {
        protected override VisualElement CreateRoot() => new() { name = "Lobby" };
    }

    public sealed class ShopView : View<ShopViewModel>
    {
        protected override VisualElement CreateRoot() => new() { name = "Shop" };
    }

    public sealed class CoinBadgeView : View<CoinBadgeViewModel>
    {
        protected override VisualElement CreateRoot() => new() { name = "CoinBadge" };
    }

    public class VContainerIntegrationTests
    {
        private IObjectResolver container;
        private IScopedObjectResolver shopScope;
        private bool containerDisposed;

        [SetUp]
        public void SetUp()
        {
            containerDisposed = false;

            var builder = new ContainerBuilder();
            builder.AddUIKit(new VisualElementRootProvider(new VisualElement()));
            builder.RegisterScreen<LobbyViewModel, LobbyView>();
            builder.RegisterView<CoinBadgeViewModel, CoinBadgeView>();
            container = builder.Build();

            Context.StallWarningSeconds = 0;
        }

        [TearDown]
        public void TearDown()
        {
            DisposeShopScope();

            if (!containerDisposed) {
                container.Dispose();
            }
        }

        private UIKitContext Context => container.Resolve<UIKitContext>();
        private IScreenNavigator Navigator => container.Resolve<IScreenNavigator>();

        [Test]
        public void AddUIKit_ContextResolvedTwice_IsTheSameInstance()
        {
            Assert.That(container.Resolve<UIKitContext>(), Is.SameAs(container.Resolve<UIKitContext>()));
        }

        [Test]
        public void AddUIKit_Navigator_IsTheContextsNavigator()
        {
            Assert.That(container.Resolve<IScreenNavigator>(), Is.SameAs(Context.Navigator));
        }

        [Test]
        public void AddUIKit_ViewFactory_IsTheContextsViewFactory()
        {
            Assert.That(container.Resolve<IViewFactory>(), Is.SameAs(Context.ViewFactory));
        }

        [Test]
        public void AddUIKit_NullPanelSettings_Throws()
        {
            var builder = new ContainerBuilder();

            Assert.Throws<ArgumentNullException>(() => builder.AddUIKit((PanelSettings)null));
        }

        [Test]
        public void AddUIKit_NullRootProvider_Throws()
        {
            var builder = new ContainerBuilder();

            Assert.Throws<ArgumentNullException>(() => builder.AddUIKit((IUIRootProvider)null));
        }

        [Test]
        public void RegisterScreen_InTheRootScope_OpensTheScreen()
        {
            var lobby = Sync.Result(Navigator.OpenAsync<LobbyViewModel>()).ViewModel;

            Assert.That(Navigator.Top, Is.SameAs(lobby));
        }

        [Test]
        public void RegisterScreen_OpenedTwice_ResolvesANewViewModelEachTime()
        {
            var first = Sync.Result(Navigator.OpenAsync<LobbyViewModel>()).ViewModel;

            var second = Sync.Result(Navigator.OpenAsync<LobbyViewModel>()).ViewModel;

            Assert.That(second, Is.Not.SameAs(first));
        }

        [Test]
        public void RegisterScreen_InAChildScope_ResolvesTheViewModelFromThatScope()
        {
            var service = new ShopService();
            CreateShopScope(service);

            var shop = Sync.Result(Navigator.OpenAsync<ShopViewModel>()).ViewModel;

            Assert.That(shop.Service, Is.SameAs(service));
        }

        [Test]
        public void ChildScope_Disposed_ClosesAndDisposesItsOpenScreens()
        {
            var lobby = Sync.Result(Navigator.OpenAsync<LobbyViewModel>()).ViewModel;
            CreateShopScope(new ShopService());
            var shop = Sync.Result(Navigator.OpenAsync<ShopViewModel>()).ViewModel;

            DisposeShopScope();

            Assert.That(Navigator.IsOpen(shop), Is.False);
            Assert.That(shop.IsDisposed, Is.True);
            Assert.That(Navigator.Top, Is.SameAs(lobby));
        }

        [Test]
        public void ChildScope_Disposed_UnregistersItsScreens()
        {
            CreateShopScope(new ShopService());

            DisposeShopScope();

            Assert.Throws<InvalidOperationException>(() => Sync.Result(Navigator.OpenAsync<ShopViewModel>()));
        }

        [Test]
        public void ChildScope_Disposed_KeepsTheParentsScreensRegistered()
        {
            CreateShopScope(new ShopService());

            DisposeShopScope();

            Assert.DoesNotThrow(() => Sync.Result(Navigator.OpenAsync<LobbyViewModel>()));
        }

        [Test]
        public void ChildScope_CreatedAgainAfterDispose_RegistersItsScreensAgain()
        {
            CreateShopScope(new ShopService());
            DisposeShopScope();

            var service = new ShopService();
            CreateShopScope(service);

            Assert.That(Sync.Result(Navigator.OpenAsync<ShopViewModel>()).ViewModel.Service, Is.SameAs(service));
        }

        [Test]
        public void RegisterView_View_IsCreatedByTheViewFactory()
        {
            var badge = new CoinBadgeViewModel();
            var parent = new VisualElement();

            var view = container.Resolve<IViewFactory>().Create<CoinBadgeView, CoinBadgeViewModel>(badge, parent);

            Assert.That(view.ViewModel, Is.SameAs(badge));
            Assert.That(view.Root.parent, Is.SameAs(parent));
            view.Dispose();
        }

        [Test]
        public void RegisterView_OpenedAsAScreen_FaultsAsAChildViewOnly()
        {
            var open = Navigator.OpenAsync<CoinBadgeViewModel>();

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
            var lobby = Sync.Result(Navigator.OpenAsync<LobbyViewModel>()).ViewModel;

            DisposeContainer();

            Assert.That(lobby.IsDisposed, Is.True);
        }

        private void DisposeShopScope()
        {
            shopScope?.Dispose();
            shopScope = null;
        }

        private void DisposeContainer()
        {
            containerDisposed = true;
            container.Dispose();
        }

        private void CreateShopScope(ShopService service)
        {
            shopScope = container.CreateScope(builder => {
                builder.RegisterInstance(service);
                builder.RegisterScreen<ShopViewModel, ShopView>();
            });
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
