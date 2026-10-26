# UIKit

An MVVM framework for Unity's UI Toolkit: views, view models, and screen/popup
navigation, with optional VContainer and Zenject integrations. Works without a DI
container too.

This repository is the Unity project used to develop and test the package. The
package itself lives in [`Assets/UIKit`](Assets/UIKit) and can be added to any
Unity project independently of this one.

## Installing

Add it via the Unity Package Manager, using the Git URL with a `path` query
pointing at the package folder:

```
https://github.com/Bugord/UIKit.git?path=Assets/UIKit#v0.6.0
```

Package Manager → `+` → *Add package from git URL...* → paste the URL above.

Requires:
- Unity 6 (`package.json` declares 6000.0; tests pass on 6000.3.20f1 and 6000.6.0f1,
  6000.0 to 6000.2 are untested)
- Optional: [VContainer](https://github.com/hadashiA/VContainer) or
  [Extenject](https://github.com/Mathijs-Bakker/Extenject), for the bundled integrations
- No UniTask dependency: async APIs return `UnityEngine.Awaitable`

## Documentation

See the [package README](Assets/UIKit/README.md) for the quickstart, the API
overview, and behaviour notes. Release history is in
[CHANGELOG.md](Assets/UIKit/CHANGELOG.md).

## Working in this repository

- `Assets/UIKit/` — the package as published.
- `Assets/Example/` — the sample as a live, editable part of this project.
- `Assets/UIKit/Samples~/Example/` — the same sample, packaged for distribution
  via Package Manager → UIKit → Samples. Unity ignores `Samples~` folders, so
  changes made in `Assets/Example` must be mirrored here to ship.

## Contributing

Tests live in `Assets/UIKit/Tests` (EditMode and PlayMode; the VContainer and Zenject adapter
tests compile only when those packages are installed). The dev-only tooling in `Assets/Dev` is
not shipped with the package: the menu *Tools → UIKit → Mirror Example to Samples~* copies
`Assets/Example` over `Assets/UIKit/Samples~/Example`, and a test fails when the two drift
apart. Run the mirror after changing the example.

## License

MIT — see [LICENSE](LICENSE).
