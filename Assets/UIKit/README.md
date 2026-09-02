# UIKit

An MVVM framework for Unity's UI Toolkit: views, view models, and screen/popup
navigation, with an optional VContainer integration.

## Installing

Add it to your project via the Unity Package Manager, using the Git URL with a
`path` query pointing at this package folder:

```
https://github.com/Bugord/UIKit.git?path=Assets/UIKit
```

Package Manager → `+` → *Add package from git URL...* → paste the URL above.

Requires:
- Unity 6000.0+
- [UniTask](https://github.com/Cysharp/UniTask) (`com.cysharp.unitask`)
- [VContainer](https://github.com/hadashiA/VContainer) — only if you use the bundled VContainer integration (`Runtime/Integrations/VContainer`)

## Quickstart

This walks through wiring up a single screen end-to-end. For a fuller, runnable
sample (popups, payloads, and result flows), import **Example** from
Package Manager → UIKit → Samples, then open its scene and press Play.

1. **Create a Screen Catalog.** Right-click in the Project window →
   `Create > UIKit > Screen Catalog`. Only one should exist per project.

2. **Write a ViewModel.** The screen's id is derived from the type name with a
   trailing `ViewModel` stripped — `MainMenuViewModel` → id `"MainMenu"`.

   ```csharp
   public class MainMenuViewModel : ViewModel
   {
       public string Greeting => "Hello, UIKit!";
   }
   ```

3. **Write a View and matching UXML.** The UXML file must be named the same as
   the screen id (`MainMenu.uxml`) so auto-sync can match it to the catalog, and
   must have exactly one root element — a view exposes a single `Root`, and
   sibling roots would be left behind in the layer when it is disposed.

   ```csharp
   public class MainMenuView : View<MainMenuViewModel>
   {
       protected override void OnAttach()
       {
           Root.Q<Label>("greeting-label").text = ViewModel.Greeting;
       }
   }
   ```

4. **Let auto-sync run.** Saving the script triggers `[DidReloadScripts]`, which
   registers `MainMenu` in the catalog. Open `Project Settings > UI Kit` to confirm
   it's listed with the UXML resolved (an unresolved UXML blocks builds); use
   "Sync with code" there if it doesn't pick it up automatically.

5. **Wire it into DI.** In a `LifetimeScope`:

   ```csharp
   protected override void Configure(IContainerBuilder builder)
   {
       builder.AddUIKit(screenCatalog, panelSettings);
       builder.RegisterScreen<MainMenuViewModel, MainMenuView>();
   }
   ```

6. **Open it.** From an `IStartable`, another ViewModel, or anywhere else with
   `IScreenNavigator` injected:

   ```csharp
   await navigator.OpenAsync<MainMenuViewModel>();
   ```

## Usage

Every ViewModel and View pair below needs one line in your `LifetimeScope`:

```csharp
builder.RegisterScreen<SettingsViewModel, SettingsView>();
```

### A screen that closes itself

A ViewModel takes `IScreenNavigator` from the container and calls `CloseAsync(this)`.
The View reads and writes the ViewModel, never the other way around.

```csharp
public class SettingsViewModel : ViewModel
{
    private readonly IScreenNavigator navigator;

    public float Volume { get; private set; } = 0.5f;

    public SettingsViewModel(IScreenNavigator navigator)
    {
        this.navigator = navigator;
    }

    public void SetVolume(float volume) => Volume = volume;

    public UniTask CloseAsync() => navigator.CloseAsync(this);
}

public class SettingsView : View<SettingsViewModel>
{
    protected override void OnAttach()
    {
        var slider = Root.Q<Slider>("volume-slider");
        slider.value = ViewModel.Volume;
        slider.RegisterValueChangedCallback(evt => ViewModel.SetVolume(evt.newValue));

        Root.Q<Button>("back-button").clicked += () => ViewModel.CloseAsync().Forget();
    }
}
```

`clicked` handlers are `void`, so navigation is started with `.Forget()`. When you
need the result, call an `async UniTaskVoid` method instead — see below.

### A popup that returns a value

Derive from `PopupViewModel<TResult>` and supply the result used when the popup is
dismissed. Auto-sync puts these on the popup layer, so backdrop clicks and
Escape/gamepad-cancel resolve them via `Cancel()` without any extra wiring.

```csharp
[UIScreen("Confirmation")]
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
```

`RunAsync` shows it, waits for a result, then hides and disposes it:

```csharp
var confirmed = await navigator.RunAsync<ConfirmPopupViewModel, bool>();
```

The `[UIScreen]` attribute is optional — it pins the id (and so the UXML file name)
when you don't want the one derived from the type name. Without it this screen's id
would be `ConfirmPopup`.

### Passing data in

Implement `IPayloadReceiver<TPayload>`; the payload arrives before the View attaches,
so `OnAttach` can already read it.

```csharp
[UIScreen("RenameName")]
public class NameInputPopupViewModel : PopupViewModel<string>, IPayloadReceiver<string>
{
    public string InitialName { get; private set; } = string.Empty;

    protected override string CancelResult => InitialName;

    public void SetPayload(string payload) => InitialName = payload;
}
```

Both entry points take a payload overload — `RunAsync<TViewModel, TPayload, TResult>`
for a value, `OpenAsync<TViewModel, TPayload>` for a screen you keep around:

```csharp
var newName = await navigator.RunAsync<NameInputPopupViewModel, string, string>(playerName);
var handle = await navigator.OpenAsync<ProfileViewModel, string>(playerName);
```

### Keeping a handle

`OpenAsync` returns a `ScreenHandle<TViewModel>` exposing `ViewModel`, `IsOpen`, and
`CloseAsync()`. It's the idiomatic way to avoid opening the same screen twice:

```csharp
private ScreenHandle<SettingsViewModel> settingsHandle;

public async UniTask OpenSettingsAsync()
{
    if (settingsHandle is { IsOpen: true }) {
        return;
    }

    settingsHandle = await navigator.OpenAsync<SettingsViewModel>();
}
```

### Awaiting a result from a View

```csharp
public class MainMenuView : View<MainMenuViewModel>
{
    private Label statusLabel;

    protected override void OnAttach()
    {
        statusLabel = Root.Q<Label>("status-label");
        Root.Q<Button>("play-button").clicked += () => PlayAsync().Forget();
    }

    private async UniTaskVoid PlayAsync()
    {
        var confirmed = await ViewModel.StartGameAsync();
        statusLabel.text = confirmed ? "Game started!" : "Start cancelled.";
    }
}
```

### Nested views and view models

A screen doesn't have to be one View. Any element of a view's tree can host a child view
built through `ViewFactory`. The child is an ordinary View/ViewModel pair — registered the
same way, with its own UXML — but the navigator never sees it: the parent owns it.

The child pair looks like any other, it just never gets opened by the navigator:

```csharp
public class PlayerBadgeViewModel : ViewModel
{
    public int Level { get; private set; } = 1;
    public int Score { get; private set; }

    public void AddScore(int amount) => Score += amount;
}

public class PlayerBadgeView : View<PlayerBadgeViewModel>
{
    private Label scoreLabel;

    protected override void OnAttach()
    {
        Root.Q<Label>("level-label").text = $"Level {ViewModel.Level}";

        scoreLabel = Root.Q<Label>("score-label");
        scoreLabel.text = $"Score: {ViewModel.Score}";

        Root.Q<Button>("add-score-button").clicked += () => {
            ViewModel.AddScore(10);
            scoreLabel.text = $"Score: {ViewModel.Score}";
        };
    }
}
```

The parent ViewModel takes the child ViewModel as a constructor dependency and exposes it.
`RegisterScreen` registers ViewModels as transient, so the parent gets its own instance:

```csharp
public class ProfileViewModel : ViewModel, IPayloadReceiver<string>
{
    private readonly IScreenNavigator navigator;

    public string PlayerName { get; private set; } = string.Empty;
    public PlayerBadgeViewModel Badge { get; }

    public ProfileViewModel(IScreenNavigator navigator, PlayerBadgeViewModel badge)
    {
        this.navigator = navigator;
        Badge = badge;
    }

    public void SetPayload(string payload) => PlayerName = payload;

    public UniTask CloseAsync() => navigator.CloseAsync(this);
}
```

The parent View clones the child's UXML into a container element and binds it in one call,
then disposes it on the way out:

```csharp
public class ProfileView : View<ProfileViewModel>
{
    private PlayerBadgeView badgeView;

    protected override void OnAttach()
    {
        Root.Q<Label>("name-label").text = $"Welcome, {ViewModel.PlayerName}!";
        Root.Q<Button>("back-button").clicked += () => ViewModel.CloseAsync().Forget();

        var badgeContainer = Root.Q<VisualElement>("badge-container");
        badgeView = ViewFactory.Create<PlayerBadgeView, PlayerBadgeViewModel>(ViewModel.Badge, badgeContainer);
    }

    public override void Dispose()
    {
        badgeView.Dispose();
        base.Dispose();
    }
}
```

Both pairs are registered side by side, in the same scope as any other screen:

```csharp
builder.RegisterScreen<ProfileViewModel, ProfileView>();
builder.RegisterScreen<PlayerBadgeViewModel, PlayerBadgeView>();
```

Worth knowing:

- The child needs a catalog entry like any screen, so `PlayerBadge.uxml` has to exist and
  have exactly one root element. Its layer and popup options are ignored — the UXML is
  cloned into the container you pass, not into a layer.
- Nesting goes as deep as you like: `ViewFactory.Create` hands the new view its own
  `ViewFactory`, so `PlayerBadgeView` can build grandchildren the same way.
- The navigator only shows, hides and disposes the screen's root view. `ShowAsync` and
  `HideAsync` are never called on children — if a child animates, forward the call from
  the parent's override.
- `badgeView.Dispose()` also disposes `ViewModel.Badge`, because a view owns its ViewModel.
  Don't dispose the child ViewModel separately. If the parent needs to keep that ViewModel
  alive past the child view (e.g. it hands the same instance to several sibling views), pass
  `disposeViewModel: false` to `Create`.

For a list of children, keep them in a collection and dispose the whole collection whenever
you rebuild it:

```csharp
private readonly List<InventorySlotView> slotViews = new();

private void RebuildSlots()
{
    foreach (var slotView in slotViews) {
        slotView.Dispose();
    }

    slotViews.Clear();

    foreach (var slot in ViewModel.Slots) {
        slotViews.Add(ViewFactory.Create<InventorySlotView, InventorySlotViewModel>(slot, slotsContainer));
    }
}
```

Each child ViewModel here comes from the parent ViewModel (`ViewModel.Slots`), not from the
container: this overload only resolves the *view*, so a parent is free to create its child
ViewModels itself when how many of them there are depends on data. The pair still needs its
`RegisterScreen<InventorySlotViewModel, InventorySlotView>()` line and its `InventorySlot.uxml`.

#### Children whose type isn't known at the call site

When the children share an abstract ViewModel base but each subclass has its own View, the
generic overload can't help — the call site only has the base type. Register each pair
normally, then build them through the non-generic overload, which picks the View by the
ViewModel's concrete runtime type:

```csharp
public abstract class FeedItemViewModel : ViewModel { }
public class TextPostViewModel  : FeedItemViewModel { /* ... */ }
public class ImagePostViewModel : FeedItemViewModel { /* ... */ }

builder.RegisterScreen<TextPostViewModel,  TextPostView>();
builder.RegisterScreen<ImagePostViewModel, ImagePostView>();
```

```csharp
private readonly List<IView> itemViews = new();

private void RebuildFeed()
{
    foreach (var itemView in itemViews) {
        itemView.Dispose();
    }

    itemViews.Clear();

    foreach (FeedItemViewModel item in ViewModel.Items) {
        // item.GetType() is TextPostViewModel / ImagePostViewModel -> the right View.
        itemViews.Add(ViewFactory.Create(item, feedContainer));
    }
}
```

```csharp
IView Create(ViewModel viewModel, VisualElement container, bool disposeViewModel = true);
```

- The lookup key is the **concrete** type: only a subclass registered with its own
  `RegisterScreen<TSubclass, TSubclassView>()` resolves. Passing a ViewModel whose exact
  type wasn't registered throws, naming it — there is no compile-time check, unlike the
  generic overloads, which stay preferred whenever the type is known.
- `disposeViewModel: false` stops the returned view from disposing the ViewModel when the
  view is disposed — for when the parent (or DI) owns that lifetime. Default `true` matches
  the generic overloads.
- The return type is `IView`: `Root`, `Attach`, `ShowAsync`/`HideAsync` and `Dispose` only.
  Reach the ViewModel through your own model, not the view.

### Screens owned by a feature scope

`AddUIKit` goes in one scope — the one that should own the UI root and the navigation
stack, usually the root. `RegisterScreen` does *not* have to go in that same scope. A
screen registered in a child `LifetimeScope` resolves its ViewModel from there, so the
ViewModel can take that scope's services:

```csharp
// Root scope: installs UIKit and registers the screens that outlive any one feature.
builder.AddUIKit(screenCatalog, panelSettings);
builder.RegisterScreen<MainMenuViewModel, MainMenuView>();

// Battle scope: BattleHudViewModel can inject IBattleState, which the root cannot see.
builder.Register<IBattleState, BattleState>(Lifetime.Scoped);
builder.RegisterScreen<BattleHudViewModel, BattleHudView>();
```

There is still exactly one `UIKitRoot`, one panel and one navigation stack, so a popup
opened from a feature scope stacks over a root screen the way you'd expect.

When the battle scope is disposed, its screens are unregistered and any of them still
open are closed immediately — without hide animations, since the services they depend on
are going away at the same moment. Opening one afterwards throws, naming the screen.

Two live scopes can't register the same ViewModel type: a screen belongs to exactly one
scope, and a duplicate registration throws at scope-build time rather than letting build
order silently pick a winner.

## What's included

- **ViewModel** (`UIKit.Runtime.Abstractions.ViewModel`) — base class, `IDisposable`.
  - `ResultViewModel<TResult>` — adds `WaitForResultAsync`/`SetResult` via a
    `UniTaskCompletionSource`, for flows that resolve to a value (e.g. confirmation
    dialogs).
  - `PopupViewModel<TResult>` — `ResultViewModel` + `ICancelable` + `IPopup`;
    `Cancel()` resolves the result with a cancel result.
- **View** (`UIKit.Runtime.Core.View<TViewModel>`) — wraps a `VisualElement` root,
  owns the `ShowAsync`/`HideAsync`/`Dispose` lifecycle, and disposes its ViewModel
  when disposed.
- **ScreenNavigator** (`UIKit.Runtime.Core.ScreenNavigator`, implements
  `IScreenNavigator`) — the main entry point for navigation:
  - `OpenAsync<TViewModel>` / `OpenAsync<TViewModel, TPayload>` → returns a
    `ScreenHandle<TViewModel>` you can later `CloseAsync()`.
  - `RunAsync<TViewModel, TResult>` / `RunAsync<TViewModel, TPayload, TResult>` →
    shows the view, awaits `ResultViewModel.WaitForResultAsync`, hides, and returns
    the result (used for modal/popup flows).
  - Transitions are serialised: a call made while another transition is still
    running is queued and applied in call order, so overlapping navigation can't
    interleave. `RunAsync`'s wait for a result is *not* part of that — an open
    modal never blocks other navigation.
- **ScreenCatalog** — a `ScriptableObject` (create via
  `Assets/Create/UIKit/Screen Catalog`) mapping screen id → `VisualTreeAsset`
  (+ layer and popup options). Kept in sync with your ViewModel types by an
  editor-only auto-sync step; a "Sync with code" button is also available under
  `Project Settings > UI Kit`.
- **ScreenRegistry** (`UIKit.Runtime.Core.ScreenRegistry`) — maps a ViewModel type
  to the `ScreenBinding` that builds it and to the scope that owns it. Scopes add
  their screens when they build and remove them when they are disposed, which is
  what lets one navigator open screens belonging to different containers.
- **VContainer integration** (`UIKit.Runtime.Integrations.VContainer`, separate
  assembly) — `BuilderExtension.AddUIKit`/`RegisterScreen` wire the catalog, root
  provider, registry, and navigator into a VContainer `IContainerBuilder`, and
  `ScreenScope` ties each scope's screens to that scope's lifetime. Kept in its own
  assembly so the core package has no *build-time* opt-out, but the integration
  itself can be omitted from a project that doesn't use VContainer.

## Things to know

- **Don't `await` a navigator call from `OnAttach`, `ShowAsync` or `HideAsync`.**
  Those run as part of a transition, so awaiting another one waits on an operation
  that can't start until the current one finishes. Use `.Forget()` to queue it.
- **A view owns its ViewModel.** `View.Dispose` disposes the bound ViewModel, and
  the navigator disposes the view when the screen closes. Child views you create
  yourself via `ViewFactory` are *not* tracked — dispose them from the parent's
  `Dispose` override (see `ProfileView` in the sample).
- **Closing is not instant.** `IsOpen` reports `false` as soon as a close starts,
  not when its hide animation ends, so re-entrancy guards like
  `if (handle is { IsOpen: true }) return;` behave sensibly during transitions.
- **A screen dies with the scope that registered it.** Screens registered in a child
  `LifetimeScope` are force-closed when it is disposed, so don't hold a
  `ScreenHandle` to one past its scope's lifetime.
- **A screen with no UXML fails the build.** `Project Settings > UI Kit` lists any
  unresolved entries, and a build preprocessor fails the build naming them rather
  than letting it surface as an exception when the screen is first opened.

## Package layout

```
Assets/UIKit/
  package.json
  Runtime/
    UIKit.asmdef
    Abstractions/
    Core/
    Utils/
    Integrations/
      VContainer/
        UIKit.VContainer.asmdef
  Editor/
    UIKit.Editor.asmdef
```

## License

MIT — see [LICENSE](LICENSE).
