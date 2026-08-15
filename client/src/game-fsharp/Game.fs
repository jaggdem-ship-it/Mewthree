namespace DarkFantasySurvivor

module Game =
    open Fable.Core
    open ThreeBindings

    type EnemyKind = Graveborn | BoneHound | BellRevenant
    type Upgrade = EmberMace | IronVow | BloodCompass | AshenStep
    type Enemy = { id: int; kind: EnemyKind; position: ArenaVector; hp: float; maxHp: float; speed: float; alive: bool }
    type Player = { position: ArenaVector; hp: float; maxHp: float; level: int; xp: int; xpToNext: int; moveSpeed: float; attackDamage: float; attackCooldown: float; attackTimer: float; kills: int }
    type Run = { elapsed: float; wave: int; nextEnemyId: int; player: Player; enemies: Enemy list; paused: bool; gameOver: bool; pendingLevelUp: bool }

    let private add a b = { x = a.x + b.x; y = a.y + b.y }
    let private scale amount value = { x = value.x * amount; y = value.y * amount }
    let private distance a b = sqrt ((a.x - b.x) ** 2.0 + (a.y - b.y) ** 2.0)
    let private normalize vector =
        let magnitude = sqrt (vector.x * vector.x + vector.y * vector.y)
        if magnitude > 0.0001 then scale (1.0 / magnitude) vector else { x = 0.0; y = 0.0 }

    let initialRun = { elapsed = 0.0; wave = 1; nextEnemyId = 1; player = { position = { x = 0.0; y = 0.0 }; hp = 100.0; maxHp = 100.0; level = 1; xp = 0; xpToNext = 8; moveSpeed = 6.4; attackDamage = 18.0; attackCooldown = 0.72; attackTimer = 0.2; kills = 0 }; enemies = []; paused = false; gameOver = false; pendingLevelUp = false }

    let movePlayer deltaSeconds input run =
        if run.paused || run.gameOver || run.pendingLevelUp then run
        else
            let direction = normalize input
            let next = add run.player.position (scale (run.player.moveSpeed * deltaSeconds) direction)
            let bounded = { x = max -16.0 (min 16.0 next.x); y = max -9.0 (min 9.0 next.y) }
            { run with player = { run.player with position = bounded } }

    let nearestEnemy player enemies =
        enemies |> List.filter (fun enemy -> enemy.alive) |> List.sortBy (fun enemy -> distance player enemy.position) |> List.tryHead

    let tick deltaSeconds input run =
        let moved = movePlayer deltaSeconds input run
        { moved with elapsed = moved.elapsed + deltaSeconds; wave = int (moved.elapsed / 22.0) + 1 }

    let applyUpgrade upgrade run =
        let player =
            match upgrade with
            | EmberMace -> { run.player with attackDamage = run.player.attackDamage + 8.0 }
            | IronVow -> { run.player with maxHp = run.player.maxHp + 24.0; hp = min (run.player.maxHp + 24.0) (run.player.hp + 24.0) }
            | BloodCompass -> { run.player with attackCooldown = max 0.28 (run.player.attackCooldown * 0.85) }
            | AshenStep -> { run.player with moveSpeed = run.player.moveSpeed * 1.18 }
        { run with player = player; paused = false; pendingLevelUp = false }
