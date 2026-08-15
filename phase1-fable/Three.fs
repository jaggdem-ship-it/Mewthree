namespace DarkFantasySurvivor

module Three =
    open Fable.Core
    open Fable.Core.JsInterop

    [<AllowNullLiteral; Global("THREE.Vector3")>]
    type Vector3(x: float, y: float, z: float) =
        member _.x
            with get(): float = jsNative
            and set(_: float): unit = jsNative
        member _.y
            with get(): float = jsNative
            and set(_: float): unit = jsNative
        member _.z
            with get(): float = jsNative
            and set(_: float): unit = jsNative
        member _.set(x: float, y: float, z: float): Vector3 = jsNative
        member _.copy(value: Vector3): Vector3 = jsNative
        member _.add(value: Vector3): Vector3 = jsNative
        member _.sub(value: Vector3): Vector3 = jsNative
        member _.multiplyScalar(value: float): Vector3 = jsNative
        member _.length(): float = jsNative
        member _.normalize(): Vector3 = jsNative

    [<AllowNullLiteral>]
    type Object3D =
        abstract position: Vector3 with get
        abstract rotation: Vector3 with get
        abstract scale: Vector3 with get
        abstract castShadow: bool with get, set
        abstract receiveShadow: bool with get, set
        abstract add: child: Object3D -> unit
        abstract remove: child: Object3D -> unit

    [<AllowNullLiteral; Global("THREE.Scene")>]
    type Scene() =
        member _.fog
            with get(): obj = jsNative
            and set(_: obj): unit = jsNative
        member _.background
            with get(): obj = jsNative
            and set(_: obj): unit = jsNative
        member _.add(child: Object3D): unit = jsNative
        member _.remove(child: Object3D): unit = jsNative
    
    [<AllowNullLiteral; Global("THREE.PerspectiveCamera")>]
    type PerspectiveCamera(fieldOfView: float, aspect: float, nearClip: float, farClip: float) =
        member _.position: Vector3 = jsNative
        member _.lookAt(target: Vector3): unit = jsNative
        member _.aspect
            with get(): float = jsNative
            and set(_: float): unit = jsNative
        member _.updateProjectionMatrix(): unit = jsNative

    [<AllowNullLiteral; Global("THREE.WebGLRenderer")>]
    type WebGLRenderer(parameters: obj) =
        member _.domElement: obj = jsNative
        member _.shadowMap: ShadowMap = jsNative
        member _.setPixelRatio(value: float): unit = jsNative
        member _.setSize(width: float, height: float, ?updateStyle: bool): unit = jsNative
        member _.render(scene: Scene, camera: PerspectiveCamera): unit = jsNative
        member _.dispose(): unit = jsNative

    and [<AllowNullLiteral>]
        ShadowMap =
        abstract enabled: bool with get, set
        abstract ``type``: int with get, set

    [<AllowNullLiteral; Global("THREE.Clock")>]
    type Clock() =
        member _.getDelta(): float = jsNative
        member _.getElapsedTime(): float = jsNative
        member _.start(): unit = jsNative
        member _.stop(): unit = jsNative

    [<AllowNullLiteral; Global("THREE.AmbientLight")>]
    type AmbientLight(color: U2<int, string>, intensity: float) =
        interface Object3D with
            member _.position = Vector3(0.0, 0.0, 0.0)
            member _.rotation = Vector3(0.0, 0.0, 0.0)
            member _.scale = Vector3(1.0, 1.0, 1.0)
            member _.castShadow with get() = false and set _ = ()
            member _.receiveShadow with get() = false and set _ = ()
            member _.add _ = ()
            member _.remove _ = ()

    [<AllowNullLiteral; Global("THREE.DirectionalLight")>]
    type DirectionalLight(color: U2<int, string>, intensity: float) =
        member _.position: Vector3 = jsNative
        member _.castShadow
            with get(): bool = jsNative
            and set(_: bool): unit = jsNative
        member _.shadow: DirectionalLightShadow = jsNative
        member _.target: Object3D = jsNative
        interface Object3D with
            member _.position = Vector3(0.0, 0.0, 0.0)
            member _.rotation = Vector3(0.0, 0.0, 0.0)
            member _.scale = Vector3(1.0, 1.0, 1.0)
            member _.castShadow with get() = false and set _ = ()
            member _.receiveShadow with get() = false and set _ = ()
            member _.add _ = ()
            member _.remove _ = ()

    and [<AllowNullLiteral>]
        DirectionalLightShadow =
        abstract mapSize: ShadowMapSize with get
        abstract camera: OrthographicShadowCamera with get

    and [<AllowNullLiteral>]
        ShadowMapSize =
        abstract width: float with get, set
        abstract height: float with get, set

    and [<AllowNullLiteral>]
        OrthographicShadowCamera =
        abstract left: float with get, set
        abstract right: float with get, set
        abstract top: float with get, set
        abstract bottom: float with get, set
        abstract near: float with get, set
        abstract far: float with get, set

    [<AllowNullLiteral; Global("THREE.BoxGeometry")>]
    type BoxGeometry(width: float, height: float, depth: float) =
        interface Object3D with
            member _.position = Vector3(0.0, 0.0, 0.0)
            member _.rotation = Vector3(0.0, 0.0, 0.0)
            member _.scale = Vector3(1.0, 1.0, 1.0)
            member _.castShadow with get() = false and set _ = ()
            member _.receiveShadow with get() = false and set _ = ()
            member _.add _ = ()
            member _.remove _ = ()

    [<AllowNullLiteral; Global("THREE.SphereGeometry")>]
    type SphereGeometry(radius: float, widthSegments: int, heightSegments: int) =
        interface Object3D with
            member _.position = Vector3(0.0, 0.0, 0.0)
            member _.rotation = Vector3(0.0, 0.0, 0.0)
            member _.scale = Vector3(1.0, 1.0, 1.0)
            member _.castShadow with get() = false and set _ = ()
            member _.receiveShadow with get() = false and set _ = ()
            member _.add _ = ()
            member _.remove _ = ()

    [<AllowNullLiteral; Global("THREE.PlaneGeometry")>]
    type PlaneGeometry(width: float, height: float) =
        interface Object3D with
            member _.position = Vector3(0.0, 0.0, 0.0)
            member _.rotation = Vector3(0.0, 0.0, 0.0)
            member _.scale = Vector3(1.0, 1.0, 1.0)
            member _.castShadow with get() = false and set _ = ()
            member _.receiveShadow with get() = false and set _ = ()
            member _.add _ = ()
            member _.remove _ = ()

    [<AllowNullLiteral; Global("THREE.MeshStandardMaterial")>]
    type MeshStandardMaterial(parameters: obj) =
        interface Object3D with
            member _.position = Vector3(0.0, 0.0, 0.0)
            member _.rotation = Vector3(0.0, 0.0, 0.0)
            member _.scale = Vector3(1.0, 1.0, 1.0)
            member _.castShadow with get() = false and set _ = ()
            member _.receiveShadow with get() = false and set _ = ()
            member _.add _ = ()
            member _.remove _ = ()

    [<AllowNullLiteral; Global("THREE.Mesh")>]
    type Mesh(geometry: obj, material: obj) =
        member _.position: Vector3 = jsNative
        member _.rotation: Vector3 = jsNative
        member _.castShadow
            with get(): bool = jsNative
            and set(_: bool): unit = jsNative
        member _.receiveShadow
            with get(): bool = jsNative
            and set(_: bool): unit = jsNative
        member _.add(child: Object3D): unit = jsNative
        [<Emit("$this.material = $0")>]
        member _.SetMaterial(material: obj): unit = jsNative
        interface Object3D with
            member _.position = Vector3(0.0, 0.0, 0.0)
            member _.rotation = Vector3(0.0, 0.0, 0.0)
            member _.scale = Vector3(1.0, 1.0, 1.0)
            member _.castShadow with get() = false and set _ = ()
            member _.receiveShadow with get() = false and set _ = ()
            member _.add _ = ()
            member _.remove _ = ()

    [<AllowNullLiteral; Global("THREE.FogExp2")>]
    type FogExp2(color: U2<int, string>, density: float) =
        interface Object3D with
            member _.position = Vector3(0.0, 0.0, 0.0)
            member _.rotation = Vector3(0.0, 0.0, 0.0)
            member _.scale = Vector3(1.0, 1.0, 1.0)
            member _.castShadow with get() = false and set _ = ()
            member _.receiveShadow with get() = false and set _ = ()
            member _.add _ = ()
            member _.remove _ = ()

    [<Emit("new THREE.MeshStandardMaterial({ color: $0, roughness: $1, metalness: $2 })")>]
    let createStandardMaterial (color: U2<int, string>) (roughness: float) (metalness: float): MeshStandardMaterial = jsNative

    [<Emit("new THREE.WebGLRenderer({ canvas: $0, antialias: true, alpha: false })")>]
    let createRenderer (canvas: obj): WebGLRenderer = jsNative

    [<Emit("new THREE.Vector3($0, $1, $2)")>]
    let createVector3 (x: float) (y: float) (z: float): Vector3 = jsNative
