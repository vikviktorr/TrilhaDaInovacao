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
    /// Gera as cenas da Trilha da Inovação a partir do documento de produção:
    /// 00_Bootstrap (rig, guia, legendas, marcas no piso, diretor da visita),
    /// 01_Hub (abertura e encerramento) e uma cena por época.
    ///
    /// As cenas são GERADAS: rodar o construtor de novo sobrescreve tudo.
    /// Para mudar um cenário, mude este código (ou pare de usá-lo e edite à mão).
    /// Os roteiros (falas) ficam em assets separados e não são sobrescritos.
    /// </summary>
    public static partial class ConstrutorDeCenas
    {
        const string PastaCenas = "Assets/Scenes";
        const string PastaGerado = "Assets/TrilhaDaInovacao/Gerado";
        const string PastaMateriais = PastaGerado + "/Materiais";

        static readonly Dictionary<string, Material> materiais = new Dictionary<string, Material>();
        static Texture2D texturaBrilho;
        static Texture2D texturaFumaca;
        static Dictionary<string, SceneScript> roteiros;

        [MenuItem("Trilha da Inovação/2 · Construir cenas")]
        public static void ConstruirTudo()
        {
            Directory.CreateDirectory(PastaCenas);
            Directory.CreateDirectory(PastaMateriais);
            materiais.Clear();

            texturaBrilho = GerarTexturaRedonda("Brilho", true);
            texturaFumaca = GerarTexturaRedonda("Fumaca", false);
            roteiros = Roteiro.CriarOuAtualizar();

            var caminhos = new List<string>
            {
                ConstruirBootstrap(),
                ConstruirHub(),
                Construir1986(),
                Construir1994(),
                Construir2009(),
                Construir2018(),
                Construir2022(),
                Construir2025()
            };

            var lista = new List<EditorBuildSettingsScene>();
            foreach (var c in caminhos) lista.Add(new EditorBuildSettingsScene(c, true));
            EditorBuildSettings.scenes = lista.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(caminhos[0]);
            Debug.Log($"[Trilha da Inovação] {caminhos.Count} cenas construídas e adicionadas à Scene List.");
        }

        // ==================================================================
        // 00_Bootstrap — sempre carregada
        // ==================================================================
        static string ConstruirBootstrap()
        {
            var cena = NovaCena();
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.25f);

            var rig = MontarRigVR(out var camera, out var offset);
            var cameraT = camera.transform;

            // Véu de transição + ano grande, presos à câmera.
            var veu = Bloco("Véu", cameraT, new Vector3(0f, 0f, 0.3f), new Vector3(2f, 2f, 1f),
                Transparente("Veu", Color.black, true), PrimitiveType.Quad);
            veu.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var ano = Texto("Ano", cameraT, new Vector3(0f, 0f, 0.27f), "1986", 0.07f, 0.5f, Color.white);
            ano.fontStyle = FontStyles.Bold;
            var fader = cameraT.gameObject.AddComponent<ScreenFader>();
            fader.ConfigurarNoEditor(veu.GetComponent<Renderer>(), ano);

            // Legenda que segue o olhar.
            var legendaGo = new GameObject("Legenda");
            var fundo = Bloco("Fundo", legendaGo.transform, new Vector3(0f, 0f, 0.01f), new Vector3(1.45f, 0.24f, 1f),
                Transparente("LegendaFundo", new Color(0.03f, 0.05f, 0.09f, 0.78f), true), PrimitiveType.Quad);
            var textoLegenda = Texto("Texto", legendaGo.transform, Vector3.zero, "", 0.058f, 1.35f, Color.white);
            textoLegenda.rectTransform.sizeDelta = new Vector2(1.35f, 0.22f);
            var legenda = legendaGo.AddComponent<SubtitleController>();
            legenda.ConfigurarNoEditor(textoLegenda, fundo);

            var guia = MontarGuia();
            var marcas = MontarMarcas();
            var trilha = Linha("Trilha até a próxima marca", null, new[] { Vector3.zero, Vector3.forward }, 0.07f,
                new Color(0.35f, 0.85f, 1f), false);
            trilha.enabled = false;
            MontarPerimetroDeSeguranca();
            var portal = MontarPortal();

            // Mãos e modo desktop ficam no rig.
            var maos = rig.AddComponent<HandPoints>();
            maos.ConfigurarNoEditor(offset.transform,
                Opaco("MaoJunta", new Color(0.7f, 0.9f, 1f), new Color(0.2f, 0.55f, 0.9f)),
                Opaco("MaoPonta", Color.white, new Color(0.5f, 0.9f, 1f) * 2f));
            var desktop = rig.AddComponent<DesktopRig>();
            desktop.ConfigurarNoEditor(rig.transform, cameraT);

            var visita = new GameObject("Visita");
            var efeitos = visita.AddComponent<AudioSource>();
            efeitos.playOnAwake = false;
            efeitos.spatialBlend = 0f;
            var registro = visita.AddComponent<SessionLogger>();
            var diretor = visita.AddComponent<VisitDirector>();
            var operador = visita.AddComponent<OperatorPanel>();
            operador.ConfigurarNoEditor(diretor, rig.GetComponent<XROrigin>());
            diretor.ConfigurarNoEditor(marcas, trilha, fader, legenda, guia, registro, desktop, operador, efeitos,
                roteiros[Roteiro.Geral], portal);

            var sala = new GameObject("Área livre do Sebraelab (room-scale)").AddComponent<AreaRoomScale>();
            sala.ConfigurarNoEditor(LayoutTrilha.LarguraSala, LayoutTrilha.ProfundidadeSala);

            return Salvar(cena, LayoutTrilha.CenaBootstrap);
        }

        static GameObject MontarRigVR(out Camera camera, out GameObject offset)
        {
            var rig = new GameObject("XR Origin (Room-Scale)");

            offset = new GameObject("Camera Offset");
            offset.transform.SetParent(rig.transform, false);

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraGo.transform.SetParent(offset.transform, false);
            cameraGo.transform.localPosition = Vector3.up * 1.6f;

            camera = cameraGo.AddComponent<Camera>();
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1500f;
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraGo.AddComponent<AudioListener>();

            ConfigurarRastreamentoDeCabeca(cameraGo);

            var origin = rig.AddComponent<XROrigin>();
            origin.Camera = camera;
            origin.CameraFloorOffsetObject = offset;
            // Room-scale: o chão real é o chão virtual, escala 1:1.
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            return rig;
        }

        static void ConfigurarRastreamentoDeCabeca(GameObject cameraGo)
        {
            var driver = cameraGo.AddComponent<TrackedPoseDriver>();
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            driver.positionInput = new InputActionProperty(new InputAction(
                "Position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
            driver.rotationInput = new InputActionProperty(new InputAction(
                "Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
            driver.trackingStateInput = new InputActionProperty(new InputAction(
                "Tracking State", InputActionType.Value, "<XRHMD>/trackingState", expectedControlType: "Integer"));
        }

        /// <summary>O guia: um robô flutuante simples, feito de primitivas.</summary>
        static GuideCharacter MontarGuia()
        {
            var raiz = new GameObject("Guia");
            var corpo = new GameObject("Corpo").transform;
            corpo.SetParent(raiz.transform, false);

            var casco = Opaco("GuiaCasco", new Color(0.92f, 0.94f, 0.97f), smooth: 0.6f);
            var detalhe = Opaco("GuiaDetalhe", new Color(0.12f, 0.35f, 0.75f));
            var olho = Opaco("GuiaOlho", Color.black, new Color(0.4f, 0.85f, 1f) * 2.5f);

            Bloco("Tronco", corpo, new Vector3(0f, 0.95f, 0f), new Vector3(0.36f, 0.32f, 0.3f), casco, PrimitiveType.Capsule);
            Bloco("Saia", corpo, new Vector3(0f, 0.7f, 0f), new Vector3(0.26f, 0.06f, 0.26f), detalhe, PrimitiveType.Cylinder);
            var cabeca = Bloco("Cabeça", corpo, new Vector3(0f, 1.42f, 0f), new Vector3(0.34f, 0.3f, 0.32f), casco, PrimitiveType.Sphere);
            Bloco("Visor", cabeca.transform, new Vector3(0f, 0.02f, 0.3f), new Vector3(0.8f, 0.42f, 0.5f), detalhe, PrimitiveType.Sphere);
            Bloco("Olho E", cabeca.transform, new Vector3(-0.17f, 0.05f, 0.47f), new Vector3(0.13f, 0.16f, 0.06f), olho, PrimitiveType.Sphere);
            Bloco("Olho D", cabeca.transform, new Vector3(0.17f, 0.05f, 0.47f), new Vector3(0.13f, 0.16f, 0.06f), olho, PrimitiveType.Sphere);
            Bloco("Antena", cabeca.transform, new Vector3(0f, 0.62f, 0f), new Vector3(0.04f, 0.2f, 0.04f), detalhe, PrimitiveType.Cylinder);
            Bloco("Ponta da antena", cabeca.transform, new Vector3(0f, 0.86f, 0f), Vector3.one * 0.12f, olho, PrimitiveType.Sphere);
            Bloco("Braço E", corpo, new Vector3(-0.24f, 0.95f, 0f), new Vector3(0.08f, 0.16f, 0.08f), detalhe, PrimitiveType.Capsule,
                new Vector3(0f, 0f, -15f));
            Bloco("Braço D", corpo, new Vector3(0.24f, 0.95f, 0f), new Vector3(0.08f, 0.16f, 0.08f), detalhe, PrimitiveType.Capsule,
                new Vector3(0f, 0f, 15f));
            var peito = Bloco("Luz do peito", corpo, new Vector3(0f, 1.0f, 0.15f), new Vector3(0.09f, 0.09f, 0.03f),
                Opaco("GuiaPeito", new Color(0.2f, 0.5f, 0.8f), new Color(0.3f, 0.75f, 1f)), PrimitiveType.Sphere);

            raiz.AddComponent<AudioSource>();
            var guia = raiz.AddComponent<GuideCharacter>();
            guia.ConfigurarNoEditor(corpo, cabeca.transform, peito.GetComponent<Renderer>());
            return guia;
        }

        /// <summary>
        /// Botão PRÓXIMO e portal entre as épocas. Ficam no Bootstrap e o diretor
        /// os leva para a marca da vez; tudo sem luz (brilho próprio), para ler
        /// igual na central de 1986 e na noite de Alcântara.
        /// </summary>
        static PortalEpoca MontarPortal()
        {
            const float meiaLargura = 0.5f, meiaAltura = 1.0f;
            var raiz = new GameObject("Portal de época");
            var botao = BotaoDeToque("Botão Próximo", raiz.transform, Vector3.zero, Quaternion.identity, "PRÓXIMO",
                new Color(0.15f, 0.55f, 1f));
            var pedestal = botao.transform.parent;

            var portal = new GameObject("Portal").transform;
            portal.SetParent(raiz.transform, false);
            var abertura = new GameObject("Abertura").transform;
            abertura.SetParent(portal, false);
            abertura.localPosition = new Vector3(0f, meiaAltura + 0.08f, 0f);

            // Vão oval: moldura de luz, véu brilhante e um anel interno mais fraco.
            var ciano = new Color(0.35f, 0.85f, 1f);
            var oval = new Vector3[40];
            for (var i = 0; i < oval.Length; i++)
            {
                var a = i / (float)oval.Length * Mathf.PI * 2f;
                oval[i] = new Vector3(Mathf.Cos(a) * meiaLargura, Mathf.Sin(a) * meiaAltura, 0f);
            }

            var moldura = Linha("Moldura", abertura, oval, 0.07f, ciano * 1.4f, true);
            moldura.loop = true;
            var interno = new Vector3[oval.Length];
            for (var i = 0; i < oval.Length; i++) interno[i] = oval[i] * 0.86f;
            var anel = Linha("Anel interno", abertura, interno, 0.025f, new Color(0.7f, 0.5f, 1f), true);
            anel.loop = true;

            var escalaVeu = new GameObject("Forma do véu").transform;
            escalaVeu.SetParent(abertura, false);
            escalaVeu.localScale = new Vector3(meiaLargura * 2.2f, meiaAltura * 2.2f, 1f);
            var veu = Bloco("Véu", escalaVeu, Vector3.zero, Vector3.one, Particula("PortalVeu", true, new Color(0.35f, 0.7f, 1f, 0.8f)),
                PrimitiveType.Quad);
            veu.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // Faíscas que sobem pela borda do vão.
            var faiscas = Particulas("Faíscas", abertura, Vector3.zero, Particula("PortalFaisca", true), 1.4f, 0.25f, 0.05f, 30f,
                new Color(0.6f, 0.9f, 1f), 0f, false, -0.05f, 80);
            var forma = faiscas.shape;
            forma.shapeType = ParticleSystemShapeType.Circle;
            forma.radius = 1f;
            forma.radiusThickness = 0f;
            forma.scale = new Vector3(meiaLargura, meiaAltura, 1f);
            forma.rotation = Vector3.zero;

            var destino = Texto("Destino", portal, new Vector3(0f, meiaAltura * 2f + 0.45f, 0f), "1994", 0.16f, 1.6f, Color.white);
            destino.gameObject.AddComponent<EncararUsuario>();

            var componente = raiz.AddComponent<PortalEpoca>();
            componente.ConfigurarNoEditor(pedestal, botao, portal, abertura, veu.transform, faiscas, destino, meiaLargura);
            return componente;
        }

        static FloorMarker[] MontarMarcas()
        {
            var raiz = new GameObject("Marcas no piso").transform;
            var marcas = new FloorMarker[LayoutTrilha.Marcas.Length];
            var linhaMat = LinhaSolida("Linha");
            var disco = Transparente("MarcaDisco", new Color(0.35f, 0.85f, 1f, 0.2f), true);

            for (var i = 0; i < marcas.Length; i++)
            {
                var go = new GameObject($"Marca {i} · {(i == 0 ? "Início" : LayoutTrilha.Anos[i])}");
                go.transform.SetParent(raiz, false);
                go.transform.SetPositionAndRotation(LayoutTrilha.Marcas[i], LayoutTrilha.Orientacao(i));

                var anel = Linha("Anel", go.transform, Circulo(LayoutTrilha.RaioMarca, 64, 0.008f), 0.04f, Color.white, true, linhaMat);
                anel.loop = true;

                var fundo = Bloco("Disco", go.transform, new Vector3(0f, 0.004f, 0f),
                    new Vector3(LayoutTrilha.RaioMarca * 2f, 0.002f, LayoutTrilha.RaioMarca * 2f), disco, PrimitiveType.Cylinder);

                var rotulo = Texto("Rótulo", go.transform, new Vector3(0f, 0.01f, LayoutTrilha.RaioMarca + 0.14f),
                    i == 0 ? "INÍCIO" : LayoutTrilha.Anos[i] == "2023" ? "2023–2025" : LayoutTrilha.Anos[i],
                    0.13f, 1.2f, Color.white);
                rotulo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                rotulo.fontStyle = FontStyles.Bold;

                var marca = go.AddComponent<FloorMarker>();
                marca.ConfigurarNoEditor(i, anel, fundo.GetComponent<Renderer>(), rotulo);
                marcas[i] = marca;
            }

            // Saída: onde o monitor recebe o visitante (só sinalização).
            var saida = new GameObject("Saída (monitor da equipe)");
            saida.transform.SetParent(raiz, false);
            saida.transform.position = LayoutTrilha.Saida;
            var anelSaida = Linha("Anel", saida.transform, Circulo(0.45f, 48, 0.008f), 0.025f,
                new Color(0.6f, 0.65f, 0.7f), true, linhaMat);
            anelSaida.loop = true;
            var textoSaida = Texto("Rótulo", saida.transform, new Vector3(0f, 0.01f, 0f), "SAÍDA", 0.1f, 1f,
                new Color(0.7f, 0.75f, 0.8f));
            textoSaida.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            return marcas;
        }

        /// <summary>
        /// O retângulo da área livre real aparece fraco no chão em TODAS as
        /// épocas: o cenário muda, mas o visitante sempre vê até onde pode ir.
        /// </summary>
        static void MontarPerimetroDeSeguranca()
        {
            var x = LayoutTrilha.LarguraSala * 0.5f;
            var z = LayoutTrilha.ProfundidadeSala * 0.5f;
            var linha = Linha("Perímetro da área livre", null, new[]
            {
                new Vector3(-x, 0.012f, -z), new Vector3(x, 0.012f, -z),
                new Vector3(x, 0.012f, z), new Vector3(-x, 0.012f, z)
            }, 0.03f, new Color(0.5f, 0.27f, 0.12f), false);
            linha.loop = true;
        }

        // ==================================================================
        // 01_Hub — Cena 0 (abertura) e Cena 7 (encerramento)
        // ==================================================================
        static string ConstruirHub()
        {
            var cena = NovaCena();
            Ambiente(new Color(0.55f, 0.58f, 0.64f), Ceu("CeuHub", new Color(0.55f, 0.7f, 0.9f), 1.2f, 0.6f),
                new Color(1f, 0.98f, 0.95f), 1f, new Vector3(55f, -35f, 0f));

            // A visita começa (e termina) dentro do Sebraelab real; as épocas o transformam.
            var sala = new GameObject("Sebraelab").transform;
            if (!MontarSebraelab(sala)) ReplicaDoSebraelab(sala);

            // A linha luminosa no piso que mostra todo o percurso.
            var pontos = new List<Vector3>();
            foreach (var m in LayoutTrilha.Marcas) pontos.Add(m + Vector3.up * 0.004f);
            pontos.Add(LayoutTrilha.Saida + Vector3.up * 0.004f);
            Linha("Trilha no piso", sala, pontos.ToArray(), 0.035f, new Color(0.25f, 0.6f, 1f, 0.8f), false);

            // Linha do tempo 1986–2025 (vira para o visitante). Fica à esquerda da
            // marca de início, fora do U, para não esconder a mesa e o guia atrás dela.
            var linhaGo = new GameObject("Linha do tempo");
            linhaGo.transform.position = new Vector3(-2.9f, 1.6f, 0f);
            linhaGo.AddComponent<EncararUsuario>();
            Bloco("Painel", linhaGo.transform, new Vector3(0f, 0f, 0.03f), new Vector3(1.6f, 0.78f, 0.03f),
                Opaco("HubPainel", new Color(0.06f, 0.1f, 0.2f)));
            Texto("Título", linhaGo.transform, new Vector3(0f, 0.26f, 0f), "TRILHA DA INOVAÇÃO", 0.085f, 1.5f, Color.white)
                .fontStyle = FontStyles.Bold;
            Texto("Subtítulo", linhaGo.transform, new Vector3(0f, 0.15f, 0f), "Marcos tecnológicos do Maranhão · 1986 a 2025",
                0.042f, 1.5f, new Color(0.75f, 0.85f, 1f));
            var barra = Bloco("Barra", linhaGo.transform, new Vector3(0f, -0.07f, 0f), new Vector3(1.32f, 0.015f, 0.015f),
                Opaco("HubBarra", new Color(0.2f, 0.4f, 0.6f)));
            var anos = new[] { "1986", "1994", "2009", "2018", "2022", "2025" };
            var pontosLinha = new Renderer[anos.Length];
            for (var i = 0; i < anos.Length; i++)
            {
                var x = -0.6f + i * 0.24f;
                pontosLinha[i] = Bloco($"Ponto {anos[i]}", linhaGo.transform, new Vector3(x, -0.07f, -0.01f), Vector3.one * 0.055f,
                    Opaco("HubPonto" + i, new Color(0.3f, 0.45f, 0.6f)), PrimitiveType.Sphere).GetComponent<Renderer>();
                Texto($"Ano {anos[i]}", linhaGo.transform, new Vector3(x, -0.17f, 0f), anos[i], 0.045f, 0.3f, Color.white);
            }

            var linhaDoTempo = linhaGo.AddComponent<LinhaDoTempoHub>();
            linhaDoTempo.barra = barra.GetComponent<Renderer>();
            linhaDoTempo.pontos = pontosLinha;

            // Botão "Começar" à frente da marca de início.
            var botao = BotaoDeToque("Botão Começar", null, LayoutTrilha.Local(0, -0.05f, 0f, 0.55f),
                LayoutTrilha.Orientacao(0), "COMEÇAR", new Color(0.1f, 0.65f, 0.35f));

            // Créditos, perto da marca 6 (onde a visita termina).
            var creditos = new GameObject("Créditos");
            creditos.transform.position = LayoutTrilha.Local(6, -0.7f, 1.5f, 1.2f);
            creditos.AddComponent<EncararUsuario>();
            Bloco("Painel", creditos.transform, new Vector3(0f, 0f, 0.03f), new Vector3(1.35f, 0.85f, 0.03f),
                Opaco("HubPainel", new Color(0.06f, 0.1f, 0.2f)));
            Texto("Texto", creditos.transform, new Vector3(0f, 0f, 0f),
                "<b>Fofucho's Games Group</b>\n<size=70%>Pedro Victor Santos Velôso · Davi Chaves de Oliveira\n" +
                "Gabriel Araujo Meneses · Kauã Carneiro de Morais\nLucas Gomes Antunes · Kalil Ramos Caldeira\n\n" +
                "Tecnologias Emergentes · Prof. Kassio Romulo Lima Souas\n" +
                "Fontes: TELEBRASIL, INPE, RNP, FIEMA, ANATEL, AEB</size>",
                0.06f, 1.25f, Color.white).rectTransform.sizeDelta = new Vector2(1.25f, 0.8f);

            var sons = Fonte("Sons", null, LayoutTrilha.Marcas[6] + Vector3.up, false);

            // Na abertura o guia espera atrás da mesa de madeira do Sebraelab.
            var abertura = Sequencia<Cena0Abertura>("Cena 0 · Abertura", 0, 0, LayoutTrilha.DoSebraelab(LayoutTrilha.GuiaNoSebraelab));
            abertura.linhaDoTempo = linhaDoTempo;
            abertura.botaoComecar = botao;
            abertura.creditos = creditos;

            // No encerramento o guia fica de lado, sem cobrir os créditos.
            var encerramento = Sequencia<Cena7Encerramento>("Cena 7 · Encerramento", 7, 6, LayoutTrilha.Local(6, -1.15f, 0f, 0.6f));
            encerramento.linhaDoTempo = linhaDoTempo;
            encerramento.creditos = creditos;
            encerramento.botaoComecar = botao.gameObject;
            encerramento.sons = sons;

            return Salvar(cena, LayoutTrilha.CenaHub);
        }

        /// <summary>Sala de blocos do tamanho da área livre, usada só sem o pacote do Sebraelab.</summary>
        static void ReplicaDoSebraelab(Transform sala)
        {
            var w = LayoutTrilha.LarguraSala;
            var d = LayoutTrilha.ProfundidadeSala;
            var branco = Opaco("HubParede", new Color(0.93f, 0.94f, 0.96f));
            var azul = Opaco("HubAzul", new Color(0.05f, 0.3f, 0.7f));
            var piso = Opaco("HubPiso", new Color(0.78f, 0.8f, 0.84f), smooth: 0.45f);

            Bloco("Piso", sala, new Vector3(0f, -0.05f, 0f), new Vector3(w, 0.1f, d), piso);
            Bloco("Piso externo", sala, new Vector3(0f, -0.07f, 0f), new Vector3(16f, 0.1f, 16f), Opaco("HubPisoExterno", new Color(0.3f, 0.33f, 0.38f)));
            Paredes(sala, w, d, 2.8f, branco, azul);
            DecorarHub(sala);
            Bloco("Teto", sala, new Vector3(0f, 2.85f, 0f), new Vector3(w + 0.3f, 0.1f, d + 0.3f), branco);
            for (var i = -1; i <= 1; i++)
            {
                Bloco($"Luminária {i}", sala, new Vector3(i * 1.6f, 2.79f, 0f), new Vector3(1.2f, 0.02f, 0.3f),
                    Opaco("HubLuminaria", Color.white, Color.white * 1.5f));
            }
        }

        // ==================================================================
        // Blocos de montagem
        // ==================================================================

        /// <summary>
        /// Cria o componente de sequência com seu roteiro e o ponto do guia.
        /// </summary>
        static T Sequencia<T>(string nome, int epoca, int marca, Vector3? posicaoGuia = null) where T : EraSequence
        {
            var go = new GameObject(nome);
            var seq = go.AddComponent<T>();
            var guia = new GameObject("Posição do guia").transform;
            guia.SetParent(go.transform, false);
            guia.SetPositionAndRotation(posicaoGuia ?? PosicaoGuia(marca), Quaternion.LookRotation(-LayoutTrilha.Foco(marca)));

            var so = new SerializedObject(seq);
            so.FindProperty("epoca").intValue = epoca;
            so.FindProperty("roteiro").objectReferenceValue = roteiros[Roteiro.Nomes[epoca]];
            so.FindProperty("posicaoGuia").objectReferenceValue = guia;
            so.ApplyModifiedPropertiesWithoutUndo();
            return seq;
        }

        /// <summary>
        /// O guia fica ao lado da marca, no centro do U (que ninguém percorre)
        /// e nunca entre o visitante e a próxima marca.
        /// </summary>
        static Vector3 PosicaoGuia(int marca)
        {
            switch (marca)
            {
                case 0: return LayoutTrilha.Local(0, 0.9f, 0f, 1.25f);
                case 1: return LayoutTrilha.Local(1, -1.0f, 0f, 1.0f);
                case 2: return LayoutTrilha.Local(2, -1.0f, 0f, 0.7f);    // ao lado do console do radar
                case 3: return LayoutTrilha.Local(3, -1.2f, 0f, 0.25f);   // à frente da cadeira, não sobre ela
                case 4: return LayoutTrilha.Local(4, 1.25f, 0f, 0.6f);
                default: return LayoutTrilha.Local(marca, -0.75f, 0f, 0.95f);
            }
        }

        /// <summary>Raiz de um cenário, posicionada na marca e virada para o foco.</summary>
        static Transform Quadro(int marca, string nome)
        {
            var raiz = new GameObject(nome).transform;
            raiz.SetPositionAndRotation(LayoutTrilha.Marcas[marca], LayoutTrilha.Orientacao(marca));
            return raiz;
        }

        static PokeTrigger BotaoDeToque(string nome, Transform pai, Vector3 posicao, Quaternion rotacao, string rotulo, Color cor)
        {
            var raiz = new GameObject(nome).transform;
            raiz.SetParent(pai, true);
            raiz.SetPositionAndRotation(new Vector3(posicao.x, 0f, posicao.z), rotacao);

            var metal = Opaco("Pedestal", new Color(0.3f, 0.32f, 0.36f), smooth: 0.5f);
            Bloco("Pedestal", raiz, new Vector3(0f, 0.5f, 0f), new Vector3(0.12f, 0.5f, 0.12f), metal, PrimitiveType.Cylinder);
            Bloco("Base do botão", raiz, new Vector3(0f, 1.02f, 0f), new Vector3(0.28f, 0.04f, 0.28f), metal, PrimitiveType.Cylinder);

            var alvo = new GameObject("Botão").transform;
            alvo.SetParent(raiz, false);
            alvo.localPosition = new Vector3(0f, 1.06f, 0f);
            Bloco("Tampa", alvo, Vector3.zero, new Vector3(0.2f, 0.03f, 0.2f), Opaco("Botao" + rotulo, cor, cor * 0.3f), PrimitiveType.Cylinder);
            var colisor = alvo.gameObject.AddComponent<BoxCollider>();
            colisor.size = new Vector3(0.24f, 0.12f, 0.24f);

            var placa = Texto("Rótulo", raiz, new Vector3(0f, 1.28f, 0.02f), rotulo, 0.07f, 0.6f, Color.white);
            placa.fontStyle = FontStyles.Bold;
            placa.gameObject.AddComponent<EncararUsuario>();

            var poke = alvo.gameObject.AddComponent<PokeTrigger>();
            poke.ConfigurarNoEditor(alvo.GetComponentsInChildren<Renderer>(), new Color(0.6f, 1f, 0.7f));
            return poke;
        }

        static void Paredes(Transform pai, float largura, float profundidade, float altura, Material parede, Material faixa)
        {
            const float e = 0.12f;
            Bloco("Parede N", pai, new Vector3(0f, altura / 2f, profundidade / 2f + e / 2f), new Vector3(largura + 2 * e, altura, e), parede);
            Bloco("Parede S", pai, new Vector3(0f, altura / 2f, -profundidade / 2f - e / 2f), new Vector3(largura + 2 * e, altura, e), parede);
            Bloco("Parede O", pai, new Vector3(-largura / 2f - e / 2f, altura / 2f, 0f), new Vector3(e, altura, profundidade), parede);
            Bloco("Parede L", pai, new Vector3(largura / 2f + e / 2f, altura / 2f, 0f), new Vector3(e, altura, profundidade), parede);
            if (faixa == null) return;
            Bloco("Faixa N", pai, new Vector3(0f, 1.05f, profundidade / 2f - 0.005f), new Vector3(largura, 0.12f, 0.02f), faixa);
            Bloco("Faixa S", pai, new Vector3(0f, 1.05f, -profundidade / 2f + 0.005f), new Vector3(largura, 0.12f, 0.02f), faixa);
            Bloco("Faixa O", pai, new Vector3(-largura / 2f + 0.005f, 1.05f, 0f), new Vector3(0.02f, 0.12f, profundidade), faixa);
            Bloco("Faixa L", pai, new Vector3(largura / 2f - 0.005f, 1.05f, 0f), new Vector3(0.02f, 0.12f, profundidade), faixa);
        }

        /// <summary>Sala fechada (6 × 6 m por padrão) no referencial de um quadro.</summary>
        static void SalaFechada(Transform quadro, Vector3 centro, Vector3 tamanho, Material piso, Material parede, Material teto)
        {
            Bloco("Piso", quadro, centro + new Vector3(0f, -0.05f, 0f), new Vector3(tamanho.x, 0.1f, tamanho.z), piso);
            Bloco("Teto", quadro, centro + new Vector3(0f, tamanho.y + 0.05f, 0f), new Vector3(tamanho.x, 0.1f, tamanho.z), teto);
            var h = tamanho.y / 2f;
            Bloco("Parede fundo", quadro, centro + new Vector3(0f, h, tamanho.z / 2f), new Vector3(tamanho.x, tamanho.y, 0.1f), parede);
            Bloco("Parede trás", quadro, centro + new Vector3(0f, h, -tamanho.z / 2f), new Vector3(tamanho.x, tamanho.y, 0.1f), parede);
            Bloco("Parede esq", quadro, centro + new Vector3(-tamanho.x / 2f, h, 0f), new Vector3(0.1f, tamanho.y, tamanho.z), parede);
            Bloco("Parede dir", quadro, centro + new Vector3(tamanho.x / 2f, h, 0f), new Vector3(0.1f, tamanho.y, tamanho.z), parede);
        }

        static GameObject Bloco(string nome, Transform pai, Vector3 posicaoLocal, Vector3 escala, Material material,
            PrimitiveType tipo = PrimitiveType.Cube, Vector3? rotacao = null)
        {
            var bloco = GameObject.CreatePrimitive(tipo);
            bloco.name = nome;
            Object.DestroyImmediate(bloco.GetComponent<Collider>());
            bloco.transform.SetParent(pai, false);
            bloco.transform.localPosition = posicaoLocal;
            bloco.transform.localRotation = Quaternion.Euler(rotacao ?? Vector3.zero);
            bloco.transform.localScale = escala;
            var r = bloco.GetComponent<Renderer>();
            if (material != null) r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return bloco;
        }

        /// <summary>
        /// Texto 3D. <paramref name="altura"/> é a altura da letra em metros
        /// (no TextMeshPro 3D, tamanho 10 ≈ 1 m de em).
        /// </summary>
        static TMP_Text Texto(string nome, Transform pai, Vector3 posicaoLocal, string conteudo, float altura, float largura, Color cor)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            go.transform.localPosition = posicaoLocal;

            var texto = go.AddComponent<TextMeshPro>();
            texto.font = TMP_Settings.defaultFontAsset;
            texto.text = conteudo;
            texto.fontSize = altura * 10f;
            texto.alignment = TextAlignmentOptions.Center;
            texto.textWrappingMode = TextWrappingModes.Normal;
            texto.color = cor;
            texto.rectTransform.sizeDelta = new Vector2(largura, Mathf.Max(altura * 3f, 0.1f));
            return texto;
        }

        static LineRenderer Linha(string nome, Transform pai, Vector3[] pontos, float largura, Color cor, bool local,
            Material material = null)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            var linha = go.AddComponent<LineRenderer>();
            linha.useWorldSpace = !local;
            linha.positionCount = pontos.Length;
            linha.SetPositions(pontos);
            linha.widthMultiplier = largura;
            linha.startColor = linha.endColor = cor;
            linha.sharedMaterial = material != null ? material : LinhaSolida("Linha");
            linha.shadowCastingMode = ShadowCastingMode.Off;
            linha.receiveShadows = false;
            linha.numCapVertices = 2;
            linha.numCornerVertices = 2;
            linha.alignment = LineAlignment.View;
            return linha;
        }

        static Vector3[] Circulo(float raio, int segmentos, float y)
        {
            var pontos = new Vector3[segmentos];
            for (var i = 0; i < segmentos; i++)
            {
                var a = i / (float)segmentos * Mathf.PI * 2f;
                pontos[i] = new Vector3(Mathf.Cos(a) * raio, y, Mathf.Sin(a) * raio);
            }

            return pontos;
        }

        static AudioSource Fonte(string nome, Transform pai, Vector3 posicao, bool espacial)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, true);
            go.transform.position = posicao;
            var fonte = go.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.spatialBlend = espacial ? 1f : 0f;
            fonte.minDistance = 1.5f;
            fonte.maxDistance = 40f;
            return fonte;
        }

        static Light Luz(string nome, Transform pai, Vector3 posicaoLocal, LightType tipo, Color cor, float intensidade, float alcance)
        {
            var luz = new GameObject(nome).AddComponent<Light>();
            luz.transform.SetParent(pai, false);
            luz.transform.localPosition = posicaoLocal;
            luz.type = tipo;
            luz.color = cor;
            luz.intensity = intensidade;
            luz.range = alcance;
            luz.shadows = LightShadows.None;
            return luz;
        }

        static Pulsar AdicionarPulsar(GameObject alvo, Color cor, float intensidade = 1.5f, float frequencia = 1.5f, float escala = 0f)
        {
            var p = alvo.AddComponent<Pulsar>();
            p.alvo = alvo.GetComponent<Renderer>();
            p.cor = cor;
            p.intensidade = intensidade;
            p.frequencia = frequencia;
            p.escala = escala;
            p.ativo = false;
            return p;
        }

        static ParticleSystem Particulas(string nome, Transform pai, Vector3 posicaoLocal, Material material,
            float vida, float velocidade, float tamanho, float taxa, Color cor, float angulo, bool mundo = true,
            float gravidade = 0f, int maximo = 600)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            go.transform.localPosition = posicaoLocal;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(vida * 0.7f, vida);
            main.startSpeed = new ParticleSystem.MinMaxCurve(velocidade * 0.6f, velocidade);
            main.startSize = new ParticleSystem.MinMaxCurve(tamanho * 0.6f, tamanho);
            main.startColor = cor;
            main.gravityModifier = gravidade;
            main.maxParticles = maximo;
            main.simulationSpace = mundo ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;

            var emissao = ps.emission;
            emissao.rateOverTime = taxa;

            var forma = ps.shape;
            forma.shapeType = ParticleSystemShapeType.Cone;
            forma.angle = angulo;
            forma.radius = 0.2f;

            var cores = ps.colorOverLifetime;
            cores.enabled = true;
            var gradiente = new Gradient();
            gradiente.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
            cores.color = gradiente;

            var tamanhoVida = ps.sizeOverLifetime;
            tamanhoVida.enabled = true;
            tamanhoVida.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return ps;
        }

        // ==================================================================
        // Cena, ambiente e materiais
        // ==================================================================
        static UnityEngine.SceneManagement.Scene NovaCena()
            => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        static string Salvar(UnityEngine.SceneManagement.Scene cena, string nome)
        {
            if (nome != LayoutTrilha.CenaBootstrap)
            {
                AuditarSobreposicoes(cena, nome);
                MesclarEstaticos(cena, nome);
            }
            var caminho = $"{PastaCenas}/{nome}.unity";
            EditorSceneManager.SaveScene(cena, caminho);
            return caminho;
        }

        static void Ambiente(Color ambiente, Material ceu, Color corSol, float intensidadeSol, Vector3 rotacaoSol,
            bool neblina = false, Color? corNeblina = null, float densidade = 0.01f)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambiente;
            RenderSettings.skybox = ceu;
            RenderSettings.fog = neblina;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = densidade;
            RenderSettings.fogColor = corNeblina ?? Color.gray;

            var sol = new GameObject("Luz principal").AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.color = corSol;
            sol.intensity = intensidadeSol;
            sol.transform.rotation = Quaternion.Euler(rotacaoSol);
            // Sem sombras em tempo real: orçamento de desempenho do Quest 2.
            sol.shadows = LightShadows.None;
            RenderSettings.sun = sol;
        }

        static Material Ceu(string nome, Color tinta, float exposicao, float atmosfera, Color? chao = null, float tamanhoSol = 0.04f)
        {
            if (materiais.TryGetValue(nome, out var pronto)) return pronto;
            var m = ObterMaterial(nome, "Skybox/Procedural");
            m.SetColor("_SkyTint", tinta);
            m.SetColor("_GroundColor", chao ?? new Color(0.37f, 0.35f, 0.34f));
            m.SetFloat("_Exposure", exposicao);
            m.SetFloat("_AtmosphereThickness", atmosfera);
            m.SetFloat("_SunSize", tamanhoSol);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Opaco(string nome, Color cor, Color? emissao = null, float smooth = 0.2f, float metal = 0f)
        {
            // Mesmo nome = mesmo material: a primeira chamada da construção define as cores.
            if (materiais.TryGetValue(nome, out var pronto)) return pronto;
            var m = ObterMaterial(nome, "Universal Render Pipeline/Lit");
            m.SetColor("_BaseColor", cor);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", emissao ?? Color.black);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Transparente(string nome, Color cor, bool unlit = false, Color? emissao = null)
        {
            if (materiais.TryGetValue(nome, out var pronto)) return pronto;
            var m = ObterMaterial(nome, unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            m.SetColor("_BaseColor", cor);
            ConfigurarTransparencia(m, false);
            if (!unlit)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emissao ?? Color.black);
                m.SetFloat("_Smoothness", 0.6f);
            }

            m.SetFloat("_Cull", (float)CullMode.Off);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Material de partícula/linha: usa a cor por vértice. Aditivo = brilho.</summary>
        static Material Particula(string nome, bool aditivo, Color? cor = null)
        {
            if (materiais.TryGetValue(nome, out var pronto)) return pronto;
            var m = ObterMaterial(nome, "Universal Render Pipeline/Particles/Unlit");
            // Aditivo: textura com RGB caindo a zero. Alfa: RGB branco, só o alfa cai.
            m.SetTexture("_BaseMap", aditivo ? texturaBrilho : texturaFumaca);
            m.SetColor("_BaseColor", cor ?? Color.white);
            ConfigurarTransparencia(m, aditivo);
            m.SetFloat("_Blend", aditivo ? 2f : 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Material de linha sólida (sem a textura de brilho redondo).</summary>
        static Material LinhaSolida(string nome)
        {
            if (materiais.TryGetValue(nome, out var pronto)) return pronto;
            var m = ObterMaterial(nome, "Universal Render Pipeline/Particles/Unlit");
            m.SetTexture("_BaseMap", null);
            m.SetColor("_BaseColor", Color.white);
            ConfigurarTransparencia(m, true);
            m.SetFloat("_Blend", 2f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void ConfigurarTransparencia(Material m, bool aditivo)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(aditivo ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("ShadowCaster", false);
            m.SetShaderPassEnabled("DepthOnly", false);
        }

        static Material ObterMaterial(string nome, string shader)
        {
            if (materiais.TryGetValue(nome, out var existente)) return existente;

            var caminho = $"{PastaMateriais}/{nome}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            var s = Shader.Find(shader);
            if (s == null) Debug.LogError($"[Trilha da Inovação] Shader '{shader}' não encontrado.");

            if (m == null)
            {
                m = new Material(s);
                AssetDatabase.CreateAsset(m, caminho);
            }
            else if (m.shader != s)
            {
                m.shader = s;
            }

            materiais[nome] = m;
            return m;
        }

        /// <summary>Textura redonda e suave para partículas (brilho aditivo ou fumaça).</summary>
        static Texture2D GerarTexturaRedonda(string nome, bool premultiplicada)
        {
            var caminho = $"{PastaGerado}/{nome}.png";
            if (!File.Exists(caminho))
            {
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    var a = Mathf.Clamp01(1f - d);
                    // RGB também cai para zero: funciona em blend aditivo e alfa.
                    var k = a * a;
                    tex.SetPixel(x, y, premultiplicada ? new Color(k, k, k, k) : new Color(1f, 1f, 1f, k));
                }

                File.WriteAllBytes(caminho, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceSynchronousImport);
                var importador = (TextureImporter)AssetImporter.GetAtPath(caminho);
                importador.alphaIsTransparency = true;
                importador.mipmapEnabled = true;
                importador.wrapMode = TextureWrapMode.Clamp;
                importador.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
        }
    }
}
