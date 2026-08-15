namespace DarkFantasySurvivor

module Phase4 =
    open System
    open System.Collections.Generic
    open Browser.Dom
    open Browser.Types
    open Fable.Core
    open Three
    open HordeEngine

    [<Literal>]
    let private SoulShardColor = 0x66E39A

    [<Literal>]
    let private HitFlashColor = 0xFF2438

    [<Literal>]
    let private SoulShardPickupRadius = 6.0

    [<Literal>]
    let private SoulShardCollectRadius = 0.8

    [<Literal>]
    let private LevelUpBaseThreshold = 8

    [<Literal>]
    let private EnemyContactCooldownSeconds = 0.65

    [<Literal>]
    let private OrbContactCooldownSeconds = 0.22

    type WeaponCollider =
        { Mesh: Mesh
          mutable Damage: float
          Radius: float
          mutable Cooldown: float }

    type EnemyVisualState =
        { Enemy: Enemy
          Radius: float
          ContactDamage: float
          BaseMaterial: obj
          FlashMaterial: obj
          mutable HitFlashSeconds: float
          mutable ContactCooldown: float }

    type SoulShard =
        { Id: int
          Mesh: Mesh
          Value: int
          Radius: float
          mutable Collected: bool }

    type GameLoopControl =
        { mutable AnimationHandle: float option
          mutable Paused: bool }

    type LevelUpChoice =
        | IncreaseOrbSpeed
        | HealTwentyHP
        | UnleashBloodAura

    type Phase4State =
        { mutable PlayerHP: float
          PlayerMaxHP: float
          mutable Experience: int
          mutable ExperienceThreshold: int
          mutable Level: int
          mutable OrbSpeedMultiplier: float
          mutable BloodAuraActive: bool
          mutable Paused: bool
          SoulShards: ResizeArray<SoulShard>
          mutable NextShardId: int
          mutable Overlay: HTMLElement option }

    type LevelUpCallbacks =
        { PauseLoop: unit -> unit
          ResumeLoop: unit -> unit
          ApplyChoice: LevelUpChoice -> unit }

    let createState playerMaxHP =
        { PlayerHP = playerMaxHP
          PlayerMaxHP = playerMaxHP
          Experience = 0
          ExperienceThreshold = LevelUpBaseThreshold
          Level = 1
          OrbSpeedMultiplier = 1.0
          BloodAuraActive = false
          Paused = false
          SoulShards = ResizeArray<SoulShard>()
          NextShardId = 1
          Overlay = None }

    let private setMeshMaterial (mesh: Mesh) (material: obj) : unit =
        mesh.SetMaterial material

    let private enemyRadius enemyType =
        match enemyType with
        | SkeletonWarrior -> 0.82
        | BloodFiend -> 0.76

    let private enemyContactDamage enemyType =
        match enemyType with
        | SkeletonWarrior -> 8.0
        | BloodFiend -> 14.0

    let createEnemyVisualState (enemy: Enemy) (baseMaterial: obj) (flashMaterial: obj) =
        { Enemy = enemy
          Radius = enemyRadius enemy.EnemyType
          ContactDamage = enemyContactDamage enemy.EnemyType
          BaseMaterial = baseMaterial
          FlashMaterial = flashMaterial
          HitFlashSeconds = 0.0
          ContactCooldown = 0.0 }

    let createWeaponCollider mesh damage radius =
        { Mesh = mesh
          Damage = damage
          Radius = radius
          Cooldown = 0.0 }

    let private createSoulShardMesh () =
        let geometry = SphereGeometry(0.22, 10, 8)
        let material = createStandardMaterial (U2.Case1 SoulShardColor) 0.18 0.15
        let mesh = Mesh(geometry :> obj, material :> obj)
        mesh.castShadow <- true
        mesh.receiveShadow <- true
        mesh

    [<Emit("$0.geometry.dispose(); $0.material.dispose(); $0.removeFromParent();")>]
    let private disposeMesh (mesh: Mesh) : unit = jsNative

    let private distanceSquaredOnPlane (left: Vector3) (right: Vector3) =
        let deltaX = left.x - right.x
        let deltaZ = left.z - right.z
        deltaX * deltaX + deltaZ * deltaZ

    let private intersects (leftMesh: Mesh) leftRadius (rightMesh: Mesh) rightRadius =
        let radius = leftRadius + rightRadius
        distanceSquaredOnPlane leftMesh.position rightMesh.position <= radius * radius

    let private triggerEnemyHitFlash (enemy: EnemyVisualState) =
        enemy.HitFlashSeconds <- 0.11
        setMeshMaterial enemy.Enemy.Mesh enemy.FlashMaterial

    let private updateEnemyFlash deltaSeconds (enemy: EnemyVisualState) =
        if enemy.HitFlashSeconds > 0.0 then
            enemy.HitFlashSeconds <- max 0.0 (enemy.HitFlashSeconds - deltaSeconds)
            if enemy.HitFlashSeconds = 0.0 then
                setMeshMaterial enemy.Enemy.Mesh enemy.BaseMaterial

    let private updateEnemyContactCooldown deltaSeconds (enemy: EnemyVisualState) =
        enemy.ContactCooldown <- max 0.0 (enemy.ContactCooldown - deltaSeconds)

    let private updateWeaponCooldown deltaSeconds (weapon: WeaponCollider) =
        weapon.Cooldown <- max 0.0 (weapon.Cooldown - deltaSeconds)

    let private checkOrbEnemyCollision (weapon: WeaponCollider) (enemy: EnemyVisualState) =
        if weapon.Cooldown = 0.0 && enemy.Enemy.Health > 0.0 && intersects weapon.Mesh weapon.Radius enemy.Enemy.Mesh enemy.Radius then
            HordeEngine.damageEnemy weapon.Damage enemy.Enemy
            triggerEnemyHitFlash enemy
            weapon.Cooldown <- OrbContactCooldownSeconds

    let private checkEnemyPlayerCollision (deltaSeconds: float) (playerMesh: Mesh) (playerRadius: float) (enemy: EnemyVisualState) (state: Phase4State) =
        if enemy.Enemy.Health > 0.0 && enemy.ContactCooldown = 0.0 && intersects enemy.Enemy.Mesh enemy.Radius playerMesh playerRadius then
            state.PlayerHP <- max 0.0 (state.PlayerHP - enemy.ContactDamage)
            enemy.ContactCooldown <- EnemyContactCooldownSeconds
        updateEnemyContactCooldown deltaSeconds enemy

    let private spawnSoulShard (scene: Scene) (state: Phase4State) (enemy: EnemyVisualState) =
        let shardMesh = createSoulShardMesh ()
        shardMesh.position.copy(enemy.Enemy.Mesh.position) |> ignore
        scene.add(shardMesh :> Object3D)
        state.SoulShards.Add
            { Id = state.NextShardId
              Mesh = shardMesh
              Value = 1
              Radius = 0.22
              Collected = false }
        state.NextShardId <- state.NextShardId + 1

    let private spawnDropsForDefeatedEnemies (scene: Scene) (state: Phase4State) (enemies: seq<EnemyVisualState>) =
        enemies
        |> Seq.iter (fun enemy ->
            if enemy.Enemy.Health <= 0.0 && enemy.HitFlashSeconds >= 0.0 then
                spawnSoulShard scene state enemy
                enemy.HitFlashSeconds <- -1.0)

    let private magnetizeSoulShard (deltaSeconds: float) (playerMesh: Mesh) (shard: SoulShard) =
        let distanceSquared = distanceSquaredOnPlane shard.Mesh.position playerMesh.position
        if distanceSquared <= SoulShardPickupRadius * SoulShardPickupRadius then
            let direction =
                createVector3
                    (playerMesh.position.x - shard.Mesh.position.x)
                    0.0
                    (playerMesh.position.z - shard.Mesh.position.z)
            if direction.length() > 0.0001 then
                direction.normalize() |> ignore
                let magnetSpeed = 8.0 + 3.0 / max 0.5 (sqrt distanceSquared)
                shard.Mesh.position.x <- shard.Mesh.position.x + direction.x * magnetSpeed * deltaSeconds
                shard.Mesh.position.z <- shard.Mesh.position.z + direction.z * magnetSpeed * deltaSeconds

    let private collectSoulShards (playerMesh: Mesh) (state: Phase4State) =
        state.SoulShards
        |> Seq.iter (fun shard ->
            if not shard.Collected && distanceSquaredOnPlane shard.Mesh.position playerMesh.position <= SoulShardCollectRadius * SoulShardCollectRadius then
                shard.Collected <- true
                state.Experience <- state.Experience + shard.Value)

    let private cleanupCollectedShards (scene: Scene) (state: Phase4State) =
        let mutable index = state.SoulShards.Count - 1
        while index >= 0 do
            let shard = state.SoulShards[index]
            if shard.Collected then
                scene.remove(shard.Mesh :> Object3D)
                disposeMesh shard.Mesh
                state.SoulShards.RemoveAt(index)
            index <- index - 1

    let private levelThresholdReached (state: Phase4State) =
        state.Experience >= state.ExperienceThreshold

    let private advanceLevel (state: Phase4State) =
        state.Experience <- state.Experience - state.ExperienceThreshold
        state.Level <- state.Level + 1
        state.ExperienceThreshold <- int (ceil (float state.ExperienceThreshold * 1.35))

    let private parseChoice value =
        match value with
        | "orb-speed" -> Some IncreaseOrbSpeed
        | "heal" -> Some HealTwentyHP
        | "blood-aura" -> Some UnleashBloodAura
        | _ -> None

    let private overlayMarkup =
        "<div class=\"level-up-eyebrow\">A RELIC ANSWERS</div>" +
        "<h1>CHOOSE YOUR VOW</h1>" +
        "<p>The crypt pauses. Take one gift into the dark.</p>" +
        "<div class=\"level-up-options\">" +
        "<button class=\"level-up-card\" data-choice=\"orb-speed\"><strong>Increase Unholy Orb Speed</strong><small>Make the orbit turn 22% faster.</small><em>ACCEPT VOW</em></button>" +
        "<button class=\"level-up-card\" data-choice=\"heal\"><strong>Heal 20 HP</strong><small>Restore twenty points of vitality.</small><em>ACCEPT VOW</em></button>" +
        "<button class=\"level-up-card\" data-choice=\"blood-aura\"><strong>Unleash Blood Aura</strong><small>Empower future close-range damage.</small><em>ACCEPT VOW</em></button>" +
        "</div>"

    let private attachLevelUpButtons (callbacks: LevelUpCallbacks) (overlay: HTMLElement) =
        let buttons = overlay.querySelectorAll("button")
        for index in 0 .. buttons.length - 1 do
            let button = buttons.item(index)
            button.addEventListener("click", fun _ ->
                match button.getAttribute("data-choice") |> Option.ofObj |> Option.bind parseChoice with
                | Some choice -> callbacks.ApplyChoice choice
                | None -> ())

    let private injectLevelUpOverlay (callbacks: LevelUpCallbacks) =
        let overlay = document.createElement("div")
        overlay.id <- "phase4-level-up-overlay"
        overlay.className <- "phase4-level-up-overlay"
        overlay.innerHTML <- overlayMarkup
        overlay.setAttribute("style", "position:fixed;inset:0;z-index:10000;display:grid;place-items:center;background:rgba(5,3,5,.78);backdrop-filter:blur(8px);color:#e5d4b5;font-family:Georgia,serif;")
        document.body.appendChild(overlay) |> ignore
        attachLevelUpButtons callbacks overlay
        overlay

    let private removeLevelUpOverlay (state: Phase4State) =
        match state.Overlay with
        | Some overlay ->
            overlay.remove()
            state.Overlay <- None
        | None -> ()

    let private openLevelUp (state: Phase4State) (callbacks: LevelUpCallbacks) =
        if state.Overlay.IsNone then
            state.Paused <- true
            state.Overlay <- Some (injectLevelUpOverlay callbacks)
            callbacks.PauseLoop()

    let applyChoice (state: Phase4State) choice =
        match choice with
        | IncreaseOrbSpeed -> state.OrbSpeedMultiplier <- state.OrbSpeedMultiplier * 1.22
        | HealTwentyHP -> state.PlayerHP <- min state.PlayerMaxHP (state.PlayerHP + 20.0)
        | UnleashBloodAura -> state.BloodAuraActive <- true

    let selectChoice (state: Phase4State) (callbacks: LevelUpCallbacks) choice =
        applyChoice state choice
        removeLevelUpOverlay state
        state.Paused <- false
        callbacks.ResumeLoop()

    let tick
        (deltaSeconds: float)
        (scene: Scene)
        (playerMesh: Mesh)
        (playerRadius: float)
        (weapons: seq<WeaponCollider>)
        (enemies: seq<EnemyVisualState>)
        (state: Phase4State)
        (loopControl: GameLoopControl)
        (callbacks: LevelUpCallbacks) =
        if not loopControl.Paused then
            let safeDeltaSeconds = deltaSeconds |> max 0.0 |> min 0.1
            weapons |> Seq.iter (updateWeaponCooldown safeDeltaSeconds)
            enemies |> Seq.iter (updateEnemyFlash safeDeltaSeconds)
            weapons |> Seq.iter (fun weapon -> enemies |> Seq.iter (checkOrbEnemyCollision weapon))
            enemies |> Seq.iter (fun enemy -> checkEnemyPlayerCollision safeDeltaSeconds playerMesh playerRadius enemy state)
            spawnDropsForDefeatedEnemies scene state enemies
            state.SoulShards |> Seq.iter (magnetizeSoulShard safeDeltaSeconds playerMesh)
            collectSoulShards playerMesh state
            cleanupCollectedShards scene state
            if levelThresholdReached state then
                advanceLevel state
                openLevelUp state callbacks

    let orbSpeedMultiplier (state: Phase4State) =
        state.OrbSpeedMultiplier

    let bloodAuraActive (state: Phase4State) =
        state.BloodAuraActive
