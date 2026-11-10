# Run v4 fix spec (Fable review of v3, 2026-11-10)

Coordinates are (x,y) in the 64x64 cell, x right, y down, facing right. Source palette (hero_run_6f.png): INK #080b14, DEEP #1a1d33, ROBE #2d3050, LIGHT #3f4a6e, GREY #62677d, WHITE #d6d7d0, skin #efd0b6/#d7b4a0/#a37b70, gold #f2c94c, red #7f1a22/#b94445, browns #9b7043/#c49a5c, cyan #42a9bd/#226076.

## 1. Diagnosis of v3

Global faults (all frames)
- Arm spacing is two-position: near-hand x = [26,29,43,46,49,46,29,28], deltas 3,14,3,3,3,17,1,2. The hand parks for 4 frames, snaps 14-17 px, parks again. Reads as popping, not swinging.
- Back-swing is inverted: ARM_POSES 0/1/6/7 put the ELBOW at x 21-26 only 2-6 rows under the shoulder (upper arm horizontal backwards, chicken-wing) and the fist at x 26-29 directly under the armpit. The hand never goes behind the hip.
- Fist/cuff orientation is reused from the source guard pose (forearm pointing up: fist above cuff). In 0/1/6/7 the forearm points down, so the WHITE/GREY cuff block sits BELOW the fist - a white blob hanging under the hand.
- A forward fist at chin height (rows 24-28) is visible in 6 of 8 frames, so the pump has almost no vertical arc.
- Sleeve = 3 px interior + outline = 5 px uniform hose, vs ~8 px upper arm / 6 px forearm in the source; shoulder is a point at (33,26) with no deltoid. Sleeve fill is ROBE = torso colour with no highlight, so at 1x the near arm is just an INK loop on the chest.
- Shoulder jumps to (31,26) in frames 4/5 only: sleeve root pops 2 px.
- Legs: hip row 41 to ground 61 = 20 px, but LEGS stretches hip->ankle to 22-23 px and pastes the 12 px shoe wholly ahead of the ankle. Thigh 9 px wide at hip, shin 5: wedge legs. Shoes never rotate.

Per frame
- F0/F4 (contact): both feet grounded with a 52 px foot span: a splits lunge, not a contact. Legs collinear (no knee bend). Rear leg does not push off.
- F1/F5 (down): rear foot jumps from the ground to rows 45-52 with the thigh horizontal back: pop.
- F2/F6 (labelled passing, actually push): support leg fully extended back with the heel FLAT; swing knee at hip row, thigh horizontal, shin vertical -> "sitting on a chair". No true passing pose.
- F3/F7 (flight): both thighs horizontal; rear foot folded up level with the sash - reads as a shoe floating behind the belt. 9 px air = a hop. Arm freezes forward for 3 frames.

## 2. Fix spec

Anchors (before bob): near shoulder S=(33,26), far shoulder s=(31,27); near hip H=(32,41), far hip h=(30,42). Bob per phase (apply to EVERYTHING incl. legs and arms): contact 0, down +2, pass +1, flight -3. Keep head (+4 px forward), collar, sash and hem cutouts and the 2/9 shear exactly as v3. Segment lengths: upper arm 7, forearm 6-7, thigh 8, shin 6-7 (Euclidean, +-1). Ground = sole outline row 61.

Legs (hip->knee->ankle; F = flat shoe cutout, A = angle cutout; "bottom" = lowest opaque shoe row; ankle = top row of the cutout, flat at its 5th column from the left, angle at its 7th). Values already include bob.

| Frame | Near leg | Far leg |
|---|---|---|
| 0 contact | (32,41)->(38,46)->(42,54) F bottom 61, cols 38-49 | (30,42)->(25,48)->(22,54) A bottom 59 |
| 1 down | (32,43)->(36,49)->(35,55) F bottom 61, cols 30-41 | (30,44)->(24,48)->(19,51) A bottom 56 |
| 2 pass | (32,42)->(32,49)->(30,55) F bottom 61, cols 25-36 | (30,43)->(35,48)->(30,52) A bottom 57 (knee 3 px ahead of near leg) |
| 3 flight | (32,38)->(27,45)->(23,51) A bottom 58 | (30,39)->(38,41)->(40,48) F bottom 54 |
| 4-7 | far column of 0-3 moved by (+2,-1) | near column of 0-3 moved by (-2,+1) |

Foot span max 32 px (cols 17-49). Knee always ahead of the hip-ankle line (bent), rear foot never above row 46 (below sash rows 36-44). Lowest airborne pixel 54-58.

Arms (near; add bob to y). Far arm in frame i = near pose of frame (i+4)%8 shifted (-2,+1), drawn behind torso.

| Frame | Elbow | Hand centre |
|---|---|---|
| 0 | (28,32) | (22,30) |
| 1 | (29,33) | (24,37) |
| 2 | (32,33) | (34,39) |
| 3 | (36,33) | (42,32) |
| 4 | (39,31) | (46,27) |
| 5 | (37,33) | (43,32) |
| 6 | (33,33) | (35,39) |
| 7 | (30,33) | (25,37) |

Hand x deltas 2,10,8,4,3,8,10,3 (ease at extremes); hand arc dips to row 39 at the pass, chin only on frame 4. Shoulder never moves. Fist cutout (5x6) centred on the hand point; cuff = 2 px band across the forearm on the ELBOW side of the fist (at the wrist), never below the fist. Near fist may cover sash pixels in 2/6; far-side sash tail must stay visible.

Thickness: upper arm interior 5 (7 with outline) with a 7-wide cap for rows S..S+2 (deltoid), forearm interior 4, cuff = forearm width +1; thigh interior 6, shin interior 4; taper only at joints.

Colour/separation: near sleeve fill #2d3050, 1 px #3f4a6e rim along the sky-facing edge of both segments inside the #080b14 outline; where the sleeve crosses the torso, add a 1 px #1a1d33 line on the torso just outside the sleeve's lower/rear outline (contact shadow). Far sleeve fill #1a1d33, outline #080b14, no rim, cuff #62677d only, fist darkened (#d7b4a0->#a37b70, #efd0b6->#d7b4a0). Near leg #2d3050 thigh / #1a1d33 shin, far leg #1a1d33 throughout; 1 px #080b14 gap wherever far limb touches near limb or torso. Layer order: far arm, far leg, hem, near leg, torso, near arm, collar, sash, head (sash never over the near fist).

## 3. Timing
Keep 8 frames at 20 fps (0.4 s, 128 px per cycle). Foot slide cannot be fully removed in 64 px cells; code may scale the run fps with speed.
