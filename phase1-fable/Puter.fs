namespace DarkFantasySurvivor

module Puter =
    open Fable.Core
    open Fable.Core.JsInterop

    [<AllowNullLiteral>]
    type Auth =
        abstract signIn: unit -> JS.Promise<obj>
        abstract isSignedIn: unit -> bool
        abstract signOut: unit -> JS.Promise<obj>

    [<AllowNullLiteral>]
    type KeyValue =
        abstract get: key: string -> JS.Promise<obj>
        abstract set: key: string * value: obj -> JS.Promise<obj>

    [<Global("window.puter.auth")>]
    let auth: Auth = jsNative

    [<Global("window.puter.kv")>]
    let kv: KeyValue = jsNative

    [<Emit("typeof window !== 'undefined' && typeof window.puter !== 'undefined' && typeof window.puter.auth !== 'undefined' && typeof window.puter.kv !== 'undefined'")>]
    let isAvailable () : bool = jsNative

    [<Emit("Number($0 ?? 0)")>]
    let private toNumberOrZero (value: obj) : float = jsNative

    let ensureSignedIn () =
        async {
            if not (isAvailable ()) then
                return false
            elif auth.isSignedIn () then
                return true
            else
                do! auth.signIn () |> Async.AwaitPromise |> Async.Ignore
                return auth.isSignedIn ()
        }

    let readHighScore key =
        async {
            if not (isAvailable ()) then
                return 0
            else
                let! rawValue = kv.get key |> Async.AwaitPromise
                return toNumberOrZero rawValue |> max 0.0 |> int
        }

    let writeHighScore (key: string) (score: int) =
        async {
            if isAvailable () then
                do! kv.set(key, box score) |> Async.AwaitPromise |> Async.Ignore
            else
                return ()
        }

    let loadHighScore key =
        async {
            try
                let! signedIn = ensureSignedIn ()
                if signedIn then
                    return! readHighScore key
                else
                    return 0
            with error ->
                return 0
        }

    let syncHighScore (key: string) (currentScore: int) =
        async {
            try
                let! signedIn = ensureSignedIn ()
                if not signedIn then
                    return currentScore
                else
                    let! previousBest = readHighScore key
                    if currentScore > previousBest then
                        do! writeHighScore key currentScore
                        return currentScore
                    else
                        return previousBest
            with error ->
                return currentScore
        }
