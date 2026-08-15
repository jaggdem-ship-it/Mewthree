# Dark Fantasy Survivor — Design Direction

## Three Directions Considered

### Theme Name: Black-Iron Reliquary
Very dark gothic survival fantasy with hand-inked silhouettes, tarnished metal, parchment UI, and restrained blood-red accents. The mood is oppressive, tactile, and ecclesiastical rather than neon or cartoonish.

**Probability:** 0.07

### Theme Name: Moonlit Ossuary
A cool nocturnal battlefield built from slate blue, bone white, and ghostly silver, with a quiet supernatural tone and readable high-contrast combat effects.

**Probability:** 0.04

### Theme Name: Ember Cathedral
A warmer hellscape of charcoal stone, candle flame, and oxidized copper, using dramatic chiaroscuro and theatrical combat readability.

**Probability:** 0.09

## Selected Direction: Black-Iron Reliquary

### Design Movement
Neo-gothic editorial game interface: the visual language of illuminated manuscripts, iron reliquaries, cathedral floor plans, and 1990s dark-fantasy action RPG packaging translated into a responsive game HUD.

### Core Principles
1. **Tactile darkness:** surfaces should feel like worn iron, vellum, ash, and wax rather than flat black UI panels.
2. **Readable menace:** enemies and attack telegraphs must separate cleanly from the environment through silhouette, edge light, and controlled color accents.
3. **Asymmetric hierarchy:** the battlefield is dominant; HUD elements sit like marginalia and instrument panels rather than a centered dashboard.
4. **Ritualized progression:** level-ups, relic choices, and pauses should feel like opening a dangerous artifact.

### Color Philosophy
The foundation is near-black umber and charcoal rather than pure black, preserving texture and depth. Bone parchment carries primary text because it reads as an artifact surface. Ox-blood crimson signals danger and damage, while tarnished brass marks progression and chosen upgrades. The signature accent is **Reliquary Vermilion** (#b6423f): a dry, mineral red used sparingly so every flash feels consequential.

### Layout Paradigm
The playfield occupies the full viewport with HUD islands anchored to corners and edges. The top-left carries the character identity and survivability, the top-right carries run time and threat, and the bottom edge carries a segmented relic inventory. Level-up choices enter as a left-anchored ritual drawer so the arena remains visible behind it.

### Signature Elements
- Hairline brass rules with small engraved notch marks.
- Vellum-like parchment cards with torn-corner silhouettes and inked labels.
- A circular reliquary sigil that doubles as player health framing and pause affordance.

### Interaction Philosophy
Controls should feel immediate and physical. Pointer/touch input maps to a normalized movement vector with no dead-zone surprise, while keyboard input remains available as a precise fallback. Hover and focus states expose brass edge light; clicks feel like a stamped seal, not a floating web button.

### Animation
Combat motion uses short, decisive bursts: hit flashes under 140ms, pickup attraction with cubic ease-out, and relic cards entering from a slight lateral offset with opacity and transform only. The player aura breathes slowly, enemy approach uses subtle bobbing, and background particulate motion remains low contrast. Reduced-motion users receive the same state changes without nonessential drift.

### Typography System
Display: **Cinzel Decorative** for chapter labels and relic titles, used in restrained uppercase. Body/UI: **Source Sans 3** for readable meters, controls, and live values. Numbers use tabular lining figures and slightly increased tracking for timers and counters.

### Brand Essence
A grim, readable horde-survival ritual for players who want fast decisions inside a hand-crafted gothic world. Personality: **forbidding, tactile, precise**.

### Brand Voice
Headlines are terse and ceremonial. CTAs are verbs with consequence, never generic onboarding filler.

- Example headline: “THE BELLS HAVE NOT FORGOTTEN YOU.”
- Example CTA: “BREAK THE SEAL”

### Wordmark & Logo
The mark is a broken iron halo enclosing a single vertical reliquary nail, with three offset cuts suggesting a bell clapper and a bloodied seal. The wordmark uses a custom narrow serif treatment rather than a default font rendering.

### Signature Brand Color
**Reliquary Vermilion — #b6423f**.

## Implementation Contract

The browser host remains React-based because that is the available WebDev scaffold, but gameplay logic is isolated in strongly typed modules. The requested Fable/Three.js boundary is represented by complete F# module sources under `client/src/game-fsharp/` alongside the browser runtime adapter. The runtime uses Three.js for the scene, with deterministic object pools, normalized pointer vectors, auto-attack scheduling, enemy waves, XP pickups, level-up choices, and local run persistence. Puter.js access is isolated behind an optional binding module so the game remains playable when the cloud service is unavailable.
