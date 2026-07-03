# UI Toolkit MVVM

An MVVM framework for Unity's UI Toolkit: views, view models, widgets, and screen/popup navigation, with an optional VContainer integration.

This repository is the Unity project used to develop and test the package. The package itself lives in [`Assets/UIToolkitMVVM`](Assets/UIToolkitMVVM) and can be added to any Unity project independently of this project.

## Installing

Add it to your project via the Unity Package Manager, using the Git URL with a `path` query pointing at the package folder:

```
https://github.com/Bugord/UI-Tookit-MVVM.git?path=Assets/UIToolkitMVVM
```

Package Manager → `+` → *Add package from git URL...* → paste the URL above.

Requires:
- Unity 6000.0+
- [UniTask](https://github.com/Cysharp/UniTask) (`com.cysharp.unitask`)
- [VContainer](https://github.com/hadashiA/VContainer) — only if you use the bundled VContainer integration (`Runtime/Integrations/VContainer`)

## What's included

- **Views** (`UI.Navigation`) — `IView`, `View<TModel>`, and result-returning variants `IResultView<TResult>` / `ResultView<TViewModel, TResult>` for views that resolve with a value (e.g. a confirmation dialog).
- **ViewModels** (`UI.Navigation`) — `ViewModel` base class, and `ResultViewModel<TResult>` for awaiting a result via UniTask.
- **Navigation** (`UI.Navigation`) — `IUIService` for showing/hiding screens and popups (`ShowScreenAsync`, `ShowPopupAsync`, `ShowScreenAndWaitResultAsync`, ...), backed by `UIService`, `IUIScreenRegistry`/`UITemplateRegistry` for resolving `VisualTreeAsset`s by id, and `RootPanelProvider` for the root `UIDocument`.
- **Widgets** (`UI.Widgets`) — `IWidget`/`Widget<TModel>` for reusable sub-views, and `WidgetsHost` to attach them to a `VisualElement` via `IWidgetsFactory` and `IWidgetTemplateRegistry`.
- **VContainer integration** (`Runtime/Integrations/VContainer`) — `VContainerUIScreenFactory` and `VContainerWidgetsFactory`, resolving screens/widgets through VContainer's `IObjectResolver`. Kept in a separate assembly so the core package has no hard dependency on VContainer.

## Package layout

```
Assets/UIToolkitMVVM/
  package.json
  Runtime/
    UIToolkitMVVM.asmdef
    Navigation/
    ViewModels/
    Views/
    Widgets/
    Utils/
    Integrations/
      VContainer/
        UIToolkitMVVM.VContainer.asmdef
```

## License

MIT — see [LICENSE](LICENSE).
