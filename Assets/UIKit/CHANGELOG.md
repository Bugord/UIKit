# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
(pre-1.0, so minor bumps may include breaking changes).

## [0.6.0] - 2026-10-27

This release has many breaking changes. See **Migrating from 0.5.x** at the end of this entry.

### Added
- **No-DI entry point:** `UIKitContext` owns the navigator, the view factory and the screen
  registrations: `new UIKitContext(panelSettings, catalogs...)`, then
  `ui.Register<TViewModel, TView>(() => new TViewModel(...))`. `CreateGroup()` returns a
  `ScreenGroup` whose screens are closed and unregistered together when it is disposed.
- **`RegisterView<TViewModel, TView>()`** on `UIKitContext` / `ScreenGroup`, plus
  `builder.RegisterView` (VContainer) and `Container.RegisterView` (Zenject), for views that
  are only created as children of another view; no ViewModel factory is needed. Opening such
  a type as a screen throws `InvalidOperationException`.
- **Zenject / Extenject integration** (`UIKit.Zenject`): `Container.BindUIKit(panelSettings,
  catalogs...)` and `Container.RegisterScreen<TViewModel, TView>()`. Screens registered in a
  subcontainer close when it is disposed. Compiles only when `com.svermeulen.extenject` is
  installed.
- **Unity runtime data binding.** `ViewModel` implements `INotifyBindablePropertyChanged`
  (with `SetProperty` and `Notify` helpers) and a view sets its root's `dataSource` to its
  ViewModel, so `[CreateProperty]` properties work with UXML bindings. `View` also gained
  `OnPropertyChanged` and `BindProperty` for bindings made in code, and `Observe` for
  one-off reactions to a single property's change.
- **Typed bindings in code:** `BindText` (a `TextElement`'s text, one way) and `BindValue` (a
  field's value, two-way by default), each by element name or element, and `Require<T>(name)`,
  which throws `InvalidOperationException` naming the view, the type and the name instead of
  returning `null`.
- **Binding validation in the editor and development builds:** `BindProperty`, `BindText`,
  `BindValue`, `Observe` and `Observe<T>` resolve the ViewModel property through
  Unity.Properties (against the ViewModel's runtime type) when called, and throw
  `ArgumentException` for an unknown property, one without `[CreateProperty]`, or a read-only
  property bound `TwoWay` / `ToSource`. Release builds skip the check.
- **`Observe<T>(propertyName, Action<T>)`** passes the new value; a property of another type
  throws when registered (editor and development builds).
- **Commands:** `ICommand`, `Command` (sync, optional predicate, `RaiseCanExecuteChanged`) and
  `AsyncCommand` (runs an `Awaitable`, cannot run twice at once, `IsRunning`, `ExecuteAsync`).
  `AsyncCommand`s sharing a `CommandGate` are mutually exclusive. `View.BindCommand(button or
  name, command)` runs it on click and enables the button only while it can execute; the view
  unsubscribes when disposed.
- **`[DependsOn(...)]`** on a computed property notifies it whenever one of its sources is
  notified, transitively, each property once per call. An unknown name or a cycle throws when
  the ViewModel is constructed. `Notify(first, second, params rest)` raises several properties
  (and their dependents) at once.
- **`[UIScreen]` takes the layer and the dismiss triggers:**
  `[UIScreen("Id", Dismiss = DismissTriggers.None)]`, `[UIScreen(UILayer.Popup)]`.
  `DismissTriggers` (`None`, `BackdropClick`, `NavigationCancel`, `All`) replaces
  `PopupOptions`. `ScreenInfo` resolves id, layer and triggers from the attribute and the
  conventions.
- **Several catalogs.** Each `ScreenCatalog` lists the assemblies it claims; the one that
  lists none takes every assembly nobody claims. `AddUIKit`, `BindUIKit` and `UIKitContext`
  take any number of catalogs. A screen listed in two catalogs throws.
- **Hosting inside your own UI:** `VisualElementRootProvider` puts the screen and popup
  layers into any `VisualElement`, e.g. the root of your own `UIDocument`. `AddUIKit`,
  `BindUIKit` and `UIKitContext` have overloads taking an `IUIRootProvider`.
- **Navigation:** `ReplaceAsync`, `GoBackAsync`, `PopToRootAsync`, `CloseAllAsync` and `Top`.
- **ViewModel lifecycle:** `OnOpened`, `OnClosed`, `OnDispose`, `IsDisposed` and
  `LifetimeToken` (cancelled when the ViewModel is disposed).
- **`View.CreateRoot()`** for views built in code instead of from UXML, and `View.IsDisposed`.
- **`Awaitables`:** `Completed()`, `FromResult()` and `.Forget()` for `Awaitable` and
  `Awaitable<T>`.
- **Navigation stall watchdog.** If an operation holds the navigation queue for longer than
  `UIKitContext.StallWarningSeconds` (default 5) while another waits, UIKit logs an error
  naming both. The usual cause is a view awaiting a navigator call from `OnAttach`,
  `ShowAsync` or `HideAsync`. Set it to 0 to disable.
- **Sample:** a scene that needs no DI container, plus VContainer and Zenject variants that
  compile only when those packages are installed.
- **Tests:** the package ships EditMode and PlayMode tests (`Tests/`), including VContainer and
  Zenject adapter tests that compile only when those packages are installed. To run them in a
  consumer project, add `"testables": ["com.bugord.uikit"]` to the manifest.

### Changed
- **Breaking: namespaces.** `UIKit.Runtime.Abstractions`, `UIKit.Runtime.Core` and
  `UIKit.Runtime.Utils` are now `UIKit`; `UIKit.Runtime.Integrations.VContainer` is now
  `UIKit.VContainer`.
- **Breaking: no UniTask.** Everything async returns `UnityEngine.Awaitable` /
  `Awaitable<T>`. An `Awaitable` can be awaited only once. From UniTask code, convert with
  `.AsUniTask()`.
- **Breaking: VContainer is optional.** The adapter assembly compiles only when
  `jp.hadashikick.vcontainer` is installed as a UPM package (it defines `UIKIT_VCONTAINER`).
  With VContainer as plain files in `Assets`, add `UIKIT_VCONTAINER` to Scripting Define
  Symbols yourself. The same goes for `UIKIT_ZENJECT`.
- **Breaking: `AddUIKit(panelSettings, catalogs...)`.** The argument order is reversed and a
  `PanelSettings` is required (or pass an `IUIRootProvider`). Before, a missing one was
  replaced by an empty `PanelSettings` that rendered every control unstyled.
- **Breaking: `View.Dispose()` and `ViewModel.Dispose()` are no longer virtual.** Override
  `OnDispose()`. Both are safe to call more than once, and a view disposes its child views
  before `OnDispose` runs.
- **Breaking: catalog entries hold only `screenId` and `uxml`.** The layer and the popup
  options moved to the ViewModel: `IPopup` or `[UIScreen(UILayer.Popup)]` for the layer,
  `[UIScreen(Dismiss = ...)]` for the triggers.
- **Breaking: dismiss defaults.** Screens now close on `NavigationCancel` (Escape / gamepad
  cancel) too, except the bottom screen of the stack. Popups default to
  `DismissTriggers.All`. Opt out with `[UIScreen(Dismiss = DismissTriggers.None)]`.
- **Breaking: `CloseAsync(ViewModel, token)` and `IsOpen(ViewModel)` are no longer generic.**
- **Breaking: `IViewFactory.Create<TView, TViewModel>()` without arguments is removed.**
  Create views with a ViewModel and a container.
- **Breaking: `IResultViewModel<T>.WaitForResultAsync` returns `Awaitable<T>`,** and the
  `RunAsync` constraint is `IResultViewModel<TResult>` instead of `ResultViewModel<TResult>`.
- **Breaking: `ScreenNavigator`, `ScreenRegistry`, `ScreenBinding`, `ViewFactory` and
  `IViewBinder` are no longer public.** Use `IScreenNavigator` / `IViewFactory`, or
  `UIKitContext`.
- Only the top screen or popup takes input and focus, but covered views are no longer
  disabled, so they keep their normal look.
- A `ICancelable` ViewModel that is closed from outside (backdrop, Escape, `GoBackAsync`,
  `CloseAsync`) is cancelled first, so a pending `RunAsync` returns the cancel result
  instead of throwing.
- Dev project: the Unity MCP package is replaced by `com.unity.pipeline`.
- `package.json` says Unity `6000.0`. The tests pass on 6000.3.20f1 and 6000.6.0f1 (the
  6000.3 run without any DI package or UniTask); 6000.0 to 6000.2 are untested.

### Removed
- The `com.cysharp.unitask` dependency.
- `PopupOptions`, `ScreenCatalog.PopupOptionsData`, and the `layer` / `popupOptions` fields
  of catalog entries.
- `UIId`, `UIIdConventions`, `UIRootProviderInitializer`, `IScreen`, and the `Utils` helpers
  `DictionaryExtensions`, `IsExternalInit` and `VisualElementExtensions`.
- `ScreenCatalog.Get(string)`; catalogs are merged internally.
- `View.Bind` and `View.Attach` as public methods.

### Fixed
- Cancelling the token of one `WaitForResultAsync` cancelled the shared result source, and
  with it every other wait on that ViewModel. Each wait now has its own awaitable and its
  token cancels only that wait.
- A ViewModel leaked when opening failed before its view took ownership of it (a payload
  that threw, a missing UXML, a failing view factory). It is now disposed.
- Closing a screen from outside no longer makes a pending `RunAsync` throw (see above).
- Disposing a scope while one of its screens was still opening left that screen attached;
  the open is now aborted with an `OperationCanceledException`.
- Disposing a scope no longer shows the uncovered screen in the middle of a running
  navigation; the show is queued behind it.
- Disposing a `ScreenGroup`, `UIKitContext` or DI scope while a screen was playing its show or
  hide animation left the screen alive until the animation ended. It is now disposed
  immediately and the animation's `CancellationToken` is cancelled (views with a custom
  `ShowAsync` / `HideAsync` should honour it). An open in flight faults with
  `OperationCanceledException`.
- The top screen was not focused when its layer reached a panel late, or again after the host
  rebuilt its root, so Escape / gamepad cancel did nothing until the user clicked. It is now
  focused as soon as the layer is attached to a panel.
- The navigation stall watchdog kept polling after the context was disposed. It now stops.

### Migrating from 0.5.x
Work through this list in order; most steps are mechanical.

1. **Packages.** UniTask is no longer required. You may keep it for your own code. VContainer
   as a UPM package keeps working with no change. If VContainer is plain files under
   `Assets`, add `UIKIT_VCONTAINER` to Player Settings > Scripting Define Symbols.
2. **Namespaces.** Replace every `using UIKit.Runtime.Abstractions;`,
   `using UIKit.Runtime.Core;` and `using UIKit.Runtime.Utils;` with `using UIKit;`, and
   `using UIKit.Runtime.Integrations.VContainer;` with `using UIKit.VContainer;`.
3. **Return types.** `UniTask` becomes `Awaitable`, `UniTask<T>` becomes `Awaitable<T>`,
   `UniTaskCompletionSource` becomes `AwaitableCompletionSource`. An `Awaitable` is awaited
   once: keep the result, do not await the same instance twice. If you await a UIKit call in
   UniTask code, add `.AsUniTask()`:
   ```csharp
   var ok = await navigator.RunAsync<ConfirmPopupViewModel, bool>().AsUniTask();
   ```
   `.Forget()` for `Awaitable` is in `UIKit.Awaitables` (`using UIKit;`).
4. **Registration.** The arguments of `AddUIKit` swapped and `PanelSettings` is required:
   ```csharp
   // before
   builder.AddUIKit(screenCatalog, panelSettings);
   // after
   builder.AddUIKit(panelSettings, screenCatalog);
   ```
   Without DI, build a `UIKitContext` (see the README).
5. **Dispose overrides.** `public override void Dispose()` in a View or ViewModel becomes
   `protected override void OnDispose()`, and the `base.Dispose()` call goes away:
   ```csharp
   // before
   public override void Dispose() { badgeView.Dispose(); base.Dispose(); }
   // after: not needed for views made with ViewFactory, it disposes them.
   protected override void OnDispose() { /* your own cleanup */ }
   ```
6. **Popups and layers.** Catalog entries lost `layer` and `popupOptions`. A ViewModel
   that was on the Popup layer must now say so: derive from `PopupViewModel<T>`, implement
   `IPopup`, or add `[UIScreen(UILayer.Popup)]`. Press "Sync with code" in Project Settings >
   UI Kit afterwards. The old `PopupOptions.Blocking` (no backdrop / Escape dismissal) is
   `[UIScreen(Dismiss = DismissTriggers.None)]`; "backdrop only" is
   `Dismiss = DismissTriggers.BackdropClick`.
7. **Escape on screens.** Screens now close on Escape / gamepad cancel unless they are the
   bottom screen. If a screen must not, add `[UIScreen(Dismiss = DismissTriggers.None)]`.
8. **`CloseAsync` takes the ViewModel only:** `navigator.CloseAsync(this)` is unchanged, but
   `CloseAsync<MyViewModel>(vm)` becomes `CloseAsync(vm)`. `IsOpen` likewise.
9. **`IViewFactory.Create<TView, TViewModel>()`** with no arguments is gone. Use
   `Create<TView, TViewModel>(viewModel, container)`.
10. **Result ViewModels.** `ResultViewModel<T>.ResultSource` and the virtual
    `WaitForResultAsync` are gone. Call `SetResult(value)`; implement
    `IResultViewModel<T>` yourself only if you do not derive from `ResultViewModel<T>`.
11. **Internal types.** Code that injected or constructed `ScreenNavigator`,
    `ScreenRegistry`, `ScreenBinding` or `ViewFactory` should use `IScreenNavigator`,
    `IViewFactory` or `UIKitContext`.
12. **Views.** `View.Bind` / `View.Attach` are no longer public; `Root` and `ViewModel` are
    set before `OnAttach`.
13. **Catalogs.** A project can have more than one. A catalog that lists no assemblies is the
    default. Existing catalogs keep working after a sync, which also drops the old
    per-entry fields.

## [0.5.2] - 2026-10-20

### Fixed
- Before Unity 6000.5, screens and popups disappeared when the `UIDocument` of the UI root
  built a new root element, which it does whenever it is enabled again. The navigation stack
  stayed intact, so nothing showed until the next restart. The layer roots are now added to
  the new root element within a frame.

## [0.5.1] - 2026-10-08

### Fixed
- Screen catalog sync no longer registers ViewModels from Editor and test assemblies.
  They never reach the player, yet their UXML-less entries made
  `ScreenCatalogBuildValidator` fail the build. Entries already in the catalog for such
  types are removed on the next sync. Precompiled DLLs are included when their import
  settings ship them to the active build target.

## [0.5.0] - 2026-09-02

### Added
- `IViewFactory.Create(ViewModel viewModel, VisualElement container, bool disposeViewModel = true)`
  — a non-generic overload that resolves the View by the ViewModel's concrete runtime
  type, for nested children whose subclasses each register their own View. Returns `IView`.
- `disposeViewModel` (default `true`) on both `Create` overloads that take a ViewModel:
  pass `false` to stop the returned view from disposing that ViewModel with it, for when a
  parent owns its lifetime.

### Changed
- `IViewFactory.Create<TView, TViewModel>(TViewModel, VisualElement)` gained an optional
  `bool disposeViewModel = true` parameter. Source-compatible; existing calls are unaffected.

## [0.4.0] - 2026-08-07

### Added
- Screens can be registered in any `LifetimeScope`, not only the one that calls
  `AddUIKit`. A screen registered in a child scope resolves its ViewModel from
  that scope, so the ViewModel can depend on that scope's services, while the UI
  root and the navigation stack stay single and shared.
- `ScreenRegistry`/`ScreenBinding` — the ViewModel type → factory + owning scope
  lookup the navigator now resolves through.
- `ScreenScope` (VContainer integration) — one per scope that registers screens.
  Disposing the scope force-closes its open screens (no hide animations, since
  their services are going away) and removes its registry entries.

### Changed
- **Breaking:** `IScreenResolver` and `VContainerScreenResolver` are gone.
  `ScreenNavigator` and `ViewFactory` take `ScreenRegistry` instead. Code that only
  calls `AddUIKit`/`RegisterScreen` and injects `IScreenNavigator` is unaffected.
- **Breaking:** `RegisterScreen` no longer registers the View as
  `View<TViewModel>`; the registry is the lookup path. Inject the concrete View
  type if you were resolving it directly.
- `ActiveView` carries the scope its screen was resolved from.
- Registering the same ViewModel type in two live scopes now throws at scope-build
  time instead of silently letting build order decide which one wins.

## [0.3.0] - 2026-07-18

### Added
- Screen/popup navigation stack in `ScreenNavigator`, tracking the top-most
  interactive view and driving keyboard/gamepad focus accordingly.
- Per-screen popup options (`cancelOnBackdropClick`, `cancelOnNavigationCancel`)
  configurable from the `ScreenCatalog`.
- `Samples~/Example` wired up as a distributable UPM sample via `package.json`.

### Changed
- VContainer integration split into its own assembly (`UIKit.VContainer`); the
  core `UIKit` assembly no longer references VContainer.
- Removed an extra wrapper `VisualElement` created on view instantiation.

### Fixed
- `ScreenCatalog.Get` now throws a descriptive `InvalidOperationException` for
  an unregistered screen id instead of returning `null`, which previously
  surfaced as an unhelpful `NullReferenceException` at the call site.
- `ScreenNavigator.RunAsync` no longer leaves an orphaned, visible view behind
  when its `CancellationToken` is cancelled while awaiting a result.
- `ScreenNavigator.PushAsync` now rolls back (untracks and disposes the view,
  restores the previous screen) if showing/hiding a view fails or is
  cancelled mid-push.

## Earlier versions

Versions prior to 0.3.0 predate this changelog — see git history.
