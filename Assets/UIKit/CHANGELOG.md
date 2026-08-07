# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
(pre-1.0, so minor bumps may include breaking changes).

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
