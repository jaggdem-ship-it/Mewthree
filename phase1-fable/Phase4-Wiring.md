# Phase 4 Wiring

`Phase4.fs` is compiled after `HordeEngine.fs` and before `App.fs`. The existing `Three.fs` binding now exposes `Mesh.SetMaterial`, which is used to swap the enemy material during the red hit flash.

In `App.fs`, import the module:

```fsharp
open Phase4
```

Create persistent state after the player, Unholy Orbs, and horde state have been initialized:

```fsharp
let phase4State = Phase4.createState player.MaxHP
let loopControl = { AnimationHandle = None; Paused = false }
```

Create a small helper for the current animation-frame handle. The callbacks supplied to `Phase4.tick` must cancel and restart the same render loop:

```fsharp
let pauseLoop () =
    match loopControl.AnimationHandle with
    | Some handle ->
        window.cancelAnimationFrame(handle)
        loopControl.AnimationHandle <- None
    | None -> ()
    loopControl.Paused <- true

let rec renderFrame (timestamp: float) =
    let deltaSeconds = clock.getDelta() |> min 0.05
    if not loopControl.Paused then
        movePlayer deltaSeconds input player playerMesh
        updateCamera deltaSeconds player camera
        HordeEngine.tick deltaSeconds scene playerMesh hordeState
        Phase4.tick deltaSeconds scene playerMesh 0.72 activeWeapons enemyVisualStates phase4State loopControl callbacks
        renderer.render(scene, camera)
        loopControl.AnimationHandle <- Some (window.requestAnimationFrame(renderFrame))

let resumeLoop () =
    if loopControl.Paused then
        loopControl.Paused <- false
        loopControl.AnimationHandle <- Some (window.requestAnimationFrame(renderFrame))
```

The callbacks are:

```fsharp
let rec callbacks : Phase4.LevelUpCallbacks =
    { PauseLoop = pauseLoop
      ResumeLoop = resumeLoop
      ApplyChoice = fun choice -> Phase4.selectChoice phase4State callbacks choice }
```

For each active Unholy Orb, construct a `WeaponCollider` using its current mesh and radius. For each horde enemy, construct an `EnemyVisualState` with its base material and a red flash material. The `activeWeapons` and `enemyVisualStates` collections should persist across frames and be refreshed when HordeEngine spawns or removes entities.

When Phase4 detects an orb/enemy intersection, it calls `HordeEngine.damageEnemy`, changes the enemy material to the flash material for 110 milliseconds, and applies an orb cooldown. When an enemy reaches zero health, `HordeEngine.tick` performs the authoritative mesh removal and disposal on the following horde tick, while Phase4 leaves a Soul Shard at the defeated enemy position.

The level-up overlay is created through `document.createElement`, uses three `data-choice` buttons, and removes itself after a selection. The `PauseLoop` callback calls `window.cancelAnimationFrame` on the current handle; the selected upgrade is applied, the overlay is removed, and `ResumeLoop` schedules the next frame.
