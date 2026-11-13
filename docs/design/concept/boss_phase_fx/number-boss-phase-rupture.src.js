// NUMBER boss phase-change draft A. Original choreography drawn with pixelfx (MIT).
// The inward streaks and the pop follow the library's implosion (itself adapted from Sprite Fusion's open-source
// energy charge, https://www.spritefusion.com/pixel-art-vfx/energy, MIT License, Copyright (c) 2026 Hugo Duprez).
import { Particles, Lights, P, Rng, fit, clamp, ease, TAU, WHITE, hex, mix, rand } from "../../core/index.js";

export const meta = {
  name: "Number boss phase: override rupture",
  desc: "The core swallows the light (red streaks rush in, a dark ring contracts), a one-frame white blink, then a crimson shockwave with a cyan ghost ring tears outward while horizontal glitch bands rip across the whole frame and data cells scatter. Ends empty.",
  tags: ["boss", "phase", "shockwave", "glitch", "number", "draft", "one-shot"],
  size: [384, 224],
  origin: [192, 112],
  duration: 1.35,
  loop: false,
  fps: 24,
  bg: "dark",
  options: {
    palette: ["#12040a", "#2a0610", "#5a0a16", "#8e0c18", "#c8101c", "#f0141e", "#ff6a5a", "#ffd0c8"],
    cyan: ["#0a3a48", "#1a8ca0", "#4ff0ff", "#c8fcff"],
    seed: 3,
  },
};

const POP = 0.42, VOID = hex("#05040c"), W = 192, H = 112;

export function create(o) {
  const C = fit(o.palette, 8), Q = fit(o.cyan, 4), R = new Rng(o.seed);
  const ps = new Particles({ rng: R, sparks: [WHITE, C[7], C[5], C[3]] }), lights = new Lights();
  // glitch bands: each a horizontal tear at a fixed row, live in stutters after the pop
  const bands = Array.from({ length: 15 }, (_, i) => ({
    y: Math.round(-H + 8 + ((i + R.range(0.1, 0.9)) / 15) * (2 * H - 16)),
    h: R.pick([2, 3, 4, 5, 7, 9]),
    x0: R.range(-W, W * 0.2), len: R.range(70, 260), shift: R.sign() * R.range(4, 18),
    t0: POP + R.range(-0.02, 0.35), t1: 0, seed: i * 7 + 1,
  }));
  for (const b of bands) b.t1 = b.t0 + R.range(0.25, 0.5);
  // data cells thrown out with the wave
  const cells = Array.from({ length: 18 }, () => {
    const a = R.range(0, TAU);
    return { a, sp: R.range(140, 300), w: R.pick([2, 3, 3, 4]), life: R.range(0.45, 0.85), cyan: R.chance(0.3) };
  });
  let t = 0, popped = false;
  return {
    update(dt) {
      t += dt;
      if (t < POP - 0.1) {
        const k = t / POP;
        for (let n = 0; n < 2 + Math.floor(k * 5); n++) {
          const a = R.range(0, TAU), D = R.range(60, 110) * (1 - 0.35 * k), T = R.range(0.12, 0.2), v = (D * 3) / (Math.exp(3 * T) - 1);
          ps.spawn(P.STREAK, Math.cos(a) * D, Math.sin(a) * D * 0.8, -Math.cos(a) * v, -Math.sin(a) * v * 0.8, T, R.chance(0.2) ? Q[2] : R.chance(0.5) ? C[6] : C[5]);
        }
      }
      if (!popped && t >= POP) {
        popped = true;
        lights.add(0, 0, 140, C[5], 1, 0.5);
        lights.add(0, 0, 40, WHITE, 1, 0.1);
        for (let i = 0; i < 40; i++) {
          const a = R.range(0, TAU), sp = R.range(160, 380);
          ps.spawn(P.SPARK, 0, 0, Math.cos(a) * sp, Math.sin(a) * sp * 0.75 - 40, R.range(0.25, 0.6), R.chance(0.15) ? Q[2] : C[6]);
        }
      }
      ps.update(dt);
      const L = ps.list;
      for (let i = L.length - 1; i >= 0; i--) if (Math.abs(L[i].x) > W - 3 || Math.abs(L[i].y) > H - 3) L.splice(i, 1);
      lights.update(dt);
    },
    draw(p) {
      if (t >= meta.duration) return;
      if (t < POP) {
        // the core swallowing the room: a dark ring contracts, the core pulses hotter, a stutter just before the pop
        const k = clamp(t / POP), vr = 78 * (1 - ease.inCubic(k)) + 4;
        p.disc(0, 0, vr, VOID, 0.25 + 0.45 * k);
        p.ring(0, 0, vr + 1, 1, k < 0.6 ? C[4] : C[6], 0.6 + 0.4 * k);
        p.glowRing(0, 0, vr, 6, C[5], 0.2 + 0.3 * k);
        for (let i = 0; i < 3; i++) {
          const u = (t - i * 0.09) / 0.22;
          if (u <= 0 || u >= 1) continue;
          p.ring(0, 0, (96 - i * 12) * (1 - ease.inQuad(u)) + 3, 1, i === 1 ? Q[1] : C[3], 0.5 + 0.5 * u);
        }
        const jit = k > 0.75 ? (Math.floor(t * 48) % 2 ? 1 : -1) : 0, cr = 2 + 3 * k;
        p.circle(jit, 0, cr + 2, C[3]);
        p.circle(jit, 0, cr + 1, C[5]);
        p.circle(jit, 0, cr, k > 0.7 ? WHITE : C[7]);
        p.glow(0, 0, 14 + 30 * k, C[5], 0.35 + 0.5 * k);
      } else {
        const q = t - POP, e = ease.outQuart(clamp(q / 0.7));
        // the blink: one solid white frame core plus a white horizontal slit through the whole frame
        if (q < 0.045) {
          p.circle(0, 0, 14, WHITE);
          p.rect(-W, -2, 2 * W, 4, WHITE);
          p.rect(-W, -4, 2 * W, 1, C[6]);
          p.rect(-W, 3, 2 * W, 1, C[6]);
        } else if (q < 0.12) {
          p.circle(0, 0, 9 * (1 - q / 0.12) + 2, C[7]);
          p.rect(-W, -1, 2 * W, 2, C[6], 1 - q / 0.12);
        }
        // a red wash over the whole frame for two frames: the room turns hostile
        if (q > 0.04 && q < 0.13) { const wa = 0.5 * (1 - (q - 0.04) / 0.09) + 0.15; for (let yy = -H; yy < H; yy += 2) for (let xx = -W; xx < W; xx++) p.blend(xx, yy, C[3], wa); }
        // the shockwave: crimson ring, dark inner rim, cyan ghost a step behind (a broken signal, not a clean wave)
        if (q < 0.9) {
          const r = 6 + 200 * e, fa = 1.1 - q / 0.9, wd = Math.max(1, 5 * (1 - q / 0.6));
          p.ring(0, 0, r - wd - 1, 1, C[1], fa * 0.8);
          p.ring(0, 0, r, wd, q < 0.08 ? WHITE : C[5], fa);
          p.ring(0, 0, r + wd * 0.5, 1, C[7], fa * 0.7);
          p.ring(3, 1, r * 0.9 - 4, 1, Q[2], fa * 0.6);
          p.glowRing(0, 0, r, 8, C[5], 0.35 * fa);
          // a second, slower ring
          const r2 = 4 + 120 * ease.outCubic(clamp((q - 0.08) / 0.8));
          if (q > 0.08) p.ring(0, 0, r2, 1, C[4], fa * 0.8);
        }
        // glitch tears across the frame: the slice, a red edge, a cyan copy offset sideways, a few data blocks
        for (const b of bands) {
          if (t < b.t0 || t > b.t1) continue;
          const f = Math.floor(t * 24), on = rand(b.seed, f) < 0.72;
          if (!on) continue;
          const life = (t - b.t0) / (b.t1 - b.t0), jx = Math.round((rand(b.seed, f, 3) - 0.5) * 2 * b.shift);
          const x0 = b.x0 + jx, x1 = Math.min(W, x0 + b.len * (1 - 0.4 * life)), a = life < 0.7 ? 1 : 1 - (life - 0.7) / 0.3;
          for (let yy = 0; yy < b.h; yy++) p.line(x0 + (yy % 2 ? jx : 0), b.y + yy, x1, b.y + yy, yy % 3 === 1 ? C[2] : C[1], a);
          p.line(x0, b.y, x1, b.y, C[5], a);
          if (b.h > 2) p.line(x0 + 6, b.y + b.h - 1, x1 - 10, b.y + b.h - 1, C[3], a);
          p.line(x0 + b.shift, b.y + Math.min(1, b.h - 1), x0 + b.shift + (x1 - x0) * 0.55, b.y + Math.min(1, b.h - 1), Q[2], a * 0.85);
          for (let k = 0; k < 3; k++) {
            const bx = x0 + rand(b.seed, f, k + 10) * (x1 - x0), bw = 2 + Math.floor(rand(b.seed, k) * 6);
            p.rect(bx, b.y - 1, bw, b.h + 2, k === 0 ? C[6] : C[4], a);
          }
          p.glowLine(x0, b.y, x1, b.y, 3, C[5], 0.25 * a);
        }
        // data cells: tiny hollow squares, never glyphs
        for (const c of cells) {
          if (q < 0.03 || q > c.life) continue;
          const d = (1 - Math.exp(-4 * q)) / 4, x = Math.cos(c.a) * c.sp * d, y = Math.sin(c.a) * c.sp * d * 0.75 + 60 * q * q;
          const col = c.cyan ? Q[2] : q < c.life * 0.6 ? C[6] : C[4], a = q < c.life * 0.7 ? 1 : 1 - (q / c.life - 0.7) / 0.3;
          p.rect(x, y, c.w + 1, 1, col, a); p.rect(x, y + c.w, c.w + 1, 1, col, a);
          p.rect(x, y, 1, c.w, col, a); p.rect(x + c.w, y, 1, c.w, col, a);
          p.glow(x + 1, y + 1, 4, c.cyan ? Q[2] : C[5], 0.18 * a);
        }
        // the core stays hot, then cools
        if (q < 0.9) {
          const cr = 3 * (1 - q / 0.9);
          if (cr > 0.6) { p.circle(0, 0, cr + 1, C[5]); p.circle(0, 0, cr, C[7]); }
          p.glow(0, 0, 30, C[5], 0.5 * (1 - q / 0.9));
        }
      }
      ps.draw(p);
      lights.draw(p);
    },
  };
}
