# NiziKit

NiziKit is a thin C#/.NET 9 layer over the [DenOfIz](https://github.com/MMuratCengiz/DenOfIz) graphics library.

Since a lot of the project is work in progress, documentation lives within the code base itself.

## Usage

The NuGet package makes the `DenOfIz` and `NiziKit.*` namespaces ambient in consuming projects, so no `using` lines are needed. Opt out of any of them with `<Using Remove="DenOfIz" />` in your csproj.

Input: `NiziKit.Inputs.Input` only tracks per-frame edges (`WasKeyPressed`, `WasMouseButtonReleased`, `MouseDelta`, `MouseScroll`). Held state, mouse position, cursor and controllers come straight from `DenOfIz.InputSystem` (the instance is on `Game.InputSystem`).

## UI debugging and headless runs

`UiDebug` inspects and drives the UI: `Snapshot()` returns the laid out widget tree with rects in points, values, state and layout issues (overflow, clipping, truncated text, overlaps, covered or empty click targets), `Capture(path)` writes a PNG, and `Click`, `Type`, `Key`, `Drag` and `Scroll` inject input. An app opts into a windowless mode with `if (UiDebug.Run(args, () => new MyView())) return;`:

```bash
dotnet run --project Samples/UIGallery -- --ui-debug --size 1360x900 --out out        # out/ui.txt, ui.json, ui.png
dotnet run --project Samples/UIGallery -- --ui-debug --run "click Lists; scroll ListView 5; find ListView; capture out/list.png"
dotnet run --project Samples/UIGallery -- --ui-debug --script -                       # commands from stdin, `help` lists them
```

## Demos

- `Pong` — a small 2D game: one pipeline, a camera, a handful of quads and a score table.
- `Swarm` — a performance demo: thousands of individually bound objects, up to four million instanced shards and HDR bloom, with a HUD that shows what each frame cost. `dotnet run --project Swarm -c Release`; append `-- --benchmark` for a printed report or `-- --help` for the options.

## Building

```bash
dotnet build NiziKit.slnx
```

## License

GPL-3.0 — see [LICENSE](LICENSE).
