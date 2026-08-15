namespace DarkFantasySurvivor

module ThreeBindings =
    open Fable.Core
    open Fable.Core.JsInterop

    [<AllowNullLiteral>]
    type Vector2(x: float, y: float) =
        member _.x with get(): float = jsNative and set(_: float): unit = jsNative
        member _.y with get(): float = jsNative and set(_: float): unit = jsNative
        member _.length(): float = jsNative
        member _.normalize(): Vector2 = jsNative

    [<AllowNullLiteral>]
    type Object3D() =
        member _.position with get(): Vector2 = jsNative
        member _.visible with get(): bool = jsNative and set(_: bool): unit = jsNative
        member _.rotationY with get(): float = jsNative and set(_: float): unit = jsNative
        member _.add(child: Object3D): unit = jsNative
        member _.remove(child: Object3D): unit = jsNative

    [<AllowNullLiteral>]
    type Scene() =
        inherit Object3D()
        member _.clear(): unit = jsNative

    [<AllowNullLiteral>]
    type OrthographicCamera() =
        inherit Object3D()
        member _.lookAt(x: float, y: float, z: float): unit = jsNative
        member _.updateProjectionMatrix(): unit = jsNative

    [<AllowNullLiteral>]
    type WebGLRenderer(canvas: obj, antialias: bool) =
        member _.setPixelRatio(value: float): unit = jsNative
        member _.setSize(width: float, height: float): unit = jsNative
        member _.render(scene: Scene, camera: OrthographicCamera): unit = jsNative
        member _.dispose(): unit = jsNative

    type PointerSpace = { viewportWidth: float; viewportHeight: float }
    type ArenaVector = { x: float; y: float }

    let normalizePointer (space: PointerSpace) (clientX: float) (clientY: float) (originX: float) (originY: float) : ArenaVector =
        let radius = max 48.0 (min space.viewportWidth space.viewportHeight * 0.2)
        let x = (clientX - originX) / radius
        let y = (clientY - originY) / radius
        let length = sqrt (x * x + y * y)
        if length > 1.0 then { x = x / length; y = y / length } else { x = x; y = y }

    type Pool<'T when 'T : not struct> =
        { items: ResizeArray<'T>
          acquire: unit -> 'T
          release: 'T -> unit }

    let createPool (factory: unit -> 'T) (initialSize: int) : Pool<'T> =
        let items = ResizeArray<'T>(initialSize)
        for _ in 1 .. initialSize do items.Add(factory())
        let acquire () =
            if items.Count > 0 then let item = items[items.Count - 1] in items.RemoveAt(items.Count - 1); item else factory()
        let release item = items.Add(item)
        { items = items; acquire = acquire; release = release }
