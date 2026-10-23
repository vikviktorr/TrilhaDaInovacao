using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrilhaDaInovacao.EditorTools
{
    /// <summary>
    /// Modelos 3D gratuitos (Kenney, CC0) usados nos cenários. Os arquivos vêm
    /// de Ferramentas/baixar-modelos.sh. O helper <see cref="Modelo"/> mede os
    /// bounds reais do FBX e escala para o tamanho pedido, então não importa a
    /// unidade em que cada pacote foi exportado.
    ///
    /// Os objetos históricos específicos (bastidores Trópico-R, HANBIT, SISNAV)
    /// continuam modelados no código, como pede a lista de assets do documento.
    /// </summary>
    public static partial class ConstrutorDeCenas
    {
        const string PastaModelos = "Assets/TrilhaDaInovacao/Modelos/";

        enum Medida { Altura, Largura, Profundidade, MaiorLado }

        /// <summary>
        /// Instancia um modelo com a base em <paramref name="posicaoLocal"/>
        /// (centro em x/z, chão em y) e escala uniforme até a medida pedida.
        /// </summary>
        static GameObject Modelo(string caminho, Transform pai, Vector3 posicaoLocal, float giroY, float tamanho,
            Medida medida = Medida.Altura)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{PastaModelos}{caminho}.fbx");
            if (asset == null)
            {
                Debug.LogWarning($"[Trilha da Inovação] Modelo '{caminho}' não encontrado. Rode Ferramentas/baixar-modelos.sh.");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            // Objeto comum (não instância de prefab): o construtor pode mesclá-lo depois.
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.transform.SetParent(pai, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0f, giroY, 0f);
            go.transform.localScale = Vector3.one;

            var b = BoundsNoPai(go, pai);
            var atual = medida switch
            {
                Medida.Altura => b.size.y,
                Medida.Largura => b.size.x,
                Medida.Profundidade => b.size.z,
                _ => Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z))
            };

            if (atual > 0.0001f) go.transform.localScale = Vector3.one * (tamanho / atual);

            b = BoundsNoPai(go, pai);
            go.transform.localPosition += new Vector3(posicaoLocal.x - b.center.x, posicaoLocal.y - b.min.y, posicaoLocal.z - b.center.z);

            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }

            return go;
        }

        /// <summary>Bounds do modelo no espaço do pai (o quadro da marca).</summary>
        static Bounds BoundsNoPai(GameObject go, Transform pai)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var inicial = true;
            var b = new Bounds();
            foreach (var r in renderers)
            {
                var rb = r.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var canto = rb.center + Vector3.Scale(rb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var local = pai != null ? pai.InverseTransformPoint(canto) : canto;
                    if (inicial)
                    {
                        b = new Bounds(local, Vector3.zero);
                        inicial = false;
                    }
                    else
                    {
                        b.Encapsulate(local);
                    }
                }
            }

            return b;
        }

        /// <summary>
        /// Personagens Kenney vêm em pose T; com importação Legacy o FBX traz as
        /// animações (idle, walk, interact-right, emote-yes...) e tocamos uma em loop.
        /// </summary>
        /// <summary>
        /// Personagem animado (mini-characters) de pé em <paramref name="posicaoLocal"/>,
        /// com <paramref name="altura"/> metros e um <see cref="Figurante"/> que alterna
        /// repouso e <paramref name="gestos"/>. Os bounds do SkinnedMeshRenderer não
        /// acompanham o corpo posado (o <see cref="Modelo"/> o deixava com ~1 m e
        /// flutuando), então a medida sai da malha assada na pose de repouso.
        /// </summary>
        static Figurante Personagem(string caminho, Transform pai, Vector3 posicaoLocal, float giroY, float altura,
            params string[] gestos)
        {
            // Os clipes animam a própria raiz do personagem (escala e posição): a escala
            // e o giro ficam num objeto acima dela, que a animação não toca.
            var suporte = new GameObject($"Personagem {System.IO.Path.GetFileName(caminho)}").transform;
            suporte.SetParent(pai, false);
            suporte.localPosition = posicaoLocal;
            suporte.localRotation = Quaternion.Euler(0f, giroY, 0f);

            var go = Modelo(caminho, suporte, Vector3.zero, 0f, altura);
            if (go == null) return null;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var anim = go.GetComponent<Animation>();
            var repouso = anim != null ? anim.GetClip("idle") : null;
            if (repouso == null)
            {
                Debug.LogWarning($"[Trilha da Inovação] '{caminho}' sem animação 'idle'.");
                return null;
            }

            repouso.SampleAnimation(go, 0f);
            var b = BoundsAssados(go, suporte);
            var escala = altura / b.size.y;
            suporte.localScale = Vector3.one * escala;
            // A origem do personagem fica entre os pés; o suporte desce até eles tocarem o chão.
            suporte.localPosition = posicaoLocal - Vector3.up * (b.min.y * escala);

            anim.clip = repouso;
            anim.playAutomatically = true;
            anim.cullingType = AnimationCullingType.BasedOnRenderers;
            var figurante = go.AddComponent<Figurante>();
            figurante.gestos = gestos.Where(g => anim.GetClip(g) != null).ToArray();
            return figurante;
        }

        /// <summary>Bounds, no espaço do pai, das malhas com skin na pose atual.</summary>
        static Bounds BoundsAssados(GameObject go, Transform pai)
        {
            var b = new Bounds();
            var inicial = true;
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var assada = new Mesh();
                smr.BakeMesh(assada, false);
                foreach (var v in assada.vertices)
                {
                    var p = pai.InverseTransformPoint(smr.transform.TransformPoint(v));
                    if (inicial) { b = new Bounds(p, Vector3.zero); inicial = false; }
                    else b.Encapsulate(p);
                }

                Object.DestroyImmediate(assada);
            }

            return b;
        }

        /// <summary>Junta várias peças de mesmo material numa malha só (menos draw calls).</summary>
        static Renderer Combinar(string nome, Transform pai, List<GameObject> partes, Material material)
        {
            var combinar = new List<CombineInstance>();
            foreach (var p in partes)
            {
                combinar.Add(new CombineInstance
                {
                    mesh = p.GetComponent<MeshFilter>().sharedMesh,
                    transform = pai.worldToLocalMatrix * p.transform.localToWorldMatrix
                });
            }

            var malha = new Mesh { name = nome };
            malha.CombineMeshes(combinar.ToArray(), true, true);
            foreach (var p in partes) Object.DestroyImmediate(p);

            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            go.AddComponent<MeshFilter>().sharedMesh = malha;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return r;
        }

        /// <summary>
        /// Mescla tudo que nunca se move numa malha por material, para caber no
        /// orçamento de draw calls do Quest 2. Fica de fora (continua separado):
        /// o que tem componente animado no caminho até a raiz, Transforms e
        /// Renderers referenciados por scripts (movidos, escalados ou com cor
        /// alterada). Objetos ligados/desligados por uma sequência (campos
        /// GameObject) são mesclados dentro do próprio grupo, que continua
        /// podendo ser escondido.
        /// </summary>
        /// <summary>
        /// Avisa no log quando dois objetos do cenário se atravessam (um móvel dentro
        /// do outro, um modelo enfiado na parede). Compara as caixas justas de cada
        /// malha no mundo, entre objetos de grupos diferentes
        /// (filhos diretos do quadro); encostar (apoiar na mesa, no chão, na parede) não conta.
        /// </summary>
        static void AuditarSobreposicoes(UnityEngine.SceneManagement.Scene cena, string nomeCena)
        {
            const float tolerancia = 0.03f;
            var pecas = new List<(Transform grupo, string nome, Bounds caixa)>();
            foreach (var raiz in cena.GetRootGameObjects())
            foreach (var filtro in raiz.GetComponentsInChildren<MeshFilter>())
            {
                if (filtro.sharedMesh == null || !filtro.gameObject.activeInHierarchy) continue;
                var r = filtro.GetComponent<MeshRenderer>();
                if (r == null || r.sharedMaterial == null || r.sharedMaterial.renderQueue >= 3000) continue;
                var grupo = filtro.transform;
                while (grupo.parent != null && grupo.parent != raiz.transform) grupo = grupo.parent;
                // Piso e teto sustentam tudo: apoiar neles não é atravessar.
                if (grupo.name == "Piso" || grupo.name == "Teto" || grupo.name.StartsWith("Piso ")) continue;

                var vertices = filtro.sharedMesh.vertices;
                if (vertices.Length == 0) continue;
                var paraRaiz = filtro.transform.localToWorldMatrix;
                var caixa = new Bounds(paraRaiz.MultiplyPoint3x4(vertices[0]), Vector3.zero);
                foreach (var v in vertices) caixa.Encapsulate(paraRaiz.MultiplyPoint3x4(v));
                pecas.Add((grupo, $"{grupo.name}/{filtro.name}", caixa));
            }

            var avisos = 0;
            for (var i = 0; i < pecas.Count; i++)
            for (var j = i + 1; j < pecas.Count; j++)
            {
                var (ga, na, a) = pecas[i];
                var (gb, nb, b) = pecas[j];
                if (ga == gb || Esperada(ga.name, gb.name)) continue;
                var dentro = Vector3.Min(a.max, b.max) - Vector3.Max(a.min, b.min);
                if (dentro.x <= tolerancia || dentro.y <= tolerancia || dentro.z <= tolerancia) continue;
                avisos++;
                Debug.LogWarning($"[Trilha da Inovação] {nomeCena}: '{na}' {a.min:F2}..{a.max:F2} atravessa '{nb}' {b.min:F2}..{b.max:F2} " +
                                 $"({dentro.x * 100:0} × {dentro.y * 100:0} × {dentro.z * 100:0} cm).");
            }

            if (avisos == 0) Debug.Log($"[Trilha da Inovação] {nomeCena}: nenhum objeto atravessando outro.");
        }

        /// <summary>
        /// Sobreposições de propósito: paredes se cruzam nos cantos, vegetação cresce
        /// junta, chão e mar se encontram, e o Sebraelab é uma malha só (a caixa dele
        /// é o prédio inteiro, onde o hub fica).
        /// </summary>
        static bool Esperada(string a, string b)
        {
            bool Natureza(string n) => n.StartsWith("plant_") || n.StartsWith("tree_") || n.StartsWith("grass") || n.StartsWith("rock_");
            bool Chao(string n) => n == "Terreno" || n == "Mar" || n.StartsWith("Chão") || n.StartsWith("Sebraelab");
            bool Muro(string n) => n.StartsWith("Parede") || n.StartsWith("Parapeito");
            return Muro(a) && Muro(b) || Natureza(a) && Natureza(b) || Chao(a) || Chao(b);
        }

        static void MesclarEstaticos(UnityEngine.SceneManagement.Scene cena, string nomeCena)
        {
            var dinamicos = new HashSet<Transform>();
            var grupos = new HashSet<Transform>();
            var raizes = cena.GetRootGameObjects();

            foreach (var comp in raizes.SelectMany(r => r.GetComponentsInChildren<Component>(true)))
            {
                if (comp == null) continue;
                if (comp is MonoBehaviour || comp is ParticleSystem || comp is Light || comp is Animation ||
                    comp is SkinnedMeshRenderer || comp is LineRenderer || comp is TrailRenderer || comp is AudioSource)
                {
                    dinamicos.Add(comp.transform);
                }

                if (!(comp is MonoBehaviour)) continue;
                var prop = new SerializedObject(comp).GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    switch (prop.objectReferenceValue)
                    {
                        case GameObject go: grupos.Add(go.transform); break;
                        case Transform t: dinamicos.Add(t); break;
                        case Component c: dinamicos.Add(c.transform); break;
                    }
                }
            }

            // Agrupa por (grupo que liga/desliga, material equivalente). Cada FBX da
            // Kenney traz cópias próprias de materiais iguais; a chave os unifica.
            var lotes = new Dictionary<(Transform, string), List<CombineInstance>>();
            var representante = new Dictionary<string, Material>();
            var mesclados = new List<MeshRenderer>();
            foreach (var r in raizes.SelectMany(o => o.GetComponentsInChildren<MeshRenderer>(true)))
            {
                var filtro = r.GetComponent<MeshFilter>();
                if (filtro == null || filtro.sharedMesh == null || !r.gameObject.activeInHierarchy && !DentroDeGrupo(r.transform, grupos)) continue;

                Transform grupo = null;
                var podeMesclar = true;
                for (var t = r.transform; t != null; t = t.parent)
                {
                    if (dinamicos.Contains(t)) { podeMesclar = false; break; }
                    if (grupo == null && grupos.Contains(t)) grupo = t;
                }

                if (!podeMesclar) continue;
                var malha = filtro.sharedMesh;
                var mats = r.sharedMaterials;
                for (var i = 0; i < malha.subMeshCount && i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    var id = ChaveMaterial(mats[i]);
                    if (!representante.ContainsKey(id)) representante[id] = mats[i];
                    var chave = (grupo, id);
                    if (!lotes.TryGetValue(chave, out var lista)) lotes[chave] = lista = new List<CombineInstance>();
                    var paraGrupo = grupo != null ? grupo.worldToLocalMatrix : Matrix4x4.identity;
                    lista.Add(new CombineInstance { mesh = malha, subMeshIndex = i, transform = paraGrupo * r.transform.localToWorldMatrix });
                }

                mesclados.Add(r);
            }

            var pasta = $"{PastaGerado}/Malhas";
            System.IO.Directory.CreateDirectory(pasta);
            var raizMescla = new GameObject("Geometria estática (mesclada)").transform;
            var n = 0;
            foreach (var lote in lotes)
            {
                var (grupo, id) = lote.Key;
                var material = representante[id];
                var malha = new Mesh { name = $"{nomeCena} {material.name}", indexFormat = IndexFormat.UInt32 };
                malha.CombineMeshes(lote.Value.ToArray(), true, true);
                malha.RecalculateBounds();
                var caminho = $"{pasta}/{nomeCena}_{n++:000}.asset";
                AssetDatabase.DeleteAsset(caminho);
                AssetDatabase.CreateAsset(malha, caminho);

                var go = new GameObject($"Mescla · {material.name}");
                go.transform.SetParent(grupo != null ? grupo : raizMescla, false);
                go.AddComponent<MeshFilter>().sharedMesh = malha;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = material;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                // Colisão do cenário para o modo desktop (no Quest o visitante anda no espaço real).
                go.layer = LayoutTrilha.CamadaCenario;
                go.AddComponent<MeshCollider>().sharedMesh = malha;
                // Sem static batching: a malha já é única e precisa continuar
                // legível no build (o QA testa o corredor pelos vértices).
            }

            // Remove os originais (e pais que ficaram vazios, como instâncias de modelos).
            foreach (var r in mesclados)
            {
                if (r == null) continue;
                var go = r.gameObject;
                Object.DestroyImmediate(r.GetComponent<MeshFilter>());
                Object.DestroyImmediate(r);
                if (go.transform.childCount == 0 && go.GetComponents<Component>().Length == 1) Object.DestroyImmediate(go);
            }

            Debug.Log($"[Trilha da Inovação] {nomeCena}: {mesclados.Count} renderers estáticos mesclados em {lotes.Count} malhas.");
        }

        static string ChaveMaterial(Material m)
        {
            Color Cor(string p) => m.HasProperty(p) ? m.GetColor(p) : Color.clear;
            var tex = m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")) : "-";
            var emissao = m.IsKeywordEnabled("_EMISSION") ? ColorUtility.ToHtmlStringRGB(Cor("_EmissionColor")) : "0";
            var liso = m.HasProperty("_Smoothness") ? Mathf.Round(m.GetFloat("_Smoothness") * 10f) : 0f;
            var superficie = m.HasProperty("_Surface") ? m.GetFloat("_Surface") : 0f;
            return $"{m.shader.name}|{tex}|{ColorUtility.ToHtmlStringRGBA(Cor("_BaseColor"))}|{emissao}|{liso}|{superficie}";
        }

        static bool DentroDeGrupo(Transform t, HashSet<Transform> grupos)
        {
            for (; t != null; t = t.parent) if (grupos.Contains(t)) return true;
            return false;
        }

        static void TrocarMateriais(GameObject go, Material material)
        {
            if (go == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var lista = new Material[r.sharedMaterials.Length];
                for (var i = 0; i < lista.Length; i++) lista[i] = material;
                r.sharedMaterials = lista;
            }
        }

        // ------------------------------------------------------------------
        // Radar: pedestal fixo + cabeça giratória.
        // ------------------------------------------------------------------

        /// <summary>
        /// O satelliteDish_large da Kenney vem numa malha só. Corta a malha no
        /// rolamento do topo do pedestal: bloco da base e pedestal ficam no
        /// modelo (fixos); garfo e refletor vão para um objeto filho de
        /// <paramref name="giro"/>, que é posto sobre o eixo do pedestal. Devolve
        /// (no mundo) o centro do prato e a direção para onde ele aponta.
        /// </summary>
        static void SepararCabecaDoRadar(GameObject modelo, Transform giro, out Vector3 centroPrato, out Vector3 eixoPrato)
        {
            var filtro = modelo.GetComponentInChildren<MeshFilter>();
            var renderer = filtro.GetComponent<MeshRenderer>();
            var original = filtro.sharedMesh;
            var vertices = original.vertices;
            var b = original.bounds;
            var h = b.size.y;

            // Proporções do FBX (altura total 8,14): bloco da base até 0,5; pedestal
            // tronco-cônico até o rolamento em 3,73, com meia-largura 1,0 no topo.
            var topoBloco = b.min.y + h * (0.5f / 8.14f) + h * 0.005f;
            var rolamento = b.min.y + h * (3.73f / 8.14f) + h * 0.005f;
            var meiaLargura = h * (1f / 8.14f) + h * 0.005f;

            var bloco = new Bounds();
            var primeiro = true;
            foreach (var v in vertices)
            {
                if (v.y > topoBloco) continue;
                if (primeiro) { bloco = new Bounds(v, Vector3.zero); primeiro = false; }
                else bloco.Encapsulate(v);
            }

            var eixo = bloco.center;
            var naBase = new bool[vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                naBase[i] = v.y <= topoBloco ||
                            v.y <= rolamento && Mathf.Abs(v.x - eixo.x) <= meiaLargura && Mathf.Abs(v.z - eixo.z) <= meiaLargura;
            }

            filtro.sharedMesh = ParteDaMalha(original, naBase, true, "Radar pedestal");
            var cabeca = new GameObject("Cabeça do radar");
            cabeca.transform.SetParent(filtro.transform, false);
            cabeca.AddComponent<MeshFilter>().sharedMesh = ParteDaMalha(original, naBase, false, "Radar cabeça");
            var mr = cabeca.AddComponent<MeshRenderer>();
            mr.sharedMaterials = renderer.sharedMaterials;
            mr.shadowCastingMode = renderer.shadowCastingMode;
            mr.receiveShadows = renderer.receiveShadows;

            giro.position = filtro.transform.TransformPoint(new Vector3(eixo.x, rolamento, eixo.z));
            cabeca.transform.SetParent(giro, true);

            // Borda do prato: o ponto mais alto (atrás) e o mais baixo (na frente,
            // 2,93 de altura e 2,4 à frente do eixo). O eixo do refletor é
            // perpendicular à corda entre os dois, apontando para cima.
            var topo = vertices[0];
            foreach (var v in vertices) if (v.y > topo.y) topo = v;
            var frente = Mathf.Sign(eixo.z - topo.z);
            var baixo = new Vector3(eixo.x, b.min.y + h * (2.93f / 8.14f), eixo.z + frente * h * (2.4f / 8.14f));
            var corda = baixo - topo;
            var normal = new Vector3(0f, Mathf.Abs(corda.z), frente * Mathf.Abs(corda.y)).normalized;
            centroPrato = filtro.transform.TransformPoint((topo + baixo) * 0.5f);
            eixoPrato = filtro.transform.TransformDirection(normal).normalized;
        }

        /// <summary>Cópia da malha só com os triângulos de um lado do corte, salva como asset.</summary>
        static Mesh ParteDaMalha(Mesh original, bool[] naBase, bool base_, string nome)
        {
            var pasta = $"{PastaGerado}/Malhas";
            System.IO.Directory.CreateDirectory(pasta);
            var caminho = $"{pasta}/{nome}.asset";
            var malha = AssetDatabase.LoadAssetAtPath<Mesh>(caminho);
            if (malha == null)
            {
                malha = new Mesh();
                AssetDatabase.CreateAsset(malha, caminho);
            }

            malha.Clear();
            malha.name = nome;
            malha.vertices = original.vertices;
            malha.normals = original.normals;
            malha.uv = original.uv;
            malha.colors = original.colors;
            malha.subMeshCount = original.subMeshCount;
            for (var s = 0; s < original.subMeshCount; s++)
            {
                var tris = original.GetTriangles(s);
                var parte = new List<int>();
                for (var i = 0; i < tris.Length; i += 3)
                {
                    var ehBase = naBase[tris[i]] && naBase[tris[i + 1]] && naBase[tris[i + 2]];
                    if (ehBase == base_) parte.AddRange(new[] { tris[i], tris[i + 1], tris[i + 2] });
                }

                malha.SetTriangles(parte, s);
            }

            malha.RecalculateBounds();
            EditorUtility.SetDirty(malha);
            return malha;
        }

        // ------------------------------------------------------------------
        // Hub: o Sebraelab de verdade (pacote SebraeLabUnityAssets).
        // ------------------------------------------------------------------
        const string ModeloSebraelab = "Assets/Sebraelab3D/SM_Combined.fbx";

        /// <summary>Material do FBX → material gerado (nome, lisura).</summary>
        static readonly Dictionary<string, (string nome, float liso)> materiaisSebraelab = new Dictionary<string, (string, float)>
        {
            ["Parede"] = ("SebraelabParede", 0.1f),
            ["Calçada"] = ("SebraelabCalcada", 0.05f),
            ["Chao Azulejo"] = ("SebraelabPisoAzulejo", 0.4f),
            ["Chao Madeira"] = ("SebraelabPisoMadeira", 0.3f),
            ["Frame Metal"] = ("SebraelabMetal", 0.5f),
            ["CombinedTexture"] = ("SebraelabMobilia", 0.2f)
        };

        /// <summary>
        /// Instancia o modelo do Sebraelab em escala 1:1, com o centro da área
        /// livre (<see cref="LayoutTrilha.CentroNoSebraelab"/>) na origem da
        /// trilha. Falso se o pacote não foi importado.
        /// </summary>
        static bool MontarSebraelab(Transform sala)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModeloSebraelab);
            if (asset == null)
            {
                Debug.LogWarning("[Trilha da Inovação] Modelo do Sebraelab não encontrado. " +
                                 "Importe SebraeLabUnityAssets.unitypackage (fica em Assets/Sebraelab3D).");
                return false;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = "Sebraelab (modelo 3D)";
            go.transform.SetParent(sala, false);
            go.transform.localPosition = -LayoutTrilha.CentroNoSebraelab;

            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.sharedMaterials = r.sharedMaterials.Select(MaterialSebraelab).ToArray();
            }

            // O modelo é uma casca de faces simples voltadas para fora, e o visitante
            // fica dentro dela: sem o lado de dentro, paredes e teto somem e o céu aparece.
            foreach (var filtro in go.GetComponentsInChildren<MeshFilter>())
                filtro.sharedMesh = MalhaDuplaFace(filtro.sharedMesh, $"Sebraelab {filtro.name} dupla face");

            return true;
        }

        /// <summary>
        /// Cópia da malha com cada triângulo também do lado de trás (ordem invertida e
        /// normais negadas), para ser vista e iluminada de dentro e de fora. O verso
        /// sai 2 mm para o seu lado: o modelo tem faces coplanares (calçada sob o
        /// piso, telhado sobre o forro) que, sem isso, piscam em z-fighting.
        /// Salva como asset.
        /// </summary>
        static Mesh MalhaDuplaFace(Mesh original, string nome)
        {
            const float afastamento = 0.002f;
            var pasta = $"{PastaGerado}/Malhas";
            System.IO.Directory.CreateDirectory(pasta);
            var caminho = $"{pasta}/{nome}.asset";
            var malha = AssetDatabase.LoadAssetAtPath<Mesh>(caminho);
            if (malha == null)
            {
                malha = new Mesh();
                AssetDatabase.CreateAsset(malha, caminho);
            }

            var n = original.vertexCount;
            var vertices = original.vertices;
            var normais = original.normals;
            var uv = original.uv;
            malha.Clear();
            malha.name = nome;
            malha.indexFormat = IndexFormat.UInt32;
            malha.vertices = vertices.Concat(vertices.Select((v, i) => v - normais[i] * afastamento)).ToArray();
            malha.normals = normais.Concat(normais.Select(v => -v)).ToArray();
            if (uv.Length == n) malha.uv = uv.Concat(uv).ToArray();
            malha.subMeshCount = original.subMeshCount;
            for (var s = 0; s < original.subMeshCount; s++)
            {
                var tris = original.GetTriangles(s);
                var dupla = new int[tris.Length * 2];
                tris.CopyTo(dupla, 0);
                for (var i = 0; i < tris.Length; i += 3)
                {
                    dupla[tris.Length + i] = tris[i] + n;
                    dupla[tris.Length + i + 1] = tris[i + 2] + n;
                    dupla[tris.Length + i + 2] = tris[i + 1] + n;
                }

                malha.SetTriangles(dupla, s);
            }

            malha.RecalculateBounds();
            EditorUtility.SetDirty(malha);
            return malha;
        }

        /// <summary>
        /// Troca o material embutido no FBX (todos com lisura 0,5) por um gerado,
        /// com a mesma cor e textura, como os demais materiais do projeto.
        /// </summary>
        static Material MaterialSebraelab(Material importado)
        {
            if (importado == null) return null;
            var cor = importado.GetColor("_BaseColor");
            if (importado.name == "Vidro") return Transparente("SebraelabVidro", new Color(cor.r, cor.g, cor.b, 0.18f));

            var (nome, liso) = materiaisSebraelab.TryGetValue(importado.name, out var par) ? par : ("Sebraelab" + importado.name, 0.2f);
            var m = Opaco(nome, cor, smooth: liso);
            m.SetTexture("_BaseMap", importado.GetTexture("_BaseMap"));
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------
        // Hub sem o pacote: mobiliário da réplica, só junto às paredes.
        // ------------------------------------------------------------------
        static void DecorarHub(Transform sala)
        {
            const string m = "Kenney/furniture-kit/";
            Modelo(m + "loungeSofa", sala, new Vector3(0.5f, 0f, 1.68f), 180f, 1.8f, Medida.Largura);
            Modelo(m + "tableCoffee", sala, new Vector3(0.5f, 0f, 1.2f), 0f, 0.8f, Medida.Largura);
            Modelo(m + "bookcaseOpen", sala, new Vector3(-1.1f, 0f, 1.82f), 180f, 1.8f);
            Modelo(m + "pottedPlant", sala, new Vector3(2.2f, 0f, 1.75f), 0f, 1.1f);
            Modelo(m + "pottedPlant", sala, new Vector3(-2.25f, 0f, -1.78f), 30f, 1f);
            Modelo(m + "lampSquareFloor", sala, new Vector3(1.6f, 0f, 1.8f), 0f, 1.5f);
            Modelo(m + "plantSmall1", sala, new Vector3(-0.3f, 0f, 1.85f), 0f, 0.45f);
        }

        // ------------------------------------------------------------------
        // 1986: mobília de escritório da época ao redor da central.
        // ------------------------------------------------------------------
        static void Decorar1986(Transform q)
        {
            const string f = "Kenney/furniture-kit/";
            // Parede esquerda (face em x = -4,35), de trás para a frente: estante,
            // console de operação e a cadeira de frente para ele.
            Modelo("Kenney/space-kit/desk_computer", q, new Vector3(-3.99f, 0f, 2.0f), 90f, 1.6f, Medida.MaiorLado);
            Modelo(f + "chairDesk", q, new Vector3(-3.3f, 0f, 2.0f), -90f, 0.95f);
            Modelo(f + "bookcaseClosedWide", q, new Vector3(-4.04f, 0f, -0.3f), 90f, 1.9f);
            Modelo(f + "coatRackStanding", q, new Vector3(-4.0f, 0f, 5.9f), 0f, 1.75f);
            // Rádio retrô (Ferramentas/blender/radio_retro.py), 1:1: 30,6 cm de largura com a antena.
            // Os bounds incluem a antena, que avança 2,3 cm para a esquerda: o gabinete
            // fica em x 0,08…0,34, no balcão e longe do fio do telefone.
            Modelo("Radio/radio-retro", q, new Vector3(0.187f, 0.88f, 0.85f), 180f, 0.306f, Medida.Largura);
            // Parede direita (face em x = 5,55).
            Modelo(f + "televisionVintage", q, new Vector3(5.15f, 0f, 0.6f), -90f, 0.75f);
            Modelo(f + "cardboardBoxClosed", q, new Vector3(5.1f, 0f, 5.8f), 15f, 0.5f);
            Modelo(f + "cardboardBoxOpen", q, new Vector3(5.15f, 0f, 5.1f), -10f, 0.45f);
            Modelo(f + "trashcan", q, new Vector3(0.55f, 0f, 0.85f), 0f, 0.35f);
        }

        // ------------------------------------------------------------------
        // 1994: vegetação de restinga, palmeiras e estação de instrumentos.
        // ------------------------------------------------------------------
        /// <summary>Base do radar, atrás do corrimão do deck (z = 2,55) com folga para o prato girar.</summary>
        static readonly Vector3 PosicaoRadar1994 = new Vector3(2.6f, 0f, 3.75f);

        static void Decorar1994(Transform q)
        {
            const string n = "Kenney/nature-kit/";
            var arbustos = new[] { "plant_bush", "plant_bushLarge", "plant_bushDetailed", "plant_bushSmall", "grass_large", "grass" };
            var rng = new System.Random(1994);
            // (centro, raio) que a vegetação não pode invadir.
            var reservados = new[]
            {
                (new Vector3(6f, 0f, 9f), 2.3f), (new Vector3(-9f, 0f, 7f), 1.9f),
                (PosicaoRadar1994, 1.2f), (new Vector3(4.4f, 0f, 2.2f), 0.7f)
            };
            bool Livre(Vector3 p, float folga) => reservados.All(r => (p - r.Item1).magnitude > r.Item2 + folga);
            for (var i = 0; i < 90; i++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f;
                var r = 4.5f + (float)rng.NextDouble() * 34f;
                var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (p.z > 36f || !Livre(p, 0.9f)) continue;
                Modelo(n + arbustos[i % arbustos.Length], q, p, (float)rng.NextDouble() * 360f,
                    0.5f + (float)rng.NextDouble() * 1.2f);
            }

            var palmeiras = new[] { "tree_palmTall", "tree_palmDetailedTall", "tree_palmBend" };
            for (var i = 0; i < 14; i++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f;
                var r = 9f + (float)rng.NextDouble() * 28f;
                var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (p.z > 34f || Mathf.Abs(p.x + 5f) < 3f && p.z > 10f || !Livre(p, 4f)) continue;
                Modelo(n + palmeiras[i % palmeiras.Length], q, p, (float)rng.NextDouble() * 360f, 6f + (float)rng.NextDouble() * 4f);
            }

            Modelo(n + "rock_largeA", q, new Vector3(4.4f, 0f, 2.2f), 40f, 1.2f, Medida.MaiorLado);
            Modelo(n + "rock_smallA", q, new Vector3(-2.6f, 0f, 2.9f), 0f, 0.4f, Medida.MaiorLado);
            Modelo(n + "grass_large", q, new Vector3(2.4f, 0f, 1.2f), 0f, 0.45f);
            Modelo(n + "grass", q, new Vector3(-2.5f, 0f, 0.5f), 0f, 0.35f);

            // Antenas extras do campo de lançamento, ao fundo.
            Modelo("Kenney/space-kit/satelliteDish_detailed", q, new Vector3(6f, 0f, 9f), -140f, 3.2f);
            Modelo("Kenney/space-kit/satelliteDish_detailed", q, new Vector3(-9f, 0f, 7f), 150f, 2.6f);
            Modelo("Kenney/space-kit/desk_chair", q, new Vector3(-1.7f, 0f, 1.45f), 180f, 0.95f);
        }

        // ------------------------------------------------------------------
        // 2009: sala de operação de rede.
        // ------------------------------------------------------------------
        static void Decorar2009(Transform q)
        {
            const string f = "Kenney/furniture-kit/";
            Modelo(f + "chairDesk", q, new Vector3(-1.25f, 0f, 1.0f), 90f, 0.95f);
            Modelo(f + "chairDesk", q, new Vector3(1.25f, 0f, 1.0f), -90f, 0.95f);
            for (var i = 0; i < 2; i++)
            {
                var x = -1.8f + i * 1.3f;
                Modelo(f + "desk", q, new Vector3(x, 0f, 3.35f), 180f, 1.2f, Medida.Largura);
                Modelo(f + "computerScreen", q, new Vector3(x, 0.75f, 3.45f), 180f, 0.45f, Medida.Largura);
                Modelo(f + "computerKeyboard", q, new Vector3(x, 0.75f, 3.2f), 180f, 0.4f, Medida.Largura);
                Modelo(f + "chairDesk", q, new Vector3(x, 0f, 2.8f), 0f, 0.95f);
            }

            Modelo(f + "bookcaseClosedWide", q, new Vector3(-2.64f, 0f, 1.5f), 90f, 1.9f);
            Modelo(f + "pottedPlant", q, new Vector3(2.6f, 0f, -0.9f), 0f, 1f);
            Modelo("Kenney/space-kit/machine_wireless", q, new Vector3(2.4f, 0f, 2f), -90f, 1.3f);
        }

        // ------------------------------------------------------------------
        // 2018: maker space com pessoas, robô e estações de trabalho.
        // ------------------------------------------------------------------
        static (Figurante estudante, Figurante pesquisador, Figurante empreendedora) Decorar2018(Transform q)
        {
            const string f = "Kenney/furniture-kit/";
            const string c = "Kenney/mini-characters/";
            // Pessoas com uma tarefa cada, longe dos móveis: colando post-its na parede
            // de ideias, operando a cortadora a laser (de lado para o visitante) e
            // conferindo o notebook, que comenta com o visitante de vez em quando.
            var postIts = Personagem(c + "character-female-a", q, new Vector3(-1.85f, 0f, 3.0f), 15f, 1.62f,
                "interact-right", "interact-left");
            postIts.intervalo = new Vector2(1.5f, 4f);
            postIts.repeticoes = new Vector2Int(1, 2);
            var laser = Personagem(c + "character-male-b", q, new Vector3(0.7f, 0f, 2.5f), 90f, 1.72f, "interact-right", "interact-left");
            laser.intervalo = new Vector2(2f, 5f);
            var notebook = Personagem(c + "character-female-c", q, new Vector3(-1.65f, 0f, 1.85f), -150f, 1.6f, "emote-yes", "emote-no");
            DarVoz(postIts, "Estudante");
            DarVoz(laser, "Pesquisador");
            DarVoz(notebook, "Empreendedora");

            Modelo(f + "desk", q, new Vector3(-2.3f, 0f, 0.3f), 90f, 1.2f, Medida.Profundidade);
            Modelo(f + "laptop", q, new Vector3(-2.3f, 0.76f, 0.3f), 90f, 0.32f, Medida.Largura);
            Modelo(f + "chairDesk", q, new Vector3(-1.75f, 0f, 0.3f), -90f, 0.95f);
            Modelo(f + "chairDesk", q, new Vector3(1.45f, 0f, 1.45f), 200f, 0.95f);   // entre o guia, o operador da cortadora e a bancada do robô
            Modelo(f + "bookcaseOpen", q, new Vector3(0.2f, 0f, 3.68f), 180f, 1.8f);
            Modelo(f + "pottedPlant", q, new Vector3(2.6f, 0f, -0.5f), 0f, 1f);
            Modelo(f + "cardboardBoxClosed", q, new Vector3(2.7f, 0f, 3.4f), 20f, 0.45f);
            Modelo(f + "cardboardBoxOpen", q, new Vector3(2.0f, 0f, 3.55f), -15f, 0.4f);

            // Bancada secundária com braço robótico.
            Bloco("Bancada do robô", q, new Vector3(2.4f, 0.4f, 1.2f), new Vector3(0.7f, 0.8f, 1.2f), Opaco("2018Bancada", Color.white));
            Modelo("Kenney/factory-kit/robot-arm-a", q, new Vector3(2.4f, 0.8f, 1.2f), -90f, 0.75f);
            return (postIts, laser, notebook);
        }

        /// <summary>Fonte 3D da voz na cabeça do figurante, com o nome que vai na legenda.</summary>
        static void DarVoz(Figurante figurante, string papel)
        {
            if (figurante == null) return;
            figurante.papel = papel;
            var fonte = new GameObject("Voz").AddComponent<AudioSource>();
            fonte.transform.SetParent(figurante.transform.parent, false);
            fonte.transform.localPosition = Vector3.up * 1.5f / figurante.transform.parent.localScale.y;
            // Voz presente em todo o laboratório (volume cheio até 4 m) e só em parte
            // 3D: dá para saber quem fala sem a distância deixá-la abaixo dos efeitos.
            fonte.playOnAwake = false;
            fonte.spatialBlend = 0.6f;
            fonte.minDistance = 4f;
            fonte.maxDistance = 12f;
            fonte.rolloffMode = AudioRolloffMode.Logarithmic;
            figurante.voz = fonte;
        }

        // ------------------------------------------------------------------
        // 2022: a cidade vista do terraço, com prédios do City Kit.
        // ------------------------------------------------------------------
        static void CidadeKenney(Transform q, float abaixo)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PastaModelos + "Kenney/city-kit-commercial/Textures/variation-a.png");
            var noturno = Opaco("2022CidadeKenney", new Color(0.22f, 0.24f, 0.34f), new Color(0.22f, 0.18f, 0.1f));
            if (tex != null)
            {
                noturno.SetTexture("_BaseMap", tex);
                noturno.SetTexture("_EmissionMap", tex);
                EditorUtility.SetDirty(noturno);
            }

            var predios = new[]
            {
                "building-a", "building-b", "building-c", "building-d", "building-e", "building-f", "building-g", "building-h",
                "building-skyscraper-a", "building-skyscraper-b", "building-skyscraper-c",
                "low-detail-building-a", "low-detail-building-b", "low-detail-building-c", "low-detail-building-d",
                "low-detail-building-e", "low-detail-building-wide-a", "low-detail-building-wide-b"
            };

            var raiz = new GameObject("Cidade (City Kit)").transform;
            raiz.SetParent(q, false);
            var rng = new System.Random(2022);
            for (var i = 0; i < 110; i++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f;
                var r = 22f + (float)rng.NextDouble() * 95f;
                var nome = predios[rng.Next(predios.Length)];
                var arranha = nome.Contains("skyscraper");
                var go = Modelo("Kenney/city-kit-commercial/" + nome, raiz,
                    new Vector3(Mathf.Cos(a) * r, abaixo, 1f + Mathf.Sin(a) * r), rng.Next(4) * 90f,
                    (arranha ? 9f : 7f) + (float)rng.NextDouble() * 5f, Medida.Largura);
                TrocarMateriais(go, noturno);
            }
        }

        // ------------------------------------------------------------------
        // 2023–2025: equipamentos do hangar e da base de lançamento.
        // ------------------------------------------------------------------
        static void Decorar2023(Transform parteA)
        {
            const string s = "Kenney/space-kit/";
            // Medidos pelo maior lado: o gerador grande da Kenney é largo e tampava o foguete.
            Modelo(s + "machine_generatorLarge", parteA, new Vector3(3.1f, 0f, 4.4f), -90f, 1.6f, Medida.MaiorLado);
            Modelo(s + "machine_generator", parteA, new Vector3(-3.1f, 0f, 4.4f), 90f, 1.1f, Medida.MaiorLado);
            Modelo(s + "barrels", parteA, new Vector3(3.3f, 0f, 1.2f), 0f, 0.8f, Medida.MaiorLado);
            Modelo(s + "desk_computer", parteA, new Vector3(-2.6f, 0f, 1.1f), 90f, 1.1f, Medida.Largura);
            Modelo(s + "desk_chair", parteA, new Vector3(-2.05f, 0f, 1.1f), -90f, 0.95f);
        }

        static void Decorar2025(Transform parteB)
        {
            const string s = "Kenney/space-kit/";
            const string n = "Kenney/nature-kit/";
            // Antena de rastreio à direita e o posto de observação no mirante.
            Modelo(s + "satelliteDish_large", parteB, new Vector3(16f, 0f, 14f), -120f, 6f);
            Modelo(s + "barrels", parteB, new Vector3(13f, 0f, 30f), 0f, 1.1f);
            Modelo(s + "desk_computer", parteB, new Vector3(1.9f, 0f, 2.1f), 180f, 1.1f, Medida.Largura);

            var rng = new System.Random(2025);
            for (var i = 0; i < 18; i++)
            {
                var lado = i % 2 == 0 ? -1f : 1f;
                var p = new Vector3(lado * (14f + (float)rng.NextDouble() * 20f), 0f, 4f + (float)rng.NextDouble() * 40f);
                // Longe da casamata e da antena de rastreio.
                if (Vector3.Distance(p, new Vector3(-17f, 0f, 9f)) < 10f || Vector3.Distance(p, new Vector3(16f, 0f, 14f)) < 6f) continue;
                Modelo(n + (i % 3 == 0 ? "tree_palmBend" : "tree_palmTall"), parteB, p, (float)rng.NextDouble() * 360f,
                    6f + (float)rng.NextDouble() * 4f);
            }

            for (var i = 0; i < 24; i++)
            {
                var p = new Vector3(((float)rng.NextDouble() - 0.5f) * 50f, 0f, 22f + (float)rng.NextDouble() * 25f);
                if (Mathf.Abs(p.x) < 13f) continue;
                Modelo(n + "plant_bushLarge", parteB, p, (float)rng.NextDouble() * 360f, 1f + (float)rng.NextDouble());
            }
        }
    }
}
