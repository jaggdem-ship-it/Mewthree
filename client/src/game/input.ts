/* Black-Iron Reliquary: 2D viewport input is normalized before crossing into the 3D arena plane. */
import type { Vec2 } from "./simulation";

export type InputController = { start: () => void; stop: () => void; vector: () => Vec2 };

export function createInputController(onInput: (vector: Vec2) => void): InputController {
  const keys = new Set<string>(); let pointer: { id: number; x: number; y: number } | undefined; let current: Vec2 = { x: 0, y: 0 };
  const normalize = (x: number, y: number): Vec2 => { const magnitude = Math.hypot(x, y); return magnitude > 1 ? { x: x / magnitude, y: y / magnitude } : { x, y }; };
  const keyboard = () => { const x = (keys.has("ArrowRight") || keys.has("d") ? 1 : 0) - (keys.has("ArrowLeft") || keys.has("a") ? 1 : 0); const y = (keys.has("ArrowDown") || keys.has("s") ? 1 : 0) - (keys.has("ArrowUp") || keys.has("w") ? 1 : 0); current = normalize(x, y); onInput(current); };
  const down = (event: KeyboardEvent) => { keys.add(event.key); keyboard(); };
  const up = (event: KeyboardEvent) => { keys.delete(event.key); keyboard(); };
  const pointerDown = (event: PointerEvent) => { pointer = { id: event.pointerId, x: event.clientX, y: event.clientY }; (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId); };
  const pointerMove = (event: PointerEvent) => { if (!pointer || pointer.id !== event.pointerId) return; const dx = (event.clientX - pointer.x) / Math.max(48, Math.min(window.innerWidth, window.innerHeight) * 0.2); const dy = (event.clientY - pointer.y) / Math.max(48, Math.min(window.innerWidth, window.innerHeight) * 0.2); current = normalize(dx, dy); onInput(current); };
  const pointerUp = (event: PointerEvent) => { if (pointer?.id === event.pointerId) { pointer = undefined; current = { x: 0, y: 0 }; onInput(current); } };
  const start = () => { window.addEventListener("keydown", down); window.addEventListener("keyup", up); window.addEventListener("pointerdown", pointerDown); window.addEventListener("pointermove", pointerMove); window.addEventListener("pointerup", pointerUp); window.addEventListener("pointercancel", pointerUp); };
  const stop = () => { window.removeEventListener("keydown", down); window.removeEventListener("keyup", up); window.removeEventListener("pointerdown", pointerDown); window.removeEventListener("pointermove", pointerMove); window.removeEventListener("pointerup", pointerUp); window.removeEventListener("pointercancel", pointerUp); };
  return { start, stop, vector: () => current };
}
