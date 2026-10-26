# UIKit

An MVVM framework for Unity's UI Toolkit: views, view models, and screen/popup
navigation. Works without a DI container, and has optional
[VContainer](https://github.com/hadashiA/VContainer) and
[Zenject / Extenject](https://github.com/Mathijs-Bakker/Extenject) integrations.

## Installing

Add it via the Unity Package Manager, using the Git URL with a `path` query pointing
at the package folder, pinned to a release tag:

```
https://github.com/Bugord/UIKit.git?path=Assets/UIKit#v0.6.0
```

Package Manager → `+` → *Add package from git URL...* → paste the URL above.

### Requirements

- Unity 6 (`package.json` declares `6000.0`). The tests pass on 6000.3.20f1 and 6000.6.0f1;
  the 6000.3 run had no DI package and no UniTask installed. 6000.0 to 6000.2 are untested.
  Async APIs return `UnityEngine.Awaitable`, so there is no UniTask dependency.
- Optional: VContainer (`jp.hadashikick.vcontainer`) or Extenject
  (`com.svermeulen.extenject`). Each adapter assembly compiles only when its package is
  installed through the Package Manager (an asmdef version define turns on `UIKIT_VCONTAINER`
  / `UIKIT_ZENJECT`). If you have VContainer or Zenject as plain files under `Assets`, the
  define cannot fire: add `UIKIT_VCONTAINER` / `UIKIT_ZENJECT` to *Player Settings → Scripting
  Define Symbols* yourself.
- A `PanelSettings` asset with a theme (*Create → UI Toolkit → Panel Settings Asset*), or an
  `IUIRootProvider` to host the UI inside your own `UIDocument`.

## Quick start (no DI)

1. **Create a Screen Catalog:** *Create → UIKit → Screen Catalog*.

2. **Write a ViewModel.** The screen id is the type name without a trailing `ViewModel`:
   `MainMenuViewModel` → `"MainMenu"`.

   ```csharp
   using Unity.Properties;
   using UIKit;

   public class MainMenuViewModel : ViewModel
   {
       private readonly IScreenNavigator navigator;
       private int clicks;

       public MainMenuViewModel(IScreenNavigator navigator)
       {
           this.navigator = navigator;
       }

       public string Greeting => "Hello, UIKit!";

       [CreateProperty]
       public int Clicks
       {
           get => clicks;
           private set => SetProperty(ref clicks, value);
       }

       public void Click() => Clicks++;
   }
   ```

3. **Write a View and a UXML file named after the id** (`MainMenu.uxml`). The UXML must have
   exactly one root element.

   ```csharp
   using UnityEngine.UIElements;
   using UIKit;

   public class MainMenuView : View<MainMenuViewModel>
   {
       protected override void OnAttach()
       {
           Root.Q<Label>("greeting-label").text = ViewModel.Greeting;
           Root.Q<Button>("click-button").clicked += ViewModel.Click;
       }
   }
   ```

4. **Sync the catalog.** Saving a script triggers a sync that lists `MainMenu` in the
   catalog and picks up `MainMenu.uxml`. Open *Project Settings → UI Kit* to check it (an
   entry without a UXML fails the build); *Sync with code* there re-runs it.

5. **Create the context and open the screen.**

   ```csharp
   using UnityEngine;
   using UnityEngine.UIElements;
   using UIKit;

   public class Boot : MonoBehaviour
   {
       [SerializeField] private PanelSettings panelSettings;
       [SerializeField] private ScreenCatalog catalog;

       private UIKitContext ui;

       private void Start()
       {
           ui = new UIKitContext(panelSettings, catalog);
           ui.Register<MainMenuViewModel, MainMenuView>(() => new MainMenuViewModel(ui.Navigator));

           ui.Navigator.OpenAsync<MainMenuViewModel>().Forget();
       }

       private void OnDestroy() => ui?.Dispose();
   }
   ```

   `Register<TViewModel, TView>(createViewModel)` needs a view with a parameterless
   constructor; the overload `Register(createViewModel, createView)` takes both factories.
   Both are called every time the screen opens. Disposing the context disposes every open
   screen immediately, without hide animations, and destroys the panel it created. A show or
   hide animation that is still running gets its `CancellationToken` cancelled, so a view with
   a custom `ShowAsync` / `HideAsync` should honour the token. An open still in flight faults
   with `OperationCanceledException`. The same applies to a `ScreenGroup` or a DI scope.

`Forget()` is a `UIKit` extension on `Awaitable`: it runs the call without awaiting and
logs an exception instead of losing it (cancellation is ignored).

## VContainer

```csharp
using UIKit;
using UIKit.VContainer;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

public class AppLifetimeScope : LifetimeScope
{
    [SerializeField] private PanelSettings panelSettings;
    [SerializeField] private ScreenCatalog catalog;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.AddUIKit(panelSettings, catalog);
        builder.RegisterScreen<MainMenuViewModel, MainMenuView>();
        builder.RegisterEntryPoint<Boot>();
    }
}

public class Boot : IStartable
{
    private readonly IScreenNavigator navigator;

    public Boot(IScreenNavigator navigator) => this.navigator = navigator;

    public void Start() => navigator.OpenAsync<MainMenuViewModel>().Forget();
}
```

`AddUIKit` registers `UIKitContext`, `IScreenNavigator` and `IViewFactory`; call it once, in
the scope that should own the UI root and the navigation stack. `RegisterScreen` registers
the ViewModel and the View as transient, so a ViewModel can take any registered service in
its constructor. It may be called in any scope: a screen registered in a child
`LifetimeScope` resolves its ViewModel from that scope, and it is force-closed and
unregistered when that scope is disposed. There is still one panel and one navigation stack.

## Zenject / Extenject

```csharp
using UIKit;
using UIKit.Zenject;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

public class AppInstaller : MonoInstaller
{
    [SerializeField] private PanelSettings panelSettings;
    [SerializeField] private ScreenCatalog catalog;

    public override void InstallBindings()
    {
        Container.BindUIKit(panelSettings, catalog);
        Container.RegisterScreen<MainMenuViewModel, MainMenuView>();
        Container.BindInterfacesTo<Boot>().AsSingle().NonLazy();
    }
}

public class Boot : IInitializable
{
    private readonly IScreenNavigator navigator;

    public Boot(IScreenNavigator navigator) => this.navigator = navigator;

    public void Initialize() => navigator.OpenAsync<MainMenuViewModel>().Forget();
}
```

`BindUIKit` binds `UIKitContext`, `IScreenNavigator` and `IViewFactory` in that container.
`RegisterScreen` works in any container; screens registered in a subcontainer close when it is
disposed.

## Hosting inside your own UIDocument

Pass an `IUIRootProvider` instead of a `PanelSettings` to `UIKitContext`, `AddUIKit` or
`BindUIKit`. `VisualElementRootProvider` adds the screen and popup layers to any element:

```csharp
var rootProvider = new VisualElementRootProvider(uiDocument.rootVisualElement);
ui = new UIKitContext(rootProvider, catalog);
```

The context does not own a provider you pass in.

## ViewModels, Views and data binding

A `ViewModel` implements `INotifyBindablePropertyChanged`, and its `View` sets the root
element's `dataSource` to it. Mark properties with `[CreateProperty]` and raise changes with
`SetProperty` (a field-backed property) or `Notify` (a computed one), then bind in UXML:

```xml
<ui:Label>
    <Bindings>
        <ui:DataBinding property="text" data-source-path="Clicks" binding-mode="ToTarget" />
    </Bindings>
</ui:Label>
```

or in code, with `BindProperty` inside `OnAttach`:

```csharp
BindProperty(Root.Q<Label>("clicks-label"), "text", nameof(MainMenuViewModel.Clicks));
```

For a reaction a binding cannot express (an animation, an effect, focus), use `Observe` in
`OnAttach`. It runs on each change of that property, but not on registration, so keep state in
bindings:

```csharp
Observe(nameof(MainMenuViewModel.Clicks), PlayClickEffect);
```

`OnPropertyChanged(string propertyName)` is still there for a view that wants every change; it
runs after the `Observe` callbacks.

Lifecycle:

- `View.OnAttach()` runs once the root exists and the ViewModel is bound; query elements here.
  `View.CreateRoot()` lets a view build its root in code instead of from UXML.
- `ViewModel.OnOpened()` / `OnClosed()` are called by the navigator after the screen is shown
  and when it starts closing. `OnClosed` is not called when the screen is force-closed because
  its context, group or scope was disposed; `OnDispose` runs either way.
- `ViewModel.LifetimeToken` is cancelled when the ViewModel is disposed; pass it to work that
  must not outlive the screen.
- `ShowAsync(CancellationToken)` / `HideAsync(CancellationToken)` on the view return
  `Awaitable` and are virtual: override them to animate, and honour the token (it is cancelled
  when the screen's context, group or scope is disposed mid-animation).
- **`Dispose()` is not virtual** on either class. Override `OnDispose()` instead. A view
  disposes its ViewModel, and the navigator disposes the view when the screen closes.

### Child views

A view can host child views built through its `ViewFactory`. The child is an ordinary
View/ViewModel pair with its own UXML that the navigator never opens. The parent view
disposes its children automatically.

Register only the view, since the parent supplies the ViewModel:

```csharp
ui.RegisterView<PlayerBadgeViewModel, PlayerBadgeView>();            // no DI
builder.RegisterView<PlayerBadgeViewModel, PlayerBadgeView>();       // VContainer
Container.RegisterView<PlayerBadgeViewModel, PlayerBadgeView>();     // Zenject
```

Opening a view registered this way through the navigator throws `InvalidOperationException`.

```csharp
public class ProfileView : View<ProfileViewModel>
{
    protected override void OnAttach()
    {
        var container = Root.Q<VisualElement>("badge-container");
        ViewFactory.Create<PlayerBadgeView, PlayerBadgeViewModel>(
            ViewModel.Badge, container, disposeViewModel: false);   // ProfileViewModel owns Badge
    }
}
```

`Create<TView, TViewModel>(viewModel, container, disposeViewModel = true)` needs the view type
at compile time. `Create(viewModel, container)` picks the view from the ViewModel's runtime
type, and returns an `IView`. Pass `disposeViewModel: false` when something else owns the
ViewModel.

## Navigation

```csharp
var handle = await navigator.OpenAsync<SettingsViewModel>();   // ScreenHandle<SettingsViewModel>
await navigator.ReplaceAsync<LoginViewModel>();                // closes the top entry of its layer
await navigator.CloseAsync(handle.ViewModel);                  // or: await handle.CloseAsync();
await navigator.GoBackAsync();                                 // top popup, else top screen (never the last)
await navigator.PopToRootAsync();                              // everything but the bottom screen
await navigator.CloseAllAsync();
```

`IsOpen(viewModel)` and `Top` report the current state. All of these run one at a time, in call
order, through a FIFO gate. **Do not `await` a navigator call from `OnAttach`, `ShowAsync`,
`HideAsync`, `OnOpened` or `OnClosed`:** it would wait for the operation that is running it.
Use `.Forget()` to queue it. If an operation blocks the queue for longer than
`UIKitContext.StallWarningSeconds` (default 5), UIKit logs an error naming it; set it to 0 to
disable the check, or raise it if a transition really takes that long. The watchdog stops
when the context is disposed.

A screen shown by the navigator closes with the scope or context that registered it; do not
keep a `ScreenHandle` past that.

### Payloads

Implement `IPayloadReceiver<TPayload>`. The payload arrives before the view attaches.

```csharp
public class ProfileViewModel : ViewModel, IPayloadReceiver<string>
{
    public string PlayerName { get; private set; } = string.Empty;

    public void SetPayload(string payload) => PlayerName = payload;
}

await navigator.OpenAsync<ProfileViewModel, string>("Ada");
```

## Popups with results

Derive from `PopupViewModel<TResult>` and give the value that dismissal resolves to. A
`ResultViewModel<TResult>` resolves with `SetResult`; the first result wins.

```csharp
public class ConfirmPopupViewModel : PopupViewModel<bool>
{
    protected override bool CancelResult => false;
}

public class ConfirmPopupView : View<ConfirmPopupViewModel>
{
    protected override void OnAttach()
    {
        Root.Q<Button>("yes-button").clicked += () => ViewModel.SetResult(true);
        Root.Q<Button>("no-button").clicked += ViewModel.Cancel;
    }
}

bool confirmed = await navigator.RunAsync<ConfirmPopupViewModel, bool>();
```

`RunAsync` opens the popup, waits for a result, plays the hide animation and disposes it.
With a payload: `RunAsync<TViewModel, TPayload, TResult>(payload)`. If the popup is closed
from outside (backdrop, Escape, `GoBackAsync`, `CloseAsync`) and its ViewModel is `ICancelable`,
`Cancel()` runs first, so `RunAsync` returns `CancelResult`. For a ViewModel that is not
`ICancelable`, it throws `OperationCanceledException`, as does a cancelled token.
Waiting for a result does not hold the navigation queue.

The generic `TResult` has to be written out; the compiler cannot infer it.

## `[UIScreen]`, layers and dismiss triggers

With no attribute, a ViewModel's screen:

- has the id `TypeNameWithoutViewModel`, which is also its catalog key and the UXML file name;
- opens on `UILayer.Popup` if it implements `IPopup` (`PopupViewModel<T>` does), else on
  `UILayer.Screen`;
- is dismissed by `DismissTriggers.All` as a popup, and by `DismissTriggers.NavigationCancel`
  as a screen.

Screens stack and only the top one is shown; popups stack above every screen and stay visible
underneath each other. Only the top entry takes input and focus: the top screen is focused
once its layer is attached to a panel, also after the host rebuilds its root.

`[UIScreen]` overrides any of this:

```csharp
[UIScreen("Confirmation")]                                      // fixed id / UXML name
public class ConfirmPopupViewModel : PopupViewModel<bool> { /* ... */ }

[UIScreen(UILayer.Popup, "Toast", Dismiss = DismissTriggers.None)]
public class ToastViewModel : ViewModel { }

[UIScreen(Dismiss = DismissTriggers.None)]                       // a screen Escape will not close
public class LevelViewModel : ViewModel { }
```

`DismissTriggers` is a flags enum:

| Trigger | Meaning |
| --- | --- |
| `BackdropClick` | Pointer press outside the popup's content (its root element is the backdrop). Popups only. |
| `NavigationCancel` | Escape or the gamepad cancel button. The bottom screen of the stack is never dismissed this way. |
| `All` / `None` | Both / neither. |

Dismissing closes the entry like `CloseAsync` does, cancelling an `ICancelable` ViewModel first.

## Catalogs

A `ScreenCatalog` maps a screen id to its UXML. Keep it in sync with the code in *Project
Settings → UI Kit*: it syncs when scripts reload, and *Sync with code* forces it. An entry with
no UXML fails the build with a message naming it.

A project may have several catalogs, for example one per feature module. Each lists the
assemblies whose views it holds; the catalog that lists none is the default and takes every
assembly nobody claims. Pass all of them to `AddUIKit`, `BindUIKit` or `UIKitContext`:

```csharp
builder.AddUIKit(panelSettings, mainCatalog, battleCatalog);
```

A screen listed in two catalogs throws. Screens that are not in any passed catalog throw when
first opened, naming the id.

## Using UniTask

UIKit does not depend on UniTask. Its calls return `Awaitable` / `Awaitable<T>`, which can be
awaited only once. In UniTask code convert them with `.AsUniTask()`:

```csharp
using Cysharp.Threading.Tasks;

var confirmed = await navigator.RunAsync<ConfirmPopupViewModel, bool>().AsUniTask();
```

## Sample

*Package Manager → UIKit → Samples → Example* imports screens, popups, payloads, child views
and result flows in three variants: a scene with no DI container, and VContainer and Zenject
variants that compile only when those packages are installed. Open a scene and press Play.

## Running the tests

The package ships EditMode and PlayMode tests in `Tests/`, including VContainer and Zenject
adapter tests that compile only when those packages are installed. To run them from a
consumer project, add the package to `testables` in `Packages/manifest.json`:

```json
"testables": ["com.bugord.uikit"]
```

## Package layout

```
Assets/UIKit/
  package.json
  Runtime/                 UIKit.asmdef, namespace UIKit
  Integrations/
    VContainer/            UIKit.VContainer.asmdef (needs UIKIT_VCONTAINER)
    Zenject/               UIKit.Zenject.asmdef (needs UIKIT_ZENJECT)
  Editor/                  UIKit.Editor.asmdef
  Tests/                   EditMode, PlayMode, VContainer and Zenject tests
  Samples~/Example/
```

## License

MIT — see [LICENSE](LICENSE).
