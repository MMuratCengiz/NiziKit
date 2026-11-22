# NiziKit

NiziKit is a thin C#/.NET 9 layer over the [DenOfIz](https://github.com/MMuratCengiz/DenOfIz) graphics library.

Since a lot of the project is work in progress, documentation lives within the code base itself.

## Usage

The NuGet package makes the `DenOfIz` and `NiziKit.*` namespaces ambient in consuming projects, so no `using` lines are needed. Opt out of any of them with `<Using Remove="DenOfIz" />` in your csproj.

Input: `NiziKit.Inputs.Input` only tracks per-frame edges (`WasKeyPressed`, `WasMouseButtonReleased`, `MouseDelta`, `MouseScroll`). Held state, mouse position, cursor and controllers come straight from `DenOfIz.InputSystem` (the instance is on `Game.InputSystem`).

## Demos

- `Pong` — a small 2D game: one pipeline, a camera, a handful of quads and a score table.
- `Swarm` — a performance demo: thousands of individually bound objects, up to four million instanced shards and HDR bloom, with a HUD that shows what each frame cost. `dotnet run --project Swarm -c Release`; append `-- --benchmark` for a printed report or `-- --help` for the options.

## Building

```bash
dotnet build NiziKit.slnx
```

## License

GPL-3.0 — see [LICENSE](LICENSE).
