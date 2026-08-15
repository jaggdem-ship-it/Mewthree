# Phase 5 Wiring

`GameApp.fsproj` now compiles `Puter.fs` and `Phase5.fs` before `App.fs`, and `index.html` loads the official Puter.js v2 browser library from `https://js.puter.com/v2/`.

Import both modules in `App.fs`:

```fsharp
open Puter
open Phase5
```

Create persistent cloud and loop state during scene initialization:

```fsharp
let cloudState = Phase5.createCloudScoreState ()
let mutable runScore = 0
let loopControl = { AnimationHandle = None; Paused = false }
```

The start menu loads the current user-scoped high score asynchronously. Puter authentication is handled by `Puter.loadHighScore`, which checks `puter.auth.isSignedIn()`, prompts with `puter.auth.signIn()` if needed, then reads `puter.kv.get("diablo_high_score")`. The menu prints the resolved value before the run begins:

```fsharp
let startCallbacks =
    { StartRun = fun () -> loopControl.Paused <- false }

Phase5.showStartMenu cloudState startCallbacks |> ignore
```

Because Puter sign-in opens a popup, production UI should call `showStartMenu` from the initial user-facing start flow or expose a dedicated sign-in button if browsers block an automatic popup.

The render loop should retain its current `requestAnimationFrame` handle. On game over, stop scheduling frames with `window.cancelAnimationFrame`, then create the death overlay:

```fsharp
let pauseLoop () =
    match loopControl.AnimationHandle with
    | Some handle ->
        window.cancelAnimationFrame(handle)
        loopControl.AnimationHandle <- None
    | None -> ()
    loopControl.Paused <- true

let restartRun () =
    cloudState.Syncing <- false
    runScore <- 0
    player.CurrentHP <- player.MaxHP
    loopControl.Paused <- false
    loopControl.AnimationHandle <- Some (window.requestAnimationFrame(renderFrame))

let gameOverCallbacks =
    { PauseLoop = pauseLoop
      RestartRun = restartRun
      RenderScore = fun score -> runScore <- score }
```

After Phase 4 collision and enemy contact processing, detect the terminal player state once per run:

```fsharp
if player.CurrentHP <= 0.0 && not loopControl.Paused then
    let finalScore = runScore
    Phase5.showGameOver cloudState gameOverCallbacks finalScore |> ignore
```

`showGameOver` immediately pauses the loop, injects the dark “YOU DIED” overlay, displays the current score, and starts an `async { ... }` cloud synchronization. `Puter.syncHighScore` authenticates if necessary, reads `puter.kv.get("diablo_high_score")`, compares the current score, and writes the new value with `puter.kv.set` only when the run is better. All JS promises are converted through `Async.AwaitPromise`, and failures fall back to the local score without breaking the game-over UI.

The Puter wrapper uses `box score` for the KV write and a JavaScript `Number(...)` conversion for reads so values stored as numbers or numeric strings are accepted consistently.
