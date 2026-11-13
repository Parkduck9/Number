// NUMBER boss phase-change draft C. Original choreography drawn with pixelfx (MIT).
import { Rng, fit, clamp, ease, TAU, WHITE, hex, rand } from "../../core/index.js";

export const meta = {
  name: "Number boss phase: fracture",
  desc: "A white blink then one black frame, and the picture cracks like glass from the core: jagged radial cracks race out, two broken rings join them, red light bleeds through the seams with cyan glints running along them, the inner shards drop away and a red ring pulses out. Ends empty.",
  tags: ["boss", "phase", "crack", "glass", "shatter", "number", "draft", "one-shot"],
  size: [320, 240],
  origin: [160, 120],
  duration: 1.6,
  loop: false,
  fps: 24,
  bg: "dark",
  options: {
    palette: ["#12040a", "#2a0610", "#5a0a16", "#8e0c18", "#c8101c", "#f0141e", "#ff6a5a", "#ffd0c8"],
    cyan: ["#0a3a48", "#1a8ca0", "#4ff0ff", "#c8fcff"],
    seed: 9,
  },
};

const HIT = 0.1, GROW = 0.32, DROP = 0.62, SEAM = hex("#05040c");

export function create(o) {
  const C = fit(o.palette, 8), Q = fit(o.cyan, 4), R = new Rng(o.seed);
  // radial cracks: jagged polylines from near the core out to the edge
  const N = 13, rays = [];
  for (let i = 0; i < N; i++) {
    const a0 = (i / N) * TAU + R.range(-0.15, 0.15), L = R.range(95, 150), pts = [];
    let a = a0;
    for (let d = 6; d <= L; d += R.range(7, 14)) { a += R.range(-0.07, 0.07); pts.push([Math.cos(a) * d, Math.sin(a) * d * 0.85]); }
    rays.push({ pts, delay: R.range(0, 0.06), glint: R.chance(0.4), sp: R.range(0.7, 1.2) });
  }
  // broken rings: chords between neighbouring rays at two radii (some missing)
  const rings = [];
  for (const [ri, rad] of [[0, 28], [1, 62]]) for (let i = 0; i < N; i++) {
    if (R.chance(ri ? 0.35 : 0.15)) continue;
    const A = rays[i].pts, B = rays[(i + 1) % N].pts, ia = Math.min(A.length - 1, Math.round(rad / 10)), ib = Math.min(B.length - 1, Math.round(rad / 10) + R.int(-1, 1));
    const [ax, ay] = A[ia], [bx, by] = B[Math.max(0, ib)], mx = (ax + bx) / 2 + R.range(-4, 4), my = (ay + by) / 2 + R.range(-4, 4);
    rings.push({ pts: [[ax, ay], [mx, my], [bx, by]], at: rad, ri });
  }
  // inner shards: wedges between rays inside the first ring that fall away
  const shards = [];
  for (let i = 0; i < N; i++) {
    if (R.chance(0.3)) continue;
    const A = rays[i].pts, B = rays[(i + 1) % N].pts, k = 4;
    shards.push({ pts: [[0, 0], A[0], A[Math.min(k, A.length - 1)], B[Math.min(k, B.length - 1)], B[0]], vx: R.range(-30, 30), vy: R.range(-60, -10), va: R.range(-3, 3), d: R.range(0, 0.12) });
  }
  let t = 0;
  // draw part of a polyline up to share u of its length
  const part = (p, pts, u, c, a) => {
    const n = (pts.length - 1) * clamp(u);
    for (let i = 0; i < Math.floor(n); i++) p.line(pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1], c, a);
    const i = Math.floor(n), f = n - i;
    if (i < pts.length - 1 && f > 0) p.line(pts[i][0], pts[i][1], pts[i][0] + (pts[i + 1][0] - pts[i][0]) * f, pts[i][1] + (pts[i + 1][1] - pts[i][1]) * f, c, a);
  };
  const crack = (p, pts, u, a, hot) => {
    // a dark seam with a red core a pixel off it: the light bleeding through
    part(p, pts.map(([x, y]) => [x + 1, y + 1]), u, SEAM, a);
    part(p, pts, u, hot ? C[7] : C[5], a);
    // thick near the core: a second bright line beside the first, for the first third
    part(p, pts.slice(0, Math.ceil(pts.length / 3)).map(([x, y]) => [x, y - 1]), u * 3, C[6], a);
  };
  return {
    update(dt) { t += dt; },
    draw(p) {
      if (t >= meta.duration) return;
      // the blink: white disc, then a black frame where the core is (the screen "dies" for a frame)
      if (t < HIT * 0.5) { p.circle(0, 0, 22, WHITE); p.glow(0, 0, 80, C[5], 1); return; }
      if (t < HIT) { p.circle(0, 0, 40, SEAM); p.ring(0, 0, 40, 1, C[5]); p.circle(0, 0, 2, WHITE); return; }
      const q = t - HIT, g = ease.outExpo(clamp(q / GROW)), fade = t < 1.15 ? 1 : clamp(1 - (t - 1.15) / 0.35);
      if (fade <= 0) return;
      const pulse = 0.6 + 0.4 * Math.sin(q * 26);
      // red pulse ring once the cracks have run
      const pr = (q - 0.12) / 0.6;
      if (pr > 0 && pr < 1) {
        const r = 10 + 170 * ease.outCubic(pr);
        p.ring(0, 0, r, Math.max(1, 3 * (1 - pr)), C[5], 1 - pr);
        p.ring(2, 1, r - 3, 1, Q[2], 0.6 * (1 - pr));
        p.glowRing(0, 0, r, 6, C[5], 0.3 * (1 - pr));
      }
      for (const r of rays) {
        const u = clamp((q - r.delay) / (GROW * r.sp)), uu = ease.outExpo(u);
        crack(p, r.pts, uu, fade, q < 0.12);
        for (let i = 0; i < r.pts.length * uu - 1; i += 2) p.glow(r.pts[i][0], r.pts[i][1], 6, C[5], 0.18 * pulse * fade);
        // a cyan glint running out along some cracks
        if (r.glint && q > 0.2 && q < 0.9) {
          const n = r.pts.length - 1, s = ((q - 0.2) / 0.5) * n, i = Math.floor(s);
          if (i < n) {
            const [x0, y0] = r.pts[i], [x1, y1] = r.pts[i + 1], f = s - i, x = x0 + (x1 - x0) * f, y = y0 + (y1 - y0) * f;
            p.px(x, y, Q[3]); p.px(x - 1, y, Q[2]); p.px(x + 1, y, Q[2]);
            p.glow(x, y, 6, Q[2], 0.5 * fade);
          }
        }
        // the crack's branch ends: little splits at the tip
        if (u > 0.95) {
          const e = r.pts[r.pts.length - 1], b = r.pts[r.pts.length - 2], a = Math.atan2(e[1] - b[1], e[0] - b[0]);
          for (const s of [-0.6, 0.5]) p.line(e[0], e[1], e[0] + Math.cos(a + s) * 7, e[1] + Math.sin(a + s) * 7, C[4], fade);
        }
      }
      for (const r of rings) {
        const u = clamp((g * 150 - r.at) / 30);
        if (u > 0) crack(p, r.pts, u, fade * (r.ri ? 0.9 : 1), false);
      }
      // inner shards: lit edges while they sit, then they drop away into the dark
      for (const s of shards) {
        const dq = q - DROP + s.d;
        if (dq < 0) {
          if (q > 0.15) for (let i = 1; i < s.pts.length - 1; i++) p.line(s.pts[i][0], s.pts[i][1], s.pts[i + 1][0], s.pts[i + 1][1], C[6], 0.6 * pulse);
          continue;
        }
        if (dq > 0.6) continue;
        const ox = s.vx * dq, oy = s.vy * dq + 380 * dq * dq, an = s.va * dq, ca = Math.cos(an), sa = Math.sin(an), cx = s.pts[2][0] / 2, cy = s.pts[2][1] / 2;
        const pts = s.pts.map(([x, y]) => [cx + (x - cx) * ca - (y - cy) * sa + ox, cy + (x - cx) * sa + (y - cy) * ca + oy]);
        const a = 1 - dq / 0.6;
        p.poly(pts, C[2], a);
        for (let i = 0; i < pts.length; i++) p.line(pts[i][0], pts[i][1], pts[(i + 1) % pts.length][0], pts[(i + 1) % pts.length][1], i % 2 ? C[3] : C[6], a);
      }
      // the hole the shards leave: a dark core with a red rim, the core light glaring through
      const hole = clamp((q - DROP + 0.1) / 0.2);
      if (hole > 0) {
        p.circle(0, 0, 34 * hole, SEAM, fade);
        p.ring(0, 0, 34 * hole + 1, 1, C[5], fade);
      }
      p.circle(0, 0, 3, C[6], fade); p.circle(0, 0, 1.5, WHITE, fade);
      p.glow(0, 0, 40, C[5], (0.5 + 0.3 * pulse) * fade);
      if (Math.floor(t * 24) % 3 === 0) p.glow(0, 0, 12, Q[2], 0.25 * fade);
    },
  };
}
