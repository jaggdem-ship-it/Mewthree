namespace DarkFantasySurvivor

module PuterBindings =
    open Fable.Core
    open Fable.Core.JsInterop

    [<AllowNullLiteral>]
    type PuterKv =
        abstract get: key: string -> JS.Promise<obj>
        abstract set: key: string * value: obj -> JS.Promise<unit>

    [<Global("puter.kv")>]
    let kv: PuterKv = jsNative

    let private hasPuter () =
        emitJsExpr () "typeof puter !== 'undefined' && puter.kv !== undefined"

    let saveRunSummary (key: string) (summary: obj) : JS.Promise<bool> =
        promise {
            if hasPuter () then
                do! kv.set(key, summary)
                return true
            else
                return false
        }

    let loadRunSummary (key: string) : JS.Promise<obj option> =
        promise {
            if hasPuter () then
                let! value = kv.get(key)
                return if isNull value then None else Some value
            else
                return None
        }
