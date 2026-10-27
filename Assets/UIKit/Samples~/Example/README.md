# UIKit Example

A main menu with a settings screen, a profile screen with a child view (the player badge), a
yes/no confirmation popup and a name-input popup that takes a payload and returns a result.

The screens live in the `UIKit.Example` assembly, which references only UIKit. The same screens
are started three ways, one scene each:

| Scene | Entry point | Needs |
| --- | --- | --- |
| `Example.unity` | `ExampleBootstrap` creates a `UIKitContext` and registers each screen with a plain factory. | Nothing |
| `VContainer/ExampleVContainer.unity` | `ExampleLifetimeScope` calls `AddUIKit` / `RegisterScreen`; `ExampleEntryPoint` opens the menu. | VContainer |
| `Zenject/ExampleZenject.unity` | `ExampleInstaller` (listed on the `SceneContext`) calls `BindUIKit` / `RegisterScreen`; `ExampleEntryPoint` opens the menu. | Extenject / Zenject |

Open a scene and press Play.

## Optional scenes

`UIKit.Example.VContainer` and `UIKit.Example.Zenject` compile only when their container package
is installed: each assembly defines `UIKIT_VCONTAINER` / `UIKIT_ZENJECT` through a version define
on `jp.hadashikick.vcontainer` / `com.svermeulen.extenject`, and requires it as a define
constraint. Without the package the folder is skipped, and its scene shows missing scripts until
you install it.

## What to look at

- **Data binding** - `Profile/PlayerBadge.uxml` binds its labels to `[CreateProperty]` members of
  `PlayerBadgeViewModel`; the view has no label code at all. `ProfileView` and `SettingsView` do
  the same in code with `BindText` and `BindValue` (two-way for the slider and toggle). In the
  editor a misspelt property name throws when the view attaches, instead of binding nothing.
- **Computed properties** - `SettingsViewModel.VolumeText` is marked
  `[DependsOn(nameof(Volume), nameof(Muted))]`, so it refreshes whenever either changes.
  `PlayerBadgeViewModel` computes its labels from `PlayerProfile`, which it does not own, so it
  notifies all three in one `Notify(...)` call when the profile changes.
- **Commands** - buttons are bound with `BindCommand`. The four main menu buttons run
  `AsyncCommand`s that share one `CommandGate`: while one navigates, all four are disabled, so
  two clicks in the same frame cannot open two screens. Back and +10 use commands too. The
  popups keep plain `clicked` handlers: they resolve once, so there is no state to follow.
- **Reacting to a change** - `MainMenuView` binds its labels in code too, and uses `Observe` to
  flash the status label each time `Status` changes: an effect, which a binding cannot express.
- **Results and payloads** - `MainMenuViewModel` awaits `RunAsync` for both popups; the rename
  popup receives the current name through `IPayloadReceiver<string>`.
- **Child views** - `ProfileView` creates the badge through its `ViewFactory`, so it is disposed
  with the profile screen. The badge is registered with `RegisterView` (no ViewModel factory),
  because `ProfileViewModel` creates and owns the badge ViewModel.
- **Shared state** - `PlayerProfile` and `GameSettings` outlive the screens; ViewModels that
  subscribe to them unsubscribe in `OnDispose`.
- **Theme** - `ExamplePanelSettings` uses `ExampleTheme.tss`, which imports the default runtime
  theme and `Example.uss`.
