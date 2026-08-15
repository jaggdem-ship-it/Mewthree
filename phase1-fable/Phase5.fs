namespace DarkFantasySurvivor

module Phase5 =
    open Browser.Dom
    open Browser.Types
    open Fable.Core
    open Puter

    [<Literal>]
    let private HighScoreKey = "diablo_high_score"

    type CloudScoreState =
        { mutable HighScore: int
          mutable Syncing: bool
          mutable Authenticated: bool }

    type GameOverCallbacks =
        { PauseLoop: unit -> unit
          RestartRun: unit -> unit
          RenderScore: int -> unit }

    type StartMenuCallbacks =
        { StartRun: unit -> unit }

    let createCloudScoreState () =
        { HighScore = 0
          Syncing = false
          Authenticated = false }

    let private setText (element: Element) (value: string) =
        element.textContent <- value

    let private safeElementById id =
        match document.getElementById(id) with
        | null -> None
        | element -> Some element

    let private createElement tag className id =
        let element = document.createElement(tag)
        element.className <- className
        element.id <- id
        element

    let private setOverlayStyle (element: HTMLElement) style =
        element.setAttribute("style", style)

    let private menuMarkup =
        "<div class=\"phase5-menu-kicker\">THE ASHEN VIGIL</div>" +
        "<h1>WRAITH OF THE BELLS</h1>" +
        "<p>Enter the crypt. Outlast the graveborn. Leave a mark in the reliquary.</p>" +
        "<div class=\"phase5-high-score\">PERSISTENT HIGH SCORE <strong id=\"phase5-high-score-value\">LOADING</strong></div>" +
        "<button id=\"phase5-start-button\">BREAK THE SEAL</button>"

    let private gameOverMarkup =
        "<div class=\"phase5-death-kicker\">THE BELL HAS SPOKEN</div>" +
        "<h1>YOU DIED</h1>" +
        "<p id=\"phase5-run-score\">RUN SCORE: 0</p>" +
        "<p id=\"phase5-best-score\">PERSISTENT BEST: SYNCING</p>" +
        "<button id=\"phase5-restart-button\">REKINDLE THE VIGIL</button>"

    let private attachStartHandler (menu: HTMLElement) (callbacks: StartMenuCallbacks) =
        match menu.querySelector("#phase5-start-button") with
        | null -> ()
        | startButton ->
            startButton.addEventListener("click", fun _ ->
                menu.remove()
                callbacks.StartRun())

    let private attachRestartHandler (overlay: HTMLElement) (callbacks: GameOverCallbacks) =
        match overlay.querySelector("#phase5-restart-button") with
        | null -> ()
        | restartButton ->
            restartButton.addEventListener("click", fun _ ->
                overlay.remove()
                callbacks.RestartRun())

    let private updateHighScoreText (score: int) =
        match safeElementById "phase5-high-score-value" with
        | Some element -> setText element (string score)
        | None -> ()

    let private updateAuthenticatedText authenticated =
        match safeElementById "phase5-high-score-value" with
        | Some element when not authenticated && element.textContent = "LOADING" -> setText element "LOCAL RUN"
        | _ -> ()

    let showStartMenu (state: CloudScoreState) callbacks =
        let menu = createElement "section" "phase5-start-menu" "phase5-start-menu"
        menu.innerHTML <- menuMarkup
        setOverlayStyle menu "position:fixed;inset:0;z-index:9000;display:grid;place-items:center;align-content:center;gap:14px;padding:32px;background:radial-gradient(circle at center,rgba(53,16,28,.82),rgba(4,3,6,.97) 68%);color:#e5d4b5;text-align:center;font-family:Georgia,serif;"
        document.body.appendChild(menu) |> ignore
        attachStartHandler menu callbacks
        Async.StartImmediate(async {
            let! score = Puter.loadHighScore HighScoreKey
            state.HighScore <- score
            updateHighScoreText score
        })
        menu

    let private syncScoreOnGameOver (state: CloudScoreState) (score: int) (overlay: HTMLElement) =
        state.Syncing <- true
        match overlay.querySelector("#phase5-best-score") with
        | null -> ()
        | element -> setText element "PERSISTENT BEST: SYNCING"
        Async.StartImmediate(async {
            let! best = Puter.syncHighScore HighScoreKey score
            state.HighScore <- max state.HighScore best
            state.Syncing <- false
            match overlay.querySelector("#phase5-best-score") with
            | null -> ()
            | element -> setText element (sprintf "PERSISTENT BEST: %d" state.HighScore)
        })

    let showGameOver (state: CloudScoreState) (callbacks: GameOverCallbacks) (score: int) =
        callbacks.PauseLoop()
        let overlay = createElement "section" "phase5-game-over" "phase5-game-over"
        overlay.innerHTML <- gameOverMarkup
        setOverlayStyle overlay "position:fixed;inset:0;z-index:10000;display:grid;place-items:center;align-content:center;gap:10px;padding:32px;background:radial-gradient(circle at center,rgba(42,5,13,.88),rgba(4,3,6,.98) 72%);color:#e5d4b5;text-align:center;font-family:Georgia,serif;"
        document.body.appendChild(overlay) |> ignore
        match overlay.querySelector("#phase5-run-score") with
        | null -> ()
        | element -> setText element (sprintf "RUN SCORE: %d" score)
        attachRestartHandler overlay callbacks
        syncScoreOnGameOver state score overlay
        overlay

    let loadInitialHighScore (state: CloudScoreState) (update: int -> unit) =
        Async.StartImmediate(async {
            let! score = Puter.loadHighScore HighScoreKey
            state.HighScore <- score
            update score
        })

    let syncFinalScore (state: CloudScoreState) (score: int) (update: int -> unit) =
        Async.StartImmediate(async {
            let! best = Puter.syncHighScore HighScoreKey score
            state.HighScore <- max state.HighScore best
            update state.HighScore
        })
