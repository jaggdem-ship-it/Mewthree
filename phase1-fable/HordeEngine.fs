namespace DarkFantasySurvivor

module HordeEngine =
    open System
    open System.Collections.Generic
    open Fable.Core
    open Three

    [<Literal>]
    let private SpawnIntervalSeconds = 2.2

    [<Literal>]
    let private InitialSpawnDelaySeconds = 0.75

    [<Literal>]
    let private MaximumActiveEnemies = 110

    [<Literal>]
    let private MinimumClusterSize = 5

    [<Literal>]
    let private MaximumClusterSizeExclusive = 11

    [<Literal>]
    let private MinimumSpawnRadius = 18.0

    [<Literal>]
    let private SpawnRadiusVariance = 8.0

    [<Literal>]
    let private SkeletonWarriorHealth = 42.0

    [<Literal>]
    let private SkeletonWarriorSpeed = 2.35

    [<Literal>]
    let private BloodFiendHealth = 86.0

    [<Literal>]
    let private BloodFiendSpeed = 1.45

    [<Literal>]
    let private SkeletonColor = 0xC8C1C7

    [<Literal>]
    let private BloodFiendColor = 0x6E1025

    type EnemyType =
        | SkeletonWarrior
        | BloodFiend

    type Enemy =
        { Id: int
          EnemyType: EnemyType
          Mesh: Mesh
          mutable Health: float
          Speed: float }

    type HordeState =
        { Enemies: ResizeArray<Enemy>
          Random: Random
          mutable SpawnTimer: float
          mutable NextEnemyId: int }

    let createState () =
        { Enemies = ResizeArray<Enemy>()
          Random = Random()
          SpawnTimer = InitialSpawnDelaySeconds
          NextEnemyId = 1 }

    let private stats enemyType =
        match enemyType with
        | SkeletonWarrior -> SkeletonWarriorHealth, SkeletonWarriorSpeed
        | BloodFiend -> BloodFiendHealth, BloodFiendSpeed

    let private chooseEnemyType (random: Random) =
        if random.NextDouble() < 0.72 then SkeletonWarrior else BloodFiend

    let private materialFor enemyType =
        let color =
            match enemyType with
            | SkeletonWarrior -> SkeletonColor
            | BloodFiend -> BloodFiendColor
        createStandardMaterial (U2.Case1 color) 0.78 0.22

    let private meshFor enemyType =
        let geometry: obj =
            match enemyType with
            | SkeletonWarrior -> BoxGeometry(0.9, 1.7, 0.9) :> obj
            | BloodFiend -> SphereGeometry(0.82, 10, 8) :> obj
        let material = materialFor enemyType
        let mesh = Mesh(geometry, material :> obj)
        mesh.castShadow <- true
        mesh.receiveShadow <- true
        mesh

    [<Emit("$0.geometry.dispose(); $0.material.dispose(); $0.removeFromParent();")>]
    let private disposeMesh (mesh: Mesh) : unit = jsNative

    let private spawnEnemyAt (scene: Scene) (state: HordeState) (playerMesh: Mesh) angle radius =
        let enemyType = chooseEnemyType state.Random
        let health, speed = stats enemyType
        let mesh = meshFor enemyType
        let spawnX = playerMesh.position.x + cos angle * radius
        let spawnZ = playerMesh.position.z + sin angle * radius
        mesh.position.set(spawnX, playerMesh.position.y + 0.85, spawnZ) |> ignore
        scene.add(mesh :> Object3D)
        let enemy =
            { Id = state.NextEnemyId
              EnemyType = enemyType
              Mesh = mesh
              Health = health
              Speed = speed }
        state.NextEnemyId <- state.NextEnemyId + 1
        state.Enemies.Add enemy

    let private spawnCluster (scene: Scene) (state: HordeState) (playerMesh: Mesh) =
        let requestedSize = state.Random.Next(MinimumClusterSize, MaximumClusterSizeExclusive)
        let clusterSize = min requestedSize (max 0 (MaximumActiveEnemies - state.Enemies.Count))
        let angleOffset = state.Random.NextDouble() * Math.PI * 2.0
        [ 0 .. clusterSize - 1 ]
        |> List.iter (fun index ->
            let angle = angleOffset + float index * (Math.PI * 2.0 / float clusterSize)
            let radius = MinimumSpawnRadius + state.Random.NextDouble() * SpawnRadiusVariance
            spawnEnemyAt scene state playerMesh angle radius)

    let private pursuePlayer deltaSeconds (playerMesh: Mesh) (enemy: Enemy) =
        let direction =
            createVector3
                (playerMesh.position.x - enemy.Mesh.position.x)
                0.0
                (playerMesh.position.z - enemy.Mesh.position.z)
        let distanceSquared = direction.x * direction.x + direction.z * direction.z
        if enemy.Health > 0.0 && distanceSquared > 0.0001 then
            direction.normalize() |> ignore
            let travelDistance = enemy.Speed * deltaSeconds
            enemy.Mesh.position.x <- enemy.Mesh.position.x + direction.x * travelDistance
            enemy.Mesh.position.z <- enemy.Mesh.position.z + direction.z * travelDistance
            enemy.Mesh.rotation.y <- atan2 direction.x direction.z

    let private cleanupDeadEnemies (scene: Scene) (state: HordeState) =
        let mutable index = state.Enemies.Count - 1
        while index >= 0 do
            let enemy = state.Enemies[index]
            if enemy.Health <= 0.0 then
                scene.remove(enemy.Mesh :> Object3D)
                disposeMesh enemy.Mesh
                state.Enemies.RemoveAt(index)
            index <- index - 1

    let damageEnemy amount (enemy: Enemy) =
        enemy.Health <- max 0.0 (enemy.Health - max 0.0 amount)

    let cleanupDead (scene: Scene) (state: HordeState) =
        cleanupDeadEnemies scene state

    [<Emit("$0.geometry.dispose(); $0.material.dispose(); $0.removeFromParent();")>]
    let private disposeResetMesh (mesh: Mesh) : unit = jsNative

    let reset (scene: Scene) (state: HordeState) =
        state.Enemies
        |> Seq.iter (fun enemy ->
            scene.remove(enemy.Mesh :> Object3D)
            disposeResetMesh enemy.Mesh)
        state.Enemies.Clear()
        state.SpawnTimer <- InitialSpawnDelaySeconds
        state.NextEnemyId <- 1

    let tick deltaSeconds (scene: Scene) (playerMesh: Mesh) (state: HordeState) =
        let safeDeltaSeconds = deltaSeconds |> max 0.0 |> min 0.1
        state.SpawnTimer <- state.SpawnTimer - safeDeltaSeconds
        if state.SpawnTimer <= 0.0 then
            spawnCluster scene state playerMesh
            state.SpawnTimer <- SpawnIntervalSeconds

        state.Enemies
        |> Seq.iter (pursuePlayer safeDeltaSeconds playerMesh)

        ()

    let activeEnemies (state: HordeState) =
        state.Enemies |> Seq.toArray
