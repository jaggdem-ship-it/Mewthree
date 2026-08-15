/* Black-Iron Reliquary: plain deterministic simulation types, no DOM or React coupling. */

export type Vec2 = { x: number; y: number };
export type EnemyKind = "graveborn" | "bone-hound" | "bell-revenant";
export type UpgradeId = "ember-mace" | "iron-vow" | "blood-compass" | "ashen-step";

export type PlayerState = {
  position: Vec2;
  hp: number;
  maxHp: number;
  level: number;
  xp: number;
  xpToNext: number;
  moveSpeed: number;
  attackDamage: number;
  attackCooldown: number;
  attackTimer: number;
  pickupRadius: number;
  kills: number;
};

export type EnemyState = {
  id: number;
  kind: EnemyKind;
  position: Vec2;
  hp: number;
  maxHp: number;
  speed: number;
  radius: number;
  damage: number;
  hitTimer: number;
  alive: boolean;
};

export type ProjectileState = {
  id: number;
  position: Vec2;
  velocity: Vec2;
  damage: number;
  life: number;
  alive: boolean;
};

export type XpOrb = { id: number; position: Vec2; value: number; alive: boolean };

export type UpgradeCard = { id: UpgradeId; title: string; detail: string; glyph: string };

export type RunState = {
  elapsed: number;
  wave: number;
  spawnTimer: number;
  nextEnemyId: number;
  nextProjectileId: number;
  nextOrbId: number;
  player: PlayerState;
  enemies: EnemyState[];
  projectiles: ProjectileState[];
  orbs: XpOrb[];
  pendingLevelUp: boolean;
  cards: UpgradeCard[];
  gameOver: boolean;
  paused: boolean;
};

export const ARENA = { width: 34, height: 20 } as const;

export function length(v: Vec2): number { return Math.hypot(v.x, v.y); }
export function normalize(v: Vec2): Vec2 {
  const l = length(v);
  return l > 0.0001 ? { x: v.x / l, y: v.y / l } : { x: 0, y: 0 };
}
export function add(a: Vec2, b: Vec2): Vec2 { return { x: a.x + b.x, y: a.y + b.y }; }
export function scale(v: Vec2, s: number): Vec2 { return { x: v.x * s, y: v.y * s }; }
export function clamp(v: number, min: number, max: number): number { return Math.max(min, Math.min(max, v)); }

export function createInitialRun(): RunState {
  return {
    elapsed: 0, wave: 1, spawnTimer: 0.7, nextEnemyId: 1, nextProjectileId: 1, nextOrbId: 1,
    player: { position: { x: 0, y: 0 }, hp: 100, maxHp: 100, level: 1, xp: 0, xpToNext: 8, moveSpeed: 6.4, attackDamage: 18, attackCooldown: 0.72, attackTimer: 0.2, pickupRadius: 2.5, kills: 0 },
    enemies: [], projectiles: [], orbs: [], pendingLevelUp: false, cards: [], gameOver: false, paused: false,
  };
}

export function chooseTarget(player: Vec2, enemies: EnemyState[]): EnemyState | undefined {
  let best: EnemyState | undefined;
  let bestDistance = Number.POSITIVE_INFINITY;
  for (const enemy of enemies) {
    if (!enemy.alive) continue;
    const dx = enemy.position.x - player.x;
    const dy = enemy.position.y - player.y;
    const distance = dx * dx + dy * dy;
    if (distance < bestDistance) { bestDistance = distance; best = enemy; }
  }
  return best;
}

export function spawnEnemy(run: RunState): EnemyState {
  const angle = (run.nextEnemyId * 2.399963) % (Math.PI * 2);
  const radius = 12.5 + ((run.nextEnemyId * 17) % 35) / 10;
  const roll = (run.nextEnemyId * 13 + run.wave) % 10;
  const kind: EnemyKind = roll > 8 && run.elapsed > 45 ? "bell-revenant" : roll > 5 ? "bone-hound" : "graveborn";
  const stats = kind === "graveborn" ? { hp: 34 + run.wave * 3, speed: 1.25 + run.wave * 0.04, radius: 0.42, damage: 7 } : kind === "bone-hound" ? { hp: 22 + run.wave * 2, speed: 2.3 + run.wave * 0.06, radius: 0.32, damage: 5 } : { hp: 140 + run.wave * 14, speed: 0.7 + run.wave * 0.02, radius: 0.68, damage: 14 };
  return { id: run.nextEnemyId++, kind, position: { x: Math.cos(angle) * radius, y: Math.sin(angle) * radius * 0.58 }, hp: stats.hp, maxHp: stats.hp, speed: stats.speed, radius: stats.radius, damage: stats.damage, hitTimer: 0, alive: true };
}

export function randomCards(level: number): UpgradeCard[] {
  const cards: UpgradeCard[] = [
    { id: "ember-mace", title: "Ember Mace", detail: "+8 attack damage; impacts leave a cinder mark.", glyph: "✦" },
    { id: "iron-vow", title: "Iron Vow", detail: "+24 maximum health and restore 24 health.", glyph: "◇" },
    { id: "blood-compass", title: "Blood Compass", detail: "-15% attack cooldown; the relic hunts faster.", glyph: "⊙" },
    { id: "ashen-step", title: "Ashen Step", detail: "+18% movement speed and pickup radius.", glyph: "↝" },
  ];
  const offset = Math.max(0, level - 1) % cards.length;
  return [cards[offset], cards[(offset + 1) % cards.length], cards[(offset + 2) % cards.length]];
}

export function applyUpgrade(run: RunState, id: UpgradeId): void {
  if (id === "ember-mace") run.player.attackDamage += 8;
  if (id === "iron-vow") { run.player.maxHp += 24; run.player.hp = Math.min(run.player.maxHp, run.player.hp + 24); }
  if (id === "blood-compass") run.player.attackCooldown = Math.max(0.28, run.player.attackCooldown * 0.85);
  if (id === "ashen-step") { run.player.moveSpeed *= 1.18; run.player.pickupRadius += 0.6; }
  run.pendingLevelUp = false;
  run.cards = [];
}

export function grantXp(run: RunState, value: number): void {
  run.player.xp += value;
  while (run.player.xp >= run.player.xpToNext) {
    run.player.xp -= run.player.xpToNext;
    run.player.level += 1;
    run.player.xpToNext = Math.ceil(run.player.xpToNext * 1.32 + 2);
    run.pendingLevelUp = true;
    run.cards = randomCards(run.player.level);
  }
}
