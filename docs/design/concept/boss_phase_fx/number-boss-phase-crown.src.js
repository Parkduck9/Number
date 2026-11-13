// NUMBER boss phase-change draft B. Original choreography drawn with pixelfx (MIT).
// Bolts are inked with the library's lightning helpers (adapted from Sprite Fusion's open-source lightning,
// https://www.spritefusion.com/pixel-art-vfx/lightning, MIT License, Copyright (c) 2026 Hugo Duprez).
import { Particles, Lights, P, Rng, fit, clamp, ease, TAU, WHITE, hex, rand } from "../../core/index.js";
import { zigzag, stroke } from "../lightning/_lib.js";

export const meta = {
  name: "Number boss phase: crimson crown",
  desc: "Cyan arcs crawl round the core as it charges, a white blink, then a red pillar of light blasts straight up and a crown of forked red bolts erupts in a fan, re-striking every few frames; the pillar thins to a thread and embers fall. Ends empty.",
  tags: ["boss", "phase", "lightning", "pillar", "crown", "number", "draft", "one-shot"],
  size: [288, 256],
  origin: [144, 176],
  duration: 1.6,
  loop: false,
  fps: 24,
  bg: "dark",
  options: {
    palette: ["#12040a", "#2a0610", "#5a0a16", "#8e0c18", "#c8101c", "#f0141e", "#ff6a5a", "#ffd0c8"],
    cyan: ["#0a3a48", "#1a8ca0", "#4ff0ff", "#c8fcff"],
    seed: 5,
  },
};

const FIRE = 0.36, TOP = -176, END = 1.15;

export function create(o) {
  const C = fit(o.palette, 8), Q = fit(o.cyan, 4), R = new Rng(o.seed);
  const ps = new Particles({ rng: R, sparks: [WHITE, C[7], C[6], C[4]] }), lights = new Lights();
  // the crown: seven prongs fanned upward, plus two short ones stabbing down the sides
  const prongs = [-160, -138, -116, -90, -64, -42, -20, 150, 30].map((d, i) => ({ a: (d * Math.PI) / 180, L: i >= 7 ? 46 : 70 + (3 - Math.abs(i - 3)) * 22 }));
  let t = 0, fired = false;
  return {
    update(dt) {
      t += dt;
      if (t < FIRE - 0.06) {
        for (let n = 0; n < 2; n++) {
          const a = R.range(0, TAU), D = R.range(40, 80), T = R.range(0.12, 0.2), v = (D * 3) / (Math.exp(3 * T) - 1);
          ps.spawn(P.STREAK, Math.cos(a) * D, Math.sin(a) * D, -Math.cos(a) * v, -Math.sin(a) * v, T, R.chance(0.4) ? Q[2] : C[6]);
        }
      }
      if (!fired && t >= FIRE) {
        fired = true;
        lights.add(0, 0, 120, C[5], 1, 0.5);
        lights.add(0, -60, 90, C[5], 0.7, 0.5);
        lights.add(0, 0, 30, WHITE, 1, 0.1);
      }
      // sparks off the prong tips while the crown burns
      if (t > FIRE && t < END) {
        for (const q of prongs) if (R.chance(6 * dt)) {
          const x = Math.cos(q.a) * q.L, y = Math.sin(q.a) * q.L, a = q.a + R.range(-0.6, 0.6), sp = R.range(60, 160);
          ps.spawn(P.SPARK, x, y, Math.cos(a) * sp, Math.sin(a) * sp, R.range(0.2, 0.45), R.chance(0.35) ? Q[2] : C[6]);
        }
        if (R.chance(10 * dt)) ps.spawn(P.EMBER, R.range(-6, 6), R.range(TOP + 20, -20), R.range(-20, 20), R.range(10, 40), R.range(0.4, 0.8), C[5]);
      }
      ps.update(dt);
      const L = ps.list;
      for (let i = L.length - 1; i >= 0; i--) if (Math.abs(L[i].x) > 140 || L[i].y < -172 || L[i].y > 76) L.splice(i, 1);
      lights.update(dt);
    },
    draw(p) {
      if (t >= meta.duration) return;
      if (t < FIRE) {
        // charge: the core swells, cyan arcs crawl round it, getting tighter and faster
        const k = t / FIRE, f = Math.floor(t * 24), rr = 26 - 14 * k;
        for (let i = 0; i < 3; i++) {
          const a0 = rand(o.seed, f, i) * TAU, a1 = a0 + 0.8 + rand(o.seed, f, i, 2) * 1.2;
          const pts = [];
          for (let s = 0; s <= 8; s++) { const a = a0 + ((a1 - a0) * s) / 8, r = rr + (rand(o.seed, f, i, s) - 0.5) * 6; pts.push([Math.cos(a) * r, Math.sin(a) * r]); }
          p.polyline(pts, i === 0 ? Q[3] : Q[2]);
          p.glow(pts[4][0], pts[4][1], 8, Q[2], 0.25);
        }
        const jit = k > 0.7 ? (f % 2 ? 1 : -1) : 0;
        p.circle(jit, 0, 3 + 4 * k, C[4]);
        p.circle(jit, 0, 2 + 3 * k, C[6]);
        p.circle(jit, 0, 1 + 1.5 * k, k > 0.75 ? WHITE : C[7]);
        p.glow(0, 0, 16 + 24 * k, C[5], 0.3 + 0.5 * k);
      } else {
        const q = t - FIRE, f = Math.floor(t * 24);
        // blink
        if (q < 0.045) { p.circle(0, 0, 16, WHITE); p.beam(0, 0, 0, TOP, 22, WHITE); }
        // the pillar: wide and hot, then thinning to a thread that flickers out
        if (q < 0.9) {
          const w = q < 0.08 ? 18 : Math.max(1, 14 * Math.pow(1 - q / 0.9, 1.6)), a = q < 0.6 ? 1 : 1 - (q - 0.6) / 0.3;
          const wob = rand(o.seed, f, 77) < 0.5 ? 0 : 1;
          p.rect(-w / 2 - 2 + wob, TOP, w + 4, -TOP, C[3], a);
          p.rect(-w / 2 + wob, TOP, w, -TOP, C[5], a);
          if (w > 3) p.rect(-w / 4 + wob, TOP, w / 2, -TOP, w > 8 ? WHITE : C[7], a);
          p.glowLine(0, 0, 0, TOP, w + 10, C[5], 0.45 * a);
          // pulses running up it
          for (let i = 0; i < 3; i++) {
            const y = -((q * 420 + i * 60) % 176);
            p.rect(-w / 2 - 3, y, w + 6, 2, C[6], a);
          }
        }
        // the crown: forked bolts re-rolled every three frames, each strike fading until the next
        if (q < END - FIRE) {
          const strike = Math.floor(f / 3), age = (f % 3) / 3, k = (1 - age * 0.6) * (q < 0.5 ? 1 : 1 - (q - 0.5) / (END - FIRE - 0.5));
          const Rb = new Rng(o.seed * 101 + strike);
          prongs.forEach((pr, i) => {
            if (i >= 7 && Rb.chance(0.4)) return;
            const L = pr.L * (0.85 + Rb.range(0, 0.3)) * (q < 0.06 ? 0.6 + q * 6 : 1);
            const b = zigzag(Rb, 0, 0, Math.cos(pr.a) * L, Math.sin(pr.a) * L, 0.24, 0.3);
            stroke(p, b, k, C, i >= 7 ? 0.8 : 1, { light: 0.45 });
            if (k > 0.5) p.glow(Math.cos(pr.a) * L, Math.sin(pr.a) * L, 6, Q[2], 0.35 * k);
          });
          // the jagged halo joining the prongs
          const hr = 22 + 4 * Math.sin(q * 30);
          for (let s = 0; s < 18; s++) {
            const a0 = (s / 18) * TAU, a1 = ((s + 1) / 18) * TAU, r0 = hr + (s % 2 ? 6 : 0), r1 = hr + ((s + 1) % 2 ? 6 : 0);
            p.line(Math.cos(a0) * r0, Math.sin(a0) * r0, Math.cos(a1) * r1, Math.sin(a1) * r1, s % 2 ? C[6] : C[4], k);
          }
        }
        // the core, thrumming
        const ca = clamp(1 - q / 1.1);
        if (ca > 0) {
          p.circle(0, 0, 5 * ca + 1, C[4]);
          p.circle(0, 0, 3.5 * ca + 0.5, C[6]);
          if (ca > 0.4) p.circle(0, 0, 2 * ca, WHITE);
          p.glow(0, 0, 36, C[5], 0.5 * ca);
          if (f % 2) p.glow(0, 0, 10, Q[2], 0.25 * ca);
        }
      }
      if (t < meta.duration - 0.08) ps.draw(p);
      lights.draw(p);
    },
  };
}
