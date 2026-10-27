using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIKit.Tests
{
    // Records what screens do, in order, across ViewModels and views.
    internal sealed class EventLog
    {
        private readonly List<string> entries = new();

        public IReadOnlyList<string> Entries => entries;

        public void Add(string entry) => entries.Add(entry);

        public void Clear() => entries.Clear();

        public bool Contains(string entry) => entries.Contains(entry);

        public int IndexOf(string entry) => entries.IndexOf(entry);
    }

    // Logs its lifecycle as "<screen id>.<hook>".
    internal abstract class LoggedViewModel : ViewModel
    {
        public EventLog Log;
        public Action OnOpenedAction;
        public Action OnClosedAction;
        public int OnOpenedCount;
        public int OnClosedCount;
        public int OnDisposeCount;

        protected string Id => ScreenInfo.Of(GetType()).Id;

        protected internal override void OnOpened()
        {
            OnOpenedCount++;
            Log?.Add($"{Id}.OnOpened");
            OnOpenedAction?.Invoke();
        }

        protected internal override void OnClosed()
        {
            OnClosedCount++;
            Log?.Add($"{Id}.OnClosed");
            OnClosedAction?.Invoke();
        }

        protected override void OnDispose()
        {
            OnDisposeCount++;
            Log?.Add($"{Id}.OnDispose");
        }
    }

    internal sealed class ScreenAViewModel : LoggedViewModel
    {
    }

    internal sealed class ScreenBViewModel : LoggedViewModel
    {
    }

    internal sealed class ScreenCViewModel : LoggedViewModel
    {
    }

    internal sealed class PopupAViewModel : LoggedViewModel, IPopup
    {
    }

    internal sealed class PopupBViewModel : LoggedViewModel, IPopup
    {
    }

    internal sealed class UnregisteredViewModel : LoggedViewModel
    {
    }

    internal sealed class CancelableViewModel : LoggedViewModel, ICancelable
    {
        public void Cancel() => Log?.Add($"{Id}.Cancel");
    }

    internal sealed class PayloadViewModel : LoggedViewModel, IPayloadReceiver<string>
    {
        public string Payload;
        public Exception PayloadException;

        public void SetPayload(string payload)
        {
            Log?.Add($"{Id}.SetPayload:{payload}");

            if (PayloadException != null) {
                throw PayloadException;
            }

            Payload = payload;
        }
    }

    internal sealed class ScoreViewModel : LoggedViewModel
    {
        private int score;

        [CreateProperty]
        public int Score
        {
            get => score;
            set => SetProperty(ref score, value);
        }

        public bool TrySetScore(int value) => SetProperty(ref score, value, nameof(Score));

        public void NotifyProperty(string propertyName) => Notify(propertyName);

        public void NotifyFromCaller() => Notify();

        public void NotifyMany(string first, string second, params string[] rest) => Notify(first, second, rest);
    }

    // Properties of each kind the binding and observe validation tells apart.
    internal class BindingViewModel : LoggedViewModel
    {
        private string name = string.Empty;
        private float volume;

        [CreateProperty]
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        [CreateProperty]
        public float Volume
        {
            get => volume;
            set => SetProperty(ref volume, value);
        }

        // Computed, so it has no setter: a binding that writes to it must be rejected.
        [CreateProperty]
        public string Greeting => $"Hello, {name}";

        // Not a [CreateProperty], so Unity.Properties and therefore bindings cannot see it.
        public string Hidden => name;

        public void NotifyProperty(string propertyName) => Notify(propertyName);
    }

    // Bound through a view of its base type, so validation must resolve against the runtime type.
    internal sealed class DerivedBindingViewModel : BindingViewModel
    {
        private int extra;

        [CreateProperty]
        public int Extra
        {
            get => extra;
            set => SetProperty(ref extra, value);
        }
    }

    // Only ever created as a child view, so it is registered with RegisterView.
    internal sealed class BadgeViewModel : LoggedViewModel
    {
    }

    internal sealed class BadgeResultViewModel : ResultViewModel<int>
    {
    }

    // A result screen that is not ICancelable: closing it from elsewhere leaves it without a result.
    internal sealed class AnswerViewModel : ResultViewModel<int>
    {
        // Set as soon as the screen opens, before RunAsync starts waiting.
        public int? AnswerOnOpen;

        protected internal override void OnOpened()
        {
            if (AnswerOnOpen.HasValue) {
                SetResult(AnswerOnOpen.Value);
            }
        }
    }

    internal sealed class GreetingViewModel : ResultViewModel<string>, IPayloadReceiver<string>
    {
        public string Name;

        public void SetPayload(string payload) => Name = payload;
    }

    internal sealed class ConfirmViewModel : PopupViewModel<bool>
    {
        public int CancelCount;

        protected override bool CancelResult => false;

        public override void Cancel()
        {
            CancelCount++;
            base.Cancel();
        }
    }

    // A view built in code, so no UXML or catalog is needed. Logs as "<screen id>.<hook>", and
    // can hold its next show or hide animation open until the test completes it.
    internal sealed class TestView<TViewModel> : View<TViewModel> where TViewModel : ViewModel
    {
        private readonly EventLog log;

        private Hold heldShow;
        private Hold heldHide;
        private AwaitableCompletionSource lastShowHold;
        private Exception showException;

        public TestView(EventLog log = null)
        {
            this.log = log;
        }

        public static string Id => ScreenInfo.Of(typeof(TViewModel)).Id;

        public int AttachCount { get; private set; }
        public int ShowCount { get; private set; }
        public int HideCount { get; private set; }
        public int DisposeCount { get; private set; }
        public List<string> ChangedProperties { get; } = new();
        public Action<TestView<TViewModel>> Attached;
        public Action<string> PropertyChangedAction;

        // Root is null once disposed; display is what Show/HideAsync set.
        public bool IsShown => Root != null && Root.style.display.value != DisplayStyle.None;

        public IViewFactory Children => ViewFactory;

        public void ObserveProperty(string propertyName, Action onChanged) => Observe(propertyName, onChanged);

        public void ObserveValue<T>(string propertyName, Action<T> onChanged) => Observe(propertyName, onChanged);

        public T RequireElement<T>(string name) where T : VisualElement => Require<T>(name);

        public DataBinding BindElementProperty(VisualElement element, string elementProperty, string viewModelProperty,
            BindingMode mode = BindingMode.ToTarget) =>
            BindProperty(element, elementProperty, viewModelProperty, mode);

        public DataBinding BindTextOf(string elementName, string viewModelProperty) =>
            BindText(elementName, viewModelProperty);

        public DataBinding BindTextOf(TextElement element, string viewModelProperty) =>
            BindText(element, viewModelProperty);

        public DataBinding BindValueOf(string elementName, string viewModelProperty,
            BindingMode mode = BindingMode.TwoWay) =>
            BindValue(elementName, viewModelProperty, mode);

        public DataBinding BindValueOf<TValue>(BaseField<TValue> field, string viewModelProperty,
            BindingMode mode = BindingMode.TwoWay) =>
            BindValue(field, viewModelProperty, mode);

        public void BindCommandTo(Button button, ICommand command) => BindCommand(button, command);

        public void BindCommandTo(string buttonName, ICommand command) => BindCommand(buttonName, command);

        // The next ShowAsync stays pending until the returned source completes. With
        // touchRootAfterwards it then sets the root's style, as an animation's last frame would.
        public AwaitableCompletionSource HoldNextShow(bool touchRootAfterwards = false)
        {
            heldShow = new Hold(touchRootAfterwards);
            lastShowHold = heldShow.Source;
            return heldShow.Source;
        }

        // Completes the show held by the last HoldNextShow, e.g. one set up in a view factory.
        public void ReleaseHeldShow()
        {
            Assert.That(lastShowHold, Is.Not.Null, "No show was held.");
            lastShowHold.SetResult();
        }

        public AwaitableCompletionSource HoldNextHide()
        {
            heldHide = new Hold(false);
            return heldHide.Source;
        }

        public void FailNextShow(Exception exception) => showException = exception;

        public override Awaitable ShowAsync(CancellationToken token)
        {
            ShowCount++;
            log?.Add($"{Id}.show");

            if (showException != null) {
                var exception = showException;
                showException = null;
                throw exception;
            }

            if (heldShow == null) {
                return base.ShowAsync(token);
            }

            var hold = heldShow;
            heldShow = null;
            return WaitAsync(hold, DisplayStyle.Flex, token);
        }

        public override Awaitable HideAsync(CancellationToken token)
        {
            HideCount++;
            log?.Add($"{Id}.hide");

            if (heldHide == null) {
                return base.HideAsync(token);
            }

            var hold = heldHide;
            heldHide = null;
            return WaitAsync(hold, DisplayStyle.None, token);
        }

        protected override VisualElement CreateRoot()
        {
            var root = new VisualElement { name = Id };
            root.Add(new VisualElement { name = "content" });
            return root;
        }

        protected override void OnAttach()
        {
            AttachCount++;
            log?.Add($"{Id}.attach");
            Attached?.Invoke(this);
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            ChangedProperties.Add(propertyName);
            PropertyChangedAction?.Invoke(propertyName);
        }

        protected override void OnDispose()
        {
            DisposeCount++;
            log?.Add($"{Id}.view.dispose");
        }

        private async Awaitable WaitAsync(Hold hold, DisplayStyle display, CancellationToken token)
        {
            using (token.Register(() => hold.Source.TrySetCanceled())) {
                await hold.Source.Awaitable;
            }

            if (hold.TouchRootAfterwards) {
                Root.style.display = display;
            }
        }

        private sealed class Hold
        {
            public readonly AwaitableCompletionSource Source = new();
            public readonly bool TouchRootAfterwards;

            public Hold(bool touchRootAfterwards)
            {
                TouchRootAfterwards = touchRootAfterwards;
            }
        }
    }

    // A view whose root comes from the catalog's UXML.
    internal sealed class UxmlView<TViewModel> : View<TViewModel> where TViewModel : ViewModel
    {
    }

    // A parameterless code-built view, for the Register/RegisterView overloads that construct it.
    internal sealed class CodeView<TViewModel> : View<TViewModel> where TViewModel : ViewModel
    {
        protected override VisualElement CreateRoot() => new() { name = ScreenInfo.Of(typeof(TViewModel)).Id };
    }

    // A context hosted in a plain element, so no panel is needed, plus helpers to register
    // TestView screens and find the views the navigator created.
    internal sealed class UIHarness : IDisposable
    {
        private readonly List<IView> views = new();

        public UIHarness(params ScreenCatalog[] catalogs)
        {
            var provider = new VisualElementRootProvider(new VisualElement());
            ScreenLayer = provider.GetLayerRoot(UILayer.Screen);
            PopupLayer = provider.GetLayerRoot(UILayer.Popup);

            // The stall watchdog waits for frames, which EditMode tests do not run.
            Context = new UIKitContext(provider, catalogs) { StallWarningSeconds = 0f };
        }

        public EventLog Log { get; } = new();
        public UIKitContext Context { get; }
        public IScreenNavigator Navigator => Context.Navigator;
        public VisualElement ScreenLayer { get; }
        public VisualElement PopupLayer { get; }

        // Every ViewModel the registered factories created, in order.
        public List<ViewModel> Created { get; } = new();

        public void Register<TViewModel>(Action<TestView<TViewModel>> configure = null)
            where TViewModel : LoggedViewModel, new()
        {
            Context.Register<TViewModel, TestView<TViewModel>>(Factory<TViewModel>(), ViewFactory(configure));
        }

        public void Register<TViewModel>(ScreenGroup group, Action<TestView<TViewModel>> configure = null)
            where TViewModel : LoggedViewModel, new()
        {
            group.Register<TViewModel, TestView<TViewModel>>(Factory<TViewModel>(), ViewFactory(configure));
        }

        public void RegisterWith<TViewModel>(Func<TViewModel> createViewModel,
            Action<TestView<TViewModel>> configure = null)
            where TViewModel : ViewModel
        {
            Context.Register<TViewModel, TestView<TViewModel>>(Track(createViewModel), ViewFactory(configure));
        }

        public Func<TViewModel> Factory<TViewModel>() where TViewModel : LoggedViewModel, new()
        {
            return Track(() => new TViewModel { Log = Log });
        }

        public TViewModel LastCreated<TViewModel>() where TViewModel : ViewModel =>
            Created.OfType<TViewModel>().Last();

        public TestView<TViewModel> ViewOf<TViewModel>(TViewModel viewModel) where TViewModel : ViewModel =>
            views.OfType<TestView<TViewModel>>().Last(view => ReferenceEquals(view.ViewModel, viewModel));

        public TestView<TViewModel> LastView<TViewModel>() where TViewModel : ViewModel =>
            views.OfType<TestView<TViewModel>>().Last();

        public TViewModel Open<TViewModel>() where TViewModel : ViewModel =>
            AwaitableAssert.Completes(Navigator.OpenAsync<TViewModel>()).ViewModel;

        public void Dispose() => Context.Dispose();

        private Func<TViewModel> Track<TViewModel>(Func<TViewModel> create) where TViewModel : ViewModel
        {
            return () => {
                var viewModel = create();
                Created.Add(viewModel);
                return viewModel;
            };
        }

        private Func<TestView<TViewModel>> ViewFactory<TViewModel>(Action<TestView<TViewModel>> configure)
            where TViewModel : ViewModel
        {
            return () => {
                var view = new TestView<TViewModel>(Log);
                configure?.Invoke(view);
                views.Add(view);
                return view;
            };
        }
    }

    // A fresh context per test, disposed afterwards.
    public abstract class NavigatorTestBase
    {
        private protected UIHarness ui;

        [SetUp]
        public void CreateHarness()
        {
            ui = new UIHarness();
        }

        [TearDown]
        public void DisposeHarness()
        {
            ui.Dispose();
        }
    }

    internal static class AwaitableAssert
    {
        // Everything under test completes inline unless a test holds it, so a pending operation
        // is a failure here rather than a hang.
        public static void Completes(Awaitable awaitable)
        {
            Assert.That(awaitable.GetAwaiter().IsCompleted, Is.True, "The operation is still pending.");
            awaitable.GetAwaiter().GetResult();
        }

        public static T Completes<T>(Awaitable<T> awaitable)
        {
            Assert.That(awaitable.GetAwaiter().IsCompleted, Is.True, "The operation is still pending.");
            return awaitable.GetAwaiter().GetResult();
        }

        public static TException Faults<TException>(Awaitable awaitable) where TException : Exception
        {
            Assert.That(awaitable.GetAwaiter().IsCompleted, Is.True, "The operation is still pending.");
            return Assert.Catch<TException>(() => awaitable.GetAwaiter().GetResult());
        }

        public static TException Faults<TException, T>(Awaitable<T> awaitable) where TException : Exception
        {
            Assert.That(awaitable.GetAwaiter().IsCompleted, Is.True, "The operation is still pending.");
            return Assert.Catch<TException>(() => awaitable.GetAwaiter().GetResult());
        }
    }

    internal static class Catalogs
    {
        public static ScreenCatalog Create(string name, params (string id, VisualTreeAsset uxml)[] entries)
        {
            var catalog = ScriptableObject.CreateInstance<ScreenCatalog>();
            catalog.name = name;
            catalog.SetEntries(entries
                .Select(entry => new ScreenCatalog.Entry { screenId = entry.id, uxml = entry.uxml })
                .ToArray());
            return catalog;
        }
    }
}
