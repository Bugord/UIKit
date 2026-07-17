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
- **ScreenCatalog** — a `ScriptableObject` (create via
  `Assets/Create/UIKit/Screen Catalog`) mapping screen id → `VisualTreeAsset`
  (+ layer and popup options). Kept in sync with your ViewModel types by an
  editor-only auto-sync step; a "Sync with code" button is also available under
  `Project Settings > UI Kit`.
- **VContainer integration** (`UIKit.Runtime.Integrations.VContainer`, separate
  assembly) — `BuilderExtension.AddUIKit`/`RegisterScreen` wire the catalog, root
  provider, resolver, and navigator into a VContainer `IContainerBuilder`. Kept in
  its own assembly so the core package has no *build-time* opt-out, but the
  integration itself can be omitted from a project that doesn't use VContainer.

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
