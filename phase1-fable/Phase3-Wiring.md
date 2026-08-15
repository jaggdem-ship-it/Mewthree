# Phase 3 Wiring

`HordeEngine.fs` is already placed before `App.fs` in `GameApp.fsproj`, so the module is available to the application entry point.

In `App.fs`, add the module import alongside the existing imports:

```fsharp
open HordeEngine
```

Inside `initializeScene`, after `playerMesh` has been created and added to the scene, create one persistent horde state. It must not be recreated inside the animation callback:

```fsharp
let hordeState = HordeEngine.createState ()
```

Inside the existing `renderFrame` function, after the player movement has been applied and before rendering, call the horde tick with the same delta-clock value used by the rest of the simulation:

```fsharp
HordeEngine.tick deltaSeconds scene playerMesh hordeState
```

The relevant lifecycle shape is:

```fsharp
let rec renderFrame (_timestamp: float) =
    let deltaSeconds = clock.getDelta() |> min 0.05
    movePlayer deltaSeconds input player playerMesh
    updateCamera deltaSeconds player camera
    HordeEngine.tick deltaSeconds scene playerMesh hordeState
    renderer.render(scene, camera)
    window.requestAnimationFrame(renderFrame) |> ignore
```

When a future weapon or collision system kills an enemy, call `HordeEngine.damageEnemy amount enemy`. The next `HordeEngine.tick` removes the enemy from the active `ResizeArray`, removes its mesh from the scene, disposes its geometry and material, and calls `removeFromParent` to prevent retained scene references.

The spawner chooses a random cluster size with `Random.Next(5, 11)`, which produces 5 through 10 inclusive, and places each cluster member at a radius between 28 and 38 world units from the player. This keeps the initial spawn ring outside the normal direct camera view while allowing the isometric camera and infinite ground plane to remain unchanged.
