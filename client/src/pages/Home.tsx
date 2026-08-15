/* Black-Iron Reliquary: HUD as marginalia around a dominant battlefield, with ritualized progression. */
import { useCallback, useMemo, useState } from "react";
import GameCanvas from "@/components/GameCanvas";
import type { GameRuntime, RuntimeSnapshot } from "@/game/threeRuntime";

const initial: RuntimeSnapshot = { elapsed: 0, wave: 1, hp: 100, maxHp: 100, xp: 0, xpToNext: 8, level: 1, kills: 0, enemyCount: 0, gameOver: false, paused: false, pendingLevelUp: false, cards: [] };
const timeLabel = (seconds: number) => `${String(Math.floor(seconds / 60)).padStart(2, "0")}:${String(Math.floor(seconds % 60)).padStart(2, "0")}`;

export default function Home() {
  const demo = useMemo(() => new URLSearchParams(window.location.search).has("demo"), []);
  const [runtime, setRuntime] = useState<GameRuntime | undefined>();
  const [snapshot, setSnapshot] = useState<RuntimeSnapshot>(initial);
  const onRuntime = useCallback((next: GameRuntime) => setRuntime(next), []);
  const onSnapshot = useCallback((next: RuntimeSnapshot) => setSnapshot(next), []);
  const health = Math.max(0, snapshot.hp / snapshot.maxHp) * 100;
  const xp = Math.min(100, snapshot.xp / snapshot.xpToNext * 100);
  return <main className="game-shell">
    <GameCanvas demo={demo} onRuntime={onRuntime} onSnapshot={onSnapshot} />
    <div className="vignette" aria-hidden="true" />
    <header className="hud hud-top-left">
      <div className="sigil-frame"><span className="brand-mark" aria-hidden="true"><i /></span><span className="wordmark">WRAITH<br /><em>OF THE BELLS</em></span></div>
      <div className="meter-block"><div className="meter-label"><span>VITALITY</span><b>{Math.ceil(snapshot.hp)} / {snapshot.maxHp}</b></div><div className="meter meter-health"><i style={{ width: `${health}%` }} /></div></div>
      <div className="meter-block"><div className="meter-label"><span>EXPERIENCE · RITE {snapshot.level}</span><b>{snapshot.xp} / {snapshot.xpToNext}</b></div><div className="meter meter-xp"><i style={{ width: `${xp}%` }} /></div></div>
    </header>
    <aside className="hud hud-top-right"><div className="run-stat"><span>THE HOUR</span><strong>{timeLabel(snapshot.elapsed)}</strong></div><div className="run-stat"><span>WAVE</span><strong>{String(snapshot.wave).padStart(2, "0")}</strong></div><div className="run-stat"><span>TAKEN</span><strong>{String(snapshot.kills).padStart(3, "0")}</strong></div></aside>
    <div className="hud-bottom"><div className="relic-caption">RELIQUARY OF THE<br /><b>ASHEN VIGIL</b></div><div className="relic-slots"><span className="relic-slot active">✦<small>EMBER MACE</small></span><span className="relic-slot">◇<small>EMPTY RELIC</small></span><span className="relic-slot">⊙<small>EMPTY RELIC</small></span></div><button className="seal-button" onClick={() => snapshot.paused ? runtime?.resume() : runtime?.pause()} aria-label="Pause or resume">{snapshot.paused ? "RESUME" : "PAUSE"}</button></div>
    <div className="control-hint"><span className="keycap">WASD</span> or <span className="keycap">DRAG</span> TO WALK <i /> AUTO-ATTACKS ARE VOW-BOUND</div>
    {(snapshot.pendingLevelUp || snapshot.gameOver) && <div className="ritual-overlay"><section className="ritual-panel">{snapshot.gameOver ? <><div className="eyebrow">THE BELL HAS SPOKEN</div><h1>YOUR VIGIL ENDS</h1><p>{snapshot.kills} graveborn silenced in {timeLabel(snapshot.elapsed)}.</p><button className="ritual-action" onClick={() => runtime?.restart()}>REKINDLE THE VIGIL</button></> : <><div className="eyebrow">A RELIC ANSWERS</div><h1>CHOOSE YOUR VOW</h1><p>The crypt pauses. Take one gift into the dark.</p><div className="card-row">{snapshot.cards.map(card => <button key={card.id} className="upgrade-card" onClick={() => runtime?.chooseUpgrade(card.id)}><span className="card-glyph">{card.glyph}</span><strong>{card.title}</strong><small>{card.detail}</small><em>ACCEPT VOW</em></button>)}</div></>}</section></div>}
  </main>;
}
