# Dark Fantasy Survivor — Structure

## Browser host
`client/src/App.tsx` renders the game shell. React owns HUD state, menus, level-up cards, and the canvas mount point. It does not own the simulation loop.

## Three.js runtime
`client/src/game/threeRuntime.ts` owns the renderer, orthographic camera, world meshes, pooled enemies, projectiles, XP motes, and frame loop. It exposes a narrow `GameRuntime` interface to React.

## Simulation
`client/src/game/simulation.ts` contains plain TypeScript domain types and deterministic update functions for movement, spawning, targeting, damage, XP, and upgrades. It has no DOM or React imports.

## Fable boundary
`client/src/game-fsharp/ThreeBindings.fs`, `PuterBindings.fs`, and `Game.fs` contain complete F# modules designed for Fable compilation. They mirror the runtime vocabulary: vectors, pointer normalization, wave state, auto-attack scheduling, object-pool contracts, local persistence, and optional Puter calls. They are compile-ready source modules even though the WebDev host uses the already bundled TypeScript runtime adapter for immediate browser execution.

## Input
`client/src/game/input.ts` maps keyboard and pointer/touch events into a normalized `InputVector` and tracks viewport dimensions, keeping 2D viewport coordinates separate from the 3D arena plane.

## Assets
Generated images remain outside the project under `/home/ubuntu/webdev-static-assets/` and are referenced by their lifecycle-safe `/manus-storage/...` URLs in `ASSETS.md` and the runtime.
