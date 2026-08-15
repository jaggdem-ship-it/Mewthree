# Dark Fantasy Survivor — Build Plan

## Core loop
The player moves a hooded exorcist through a cathedral crypt while weapons auto-target nearby enemies. Defeated enemies drop experience motes; each level opens a ritual upgrade choice. Enemy density, movement speed, and elite chance rise with elapsed time.

## Risk slices
1. **Three.js lifecycle:** one renderer, one scene, resize-safe camera, deterministic cleanup.
2. **Input vectors:** keyboard and pointer/touch movement normalized into a clamped 2D vector, with viewport-to-world mapping independent of device pixel ratio.
3. **Combat readability:** pooled enemy meshes, player-centered orthographic arena, simple projectile/impact feedback, and enemy telegraph colors.
4. **Progression:** XP collection, level-up pause, three upgrade cards, and deterministic upgrade effects.
5. **Persistence:** local run summary plus optional Puter.js cloud adapter that never blocks local play.
6. **Generated art:** use the generated arena reference/background and reliquary mark URLs directly; sprite sheets are recorded as generated references and may be replaced by runtime geometry when their pixel-cell alpha is not reliable.

## Verification criteria
- `pnpm check` passes.
- `/` renders a full-screen playable arena without React demo chrome.
- `?demo` produces deterministic movement and combat for screenshot verification.
- Keyboard WASD/arrows and pointer/touch drag both move the player.
- Auto-attacks visibly damage enemies and enemies drop XP.
- Level-up cards appear and applying an upgrade changes the run.
- Pause/resume and restart are reachable.
- Responsive HUD remains readable at desktop and mobile viewport sizes.
- No unhandled browser console errors in the primary run.
