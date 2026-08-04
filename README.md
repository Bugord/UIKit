# UIKit

An MVVM framework for Unity's UI Toolkit: views, view models, and screen/popup
navigation, with an optional VContainer integration.

This repository is the Unity project used to develop and test the package. The
package itself lives in [`Assets/UIKit`](Assets/UIKit) and can be added to any
Unity project independently of this one.

## Installing

Add it via the Unity Package Manager, using the Git URL with a `path` query
pointing at the package folder:

```
https://github.com/Bugord/UIKit.git?path=Assets/UIKit
```

Package Manager → `+` → *Add package from git URL...* → paste the URL above.

Requires:
- Unity 6000.0+
- [UniTask](https://github.com/Cysharp/UniTask) (`com.cysharp.unitask`)
- [VContainer](https://github.com/hadashiA/VContainer) — only if you use the bundled VContainer integration

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

## License

MIT — see [LICENSE](LICENSE).
