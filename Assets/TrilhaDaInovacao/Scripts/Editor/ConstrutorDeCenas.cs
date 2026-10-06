using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;

namespace TrilhaDaInovacao.EditorTools
{
    /// <summary>
    /// Monta o whitebox das três cenas do escopo, já nas dimensões reais da
    /// sala (seção 9: "montar primeiro a versão whitebox do hub nas dimensões
    /// reais da sala"). Blocos no lugar dos modelos 3D, para validar o
    /// percurso físico e o loop da jornada antes da arte final.
    /// </summary>
    public static class ConstrutorDeCenas
    {
        // PROVISÓRIO: medir a sala do Sebrae in loco e ajustar aqui.
        // Todo o layout é derivado destes dois números.
        const float LarguraSala = 6f;
        const float ProfundidadeSala = 8f;

        const string PastaCenas = "Assets/Scenes";
        const string PastaMateriais = "Assets/TrilhaDaInovacao/Materials";

        static Material matPiso, matParede, matBalcao, matBackdrop, matNpc, matMural, matSetor, matFade;

        [MenuItem("Trilha da Inovação/2 · Construir cenas (whitebox)")]
        public static void ConstruirTudo()
        {
            Directory.CreateDirectory(PastaCenas);
            Directory.CreateDirectory(PastaMateriais);

            PrepararMateriais();
            var estandes = ConteudoEstandes.CriarOuAtualizar();

            var caminhoBoasVindas = ConstruirBoasVindas();
            var caminhoFeira = ConstruirFeira(estandes);
            var caminhoCreditos = ConstruirCreditos();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(caminhoBoasVindas, true),
                new EditorBuildSettingsScene(caminhoFeira, true),
                new EditorBuildSettingsScene(caminhoCreditos, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Trilha da Inovação] Cenas construídas e adicionadas à Scene List.");
        }

        // ------------------------------------------------------------------
        // Cena 1 — Hall de entrada / tutorial
        // ------------------------------------------------------------------
        static string ConstruirBoasVindas()
        {
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigurarIluminacao(new Color(0.06f, 0.07f, 0.10f));

            MontarSala("Hall de Entrada", LarguraSala, ProfundidadeSala);
            var rig = MontarRigVR(new Vector3(0f, 0f, -ProfundidadeSala * 0.35f), 0f, out var fade);

            var narradorGo = CriarCapsulaNpc("NPC Narrador", new Vector3(0f, 0f, 0.6f), 180f);
            var painel = CriarPainelLegenda("Painel de Legenda",
                new Vector3(0f, 1.75f, 0.1f), 3.2f, 0.7f, 0.34f);

            var titulo = CriarTexto("Titulo", new Vector3(0f, 2.6f, 1.4f), 4.4f, 1.2f, 0.5f,
                "TRILHA DA INOVAÇÃO\n<size=50%>A Feira Tecnológica do Maranhão</size>");
            titulo.alignment = TextAlignmentOptions.Center;

            var narrador = narradorGo.AddComponent<NarradorIntro>();
            narradorGo.AddComponent<AudioSource>();
            narrador.ConfigurarNoEditor(RoteiroDeBoasVindas(), painel, fade);

            GarantirProgresso(new List<EstandeData>());

            var caminho = $"{PastaCenas}/{FluxoExperiencia.CenaBoasVindas}.unity";
            EditorSceneManager.SaveScene(cena, caminho);
            return caminho;
        }

        static LinhaDeDialogo[] RoteiroDeBoasVindas() => new[]
        {
            Fala("Bem-vindo à Trilha da Inovação, a feira tecnológica do Maranhão."),
            Fala("Aqui você vai percorrer quarenta anos de inovação, de 1980 até hoje."),
            Fala("Para andar, caminhe de verdade pela sala — fique dentro dos limites marcados no chão."),
            Fala("Chegue perto de cada apresentador para ouvir a história dele. Boa visita!")
        };

        static LinhaDeDialogo Fala(string legenda) => new LinhaDeDialogo
        {
            legenda = legenda,
            duracaoSemAudio = Mathf.Max(2.5f, legenda.Split(' ').Length * 0.33f)
        };

        // ------------------------------------------------------------------
        // Cena 2 — Feira principal (hub único, sem load entre estandes)
        // ------------------------------------------------------------------
        static string ConstruirFeira(List<EstandeData> estandes)
        {
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigurarIluminacao(new Color(0.10f, 0.11f, 0.14f));

            var sala = MontarSala("Salão da Feira", LarguraSala, ProfundidadeSala);
            var area = sala.AddComponent<AreaRoomScale>();
            area.ConfigurarNoEditor(LarguraSala, ProfundidadeSala);

            MontarRigVR(new Vector3(0f, 0f, -ProfundidadeSala * 0.4f), 0f, out _);

            var progresso = GarantirProgresso(estandes);

            // Percurso em U: sobe pela lateral oeste, passa pelo Mural ao
            // fundo e desce pela lateral leste, terminando perto da entrada.
            var x = LarguraSala * 0.5f - 0.7f;
            var posicoes = new[]
            {
                new Vector3(-x, 0f, -ProfundidadeSala * 0.225f),
                new Vector3(-x, 0f,  ProfundidadeSala * 0.05f),
                new Vector3(-x, 0f,  ProfundidadeSala * 0.325f),
                new Vector3( x, 0f,  ProfundidadeSala * 0.325f),
                new Vector3( x, 0f,  ProfundidadeSala * 0.05f),
                new Vector3( x, 0f, -ProfundidadeSala * 0.225f)
            };

            var raizEstandes = new GameObject("Estandes").transform;

            for (var i = 0; i < estandes.Count; i++)
            {
                var posicao = posicoes[i % posicoes.Length];
                var giro = posicao.x < 0f ? 90f : -90f; // vira para dentro do salão
                MontarEstande(raizEstandes, estandes[i], posicao, giro);
            }

            var celebracao = MontarCelebracao();
            MontarMural(estandes, celebracao);

            EditorUtility.SetDirty(progresso);

            var caminho = $"{PastaCenas}/{FluxoExperiencia.CenaFeira}.unity";
            EditorSceneManager.SaveScene(cena, caminho);
            return caminho;
        }

        static void MontarEstande(Transform pai, EstandeData dados, Vector3 posicao, float giroY)
        {
            var raiz = new GameObject($"Estande_{dados.ordem}_{dados.periodo}");
            raiz.transform.SetParent(pai, false);
            raiz.transform.SetPositionAndRotation(posicao, Quaternion.Euler(0f, giroY, 0f));

            CriarBloco("Backdrop", raiz.transform, new Vector3(0f, 1.3f, -0.5f),
                new Vector3(2.2f, 2.6f, 0.1f), matBackdrop);

            CriarBloco("Balcão", raiz.transform, new Vector3(0f, 0.5f, 0.2f),
                new Vector3(1.8f, 1f, 0.6f), matBalcao);

            var placa = CriarTexto("Placa", new Vector3(0f, 2.1f, -0.43f), 2f, 0.7f, 0.22f,
                $"{dados.periodo}\n<size=80%>{dados.titulo}</size>");
            placa.transform.SetParent(raiz.transform, false);
            placa.transform.localPosition = new Vector3(0f, 2.1f, -0.43f);
            placa.transform.localRotation = Quaternion.identity;
            placa.alignment = TextAlignmentOptions.Center;

            var npc = CriarCapsulaNpc(dados.nomeNpc, Vector3.zero, 0f);
            npc.transform.SetParent(raiz.transform, false);
            npc.transform.localPosition = new Vector3(0f, 0.9f, -0.15f);

            var luz = new GameObject("Luz de Destaque").AddComponent<Light>();
            luz.transform.SetParent(raiz.transform, false);
            luz.transform.localPosition = new Vector3(0f, 2.4f, 0.3f);
            luz.type = LightType.Point;
            luz.range = 4f;
            luz.intensity = 2f;
            luz.color = new Color(1f, 0.92f, 0.75f);
            luz.enabled = false;

            var painel = CriarPainelLegenda("Painel de Legenda", Vector3.zero, 2.4f, 0.65f, 0.16f);
            painel.transform.SetParent(raiz.transform, false);
            painel.transform.localPosition = new Vector3(0f, 1.75f, 0.6f);

            var comportamento = npc.AddComponent<NpcEstande>();
            comportamento.ConfigurarNoEditor(dados, painel, luz);
        }

        static void MontarMural(List<EstandeData> estandes, CelebracaoFinal celebracao)
        {
            var raiz = new GameObject("Mural do Conhecimento");
            raiz.transform.SetPositionAndRotation(
                new Vector3(0f, 1.7f, ProfundidadeSala * 0.5f - 0.2f),
                Quaternion.Euler(0f, 180f, 0f)); // encara a entrada

            CriarBloco("Fundo", raiz.transform, Vector3.zero,
                new Vector3(4.6f, 2.4f, 0.12f), matMural);

            var titulo = CriarTexto("Titulo", Vector3.zero, 4.2f, 0.5f, 0.28f, "MURAL DO CONHECIMENTO");
            titulo.transform.SetParent(raiz.transform, false);
            titulo.transform.localPosition = new Vector3(0f, 0.95f, 0.08f);
            titulo.alignment = TextAlignmentOptions.Center;

            var setores = new List<SetorMural>();
            var passo = 0.72f;
            var inicio = -passo * (estandes.Count - 1) * 0.5f;

            for (var i = 0; i < estandes.Count; i++)
            {
                var dados = estandes[i];
                var setorGo = new GameObject($"Setor_{dados.ordem}");
                setorGo.transform.SetParent(raiz.transform, false);
                setorGo.transform.localPosition = new Vector3(inicio + passo * i, 0.15f, 0.08f);

                var placa = CriarBloco("Placa", setorGo.transform, Vector3.zero,
                    new Vector3(0.62f, 0.8f, 0.06f), matSetor);

                var rotulo = CriarTexto("Rótulo", Vector3.zero, 0.68f, 0.42f, 0.085f, dados.RotuloCurto);
                rotulo.transform.SetParent(setorGo.transform, false);
                rotulo.transform.localPosition = new Vector3(0f, -0.62f, 0.04f);
                rotulo.alignment = TextAlignmentOptions.Center;

                var luz = new GameObject("Luz").AddComponent<Light>();
                luz.transform.SetParent(setorGo.transform, false);
                luz.transform.localPosition = new Vector3(0f, 0f, 0.35f);
                luz.type = LightType.Point;
                luz.range = 1.6f;
                luz.intensity = 1.4f;
                luz.enabled = false;

                var setor = setorGo.AddComponent<SetorMural>();
                setor.ConfigurarNoEditor(dados, placa.GetComponent<Renderer>(), rotulo, luz);
                setores.Add(setor);
            }

            var contador = CriarTexto("Contador", Vector3.zero, 4f, 0.4f, 0.17f, "0 / 6 décadas descobertas");
            contador.transform.SetParent(raiz.transform, false);
            contador.transform.localPosition = new Vector3(0f, -1.0f, 0.08f);
            contador.alignment = TextAlignmentOptions.Center;

            var mural = raiz.AddComponent<MuralDoConhecimento>();
            mural.ConfigurarNoEditor(setores, contador, celebracao);
        }

        static CelebracaoFinal MontarCelebracao()
        {
            var raiz = new GameObject("Celebração Final");
            raiz.transform.position = new Vector3(0f, 2.6f, ProfundidadeSala * 0.25f);

            var sistemas = new List<ParticleSystem>();
            for (var i = 0; i < 2; i++)
            {
                var go = new GameObject($"Partículas {i + 1}");
                go.transform.SetParent(raiz.transform, false);
                go.transform.localPosition = new Vector3(i == 0 ? -1.5f : 1.5f, 0f, 0f);

                var particulas = go.AddComponent<ParticleSystem>();
                var principal = particulas.main;
                principal.duration = 5f;
                principal.startLifetime = 3f;
                principal.startSpeed = 2.5f;
                principal.startSize = 0.06f;
                principal.gravityModifier = 0.15f;
                principal.playOnAwake = false;
                principal.maxParticles = 300;

                var emissao = particulas.emission;
                emissao.rateOverTime = 60f;

                var forma = particulas.shape;
                forma.shapeType = ParticleSystemShapeType.Cone;
                forma.angle = 25f;

                particulas.Stop();
                sistemas.Add(particulas);
            }

            var luz = new GameObject("Luz de Celebração").AddComponent<Light>();
            luz.transform.SetParent(raiz.transform, false);
            luz.type = LightType.Point;
            luz.range = 12f;
            luz.intensity = 0f;
            luz.color = new Color(1f, 0.85f, 0.5f);
            luz.enabled = false;

            var painel = CriarPainelLegenda("Painel Final", Vector3.zero, 3.6f, 0.8f, 0.24f);
            painel.transform.SetParent(raiz.transform, false);
            painel.transform.localPosition = new Vector3(0f, -0.9f, 0f);

            var celebracao = raiz.AddComponent<CelebracaoFinal>();
            celebracao.ConfigurarNoEditor(sistemas.ToArray(), luz, painel);
            return celebracao;
        }

        // ------------------------------------------------------------------
        // Cena 3 — Encerramento / créditos
        // ------------------------------------------------------------------
        static string ConstruirCreditos()
        {
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigurarIluminacao(new Color(0.03f, 0.03f, 0.05f));

            MontarSala("Sala de Créditos", LarguraSala, ProfundidadeSala);
            MontarRigVR(Vector3.zero, 0f, out var fade);

            var texto = CriarTexto("Créditos", new Vector3(0f, 1.7f, 2.4f), 4.4f, 3.2f, 0.19f, string.Empty);
            texto.alignment = TextAlignmentOptions.Center;

            var tela = new GameObject("Tela de Créditos").AddComponent<TelaCreditos>();
            tela.ConfigurarNoEditor(texto, fade);

            var caminho = $"{PastaCenas}/{FluxoExperiencia.CenaCreditos}.unity";
            EditorSceneManager.SaveScene(cena, caminho);
            return caminho;
        }

        // ------------------------------------------------------------------
        // Blocos de montagem reutilizados pelas três cenas
        // ------------------------------------------------------------------
        static ProgressoExperiencia GarantirProgresso(List<EstandeData> estandes)
        {
            var go = new GameObject("Progresso da Experiência");
            var progresso = go.AddComponent<ProgressoExperiencia>();
            progresso.DefinirEstandesNoEditor(estandes);
            return progresso;
        }

        static GameObject MontarRigVR(Vector3 posicao, float giroY, out FadeVR fade)
        {
            var rig = new GameObject("XR Origin (Room-Scale)");
            rig.transform.SetPositionAndRotation(posicao, Quaternion.Euler(0f, giroY, 0f));

            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(rig.transform, false);

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraGo.transform.SetParent(offset.transform, false);

            var camera = cameraGo.AddComponent<Camera>();
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 60f;
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraGo.AddComponent<AudioListener>();

            ConfigurarRastreamentoDeCabeca(cameraGo);

            var origin = rig.AddComponent<XROrigin>();
            origin.Camera = camera;
            origin.CameraFloorOffsetObject = offset;
            // Room-scale: o chão real é o chão virtual, escala 1:1.
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Fade";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(cameraGo.transform, false);
            quad.transform.localPosition = new Vector3(0f, 0f, 0.3f);
            quad.GetComponent<Renderer>().sharedMaterial = matFade;

            fade = cameraGo.AddComponent<FadeVR>();
            fade.ConfigurarNoEditor(quad.GetComponent<Renderer>());

            return rig;
        }

        static void ConfigurarRastreamentoDeCabeca(GameObject cameraGo)
        {
            var driver = cameraGo.AddComponent<TrackedPoseDriver>();
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            driver.positionInput = new InputActionProperty(new InputAction(
                "Position", InputActionType.Value, "<XRHMD>/centerEyePosition",
                expectedControlType: "Vector3"));

            driver.rotationInput = new InputActionProperty(new InputAction(
                "Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation",
                expectedControlType: "Quaternion"));

            driver.trackingStateInput = new InputActionProperty(new InputAction(
                "Tracking State", InputActionType.Value, "<XRHMD>/trackingState",
                expectedControlType: "Integer"));
        }

        static GameObject MontarSala(string nome, float largura, float profundidade)
        {
            var raiz = new GameObject(nome);

            var piso = CriarBloco("Piso", raiz.transform, new Vector3(0f, -0.05f, 0f),
                new Vector3(largura, 0.1f, profundidade), matPiso);
            piso.isStatic = true;

            var altura = 3f;
            var espessura = 0.15f;

            CriarBloco("Parede Norte", raiz.transform, new Vector3(0f, altura * 0.5f, profundidade * 0.5f),
                new Vector3(largura, altura, espessura), matParede);
            CriarBloco("Parede Sul", raiz.transform, new Vector3(0f, altura * 0.5f, -profundidade * 0.5f),
                new Vector3(largura, altura, espessura), matParede);
            CriarBloco("Parede Oeste", raiz.transform, new Vector3(-largura * 0.5f, altura * 0.5f, 0f),
                new Vector3(espessura, altura, profundidade), matParede);
            CriarBloco("Parede Leste", raiz.transform, new Vector3(largura * 0.5f, altura * 0.5f, 0f),
                new Vector3(espessura, altura, profundidade), matParede);

            return raiz;
        }

        static GameObject CriarBloco(string nome, Transform pai, Vector3 posicaoLocal,
            Vector3 escala, Material material)
        {
            var bloco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bloco.name = nome;
            bloco.transform.SetParent(pai, false);
            bloco.transform.localPosition = posicaoLocal;
            bloco.transform.localScale = escala;
            if (material != null) bloco.GetComponent<Renderer>().sharedMaterial = material;
            return bloco;
        }

        static GameObject CriarCapsulaNpc(string nome, Vector3 posicao, float giroY)
        {
            var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = nome;
            Object.DestroyImmediate(npc.GetComponent<Collider>());
            npc.transform.SetPositionAndRotation(
                posicao + Vector3.up * 0.9f, Quaternion.Euler(0f, giroY, 0f));
            // Capsule padrão tem 2 m; 0,9 de escala vertical = 1,8 m de altura.
            npc.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);
            npc.GetComponent<Renderer>().sharedMaterial = matNpc;
            return npc;
        }

        static PainelLegenda CriarPainelLegenda(string nome, Vector3 posicao,
            float largura, float altura, float tamanhoFonte)
        {
            var raiz = new GameObject(nome);
            raiz.transform.position = posicao;

            var texto = CriarTexto("Texto", Vector3.zero, largura, altura, tamanhoFonte, string.Empty);
            texto.transform.SetParent(raiz.transform, false);
            texto.alignment = TextAlignmentOptions.Center;

            var painel = raiz.AddComponent<PainelLegenda>();
            painel.ConfigurarNoEditor(texto, raiz);
            return painel;
        }

        static TMP_Text CriarTexto(string nome, Vector3 posicao, float largura, float altura,
            float tamanhoFonte, string conteudo)
        {
            var go = new GameObject(nome);
            go.transform.position = posicao;

            var texto = go.AddComponent<TextMeshPro>();
            texto.text = conteudo;
            texto.fontSize = tamanhoFonte * 36f; // TMP 3D: fonte em "pontos de mundo"
            texto.alignment = TextAlignmentOptions.Center;
            texto.enableWordWrapping = true;
            texto.color = Color.white;
            texto.rectTransform.sizeDelta = new Vector2(largura, altura);

            return texto;
        }

        static void ConfigurarIluminacao(Color ambiente)
        {
            var luz = new GameObject("Luz Direcional").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;
            luz.color = new Color(1f, 0.96f, 0.9f);
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            // Sem sombras em tempo real: recomendação de desempenho do Quest 2.
            luz.shadows = LightShadows.None;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambiente;
        }

        static void PrepararMateriais()
        {
            matPiso = Material("M_Piso", new Color(0.13f, 0.14f, 0.17f));
            matParede = Material("M_Parede", new Color(0.22f, 0.23f, 0.27f));
            matBalcao = Material("M_Balcao", new Color(0.35f, 0.29f, 0.22f));
            matBackdrop = Material("M_Backdrop", new Color(0.18f, 0.32f, 0.45f));
            matNpc = Material("M_NPC", new Color(0.78f, 0.55f, 0.35f));
            matMural = Material("M_Mural", new Color(0.10f, 0.11f, 0.14f));
            matSetor = Material("M_SetorMural", new Color(0.16f, 0.17f, 0.20f), emissivo: true);
            matFade = MaterialFade();
        }

        static Material Material(string nome, Color cor, bool emissivo = false)
        {
            var caminho = $"{PastaMateriais}/{nome}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(caminho);

            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, caminho);
            }

            material.SetColor("_BaseColor", cor);
            material.SetFloat("_Smoothness", 0.15f);

            if (emissivo)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                material.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        static Material MaterialFade()
        {
            var caminho = $"{PastaMateriais}/M_FadeVR.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(caminho);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, caminho);
            }

            // Transparente, sem escrever no depth: é só um véu na frente da câmera.
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetColor("_BaseColor", Color.black);

            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
