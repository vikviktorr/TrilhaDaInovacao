# Funções comuns aos modelos feitos no Blender (telefone_1986.py, radio_retro.py).
# Medidas em metros, Z para cima, frente para -Y. No Unity a frente chega como +Z:
# o construtor de cenas gira o modelo 180° para encará-lo ao visitante.
#
# Estilo low poly, como os modelos da Kenney dos cenários: poucos segmentos,
# faces chapadas (sombreamento flat) e cores lisas foscas, sem metal nem textura.
import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

RAIZ = Path(__file__).resolve().parents[2]


def limpar_cena():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def material(nome, cor, rugosidade=0.8, metal=0.0):
    m = bpy.data.materials.new(nome)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*cor, 1.0)
    bsdf.inputs["Roughness"].default_value = rugosidade
    bsdf.inputs["Metallic"].default_value = metal
    m.diffuse_color = (*cor, 1.0)
    return m


def objeto_de_bmesh(nome, bm, mat):
    malha = bpy.data.meshes.new(nome)
    bm.to_mesh(malha)
    bm.free()
    ob = bpy.data.objects.new(nome, malha)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat)
    return ob


def facetar(ob):
    """Sombreamento flat: cada face com uma cor só, como nos modelos da Kenney."""
    bpy.context.view_layer.objects.active = ob
    for o in bpy.context.selected_objects:
        o.select_set(False)
    ob.select_set(True)
    bpy.ops.object.shade_flat()


def juntar(nome, objetos):
    for o in bpy.context.selected_objects:
        o.select_set(False)
    for o in objetos:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objetos[0]
    bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    ob.name = ob.data.name = nome
    return ob


def aplicar_transformacoes(ob):
    for o in bpy.context.selected_objects:
        o.select_set(False)
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)


def anel_retangular(larg, prof, raio, z, cy, seg=2):
    """Contorno de retângulo arredondado (anti-horário visto de cima)."""
    raio = min(raio, larg / 2 - 1e-4, prof / 2 - 1e-4)
    hx, hy = larg / 2 - raio, prof / 2 - raio
    cantos = [(hx, hy, 0), (-hx, hy, 90), (-hx, -hy, 180), (hx, -hy, 270)]
    pts = []
    for cx, cyy, a0 in cantos:
        for i in range(seg + 1):
            a = math.radians(a0 + 90 * i / seg)
            pts.append((cx + raio * math.cos(a), cy + cyy + raio * math.sin(a), z))
    return pts


def loft(nome, aneis, mat, tampa_baixo=True, tampa_cima=True):
    bm = bmesh.new()
    camadas = [[bm.verts.new(p) for p in anel] for anel in aneis]
    n = len(aneis[0])
    for a, b in zip(camadas, camadas[1:]):
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((a[i], a[j], b[j], b[i]))
    if tampa_baixo:
        bm.faces.new(list(reversed(camadas[0])))
    if tampa_cima:
        bm.faces.new(camadas[-1])
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return objeto_de_bmesh(nome, bm, mat)


def torno(nome, perfil, mat, seg=10):
    """Sólido de revolução em torno de Z a partir de pontos (raio, z), de cima para baixo."""
    bm = bmesh.new()
    camadas = []
    for r, z in perfil:
        if r < 1e-6:
            camadas.append([bm.verts.new((0, 0, z))])
        else:
            camadas.append([bm.verts.new((r * math.cos(2 * math.pi * i / seg),
                                          r * math.sin(2 * math.pi * i / seg), z)) for i in range(seg)])
    for a, b in zip(camadas, camadas[1:]):
        if len(a) == 1 and len(b) == 1:
            continue
        for i in range(seg):
            j = (i + 1) % seg
            if len(a) == 1:
                bm.faces.new((a[0], b[i], b[j]))
            elif len(b) == 1:
                bm.faces.new((a[i], b[0], a[j]))
            else:
                bm.faces.new((a[i], b[i], b[j], a[j]))
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return objeto_de_bmesh(nome, bm, mat)


def cilindro(nome, raio, altura, mat, seg=12, centro=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=raio, radius2=raio, depth=altura,
                          matrix=Matrix.Translation(centro))
    return objeto_de_bmesh(nome, bm, mat)


def tubo(nome, pontos, raio, mat, lados=6, fechado=False):
    """Tubo ao longo de uma polilinha (curva com bevel, convertida em malha)."""
    curva = bpy.data.curves.new(nome, "CURVE")
    curva.dimensions = "3D"
    curva.bevel_depth = raio
    curva.bevel_resolution = max(0, lados // 4 - 1)
    curva.use_fill_caps = True
    sp = curva.splines.new("POLY")
    sp.points.add(len(pontos) - 1)
    for p, co in zip(sp.points, pontos):
        p.co = (*co, 1.0)
    sp.use_cyclic_u = fechado
    ob = bpy.data.objects.new(nome, curva)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat)
    for o in bpy.context.selected_objects:
        o.select_set(False)
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.convert(target="MESH")
    return bpy.context.view_layer.objects.active


def catmull_rom(pontos, passos):
    pts = [Vector(p) for p in pontos]
    pts = [pts[0]] + pts + [pts[-1]]
    saida = []
    for i in range(1, len(pts) - 2):
        p0, p1, p2, p3 = pts[i - 1], pts[i], pts[i + 1], pts[i + 2]
        for k in range(passos):
            t = k / passos
            saida.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                                + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t))
    saida.append(pts[-2])
    return saida


def fixar(objetos):
    """Aplica localização, rotação e escala de cada objeto na malha."""
    for o in bpy.context.selected_objects:
        o.select_set(False)
    for o in objetos:
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        o.select_set(False)


def exportar(rotulo, saida):
    for ob in bpy.data.objects:
        ob.select_set(ob.type == "MESH")
    tris = sum(len(p.vertices) - 2 for ob in bpy.data.objects if ob.type == "MESH" for p in ob.data.polygons)
    malhas = [o for o in bpy.data.objects if o.type == "MESH"]
    pts = [o.matrix_world @ Vector(c) for o in malhas for c in o.bound_box]
    tam = [max(p[i] for p in pts) - min(p[i] for p in pts) for i in range(3)]
    print(f"[{rotulo}] objetos: {[o.name for o in malhas]} · triângulos ≈ {tris} · "
          f"largura {tam[0]:.3f} · profundidade {tam[1]:.3f} · altura {tam[2]:.3f} m")

    saida.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(saida),
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
    )
    print(f"[{rotulo}] FBX: {saida}")


def previa(rotulo, arquivo, camera, alvo, lente=50):
    """Render de conferência (Workbench: mostra as cores dos materiais sem depender de luz)."""
    cena = bpy.context.scene
    cam = bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
    cena.collection.objects.link(cam)
    cam.data.lens = lente
    cam.location = camera
    cam.rotation_euler = (Vector(alvo) - Vector(camera)).to_track_quat("-Z", "Y").to_euler()
    cena.camera = cam
    cena.render.engine = "BLENDER_WORKBENCH"
    cena.display.shading.light = "STUDIO"
    cena.display.shading.color_type = "MATERIAL"
    cena.display.shading.show_specular_highlight = True
    cena.display.shading.show_cavity = True
    cena.display.shading.background_type = "VIEWPORT"
    cena.display.shading.background_color = (0.85, 0.85, 0.85)
    cena.render.film_transparent = False
    cena.view_settings.view_transform = "Standard"
    cena.render.resolution_x, cena.render.resolution_y = 1000, 750
    cena.render.filepath = str(arquivo)
    bpy.ops.render.render(write_still=True)
    print(f"[{rotulo}] prévia: {arquivo}")
