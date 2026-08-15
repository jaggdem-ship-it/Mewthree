namespace DarkFantasySurvivor

module App =
    open System
    open Browser.Dom
    open Browser.Types
    open Fable.Core
    open Three

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
          CurrentHP: float
          MaxHP: float
          Level: int }

    type Damageable =
        { Mesh: Mesh
          mutable CurrentHP: float
          MaxHP: float }

    type UnholyOrb =
        { Mesh: Mesh
          OrbitPhase: float
          OrbitRadius: float
          AngularSpeed: float
          ContactDamage: float
          ContactRadius: float }

    type InputState =
        { mutable Keys: Set<string> }

    let private clamp minimum maximum value =
        value |> max minimum |> min maximum

    let private lengthSquared (value: Vector3) =
        value.x * value.x + value.y * value.y + value.z * value.z

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

    let private isKeyDown (input: InputState) key =
        input.Keys.Contains key

    let private keyboardDirection (input: InputState) =
        let right = isKeyDown input "d" || isKeyDown input "ArrowRight"
        let left = isKeyDown input "a" || isKeyDown input "ArrowLeft"
        let down = isKeyDown input "s" || isKeyDown input "ArrowDown"
        let up = isKeyDown input "w" || isKeyDown input "ArrowUp"
        let horizontal = (if right then 1.0 else 0.0) - (if left then 1.0 else 0.0)
        let vertical = (if down then 1.0 else 0.0) - (if up then 1.0 else 0.0)
        createVector3 horizontal 0.0 vertical |> normalizeHorizontal

    let private movePlayer deltaSeconds (input: InputState) (player: PlayerData) (playerMesh: Mesh) =
        let direction = keyboardDirection input
        let distance = player.MoveSpeed * deltaSeconds
        let nextX = player.Position.x + direction.x * distance
        let nextZ = player.Position.z + direction.z * distance
        player.Position.set(nextX, player.Position.y, nextZ) |> ignore
        playerMesh.position.copy(player.Position) |> ignore
        playerMesh.rotation.y <- atan2 direction.x direction.z

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
        let orbs = phases |> Array.map createUnholyOrb
        orbs
        |> Array.iter (fun orb ->
            orb.Mesh.position.set(player.Position.x, player.Position.y + 0.55, player.Position.z) |> ignore
            scene.add(orb.Mesh :> Object3D))
        orbs

    let private removeUnholyOrbs (scene: Scene) (orbs: UnholyOrb array) =
        orbs |> Array.iter (fun orb -> scene.remove(orb.Mesh :> Object3D))

    let private updateUnholyOrb (deltaSeconds: float) (elapsedSeconds: float) (player: PlayerData) (orb: UnholyOrb) =
        let angle = elapsedSeconds * orb.AngularSpeed + orb.OrbitPhase
        let orbitX = player.Position.x + cos angle * orb.OrbitRadius
        let orbitZ = player.Position.z + sin angle * orb.OrbitRadius
        orb.Mesh.position.set(orbitX, player.Position.y + 0.55, orbitZ) |> ignore
        orb.Mesh.rotation.x <- orb.Mesh.rotation.x + deltaSeconds * 3.0
        orb.Mesh.rotation.y <- orb.Mesh.rotation.y + deltaSeconds * 4.5
        orb

    let private applyContactDamage (orb: UnholyOrb) (damageables: ResizeArray<Damageable>) =
        damageables
        |> Seq.iter (fun target ->
            let deltaX = target.Mesh.position.x - orb.Mesh.position.x
            let deltaZ = target.Mesh.position.z - orb.Mesh.position.z
            let distanceSquared = deltaX * deltaX + deltaZ * deltaZ
            let contactDistance = orb.ContactRadius + 0.75
            if distanceSquared <= contactDistance * contactDistance then
                target.CurrentHP <- max 0.0 (target.CurrentHP - orb.ContactDamage))

    let RegisterDamageable (target: Damageable) (damageables: ResizeArray<Damageable>) =
        if not (damageables.Contains target) then
            damageables.Add target

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
        renderer.setPixelRatio(min 2.0 window.devicePixelRatio)

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
        let initialLookTarget = createVector3 0.0 0.0 0.0
        camera.lookAt(initialLookTarget)

        let renderer = createRenderer (box view)
        renderer.setPixelRatio(min 2.0 window.devicePixelRatio)
        renderer.setSize(width, height, true)
        renderer.shadowMap.enabled <- true
        renderer.shadowMap.``type`` <- 2

        let ambient = AmbientLight(U2.Case1 DarkCrimson, 0.32)
        scene.add(ambient :> Object3D)

        let moon = DirectionalLight(U2.Case1 Moonlight, 2.4)
        moon.position.set(-14.0, 26.0, 10.0) |> ignore
        moon.castShadow <- true
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
              Level = 1 }
        playerMesh.position.copy(player.Position) |> ignore
        scene.add(playerMesh :> Object3D)

        let damageables = ResizeArray<Damageable>()
        let mutable activeOrbs = Array.empty<UnholyOrb>
        let mutable orbTimer = OrbSpawnInterval
        let input = { Keys = Set.empty<string> }

        let keyDown (event: KeyboardEvent) =
            let key = event.key
            input.Keys <- input.Keys.Add key

        let keyUp (event: KeyboardEvent) =
            let key = event.key
            input.Keys <- input.Keys.Remove key

        let clock = Clock()

        let rec renderFrame (_timestamp: float) =
            let deltaSeconds = clock.getDelta() |> min 0.05
            let elapsedSeconds = clock.getElapsedTime()
            let spawnResult =
                orbTimer <- orbTimer - deltaSeconds
                if orbTimer <= 0.0 then
                    removeUnholyOrbs scene activeOrbs
                    activeOrbs <- spawnUnholyOrbs scene player
                    orbTimer <- OrbSpawnInterval
                ()
            movePlayer deltaSeconds input player playerMesh
            updateCamera deltaSeconds player camera
            activeOrbs
            |> Array.map (updateUnholyOrb deltaSeconds elapsedSeconds player)
            |> Array.iter (fun orb -> applyContactDamage orb damageables)
            renderer.render(scene, camera)
            window.requestAnimationFrame(renderFrame) |> ignore

        window.addEventListener("keydown", fun event -> keyDown (event :?> KeyboardEvent))
        window.addEventListener("keyup", fun event -> keyUp (event :?> KeyboardEvent))
        window.addEventListener("resize", fun _ -> resizeScene camera renderer ())
        clock.start()
        activeOrbs <- spawnUnholyOrbs scene player
        window.requestAnimationFrame(renderFrame) |> ignore
        scene, camera, renderer

    [<EntryPoint>]
    let main _argv =
        initializeScene () |> ignore
        0
