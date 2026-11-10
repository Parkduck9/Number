"""
Hero cut-out rig experiment: build a bone rig from parts/rig.json, keyframe idle / run / attack,
render PNG frame sequences with Blender (headless, bpy only).

Run:
  blender -b -P rig_and_render.py -- [idle run attack] [--engine EEVEE|CYCLES] [--save-blend]

Output:
  ../anim/<name>/####.png   704x704 RGBA, 1 px = 1 rig pixel (character ~509 px tall standing),
                            ground (sole of the standing feet) on image row 660, pelvis pivot at column 352.
  ../anim/hero_rig.blend    optional, for opening in the Blender UI.

Conventions (match rig.json): angles in degrees, positive = clockwise on screen (character faces right).
  upward bones (pelvis, torso, head): positive leans the top forward.
  downward bones (arms, legs): positive swings the far end backward; forearm negative = elbow flexion,
  shin positive = knee flexion.  blade: rotation relative to the forearm (0 = perpendicular, +90 = along it).
Root motion: 'root': (dx, dz) in pixels, +dx forward, +dz up.
"""
import bpy
import json
import math
import os
import sys
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
RIG_DIR = os.path.dirname(HERE)
PARTS_DIR = os.path.join(RIG_DIR, "parts")
ANIM_DIR = os.path.join(RIG_DIR, "anim")

CANVAS = 704          # render size (square)
GROUND_ROW = 660      # image row of the standing sole
CENTER_COL = 352      # image column of the pelvis pivot
FPS = 24
DEPTH_STEP = 2.0      # world units between draw-order layers

# ----------------------------------------------------------------------------------------------
# Rig layout from rig.json
# ----------------------------------------------------------------------------------------------
with open(os.path.join(PARTS_DIR, "rig.json"), "r", encoding="utf-8") as f:
    RIG = json.load(f)
PARTS = {p["name"]: p for p in RIG["parts"]}


def top_left(name):
    p = PARTS[name]
    if p["parent"] is None:
        return (p["offset"][0] - p["pivot"][0], p["offset"][1] - p["pivot"][1])
    ptl = top_left(p["parent"])
    return (ptl[0] + p["offset"][0] - p["pivot"][0], ptl[1] + p["offset"][1] - p["pivot"][1])


def pivot_px(name):
    tl = top_left(name)
    p = PARTS[name]
    return (tl[0] + p["pivot"][0], tl[1] + p["pivot"][1])


def lowest_opaque_row(name):
    """Image row (canvas px, top-left origin) just below the lowest opaque pixel of a part."""
    img = bpy.data.images.load(os.path.join(PARTS_DIR, PARTS[name]["file"]), check_existing=True)
    w, h = img.size
    px = img.pixels[:]
    for row_from_bottom in range(h):
        base = row_from_bottom * w * 4
        if any(px[base + i * 4 + 3] > 0.5 for i in range(w)):
            y_in_image = h - row_from_bottom  # rows below this are transparent
            return top_left(name)[1] + y_in_image
    return top_left(name)[1] + h


PELVIS_PX = pivot_px("pelvis")
GROUND_PX_Y = lowest_opaque_row("shin_near")


def to_world(px, py, depth=0.0):
    """canvas pixel (top-left origin) -> Blender world (X right, Z up, Y depth toward +Y = away from camera)."""
    return Vector((px - PELVIS_PX[0], depth, GROUND_PX_Y - py))


# bone name -> (part name, tail direction, length) ; head is always the part pivot
BONES = {
    "pelvis": ("pelvis", (0, 1), 30),
    "torso": ("torso", (0, 1), pivot_px("torso")[1] - pivot_px("head")[1]),
    "head": ("head", (0, 1), 70),
    "ua_n": ("upper_arm_near", (0, -1), pivot_px("forearm_near")[1] - pivot_px("upper_arm_near")[1]),
    "fa_n": ("forearm_near", (0, -1), pivot_px("blade")[1] - pivot_px("forearm_near")[1]),
    "blade": ("blade", (1, 0), 80),
    "ua_f": ("upper_arm_far", (0, -1), pivot_px("forearm_far")[1] - pivot_px("upper_arm_far")[1]),
    "fa_f": ("forearm_far", (0, -1), 91),
    "th_n": ("thigh_near", (0, -1), pivot_px("shin_near")[1] - pivot_px("thigh_near")[1]),
    "sh_n": ("shin_near", (0, -1), 131),
    "th_f": ("thigh_far", (0, -1), pivot_px("shin_far")[1] - pivot_px("thigh_far")[1]),
    "sh_f": ("shin_far", (0, -1), 131),
}
BONE_PARENT = {
    "pelvis": None, "torso": "pelvis", "head": "torso",
    "ua_n": "torso", "fa_n": "ua_n", "blade": "fa_n",
    "ua_f": "torso", "fa_f": "ua_f",
    "th_n": "pelvis", "sh_n": "th_n", "th_f": "pelvis", "sh_f": "th_f",
}
# draw order override: blade behind the near fist (fingers over the handle), see README
DRAW_ORDER = {p["name"]: p["draw_order"] for p in RIG["parts"]}
DRAW_ORDER["blade"] = 9.5

# ----------------------------------------------------------------------------------------------
# Scene
# ----------------------------------------------------------------------------------------------

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.fps = FPS
    scene.render.resolution_x = CANVAS
    scene.render.resolution_y = CANVAS
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.filter_size = 1.0
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    scene.render.image_settings.compression = 30
    scene.render.use_file_extension = True
    # flat colours: no tone mapping
    scene.display_settings.display_device = "sRGB"
    try:
        scene.view_settings.view_transform = "Standard"
    except TypeError:
        pass
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    return scene


def set_engine(scene, engine):
    if engine == "CYCLES":
        scene.render.engine = "CYCLES"
        scene.cycles.samples = 32
        scene.cycles.use_denoising = False
        scene.cycles.transparent_max_bounces = 32
        scene.cycles.max_bounces = 0
        scene.cycles.device = "CPU"
    else:
        scene.render.engine = "BLENDER_EEVEE"
        scene.eevee.taa_render_samples = 16
        scene.eevee.use_taa_reprojection = False


def make_camera(scene):
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = CANVAS
    cam_data.clip_start = 1.0
    cam_data.clip_end = 5000.0
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    # camera at -Y looking +Y, X right, Z up. Image row GROUND_ROW must be world Z=0.
    centre_z = (CANVAS / 2.0) - (CANVAS - GROUND_ROW)
    cam.location = Vector((0.0, -1000.0, centre_z))
    cam.rotation_euler = (math.pi / 2.0, 0.0, 0.0)
    scene.camera = cam
    return cam


def make_part_material(name, image):
    mat = bpy.data.materials.new("M_" + name)
    mat.use_nodes = True
    nt = mat.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = image
    tex.interpolation = "Linear"
    tex.extension = "CLIP"
    emis = nt.nodes.new("ShaderNodeEmission")
    emis.inputs["Strength"].default_value = 1.0
    transp = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(tex.outputs["Color"], emis.inputs["Color"])
    nt.links.new(tex.outputs["Alpha"], mix.inputs["Fac"])
    nt.links.new(transp.outputs["BSDF"], mix.inputs[1])
    nt.links.new(emis.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    mat.surface_render_method = "BLENDED"
    mat.use_transparency_overlap = True
    mat.use_backface_culling = False
    try:
        mat.blend_method = "BLEND"
        mat.shadow_method = "NONE"
    except (AttributeError, TypeError):
        pass
    return mat


def make_part_plane(scene, name):
    """Quad whose origin is the part pivot; 1 unit = 1 px; sits at Y = depth for its draw order."""
    p = PARTS[name]
    img = bpy.data.images.load(os.path.join(PARTS_DIR, p["file"]), check_existing=True)
    img.alpha_mode = "STRAIGHT"
    w, h = p["size"]
    pvx, pvy = p["pivot"]
    # local coords: x = px - pvx, z = -(py - pvy)
    verts = [
        Vector((0 - pvx, 0.0, -(h - pvy))),  # bottom-left
        Vector((w - pvx, 0.0, -(h - pvy))),  # bottom-right
        Vector((w - pvx, 0.0, -(0 - pvy))),  # top-right
        Vector((0 - pvx, 0.0, -(0 - pvy))),  # top-left
    ]
    mesh = bpy.data.meshes.new("P_" + name)
    mesh.from_pydata(verts, [], [[0, 1, 2, 3]])
    uv = mesh.uv_layers.new(name="UVMap")
    for li, (u, v) in zip(mesh.polygons[0].loop_indices, [(0, 0), (1, 0), (1, 1), (0, 1)]):
        uv.data[li].uv = (u, v)
    mesh.update()
    obj = bpy.data.objects.new("P_" + name, mesh)
    scene.collection.objects.link(obj)
    obj.data.materials.append(make_part_material(name, img))
    depth = -DRAW_ORDER[name] * DEPTH_STEP  # higher draw order = closer to camera (camera is at -Y)
    obj.location = to_world(*pivot_px(name), depth=depth)
    return obj


def make_armature(scene):
    arm_data = bpy.data.armatures.new("HeroRig")
    arm = bpy.data.objects.new("HeroRig", arm_data)
    scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    ebones = {}
    for bname, (part, tdir, length) in BONES.items():
        eb = arm_data.edit_bones.new(bname)
        head = to_world(*pivot_px(part))
        eb.head = head
        eb.tail = head + Vector((tdir[0] * length, 0.0, tdir[1] * length))
        eb.roll = 0.0
        eb.use_connect = False
        ebones[bname] = eb
    for bname, parent in BONE_PARENT.items():
        if parent:
            ebones[bname].parent = ebones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
    return arm


def parent_plane_to_bone(arm, obj, bname):
    """Rigid bone parenting that keeps the plane's world transform (same as Ctrl+P > Bone)."""
    bone = arm.data.bones[bname]
    world = Matrix.Translation(obj.location)  # matrix_world is stale before a depsgraph update
    obj.parent = arm
    obj.parent_type = "BONE"
    obj.parent_bone = bname
    parent_mat = arm.matrix_world @ bone.matrix_local @ Matrix.Translation((0.0, bone.length, 0.0))
    obj.matrix_parent_inverse = parent_mat.inverted()
    obj.matrix_basis = world


def build_rig(scene):
    arm = make_armature(scene)
    planes = {}
    for bname, (part, _, _) in BONES.items():
        obj = make_part_plane(scene, part)
        parent_plane_to_bone(arm, obj, bname)
        planes[part] = obj
    bpy.context.view_layer.update()
    # sanity: every plane origin must still sit on its pivot
    for bname, (part, _, _) in BONES.items():
        want = to_world(*pivot_px(part))
        got = planes[part].matrix_world.translation
        if (Vector((want.x, 0, want.z)) - Vector((got.x, 0, got.z))).length > 0.01:
            raise RuntimeError(f"plane {part} drifted: want {want[:]} got {got[:]}")
    return arm, planes


# ----------------------------------------------------------------------------------------------
# Posing helpers
# ----------------------------------------------------------------------------------------------
_axis_cache = {}


def _depth_axis(arm, bname):
    """(index, sign) of the bone-local axis that equals world +Y (the view axis)."""
    if bname not in _axis_cache:
        rest = (arm.matrix_world @ arm.data.bones[bname].matrix_local).to_3x3()
        v = rest.inverted() @ Vector((0.0, 1.0, 0.0))
        idx = max(range(3), key=lambda i: abs(v[i]))
        _axis_cache[bname] = (idx, 1.0 if v[idx] > 0 else -1.0, rest.inverted())
    return _axis_cache[bname]


def set_bone_angle(arm, bname, deg):
    idx, sign, _ = _depth_axis(arm, bname)
    pb = arm.pose.bones[bname]
    e = [0.0, 0.0, 0.0]
    e[idx] = sign * math.radians(deg)
    pb.rotation_euler = e


def set_root_offset(arm, dx, dz):
    _, _, inv = _depth_axis(arm, "pelvis")
    arm.pose.bones["pelvis"].location = inv @ Vector((dx, 0.0, dz))


ALL_BONES = list(BONES.keys())


def key_pose(arm, frame, pose):
    """pose: {'root': (dx, dz), bone: deg, ...}. Unlisted bones are keyed at 0."""
    dx, dz = pose.get("root", (0.0, 0.0))
    set_root_offset(arm, dx, dz)
    arm.pose.bones["pelvis"].keyframe_insert("location", frame=frame)
    for b in ALL_BONES:
        set_bone_angle(arm, b, pose.get(b, 0.0))
        arm.pose.bones[b].keyframe_insert("rotation_euler", frame=frame)


def key_animation(arm, keys, loop_period=None):
    """keys: list of (frame, pose). For loops the keys are also inserted one period before and after
    so that the Bezier handles inside the rendered range are computed from real neighbours."""
    arm.animation_data_clear()
    shifts = [0]
    if loop_period:
        shifts = [-loop_period, 0, loop_period]
    for s in shifts:
        for frame, pose in keys:
            key_pose(arm, frame + s, pose)
    # smooth interpolation with ease at every key
    act = arm.animation_data.action
    for fc in act_fcurves(act):
        for kp in fc.keyframe_points:
            kp.interpolation = "BEZIER"
            kp.handle_left_type = "AUTO_CLAMPED"
            kp.handle_right_type = "AUTO_CLAMPED"


def act_fcurves(act):
    if hasattr(act, "layers") and act.layers:  # slotted actions (Blender 4.4+)
        for layer in act.layers:
            for strip in layer.strips:
                for cb in strip.channelbags:
                    for fc in cb.fcurves:
                        yield fc
    else:
        for fc in act.fcurves:
            yield fc


def pose(**kw):
    return kw


def merged(base, **kw):
    p = dict(base)
    p.update(kw)
    return p


# ----------------------------------------------------------------------------------------------
# Animations
# ----------------------------------------------------------------------------------------------
# relaxed standing pose: slight elbow bend, blade tip a bit down, small stance
IDLE_A = pose(root=(0, 0), pelvis=2, torso=4, head=-3,
              ua_n=10, fa_n=-20, blade=18, ua_f=12, fa_f=-16,
              th_n=-3, sh_n=4, th_f=3, sh_f=2)
# inhale: chest lifts and body rises, arms drift back a little, head lags behind (keyed 2 frames later)
IDLE_B = pose(root=(2, 7), pelvis=0, torso=-2, head=2,
              ua_n=2, fa_n=-8, blade=2, ua_f=4, fa_f=-6,
              th_n=-3, sh_n=4, th_f=3, sh_f=2)

IDLE_PERIOD = 24


def idle_keys():
    # breathing is keyed on the body; head/arms get their own slightly offset keys (lag / overlap)
    body_a = {k: v for k, v in IDLE_A.items() if k not in ("head", "ua_n", "fa_n", "blade", "ua_f", "fa_f")}
    body_b = {k: v for k, v in IDLE_B.items() if k not in ("head", "ua_n", "fa_n", "blade", "ua_f", "fa_f")}
    return _multi_track([
        # (track keys dict, [(frame, pose)])
        (body_a.keys(), [(1, body_a), (13, body_b), (25, body_a)]),
        (("head",), [(3, {"head": IDLE_A["head"]}), (15, {"head": IDLE_B["head"]}), (27, {"head": IDLE_A["head"]})]),
        (("ua_n", "fa_n", "ua_f", "fa_f"),
         [(2, {k: IDLE_A[k] for k in ("ua_n", "fa_n", "ua_f", "fa_f")}),
          (14, {k: IDLE_B[k] for k in ("ua_n", "fa_n", "ua_f", "fa_f")}),
          (26, {k: IDLE_A[k] for k in ("ua_n", "fa_n", "ua_f", "fa_f")})]),
        (("blade",), [(4, {"blade": IDLE_A["blade"]}), (16, {"blade": IDLE_B["blade"]}), (28, {"blade": IDLE_A["blade"]})]),
    ])


def _multi_track(tracks):
    """Return list of (frame, partial_pose, channels) so channels can be keyed independently."""
    out = []
    for channels, keys in tracks:
        for frame, p in keys:
            out.append((frame, p, tuple(channels)))
    return out


def key_tracks(arm, track_keys, loop_period=None):
    """Like key_animation but each entry keys only its own channels."""
    arm.animation_data_clear()
    shifts = [0]
    if loop_period:
        shifts = [-loop_period, 0, loop_period]
    for s in shifts:
        for frame, p, channels in track_keys:
            if "root" in channels:
                dx, dz = p["root"]
                set_root_offset(arm, dx, dz)
                arm.pose.bones["pelvis"].keyframe_insert("location", frame=frame + s)
            for b in channels:
                if b == "root":
                    continue
                set_bone_angle(arm, b, p[b])
                arm.pose.bones[b].keyframe_insert("rotation_euler", frame=frame + s)
    act = arm.animation_data.action
    for fc in act_fcurves(act):
        for kp in fc.keyframe_points:
            kp.interpolation = "BEZIER"
            kp.handle_left_type = "AUTO_CLAMPED"
            kp.handle_right_type = "AUTO_CLAMPED"


# --- run: 16 frames, 8 phases of 2 frames; the near leg is the reference, far leg is 4 phases later
RUN_PERIOD = 16
# phase:       0 contact  1 down   2 pass   3 up     4 contact(far) 5 down   6 pass   7 up
RUN_TH = [-32, -18, -2, 18, 32, 28, -5, -36]      # thigh
RUN_SH = [8, 28, 14, 22, 55, 95, 85, 45]          # shin (knee flexion)
RUN_UA = [34, 26, 2, -22, -38, -30, -4, 22]       # upper arm (opposite to same-side leg)
RUN_FA = [-55, -60, -75, -90, -95, -90, -78, -62]  # forearm (more bent when forward)
RUN_BLADE = [10, 22, 28, 20, 12, 22, 28, 18]      # wrist wobble
RUN_DZ = [-3, -10, -4, 8, -3, -10, -4, 8]         # body bob: lowest just after contact, highest at 'up'
RUN_TORSO = [8, 12, 9, 5, 8, 12, 9, 5]            # upper body bends more on the down, straightens on the up
RUN_HEAD = [-9, -7, -10, -13, -9, -7, -10, -13]   # head counters the torso and lags by one phase (shifted below)


def run_keys():
    keys = []
    for ph in range(8):
        far = (ph + 4) % 8
        f = 1 + ph * 2
        keys.append((f, pose(
            root=(0, RUN_DZ[ph]), pelvis=9, torso=RUN_TORSO[ph], head=RUN_HEAD[(ph - 1) % 8],
            th_n=RUN_TH[ph], sh_n=RUN_SH[ph], th_f=RUN_TH[far], sh_f=RUN_SH[far],
            ua_n=RUN_UA[ph], fa_n=RUN_FA[ph], blade=RUN_BLADE[ph],
            ua_f=RUN_UA[far], fa_f=RUN_FA[far],
        )))
    return keys


# --- attack: 12 frames. coil (1-5) -> overhead slash in 2 frames (5-7) -> follow-through (8-9) -> recover (13 = idle)
# the 12 fps sampling (odd frames) sees: idle, half coil, full coil, contact, follow-through, recovering
ATTACK_FRAMES = 12


def attack_keys():
    a = IDLE_A
    lunge_legs = dict(th_n=-38, sh_n=42, th_f=26, sh_f=6)   # front knee bent, back leg nearly straight
    return [
        (1, a),
        (3, merged(a, root=(-5, -4), pelvis=-3, torso=-7, head=3,
                   ua_n=95, fa_n=-75, blade=70, ua_f=-10, fa_f=-30,
                   th_n=-8, sh_n=10, th_f=6, sh_f=6)),
        (5, merged(a, root=(-9, -8), pelvis=-5, torso=-12, head=7,
                   ua_n=175, fa_n=-95, blade=140, ua_f=-28, fa_f=-55,
                   th_n=-10, sh_n=14, th_f=8, sh_f=8)),
        (7, merged(a, root=(14, -24), pelvis=6, torso=14, head=0,
                   ua_n=285, fa_n=-5, blade=80, ua_f=40, fa_f=-50, **lunge_legs)),
        (8, merged(a, root=(16, -25), pelvis=7, torso=18, head=8,
                   ua_n=300, fa_n=-2, blade=86, ua_f=48, fa_f=-45, **lunge_legs)),
        (9, merged(a, root=(16, -25), pelvis=7, torso=19, head=12,
                   ua_n=306, fa_n=-2, blade=88, ua_f=50, fa_f=-42, **lunge_legs)),
        (11, merged(a, root=(8, -10), pelvis=4, torso=9, head=6,
                    ua_n=345, fa_n=-22, blade=45, ua_f=22, fa_f=-30,
                    th_n=-16, sh_n=18, th_f=12, sh_f=6)),
        (13, merged(a, ua_n=360 + a["ua_n"])),  # same pose as IDLE_A, arm keeps swinging clockwise into rest
    ]


ANIMS = {
    "idle": dict(frames=IDLE_PERIOD, loop=IDLE_PERIOD, tracks=True, keys=idle_keys),
    "run": dict(frames=RUN_PERIOD, loop=RUN_PERIOD, tracks=False, keys=run_keys),
    "attack": dict(frames=ATTACK_FRAMES, loop=None, tracks=False, keys=attack_keys),
}


# ----------------------------------------------------------------------------------------------
# Main
# ----------------------------------------------------------------------------------------------

def render_anim(scene, arm, name):
    spec = ANIMS[name]
    if spec["tracks"]:
        key_tracks(arm, spec["keys"](), loop_period=spec["loop"])
    else:
        key_animation(arm, spec["keys"](), loop_period=spec["loop"])
    out_dir = os.path.join(ANIM_DIR, name)
    os.makedirs(out_dir, exist_ok=True)
    for fn in os.listdir(out_dir):
        if fn.endswith(".png"):
            os.remove(os.path.join(out_dir, fn))
    scene.frame_start = 1
    scene.frame_end = spec["frames"]
    scene.render.filepath = os.path.join(out_dir, "")
    bpy.ops.render.render(animation=True)
    print(f"[rig] rendered {name}: {spec['frames']} frames -> {out_dir}")


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    engine = "EEVEE"
    save_blend = False
    names = []
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == "--engine":
            engine = argv[i + 1].upper()
            i += 1
        elif a == "--save-blend":
            save_blend = True
        else:
            names.append(a)
        i += 1
    if not names:
        names = ["idle", "run", "attack"]

    scene = reset_scene()
    set_engine(scene, engine)
    make_camera(scene)
    arm, _ = build_rig(scene)
    print(f"[rig] pelvis px {PELVIS_PX}, ground row px {GROUND_PX_Y}, "
          f"standing height {GROUND_PX_Y - (top_left('head')[1] + 2)} px")
    for n in names:
        render_anim(scene, arm, n)
    if save_blend:
        os.makedirs(ANIM_DIR, exist_ok=True)
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ANIM_DIR, "hero_rig.blend"))


if __name__ == "__main__":
    main()
