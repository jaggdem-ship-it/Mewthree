/* Black-Iron Reliquary: React is the picture frame; Three.js owns the canvas and simulation. */
import { useEffect, useRef } from "react";
import { createGameRuntime, type GameRuntime } from "@/game/threeRuntime";
import { createInputController } from "@/game/input";

export type GameCanvasProps = { demo: boolean; onRuntime: (runtime: GameRuntime) => void; onSnapshot: (snapshot: ReturnType<GameRuntime["snapshot"]>) => void };

export default function GameCanvas({ demo, onRuntime, onSnapshot }: GameCanvasProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const runtimeRef = useRef<GameRuntime | undefined>(undefined);
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const runtime = createGameRuntime(onSnapshot, demo);
    const input = createInputController((vector) => runtime.setInput(vector));
    runtime.mount(canvas); input.start(); runtimeRef.current = runtime; onRuntime(runtime);
    return () => { input.stop(); runtime.dispose(); runtimeRef.current = undefined; };
  }, [demo, onRuntime, onSnapshot]);
  return <canvas ref={canvasRef} className="game-canvas" aria-label="Dark Fantasy Survivor arena" />;
}
