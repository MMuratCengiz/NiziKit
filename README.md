# NiziKit

NiziKit is a thin C#/.NET 9 layer over the [DenOfIz](https://github.com/MMuratCengiz/DenOfIz) graphics library.

Since a lot of the project is work in progress, documentation lives within the code base itself.

## Usage

The NuGet package makes the `DenOfIz` and `NiziKit.*` namespaces ambient in consuming projects, so no `using` lines are needed. Opt out of any of them with `<Using Remove="DenOfIz" />` in your csproj.

Input: `NiziKit.Inputs.Input` only tracks per-frame edges (`WasKeyPressed`, `WasMouseButtonReleased`, `MouseDelta`, `MouseScroll`). Held state, mouse position, cursor and controllers come straight from `DenOfIz.InputSystem` (the instance is on `Game.InputSystem`).

## Building

```bash
dotnet build NiziKit.slnx
```

## License

GPL-3.0 — see [LICENSE](LICENSE).
