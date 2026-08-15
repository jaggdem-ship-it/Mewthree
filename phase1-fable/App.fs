namespace DarkFantasySurvivor

module App =
    open System
    open System.Collections.Generic
    open Browser.Dom
    open Browser.Types
    open Fable.Core
    open Three
    open HordeEngine

    [<Literal>]
    let private Black = 0x000000

    [<Literal>]
    let private DarkCrimson = 0x26080D

    [<Literal>]
    let private Moonlight = 0xD9E2FF

    [<Literal>]
    let private GroundIron = 0x100E12

    [<Literal>]
    let private PlayerIron = 0x241B2C

    [<Literal>]
    let private UnholyPurple = 0x4D176D

    [<Literal>]
    let private UnholyCore = 0xC26BFF

    [<Literal>]
    let private DefaultPlayerHP = 100.0

    [<Literal>]
    let private DefaultMoveSpeed = 7.5

    [<Literal>]
    let private OrbSpawnInterval = 1.5

    [<Literal>]
    let private OrbCount = 3

    [<Literal>]
    let private OrbOrbitRadius = 3.1

    [<Literal>]
    let private OrbAngularSpeed = 2.35

    [<Literal>]
    let private OrbContactRadius = 1.0

    [<Literal>]
    let private OrbContactDamage = 18.0

    type PlayerData =
        { Position: Vector3
          MoveSpeed: float
          mutable CurrentHP: float
          MaxHP: float
          mutable Level: int
          mutable VelocityX: float
          mutable VelocityZ: float }

    type UnholyOrb =
        { Mesh: Mesh
          OrbitPhase: float
          OrbitRadius: float
          AngularSpeed: float
          ContactDamage: float
          ContactRadius: float }

    type ShadowBolt =
        { Mesh: Mesh
          Target: Phase4.EnemyVisualState
          Damage: float
          Speed: float }

    type AbilityEffect =
        { Mesh: Mesh
          mutable Age: float
          Lifetime: float
          Kind: string
          mutable HasImpacted: bool
          Target: Phase4.EnemyVisualState option }

    type Particle =
        { Mesh: Mesh
          mutable Age: float
          Lifetime: float
          VelocityX: float
          VelocityY: float
          VelocityZ: float }

    type JoystickState =
        { Base: HTMLElement
          Knob: HTMLElement
          mutable Active: bool
          mutable PointerId: float
          mutable X: float
          mutable Y: float }

    type InputState =
        { mutable Keys: Set<string>
          Joystick: JoystickState
          mutable LastIntentX: float
          mutable LastIntentZ: float }

    let private clamp minimum maximum value =
        value |> max minimum |> min maximum

    let private normalizeHorizontal (value: Vector3) =
        let horizontalLength = sqrt (value.x * value.x + value.z * value.z)
        if horizontalLength > 0.0001 then
            value.x <- value.x / horizontalLength
            value.z <- value.z / horizontalLength
        value

    let private canvas () : HTMLCanvasElement =
        match document.getElementById("game-view") with
        | null -> failwith "The required #game-view canvas was not found."
        | element -> element :?> HTMLCanvasElement

    let private createJoystick () =
        let baseElement = document.createElement("div")
        let knobElement = document.createElement("div")
        baseElement.id <- "mobile-joystick"
        knobElement.id <- "mobile-joystick-knob"
        baseElement.appendChild(knobElement) |> ignore
        baseElement.setAttribute("style", "position:fixed;left:24px;bottom:32px;width:132px;height:132px;border:2px solid rgba(221,180,116,.72);border-radius:50%;background:radial-gradient(circle,rgba(104,28,48,.42),rgba(10,6,12,.72));box-shadow:0 0 28px rgba(91,17,41,.8),inset 0 0 20px rgba(0,0,0,.7);z-index:80;touch-action:none;pointer-events:auto;display:block;")
        knobElement.setAttribute("style", "position:absolute;left:41px;top:41px;width:46px;height:46px;border:1px solid rgba(240,213,163,.9);border-radius:50%;background:radial-gradient(circle at 35% 30%,#d5a9b4,#64152f 60%,#1b0b12);box-shadow:0 3px 12px rgba(0,0,0,.75);pointer-events:none;transform:translate(0px,0px);")
        document.body.appendChild(baseElement) |> ignore
        { Base = baseElement
          Knob = knobElement
          Active = false
          PointerId = -1
          X = 0.0
          Y = 0.0 }

    let private resetJoystick (joystick: JoystickState) =
        joystick.Active <- false
        joystick.PointerId <- -1
        joystick.X <- 0.0
        joystick.Y <- 0.0
        joystick.Knob.setAttribute("style", "position:absolute;left:41px;top:41px;width:46px;height:46px;border:1px solid rgba(240,213,163,.9);border-radius:50%;background:radial-gradient(circle at 35% 30%,#d5a9b4,#64152f 60%,#1b0b12);box-shadow:0 3px 12px rgba(0,0,0,.75);pointer-events:none;transform:translate(0px,0px);")

    let private updateJoystick (joystick: JoystickState) (event: PointerEvent) =
        let bounds = joystick.Base.getBoundingClientRect()
        let centerX = bounds.left + bounds.width / 2.0
        let centerY = bounds.top + bounds.height / 2.0
        let maxRadius = bounds.width * 0.34
        let rawX = event.clientX - centerX
        let rawY = event.clientY - centerY
        let magnitude = sqrt (rawX * rawX + rawY * rawY)
        let scale = if magnitude > maxRadius then maxRadius / magnitude else 1.0
        let clampedX = rawX * scale
        let clampedY = rawY * scale
        joystick.X <- clampedX / maxRadius
        joystick.Y <- clampedY / maxRadius
        joystick.Knob.setAttribute("style", sprintf "position:absolute;left:41px;top:41px;width:46px;height:46px;border:1px solid rgba(240,213,163,.9);border-radius:50%%;background:radial-gradient(circle at 35%% 30%%,#d5a9b4,#64152f 60%%,#1b0b12);box-shadow:0 3px 12px rgba(0,0,0,.75);pointer-events:none;transform:translate(%fpx,%fpx);" (clampedX * 0.62) (clampedY * 0.62))

    [<Emit("$0.geometry.dispose(); $0.material.dispose();")>]
    let private disposeCombatMesh (mesh: Mesh) : unit = jsNative

    let private createShadowBolt (scene: Scene) (player: PlayerData) (target: Phase4.EnemyVisualState) damage =
        let geometry = SphereGeometry(0.16, 8, 6)
        let material = createStandardMaterial (U2.Case1 UnholyCore) 0.22 0.65
        let mesh = Mesh(geometry :> obj, material :> obj)
        mesh.castShadow <- true
        mesh.position.set(player.Position.x, player.Position.y + 0.5, player.Position.z) |> ignore
        scene.add(mesh :> Object3D)
        { Mesh = mesh
          Target = target
          Damage = damage
          Speed = 18.0 }

    let private updateShadowBolt (deltaSeconds: float) (bolt: ShadowBolt) =
        if bolt.Target.Enemy.Health <= 0.0 then
            false
        else
            let direction =
                createVector3
                    (bolt.Target.Enemy.Mesh.position.x - bolt.Mesh.position.x)
                    0.0
                    (bolt.Target.Enemy.Mesh.position.z - bolt.Mesh.position.z)
            let distanceSquared = direction.x * direction.x + direction.z * direction.z
            if distanceSquared <= 0.65 * 0.65 then
                Phase4.hitEnemy bolt.Damage bolt.Target
                false
            else
                direction.normalize() |> ignore
                bolt.Mesh.position.x <- bolt.Mesh.position.x + direction.x * bolt.Speed * deltaSeconds
                bolt.Mesh.position.z <- bolt.Mesh.position.z + direction.z * bolt.Speed * deltaSeconds
                true

    let private createPlayerMesh () =
        let geometry = SphereGeometry(0.72, 16, 12)
        let material = createStandardMaterial (U2.Case1 PlayerIron) 0.72 0.48
        let mesh = Mesh(geometry :> obj, material :> obj)
        mesh.castShadow <- true
        mesh.receiveShadow <- true
        mesh

    let private createUnholyOrb phase =
        let geometry = SphereGeometry(0.38, 18, 14)
        let material = createStandardMaterial (U2.Case1 UnholyPurple) 0.24 0.58
        let mesh = Mesh(geometry :> obj, material :> obj)
        mesh.castShadow <- true
        mesh.receiveShadow <- true
        { Mesh = mesh
          OrbitPhase = phase
          OrbitRadius = OrbOrbitRadius
          AngularSpeed = OrbAngularSpeed
          ContactDamage = OrbContactDamage
          ContactRadius = OrbContactRadius }

    let private spawnUnholyOrbs (scene: Scene) (player: PlayerData) =
        let phases = Array.init OrbCount (fun index -> float index * (Math.PI * 2.0 / float OrbCount))
        phases
        |> Array.map createUnholyOrb
        |> Array.iter (fun orb ->
            orb.Mesh.position.set(player.Position.x, player.Position.y + 0.55, player.Position.z) |> ignore
            scene.add(orb.Mesh :> Object3D))
        phases |> Array.map createUnholyOrb

    // Black-Iron Reliquary style: orbitals are persistent combat relics, and every retired
    // relic must release both its scene attachment and GPU-backed geometry/material state.
    let private removeUnholyOrbs (scene: Scene) (orbs: UnholyOrb array) =
        orbs
        |> Array.iter (fun orb ->
            scene.remove(orb.Mesh :> Object3D)
            disposeCombatMesh orb.Mesh)

    let private updateUnholyOrb multiplier deltaSeconds elapsedSeconds (player: PlayerData) (orb: UnholyOrb) =
        let angle = elapsedSeconds * orb.AngularSpeed * multiplier + orb.OrbitPhase
        let orbitX = player.Position.x + cos angle * orb.OrbitRadius
        let orbitZ = player.Position.z + sin angle * orb.OrbitRadius
        orb.Mesh.position.set(orbitX, player.Position.y + 0.55, orbitZ) |> ignore
        orb.Mesh.rotation.x <- orb.Mesh.rotation.x + deltaSeconds * 3.0
        orb.Mesh.rotation.y <- orb.Mesh.rotation.y + deltaSeconds * 4.5
        orb

    let private movePlayer deltaSeconds (input: InputState) (player: PlayerData) (playerMesh: Mesh) =
        let right = input.Keys.Contains "d" || input.Keys.Contains "ArrowRight"
        let left = input.Keys.Contains "a" || input.Keys.Contains "ArrowLeft"
        let down = input.Keys.Contains "s" || input.Keys.Contains "ArrowDown"
        let up = input.Keys.Contains "w" || input.Keys.Contains "ArrowUp"
        let keyboardX = (if right then 1.0 else 0.0) - (if left then 1.0 else 0.0)
        let keyboardZ = (if down then 1.0 else 0.0) - (if up then 1.0 else 0.0)
        let joystickMagnitude = sqrt (input.Joystick.X * input.Joystick.X + input.Joystick.Y * input.Joystick.Y)
        let joystickDeadZone = 0.12
        let joystickScale = if joystickMagnitude <= joystickDeadZone then 0.0 else (joystickMagnitude - joystickDeadZone) / (1.0 - joystickDeadZone)
        let joystickX = if joystickMagnitude > 0.0001 then input.Joystick.X / joystickMagnitude * joystickScale else 0.0
        let joystickZ = if joystickMagnitude > 0.0001 then input.Joystick.Y / joystickMagnitude * joystickScale else 0.0
        let rawX = keyboardX + joystickX
        let rawZ = keyboardZ + joystickZ
        let intentMagnitude = sqrt (rawX * rawX + rawZ * rawZ)
        let intentX, intentZ =
            if intentMagnitude > 1.0 then rawX / intentMagnitude, rawZ / intentMagnitude else rawX, rawZ
        input.LastIntentX <- intentX
        input.LastIntentZ <- intentZ
        let acceleration = 34.0
        let friction = 22.0
        let targetVelocityX = intentX * player.MoveSpeed
        let targetVelocityZ = intentZ * player.MoveSpeed
        let response = if intentMagnitude > 0.01 then acceleration else friction
        let blend = min 1.0 (response * deltaSeconds)
        player.VelocityX <- player.VelocityX + (targetVelocityX - player.VelocityX) * blend
        player.VelocityZ <- player.VelocityZ + (targetVelocityZ - player.VelocityZ) * blend
        player.Position.x <- player.Position.x + player.VelocityX * deltaSeconds
        player.Position.z <- player.Position.z + player.VelocityZ * deltaSeconds
        playerMesh.position.copy(player.Position) |> ignore
        let speedSquared = player.VelocityX * player.VelocityX + player.VelocityZ * player.VelocityZ
        if speedSquared > 0.04 then
            playerMesh.rotation.y <- atan2 player.VelocityX player.VelocityZ

    let private updateCamera deltaSeconds (player: PlayerData) (camera: PerspectiveCamera) =
        let targetX = player.Position.x + 12.0
        let targetY = player.Position.y + 16.0
        let targetZ = player.Position.z + 12.0
        let followSharpness = 1.0 - exp (-10.0 * deltaSeconds)
        camera.position.x <- camera.position.x + (targetX - camera.position.x) * followSharpness
        camera.position.y <- camera.position.y + (targetY - camera.position.y) * followSharpness
        camera.position.z <- camera.position.z + (targetZ - camera.position.z) * followSharpness
        camera.lookAt(player.Position)

    let private resizeScene (camera: PerspectiveCamera) (renderer: WebGLRenderer) () =
        let nextWidth = float window.innerWidth
        let nextHeight = float window.innerHeight
        let nextAspect = if nextHeight > 0.0 then nextWidth / nextHeight else 1.0
        camera.aspect <- nextAspect
        camera.updateProjectionMatrix()
        renderer.setSize(nextWidth, nextHeight, true)
        renderer.setPixelRatio(1.0)

    let private createHud () =
        let hud = document.createElement("section")
        hud.id <- "game-hud"
        hud.innerHTML <-
            "<div class=\"hud-brand\">THE ASHEN VIGIL <span>WRAITH OF THE BELLS</span></div>" +
            "<div class=\"hud-stats\">" +
            "<div class=\"hud-line\"><span>VITALITY</span><strong id=\"hud-hp\">100 / 100</strong></div>" +
            "<div class=\"hud-meter\"><i id=\"hud-hp-fill\"></i></div>" +
            "<div class=\"hud-line\"><span>SOUL SHARDS</span><strong id=\"hud-xp\">0 / 8</strong></div>" +
            "<div class=\"hud-meter xp\"><i id=\"hud-xp-fill\"></i></div>" +
            "<div class=\"hud-line compact\"><span>LEVEL <strong id=\"hud-level\">1</strong></span><span>SCORE <strong id=\"hud-score\">0</strong></span></div>" +
            "</div>" +
            "<div class=\"hud-help\">WASD / ARROWS TO MOVE<br/>THE ORBS HUNT FOR YOU</div>"
        hud.setAttribute("style", "position:fixed;inset:0;z-index:20;pointer-events:none;color:#e8dec9;font-family:Georgia,serif;text-shadow:0 2px 10px #000;")
        document.body.appendChild(hud) |> ignore
        hud

    let private setHudText id value =
        match document.getElementById(id) with
        | null -> ()
        | element -> element.textContent <- value

    let private setHudWidth id value =
        match document.getElementById(id) with
        | null -> ()
        | element -> element.setAttribute("style", sprintf "width:%s%%" value)

    let private updateHud (player: PlayerData) (phase4State: Phase4.Phase4State) score =
        setHudText "hud-hp" (sprintf "%d / %d" (int player.CurrentHP) (int player.MaxHP))
        setHudText "hud-xp" (sprintf "%d / %d" phase4State.Experience phase4State.ExperienceThreshold)
        setHudText "hud-level" (string phase4State.Level)
        setHudText "hud-score" (string score)
        setHudWidth "hud-hp-fill" (string (clamp 0.0 100.0 (player.CurrentHP / player.MaxHP * 100.0)))
        setHudWidth "hud-xp-fill" (string (clamp 0.0 100.0 (float phase4State.Experience / float phase4State.ExperienceThreshold * 100.0)))

    let private initializeScene () =
        let view = canvas ()
        let width = float window.innerWidth
        let height = float window.innerHeight
        let aspect = if height > 0.0 then width / height else 1.0

        let scene = Scene()
        scene.background <- box Black
        scene.fog <- box (FogExp2(U2.Case1 Black, 0.028))

        let camera = PerspectiveCamera(58.0, aspect, 0.1, 2000.0)
        camera.position.set(12.0, 16.0, 12.0) |> ignore
        camera.lookAt(createVector3 0.0 0.0 0.0)

        let renderer = createRenderer (box view)
        renderer.setPixelRatio(1.0)
        renderer.setSize(width, height, true)
        renderer.shadowMap.enabled <- false
        renderer.shadowMap.``type`` <- 2

        let ambient = AmbientLight(U2.Case1 DarkCrimson, 0.32)
        scene.add(ambient :> Object3D)

        let moon = DirectionalLight(U2.Case1 Moonlight, 2.4)
        moon.position.set(-14.0, 26.0, 10.0) |> ignore
        moon.castShadow <- false
        moon.shadow.mapSize.width <- 2048.0
        moon.shadow.mapSize.height <- 2048.0
        moon.shadow.camera.left <- -80.0
        moon.shadow.camera.right <- 80.0
        moon.shadow.camera.top <- 80.0
        moon.shadow.camera.bottom <- -80.0
        moon.shadow.camera.near <- 0.5
        moon.shadow.camera.far <- 180.0
        scene.add(moon :> Object3D)

        let groundGeometry = PlaneGeometry(20000.0, 20000.0)
        let groundMaterial = createStandardMaterial (U2.Case1 GroundIron) 0.94 0.12
        let ground = Mesh(groundGeometry :> obj, groundMaterial :> obj)
        ground.rotation.x <- -Math.PI / 2.0
        ground.receiveShadow <- true
        scene.add(ground :> Object3D)

        let playerMesh = createPlayerMesh ()
        let player =
            { Position = createVector3 0.0 0.72 0.0
              MoveSpeed = DefaultMoveSpeed
              CurrentHP = DefaultPlayerHP
              MaxHP = DefaultPlayerHP
              Level = 1
              VelocityX = 0.0
              VelocityZ = 0.0 }
        playerMesh.position.copy(player.Position) |> ignore
        scene.add(playerMesh :> Object3D)

        let hud = createHud ()
        let joystick = createJoystick ()
        let input = { Keys = Set.empty<string>; Joystick = joystick; LastIntentX = 0.0; LastIntentZ = 0.0 }
        let clock = Clock()
        let hordeState = HordeEngine.createState ()
        let phase4State = Phase4.createState player.MaxHP
        let cloudState = Phase5.createCloudScoreState ()
        let loopControl : Phase4.GameLoopControl = { AnimationHandle = None; Paused = true }
        let mutable activeOrbs = Array.empty<UnholyOrb>
        let activeWeapons = ResizeArray<Phase4.WeaponCollider>()
        let activeShadowBolts = ResizeArray<ShadowBolt>()
        let activeAbilityEffects = ResizeArray<AbilityEffect>()
        let activeParticles = ResizeArray<Particle>()
        let mutable shadowBoltTimer = 0.0
        let mutable bloodNovaTimer = 3.8
        let mutable gravefireTimer = 5.6
        let enemyVisuals = Dictionary<int, Phase4.EnemyVisualState>()
        let mutable orbTimer = OrbSpawnInterval
        let mutable runScore = 0
        let mutable hasStarted = false
        let mutable gameOverShown = false
        let mutable renderFrame : float -> unit = ignore

        let syncWeapons () =
            activeWeapons.Clear()
            activeOrbs
            |> Array.iter (fun orb ->
                let damage = if Phase4.bloodAuraActive phase4State then orb.ContactDamage * 1.5 else orb.ContactDamage
                activeWeapons.Add(Phase4.createWeaponCollider (orb.Mesh) damage orb.ContactRadius))

        let spawnOrbs () =
            removeUnholyOrbs scene activeOrbs
            activeOrbs <-
                let phases = Array.init OrbCount (fun index -> float index * (Math.PI * 2.0 / float OrbCount))
                phases |> Array.map createUnholyOrb
            activeOrbs
            |> Array.iter (fun orb ->
                orb.Mesh.position.set(player.Position.x, player.Position.y + 0.55, player.Position.z) |> ignore
                scene.add(orb.Mesh :> Object3D))
            syncWeapons ()

        let enemyVisualState enemy =
            if enemyVisuals.ContainsKey enemy.Id then
                enemyVisuals[enemy.Id]
            else
                let color =
                    match enemy.EnemyType with
                    | SkeletonWarrior -> 0xC8C1C7
                    | BloodFiend -> 0x6E1025
                let baseMaterial = createStandardMaterial (U2.Case1 color) 0.78 0.22 :> obj
                let flashMaterial = createStandardMaterial (U2.Case1 0xFF2438) 0.42 0.36 :> obj
                let created = Phase4.createEnemyVisualState enemy baseMaterial flashMaterial
                enemyVisuals.Add(enemy.Id, created)
                created

        let activeEnemyVisuals () =
            HordeEngine.activeEnemies hordeState |> Array.map enemyVisualState

        let fireNearestEnemy () =
            let targets = activeEnemyVisuals () |> Array.filter (fun target -> target.Enemy.Health > 0.0)
            if targets.Length > 0 && activeShadowBolts.Count < 8 then
                let nearest =
                    targets
                    |> Array.minBy (fun target ->
                        let dx = target.Enemy.Mesh.position.x - player.Position.x
                        let dz = target.Enemy.Mesh.position.z - player.Position.z
                        dx * dx + dz * dz)
                activeShadowBolts.Add(createShadowBolt scene player nearest 28.0)

        let updateShadowBolts deltaSeconds =
            let mutable index = activeShadowBolts.Count - 1
            while index >= 0 do
                let bolt = activeShadowBolts[index]
                if not (updateShadowBolt deltaSeconds bolt) then
                    scene.remove(bolt.Mesh :> Object3D)
                    disposeCombatMesh bolt.Mesh
                    activeShadowBolts.RemoveAt(index)
                index <- index - 1
        let createAbilityMesh geometry color roughness metalness =
            let material = createStandardMaterial (U2.Case1 color) roughness metalness
            let mesh = Mesh(geometry, material :> obj)
            mesh.castShadow <- false
            mesh.receiveShadow <- false
            mesh
        let spawnParticleBurst (scene: Scene) (particles: ResizeArray<Particle>) (origin: Vector3) color count =
            let available = max 0 (48 - particles.Count)
            let burstCount = min count available
            for index in 0 .. burstCount - 1 do
                let angle = float index * (Math.PI * 2.0 / float (max 1 burstCount))
                let speed = 2.0 + float (index % 3) * 0.8
                let mesh = createAbilityMesh (SphereGeometry(0.08, 6, 5) :> obj) color 0.25 0.55
                mesh.position.set(origin.x, origin.y + 0.25, origin.z) |> ignore
                scene.add(mesh :> Object3D)
                particles.Add
                    { Mesh = mesh
                      Age = 0.0
                      Lifetime = 0.42 + float (index % 3) * 0.08
                      VelocityX = cos angle * speed
                      VelocityY = 1.2 + float (index % 2) * 0.6
                      VelocityZ = sin angle * speed }
        let spawnBloodNova (scene: Scene) (effects: ResizeArray<AbilityEffect>) (player: PlayerData) =
            if effects.Count < 12 then
                let mesh = createAbilityMesh (SphereGeometry(0.38, 16, 10) :> obj) 0xB92E5B 0.18 0.6
                mesh.position.copy(player.Position) |> ignore
                scene.add(mesh :> Object3D)
                effects.Add
                    { Mesh = mesh
                      Age = 0.0
                      Lifetime = 0.72
                      Kind = "blood-nova"
                      HasImpacted = false
                      Target = None }
        let spawnGravefire (scene: Scene) (effects: ResizeArray<AbilityEffect>) (target: Phase4.EnemyVisualState) =
            if effects.Count < 12 then
                let mesh = createAbilityMesh (BoxGeometry(0.42, 2.8, 0.42) :> obj) 0xF08A42 0.22 0.48
                mesh.position.set(target.Enemy.Mesh.position.x, target.Enemy.Mesh.position.y + 7.0, target.Enemy.Mesh.position.z) |> ignore
                scene.add(mesh :> Object3D)
                effects.Add
                    { Mesh = mesh
                      Age = 0.0
                      Lifetime = 0.78
                      Kind = "gravefire"
                      HasImpacted = false
                      Target = Some target }

        let updateParticles deltaSeconds (particles: ResizeArray<Particle>) =
            let mutable index = particles.Count - 1
            while index >= 0 do
                let particle = particles[index]
                particle.Age <- particle.Age + deltaSeconds
                if particle.Age >= particle.Lifetime then
                    scene.remove(particle.Mesh :> Object3D)
                    disposeCombatMesh particle.Mesh
                    particles.RemoveAt(index)
                else
                    particle.Mesh.position.x <- particle.Mesh.position.x + particle.VelocityX * deltaSeconds
                    particle.Mesh.position.y <- particle.Mesh.position.y + particle.VelocityY * deltaSeconds
                    particle.Mesh.position.z <- particle.Mesh.position.z + particle.VelocityZ * deltaSeconds
                    particle.Mesh.scale.set(1.0, 1.0, 1.0) |> ignore
                index <- index - 1
        let updateAbilityEffects deltaSeconds (player: PlayerData) (visuals: Phase4.EnemyVisualState array) (effects: ResizeArray<AbilityEffect>) (particles: ResizeArray<Particle>) =
            let mutable index = effects.Count - 1
            while index >= 0 do
                let effect = effects[index]
                effect.Age <- effect.Age + deltaSeconds
                match effect.Kind, effect.Target with
                | "blood-nova", _ ->
                    let progress = min 1.0 (effect.Age / effect.Lifetime)
                    let radius = 0.45 + progress * 5.8
                    effect.Mesh.position.copy(player.Position) |> ignore
                    effect.Mesh.scale.set(radius, 0.18 + progress * 0.3, radius) |> ignore
                    effect.Mesh.rotation.y <- effect.Mesh.rotation.y + deltaSeconds * 5.0
                    if not effect.HasImpacted && effect.Age >= 0.22 then
                        effect.HasImpacted <- true
                        visuals
                        |> Array.iter (fun enemy ->
                            let dx = enemy.Enemy.Mesh.position.x - player.Position.x
                            let dz = enemy.Enemy.Mesh.position.z - player.Position.z
                            if enemy.Enemy.Health > 0.0 && dx * dx + dz * dz <= radius * radius then
                                Phase4.hitEnemy 34.0 enemy)
                        spawnParticleBurst scene particles player.Position 0xE05A79 12
                | "gravefire", Some target ->
                    let progress = min 1.0 (effect.Age / 0.42)
                    effect.Mesh.position.x <- target.Enemy.Mesh.position.x
                    effect.Mesh.position.z <- target.Enemy.Mesh.position.z
                    effect.Mesh.position.y <- target.Enemy.Mesh.position.y + 7.0 * (1.0 - progress)
                    effect.Mesh.rotation.y <- effect.Mesh.rotation.y + deltaSeconds * 8.0
                    effect.Mesh.scale.set(1.0 + progress * 1.4, 1.0, 1.0 + progress * 1.4) |> ignore
                    if not effect.HasImpacted && effect.Age >= 0.42 then
                        effect.HasImpacted <- true
                        visuals
                        |> Array.iter (fun enemy ->
                            let dx = enemy.Enemy.Mesh.position.x - effect.Mesh.position.x
                            let dz = enemy.Enemy.Mesh.position.z - effect.Mesh.position.z
                            if enemy.Enemy.Health > 0.0 && dx * dx + dz * dz <= 3.2 * 3.2 then
                                Phase4.hitEnemy 28.0 enemy)
                        spawnParticleBurst scene particles effect.Mesh.position 0xFFB35C 10
                | _ -> ()
                if effect.Age >= effect.Lifetime then
                    scene.remove(effect.Mesh :> Object3D)
                    disposeCombatMesh effect.Mesh
                    effects.RemoveAt(index)
                index <- index - 1
            updateParticles deltaSeconds particles
        let pauseLoop () =
            loopControl.Paused <- true
            match loopControl.AnimationHandle with
            | Some handle ->
                window.cancelAnimationFrame(handle)
                loopControl.AnimationHandle <- None
            | None -> ()

        let resumeLoop () =
            if hasStarted && not gameOverShown then
                loopControl.Paused <- false
                loopControl.AnimationHandle <- Some (window.requestAnimationFrame(renderFrame))

        let resetRun () =
            Phase4.resetState scene phase4State
            HordeEngine.reset scene hordeState
            activeShadowBolts
            |> Seq.iter (fun bolt ->
                scene.remove(bolt.Mesh :> Object3D)
                disposeCombatMesh bolt.Mesh)
            activeShadowBolts.Clear()
            activeAbilityEffects
            |> Seq.iter (fun effect ->
                scene.remove(effect.Mesh :> Object3D)
                disposeCombatMesh effect.Mesh)
            activeAbilityEffects.Clear()
            activeParticles
            |> Seq.iter (fun particle ->
                scene.remove(particle.Mesh :> Object3D)
                disposeCombatMesh particle.Mesh)
            activeParticles.Clear()
            removeUnholyOrbs scene activeOrbs
            activeOrbs <- Array.empty
            activeWeapons.Clear()
            enemyVisuals.Clear()
            resetJoystick joystick
            phase4State.Paused <- false
            loopControl.Paused <- false
            gameOverShown <- false
            runScore <- 0
            orbTimer <- OrbSpawnInterval
            shadowBoltTimer <- 0.0
            bloodNovaTimer <- 3.8
            gravefireTimer <- 5.6
            player.CurrentHP <- player.MaxHP
            player.Position.set(0.0, 0.72, 0.0) |> ignore
            player.VelocityX <- 0.0
            player.VelocityZ <- 0.0
            input.LastIntentX <- 0.0
            input.LastIntentZ <- 0.0
            playerMesh.position.copy(player.Position) |> ignore
            spawnOrbs ()
            updateHud player phase4State runScore
            loopControl.AnimationHandle <- Some (window.requestAnimationFrame(renderFrame))

        let gameOverCallbacks : Phase5.GameOverCallbacks =
            { PauseLoop = pauseLoop
              RestartRun = resetRun
              RenderScore = fun score -> setHudText "hud-score" (string score) }

        let rec levelUpCallbacks : Phase4.LevelUpCallbacks =
            { PauseLoop = pauseLoop
              ResumeLoop = resumeLoop
              ApplyChoice = fun choice -> Phase4.selectChoice phase4State levelUpCallbacks choice }

        let showGameOverIfNeeded () =
            if not gameOverShown && phase4State.PlayerHP <= 0.0 then
                gameOverShown <- true
                Phase5.showGameOver cloudState gameOverCallbacks runScore |> ignore

        let rec startRun () =
            if not hasStarted then
                hasStarted <- true
                resetRun ()

        let startMenuCallbacks = { Phase5.StartMenuCallbacks.StartRun = startRun }
        Phase5.showStartMenu cloudState startMenuCallbacks |> ignore

        let keyDown (event: KeyboardEvent) =
            input.Keys <- input.Keys.Add event.key

        let keyUp (event: KeyboardEvent) =
            input.Keys <- input.Keys.Remove event.key

        renderFrame <- fun _timestamp ->
            if not loopControl.Paused && not gameOverShown then
                let deltaSeconds = clock.getDelta() |> min 0.05
                let elapsedSeconds = clock.getElapsedTime()
                movePlayer deltaSeconds input player playerMesh
                updateCamera deltaSeconds player camera

                orbTimer <- orbTimer - deltaSeconds
                if orbTimer <= 0.0 then
                    spawnOrbs ()
                    orbTimer <- OrbSpawnInterval

                activeOrbs
                |> Array.iter (fun orb ->
                    updateUnholyOrb (Phase4.orbSpeedMultiplier phase4State) deltaSeconds elapsedSeconds player orb |> ignore)

                shadowBoltTimer <- shadowBoltTimer - deltaSeconds
                if shadowBoltTimer <= 0.0 then
                    fireNearestEnemy ()
                    shadowBoltTimer <- 0.72
                updateShadowBolts deltaSeconds

                let enemyCountBefore = hordeState.Enemies.Count
                HordeEngine.tick deltaSeconds scene playerMesh hordeState
                runScore <- runScore + max 0 ((enemyCountBefore - hordeState.Enemies.Count) * 10)

                let visuals = activeEnemyVisuals ()
                bloodNovaTimer <- bloodNovaTimer - deltaSeconds
                if bloodNovaTimer <= 0.0 then
                    spawnBloodNova scene activeAbilityEffects player
                    bloodNovaTimer <- 7.2
                gravefireTimer <- gravefireTimer - deltaSeconds
                if gravefireTimer <= 0.0 then
                    visuals
                    |> Array.filter (fun target -> target.Enemy.Health > 0.0)
                    |> Array.tryFind (fun _ -> true)
                    |> Option.iter (spawnGravefire scene activeAbilityEffects)
                    gravefireTimer <- 9.5
                updateAbilityEffects deltaSeconds player visuals activeAbilityEffects activeParticles
                activeWeapons
                |> Seq.iter (fun weapon -> weapon.Damage <- if Phase4.bloodAuraActive phase4State then OrbContactDamage * 1.5 else OrbContactDamage)
                Phase4.tick deltaSeconds scene playerMesh 0.72 activeWeapons visuals phase4State loopControl levelUpCallbacks
                let enemiesBeforeCleanup = hordeState.Enemies.Count
                HordeEngine.cleanupDead scene hordeState
                runScore <- runScore + max 0 ((enemiesBeforeCleanup - hordeState.Enemies.Count) * 10)
                let liveEnemyIds = HordeEngine.activeEnemies hordeState |> Array.map (fun enemy -> enemy.Id) |> Set.ofArray
                enemyVisuals.Keys
                |> Seq.filter (fun enemyId -> not (liveEnemyIds.Contains enemyId))
                |> Seq.toArray
                |> Array.iter (fun enemyId ->
                    match enemyVisuals.TryGetValue enemyId with
                    | true, visual ->
                        Phase4.disposeEnemyVisualState visual
                        enemyVisuals.Remove enemyId |> ignore
                    | false, _ -> ())
                player.CurrentHP <- phase4State.PlayerHP
                player.Level <- phase4State.Level
                updateHud player phase4State runScore
                showGameOverIfNeeded ()
                renderer.render(scene, camera)

                if not loopControl.Paused && not gameOverShown then
                    loopControl.AnimationHandle <- Some (window.requestAnimationFrame(renderFrame))

        joystick.Base.addEventListener("pointerdown", fun event ->
            let pointer = event :?> PointerEvent
            joystick.Active <- true
            joystick.PointerId <- pointer.pointerId
            joystick.Base.setPointerCapture(pointer.pointerId)
            updateJoystick joystick pointer)
        joystick.Base.addEventListener("pointermove", fun event ->
            let pointer = event :?> PointerEvent
            if joystick.Active && pointer.pointerId = joystick.PointerId then
                updateJoystick joystick pointer)
        joystick.Base.addEventListener("pointerup", fun event ->
            let pointer = event :?> PointerEvent
            if pointer.pointerId = joystick.PointerId then resetJoystick joystick)
        joystick.Base.addEventListener("pointercancel", fun event ->
            let pointer = event :?> PointerEvent
            if pointer.pointerId = joystick.PointerId then resetJoystick joystick)
        window.addEventListener("keydown", fun event -> keyDown (event :?> KeyboardEvent))
        window.addEventListener("keyup", fun event -> keyUp (event :?> KeyboardEvent))
        window.addEventListener("resize", fun _ -> resizeScene camera renderer ())
        clock.start()
        renderer.render(scene, camera)
        scene, camera, renderer, hud

    [<EntryPoint>]
    let main _argv =
        initializeScene () |> ignore
        0
