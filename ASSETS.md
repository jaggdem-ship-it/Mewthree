# Dark Fantasy Survivor — Asset Manifest

| Asset ID | Purpose | Source | URL | Contract |
|---|---|---|---|---|
| `reliquary-master-reference` | Visual target | AI-generated reference | `/manus-storage/reliquary-master-reference_9a399a43.png` | 16:9 dark-fantasy arena direction |
| `reliquary-arena-background` | Arena backdrop | AI-generated environment plate | `/manus-storage/reliquary-arena-background_258bc23f.png` | 16:9, open center, no UI |
| `penitent-wraith-sprite-sheet` | Player sprite reference | AI-generated pixel-art sheet | `/manus-storage/penitent-wraith-sprite-sheet_a0e2d0c7.png` | 4x4 sheet, transparent prompt, prototype source |
| `graveborn-enemy-sheet` | Enemy sprite reference | AI-generated pixel-art sheet | `/manus-storage/graveborn-enemy-sheet_16ac9603.png` | 4x3 sheet, transparent prompt, prototype source |
| `reliquary-mark` | Brand mark/favicon/HUD | AI-generated graphic symbol | `/manus-storage/reliquary-mark_14de5717.png` | square, transparent prompt, no text |

## Runtime asset contract

The game uses the generated arena plate and reliquary mark directly. Combat actors are deliberately rendered as lightweight Three.js geometry with palette-matched materials so the prototype stays crisp and performant even if generated sprite-sheet transparency differs between renderers. The generated sheets remain part of the asset pack and are documented for a later texture-atlas pass.

## Sprite contract

The intended future atlas uses fixed 64x64 cells, pivot `(32, 58)`, baseline `58`, RGBA frames, and engine-neutral names such as `penitent_south_idle_00`. Prototype animation groups are `idle`, `walk`, `light_attack`, `damage`, and `death` at 8–18 fps.
