using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Relatório do modo -qa: para cada cena mede tempo (contra o roteiro),
    /// desempenho, peso da cena (orçamento do Quest 2) e checa a ergonomia
    /// pedida no documento — alvos de toque grandes e alcançáveis, guia fora
    /// do caminho, legendas de até 2 linhas e nada no corredor até a próxima
    /// marca. Erros e avisos do log também entram no relatório.
    /// No Quest (XR ativo) mede também o tempo de GPU, os quadros perdidos e o
    /// estado térmico; com -visitas N repete a visita N vezes (teste de longa
    /// duração) e compara memória, FPS e temperatura entre a primeira e a última.
    /// Testa ainda a pausa ao tirar o óculos e o recomeço depois do abandono.
    /// </summary>
    public class QARelatorio : MonoBehaviour
    {
        // Orçamento do guia de produção (seção "Orçamento de desempenho").
        public const int LimiteTriangulos = 300000;
        public const int LimiteRenderers = 100;
        public const int LimiteLuzesTempoReal = 1;

        class Cena
        {
            public string nome;
            public int marca;
            public int visita;
            public float previsto;
            public float inicio;
            public float duracao;
            public readonly List<float> quadros = new List<float>();
            public int renderers, triangulos, luzes, particulas, textos;
            public readonly List<long> drawCalls = new List<long>();
            public readonly List<long> setPass = new List<long>();
            public readonly List<long> triangulosQuadro = new List<long>();
            public readonly List<float> gpu = new List<float>();
            public int perdidos;
            public readonly List<string> gatilhos = new List<string>();
            public readonly List<string> problemas = new List<string>();
        }

        class Visita
        {
            public int numero;
            public float inicio, duracao;
            public bool completa;
            public readonly List<float> quadros = new List<float>();
            public int perdidos;
            public float memoriaProcesso, memoriaUnity;
            public int termico = -1;
            public float bateria = -1f;
        }

        static readonly string[] NomesTermicos = { "normal", "leve", "moderado", "severo", "crítico", "emergência", "desligando" };

        public static QARelatorio Instancia { get; private set; }

        readonly List<Visita> visitas = new List<Visita>();
        readonly List<string> testes = new List<string>();
        Visita visitaAtual;
        XRDisplaySubsystem tela;
        float atualizacao;
        bool esperandoRecomeco;

        readonly List<Cena> cenas = new List<Cena>();
        readonly Dictionary<string, int> erros = new Dictionary<string, int>();
        readonly Dictionary<string, int> avisos = new Dictionary<string, int>();
        readonly List<string> problemasGerais = new List<string>();
        Cena atual;
        float inicioVisita;
        ProfilerRecorder gravDrawCalls, gravSetPass, gravTriangulos;

        void Awake()
        {
            Instancia = this;
            Application.logMessageReceived += AoLogar;
            inicioVisita = Time.time;
            gravDrawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            gravSetPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            gravTriangulos = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= AoLogar;
            gravDrawCalls.Dispose();
            gravSetPass.Dispose();
            gravTriangulos.Dispose();
            if (Instancia == this) Instancia = null;
        }

        void Update()
        {
            // A pausa simulada para o tempo do jogo: esses quadros não contam.
            if (PausaVisita.Pausada) return;
            if (XRStartup.XRAtivo && (tela == null || !tela.running))
            {
                var telas = new List<XRDisplaySubsystem>();
                SubsystemManager.GetSubsystems(telas);
                tela = telas.Find(t => t.running);
                if (tela != null && tela.TryGetDisplayRefreshRate(out var hz)) atualizacao = hz;
            }

            var perdeu = 0;
            float gpu = -1f;
            if (tela != null)
            {
                if (tela.TryGetDroppedFrameCount(out var n)) perdeu = n;
                if (tela.TryGetAppGPUTimeLastFrame(out var g)) gpu = g;
            }

            if (visitaAtual != null)
            {
                visitaAtual.quadros.Add(Time.unscaledDeltaTime);
                visitaAtual.perdidos += perdeu;
            }

            if (atual == null) return;
            atual.quadros.Add(Time.unscaledDeltaTime);
            atual.perdidos += perdeu;
            if (gpu > 0f) atual.gpu.Add(gpu);
            if (gravDrawCalls.Valid && gravDrawCalls.LastValue > 0) atual.drawCalls.Add(gravDrawCalls.LastValue);
            if (gravSetPass.Valid && gravSetPass.LastValue > 0) atual.setPass.Add(gravSetPass.LastValue);
            if (gravTriangulos.Valid && gravTriangulos.LastValue > 0) atual.triangulosQuadro.Add(gravTriangulos.LastValue);
        }

        void AoLogar(string mensagem, string pilha, LogType tipo)
        {
            if (mensagem.StartsWith("[QA]")) return;
            var chave = mensagem.Length > 180 ? mensagem.Substring(0, 180) : mensagem;
            var alvo = tipo == LogType.Warning ? avisos : tipo == LogType.Log ? null : erros;
            if (alvo == null) return;
            alvo[chave] = alvo.TryGetValue(chave, out var n) ? n + 1 : 1;
        }

        public void Problema(string texto)
        {
            if (atual != null)
            {
                if (!atual.problemas.Contains(texto)) atual.problemas.Add(texto);
            }
            else if (!problemasGerais.Contains(texto))
            {
                problemasGerais.Add(texto);
            }
        }

        public void Gatilho(string tipo) => atual?.gatilhos.Add(tipo);

        // ------------------------------------------------------------------
        // Visitas (teste de longa duração com -visitas N)
        // ------------------------------------------------------------------
        public void InicioVisita()
        {
            visitaAtual = new Visita { numero = visitas.Count + 1, inicio = Time.realtimeSinceStartup };
            visitas.Add(visitaAtual);
        }

        public void FimVisita(bool completa)
        {
            if (visitaAtual == null) return;
            if (atual != null)
            {
                // Visita interrompida no meio de uma cena.
                atual.duracao = Time.time - atual.inicio;
                atual = null;
            }

            visitaAtual.duracao = Time.realtimeSinceStartup - visitaAtual.inicio;
            visitaAtual.completa = completa;
            visitaAtual.memoriaProcesso = MemoriaProcessoMB();
            visitaAtual.memoriaUnity = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            visitaAtual.termico = EstadoTermico();
            visitaAtual.bateria = SystemInfo.batteryLevel;
            visitaAtual = null;
        }

        static float MemoriaProcessoMB()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var depuracao = new AndroidJavaClass("android.os.Debug");
                return depuracao.CallStatic<long>("getPss") / 1024f;
            }
            catch (Exception)
            {
                return 0f;
            }
#else
            using var processo = System.Diagnostics.Process.GetCurrentProcess();
            return processo.WorkingSet64 / (1024f * 1024f);
#endif
        }

        /// <summary>PowerManager.getCurrentThermalStatus (Android 10+); -1 fora do Quest.</summary>
        static int EstadoTermico()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var atividade = unity.GetStatic<AndroidJavaObject>("currentActivity");
                using var energia = atividade.Call<AndroidJavaObject>("getSystemService", "power");
                return energia.Call<int>("getCurrentThermalStatus");
            }
            catch (Exception)
            {
                return -1;
            }
#else
            return -1;
#endif
        }

        // ------------------------------------------------------------------
        // Testes de comportamento
        // ------------------------------------------------------------------

        /// <summary>
        /// Finge que o visitante tirou o óculos por 2 s no meio da cena: o tempo
        /// da cena e o áudio têm de parar, as mãos sumir, e tudo voltar depois.
        /// </summary>
        public IEnumerator TestarPausa(PausaVisita pausa, string cena)
        {
            yield return new WaitForSeconds(4f);
            var antes = Time.time;
            pausa.Simular(true);
            yield return new WaitForSecondsRealtime(2f);
            var parado = Time.time - antes < 0.1f;
            var mudo = AudioListener.pause;
            var semMaos = PausaVisita.Pausada;
            pausa.Simular(false);
            yield return null;
            var voltou = Time.timeScale > 0.99f && !AudioListener.pause && !PausaVisita.Pausada;

            if (parado && mudo && semMaos && voltou)
            {
                testes.Add($"✅ Pausa ao tirar o óculos ({cena}): cena, áudio e mãos pararam por 2 s e a visita continuou.");
            }
            else
            {
                var falhas = new List<string>();
                if (!parado) falhas.Add($"a cena andou {Time.time - antes:0.0} s");
                if (!mudo) falhas.Add("o áudio continuou");
                if (!semMaos) falhas.Add("as mãos continuaram ativas");
                if (!voltou) falhas.Add("a visita não voltou ao normal");
                testes.Add($"❌ Pausa ao tirar o óculos ({cena}): {string.Join(", ", falhas)}.");
                Problema($"Pausa ao tirar o óculos falhou: {string.Join(", ", falhas)}.");
            }
        }

        /// <summary>
        /// Finge que o visitante largou o óculos por mais que o limite: quando
        /// alguém o recoloca, a visita tem de recomeçar na abertura.
        /// </summary>
        public IEnumerator TestarAbandono(PausaVisita pausa, string cena)
        {
            yield return new WaitForSeconds(3f);
            var limite = pausa.limiteAusencia;
            pausa.limiteAusencia = 3f;
            esperandoRecomeco = true;
            pausa.Simular(true);
            yield return new WaitForSecondsRealtime(3.5f);
            pausa.Simular(false);
            pausa.limiteAusencia = limite;

            yield return new WaitForSecondsRealtime(20f);
            if (!esperandoRecomeco) yield break;
            esperandoRecomeco = false;
            testes.Add($"❌ Óculos largado em {cena}: a visita não recomeçou na abertura.");
            problemasGerais.Add("Depois do óculos ficar fora além do limite, a visita não recomeçou.");
        }

        public void InicioCena(string nome, int marca, float previsto, Scene cena, EraSequence sequencia)
        {
            if (esperandoRecomeco && marca == 0)
            {
                esperandoRecomeco = false;
                testes.Add("✅ Óculos largado por 3,5 s (limite de teste: 3 s): ao recolocar, a visita recomeçou na abertura.");
            }

            atual = new Cena { nome = nome, marca = marca, previsto = previsto, inicio = Time.time, visita = visitas.Count };
            cenas.Add(atual);
            Medir(cena);
            VerificarErgonomia(cena, sequencia);
        }

        /// <summary>Alvos que a sequência realmente usa (campos públicos do tipo pedido).</summary>
        static IEnumerable<T> Usados<T>(EraSequence sequencia) where T : Component
        {
            foreach (var campo in sequencia.GetType().GetFields())
            {
                if (campo.FieldType == typeof(T) && campo.GetValue(sequencia) is T alvo && alvo != null) yield return alvo;
            }
        }

        public void FimCena(Scene cena)
        {
            if (atual == null) return;
            Medir(cena);
            atual.duracao = Time.time - atual.inicio;
            atual = null;
        }

        // ------------------------------------------------------------------
        // Peso da cena: o que o Quest 2 vai desenhar.
        // ------------------------------------------------------------------
        void Medir(Scene cena)
        {
            var raizes = cena.GetRootGameObjects().Concat(SceneManager.GetSceneByName(LayoutTrilha.CenaBootstrap).GetRootGameObjects());
            int renderers = 0, triangulos = 0, luzes = 0, particulas = 0, textos = 0;

            foreach (var raiz in raizes)
            {
                foreach (var r in raiz.GetComponentsInChildren<Renderer>())
                {
                    if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                    renderers += Mathf.Max(1, r.sharedMaterials.Length);
                    Mesh malha = null;
                    if (r is MeshRenderer) malha = r.GetComponent<MeshFilter>()?.sharedMesh;
                    else if (r is SkinnedMeshRenderer s) malha = s.sharedMesh;
                    if (malha != null)
                    {
                        for (var i = 0; i < malha.subMeshCount; i++) triangulos += (int)(malha.GetIndexCount(i) / 3);
                    }
                }

                luzes += raiz.GetComponentsInChildren<Light>().Count(l => l.enabled && l.gameObject.activeInHierarchy && l.intensity > 0f);
                particulas += raiz.GetComponentsInChildren<ParticleSystem>().Length;
                textos += raiz.GetComponentsInChildren<TMP_Text>().Count(t => t.gameObject.activeInHierarchy);
            }

            atual.renderers = Mathf.Max(atual.renderers, renderers);
            atual.triangulos = Mathf.Max(atual.triangulos, triangulos);
            atual.luzes = Mathf.Max(atual.luzes, luzes);
            atual.particulas = Mathf.Max(atual.particulas, particulas);
            atual.textos = Mathf.Max(atual.textos, textos);
        }

        // ------------------------------------------------------------------
        // Ergonomia (seções "Premissas de produção" e "Cenários").
        // ------------------------------------------------------------------
        void VerificarErgonomia(Scene cena, EraSequence sequencia)
        {
            var marca = atual.marca;
            var centro = LayoutTrilha.Marcas[marca];
            var objetos = cena.GetRootGameObjects();

            foreach (var alvo in Usados<PokeTrigger>(sequencia))
            {
                var col = alvo.GetComponent<Collider>();
                var b = col.bounds;
                var altura = b.center.y;
                var distancia = Vector2.Distance(LayoutTrilha.PlanoXZ(b.center), LayoutTrilha.PlanoXZ(centro));
                var menor = Mathf.Min(b.size.x, Mathf.Min(b.size.y, b.size.z));
                var maior = Mathf.Max(b.size.x, b.size.z);

                if (maior < 0.15f) Problema($"Alvo '{alvo.name}' tem {maior * 100f:0} cm; o mínimo do roteiro é 15 cm.");
                if (altura < 0.85f || altura > 1.45f) Problema($"Alvo '{alvo.name}' a {altura:0.00} m de altura (faixa: 0,9 a 1,4 m).");
                if (distancia > 0.8f) Problema($"Alvo '{alvo.name}' a {distancia:0.00} m da marca: fora do alcance sem dar passos.");
                if (menor < 0.05f) Problema($"Alvo '{alvo.name}' muito fino ({menor * 100f:0} cm) para o dedo rastreado.");
            }

            foreach (var prox in Usados<ProximityTrigger>(sequencia))
            {
                var d = Vector2.Distance(LayoutTrilha.PlanoXZ(prox.transform.position), LayoutTrilha.PlanoXZ(centro)) - prox.Raio;
                if (d > 0.9f) Problema($"Gatilho '{prox.name}' exige andar {d:0.00} m para fora da marca.");
            }

            VerificarCorredor(objetos, marca);
        }

        /// <summary>
        /// Depois da cena, o visitante caminha até a próxima marca com este
        /// cenário ainda visível: nada sólido pode estar no corredor.
        /// </summary>
        void VerificarCorredor(GameObject[] objetos, int marca)
        {
            if (marca >= LayoutTrilha.TotalEpocas || atual.nome.Contains("Encerramento")) return;

            var de = LayoutTrilha.Marcas[marca];
            var para = LayoutTrilha.Marcas[marca + 1];
            const float raio = 0.35f;
            var nomes = new List<string>();

            foreach (var r in objetos.SelectMany(o => o.GetComponentsInChildren<Renderer>()))
            {
                if (!r.enabled || r is LineRenderer || r is TrailRenderer || r is ParticleSystemRenderer) continue;
                if (r.GetComponentInParent<TMP_Text>() != null) continue;
                // Painéis que giram para encarar o visitante: o AABB exagera a largura.
                if (r.GetComponentInParent<EncararUsuario>() != null) continue;
                var b = r.bounds;
                if (b.max.y < 0.25f || b.min.y > 1.9f) continue;             // piso, teto, céu
                if (b.size.x > 8f || b.size.z > 8f) continue;               // terreno e paredes distantes

                var maisPerto = PontoMaisProximo(de, para, b.center);
                var fora = new Vector2(
                    Mathf.Max(0f, Mathf.Abs(maisPerto.x - b.center.x) - b.extents.x),
                    Mathf.Max(0f, Mathf.Abs(maisPerto.z - b.center.z) - b.extents.z));
                if (fora.magnitude >= raio) continue;

                // Malhas mescladas cobrem a sala inteira: testa os vértices de verdade.
                var malha = r.GetComponent<MeshFilter>()?.sharedMesh;
                if (malha != null && malha.isReadable && !VerticeNoCorredor(malha, r.transform, de, para, raio)) continue;
                nomes.Add(r.name);
            }

            if (nomes.Count > 0)
            {
                Problema($"No caminho até a marca {marca + 1}: {string.Join(", ", nomes.Distinct().Take(8))}" +
                         (nomes.Count > 8 ? $" (+{nomes.Count - 8})" : string.Empty));
            }
        }

        static bool VerticeNoCorredor(Mesh malha, Transform t, Vector3 de, Vector3 para, float raio)
        {
            // Começa e termina fora dos círculos das marcas (o visitante já está nelas).
            var dir = (para - de).normalized;
            var a = de + dir * LayoutTrilha.RaioMarca;
            var b = para - dir * LayoutTrilha.RaioMarca;
            foreach (var v in malha.vertices)
            {
                var p = t.TransformPoint(v);
                if (p.y < 0.25f || p.y > 1.9f) continue;
                var q = PontoMaisProximo(a, b, p);
                if (new Vector2(p.x - q.x, p.z - q.z).sqrMagnitude < raio * raio) return true;
            }

            return false;
        }

        static Vector3 PontoMaisProximo(Vector3 a, Vector3 b, Vector3 p)
        {
            var ab = b - a;
            ab.y = 0f;
            var ap = p - a;
            ap.y = 0f;
            var t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
            return a + ab * t;
        }

        // ------------------------------------------------------------------
        // Relatório
        // ------------------------------------------------------------------
        public string Escrever(string pasta)
        {
            var ci = CultureInfo.GetCultureInfo("pt-BR");
            var sb = new StringBuilder();
            var total = Time.time - inicioVisita;
            // Tabela de cenas: a primeira visita completa (as outras aparecem em "Visitas").
            var primeira = visitas.FirstOrDefault(v => v.completa);
            var cenasTabela = primeira != null ? cenas.Where(c => c.visita == primeira.numero).ToList() : cenas;
            var previsto = cenasTabela.Sum(c => c.previsto);

            sb.AppendLine("# Relatório de QA — Trilha da Inovação");
            sb.AppendLine();
            sb.AppendLine($"Gerado em {System.DateTime.Now.ToString("dd/MM/yyyy HH:mm", ci)} · Unity {Application.unityVersion} · " +
                          $"{SystemInfo.graphicsDeviceName} · {Screen.width}×{Screen.height}");
            sb.AppendLine();
            var aparelho = XRStartup.XRAtivo
                ? $"{SystemInfo.deviceModel} · VR a {(atualizacao > 0 ? atualizacao : 72):0} Hz"
                : $"{SystemInfo.deviceModel} · modo desktop (FPS do PC, não do Quest)";
            sb.AppendLine($"Aparelho: {aparelho}");
            sb.AppendLine();
            var tempoVisita = primeira != null ? primeira.duracao : total;
            sb.AppendLine($"Visita completa: **{Formatar(tempoVisita)}** (meta do roteiro: 4:45; soma prevista das cenas: {Formatar(previsto)}).");
            sb.AppendLine("O tempo inclui caminhadas simuladas a 0,7 m/s entre as marcas.");
            sb.AppendLine();
            if (visitas.Count > 1) sb.AppendLine("Tabela da primeira visita; as seguintes estão em \"Visitas\".");
            sb.AppendLine();
            sb.AppendLine("| Cena | Previsto | Real | Gatilhos | FPS médio | Quadro p95 | GPU p95 | Quadros perdidos | Draw calls (méd/p95/máx) | SetPass máx | Triângulos/quadro máx | Renderers | Luzes |");
            sb.AppendLine("| --- | ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
            foreach (var c in cenasTabela)
            {
                var ordenados = c.quadros.OrderBy(q => q).ToList();
                var media = ordenados.Count > 0 ? ordenados.Average() : 0f;
                var p95 = ordenados.Count > 0 ? ordenados[(int)(ordenados.Count * 0.95f)] : 0f;
                var dcMedia = c.drawCalls.Count > 0 ? c.drawCalls.Average() : 0;
                var dcMax = c.drawCalls.Count > 0 ? c.drawCalls.Max() : 0;
                var dcP = c.drawCalls.OrderBy(x => x).ToList();
                var dcP95Tab = dcP.Count > 0 ? dcP[(int)(dcP.Count * 0.95f)] : 0;
                var spMax = c.setPass.Count > 0 ? c.setPass.Max() : 0;
                var triMax = c.triangulosQuadro.Count > 0 ? c.triangulosQuadro.Max() : c.triangulos;
                var gpuOrd = c.gpu.OrderBy(g => g).ToList();
                var gpuTexto = gpuOrd.Count > 0 ? $"{gpuOrd[(int)(gpuOrd.Count * 0.95f)] * 1000f:0.0} ms" : "—";
                var perdidosTexto = XRStartup.XRAtivo ? $"{c.perdidos}" : "—";
                sb.AppendLine($"| {c.nome} | {c.previsto:0} s | {c.duracao:0.0} s | {string.Join(", ", c.gatilhos)} | " +
                              $"{(media > 0 ? 1f / media : 0):0} | {p95 * 1000f:0.0} ms | {gpuTexto} | {perdidosTexto} | {dcMedia:0}/{dcP95Tab}/{dcMax} | {spMax} | " +
                              $"{triMax.ToString("N0", ci)} | {c.renderers} | {c.luzes} |");
            }

            sb.AppendLine();
            sb.AppendLine("Draw calls e triângulos medidos pelo ProfilerRecorder neste PC (pipeline PC); no Quest, com multiview, a contagem de draw calls é semelhante.");
            sb.AppendLine($"Orçamento Quest 2 usado na checagem: ≤ {LimiteRenderers} draw calls, " +
                          $"≤ {LimiteTriangulos.ToString("N0", ci)} triângulos, {LimiteLuzesTempoReal} luz em tempo real (+ luzes pontuais pequenas).");
            sb.AppendLine();
            EscreverVisitas(sb, ci);
            sb.AppendLine("## Testes de comportamento");
            sb.AppendLine();
            if (testes.Count == 0) sb.AppendLine("Nenhum teste rodou nesta execução.");
            foreach (var t in testes) sb.AppendLine($"- {t}");
            sb.AppendLine();
            sb.AppendLine("## Problemas encontrados");
            sb.AppendLine();
            var algum = false;
            foreach (var p in problemasGerais)
            {
                sb.AppendLine($"- [ ] Geral: {p}");
                algum = true;
            }

            var linhas = new List<string>();
            foreach (var c in cenas)
            {
                // p95: um quadro isolado (aquecimento de shader ao abrir a cena) não reprova.
                var dcOrdenados = c.drawCalls.OrderBy(x => x).ToList();
                var dcP95 = dcOrdenados.Count > 0 ? dcOrdenados[(int)(dcOrdenados.Count * 0.95f)] : c.renderers;
                if (dcP95 > LimiteRenderers) c.problemas.Add($"{dcP95} draw calls em 5% dos quadros (meta ≤ {LimiteRenderers}).");
                var triPico = c.triangulosQuadro.Count > 0 ? c.triangulosQuadro.Max() : c.triangulos;
                if (triPico > LimiteTriangulos) c.problemas.Add($"{triPico:N0} triângulos por quadro (limite {LimiteTriangulos:N0}).");
                if (c.gatilhos.Contains("auto")) c.problemas.Add("A interação simulada não disparou o gatilho; a cena avançou pelo plano B.");
                if (XRStartup.XRAtivo && c.quadros.Count > 0)
                {
                    // VRC.Quest.Performance.1: 72 FPS no óculos.
                    var hz = atualizacao > 0 ? atualizacao : 72f;
                    var q = c.quadros.OrderBy(x => x).ToList();
                    var p95Quadro = q[(int)(q.Count * 0.95f)];
                    if (p95Quadro > 1.1f / hz) c.problemas.Add($"5% dos quadros levam {p95Quadro * 1000f:0.0} ms: abaixo de {hz:0} FPS no óculos.");
                    if (c.perdidos > c.quadros.Count / 100) c.problemas.Add($"{c.perdidos} quadros perdidos ({100f * c.perdidos / c.quadros.Count:0.0}% da cena).");
                }
                foreach (var p in c.problemas)
                {
                    var linha = $"- [ ] {c.nome}: {p}";
                    if (linhas.Contains(linha)) continue;
                    linhas.Add(linha);
                    sb.AppendLine(linha);
                    algum = true;
                }
            }

            if (!algum) sb.AppendLine("Nenhum.");
            sb.AppendLine();
            sb.AppendLine("## Log do player");
            sb.AppendLine();
            sb.AppendLine($"{erros.Values.Sum()} erro(s) e {avisos.Values.Sum()} aviso(s).");
            foreach (var e in erros) sb.AppendLine($"- Erro ×{e.Value}: `{e.Key.Replace('`', '\'')}`");
            foreach (var a in avisos.Take(20)) sb.AppendLine($"- Aviso ×{a.Value}: `{a.Key.Replace('`', '\'')}`");

            var caminho = Path.Combine(pasta, "relatorio-qa.md");
            File.WriteAllText(caminho, sb.ToString());
            return caminho;
        }

        /// <summary>Uma linha por visita e a comparação entre a primeira e a última.</summary>
        void EscreverVisitas(StringBuilder sb, CultureInfo ci)
        {
            if (visitas.Count == 0) return;
            sb.AppendLine("## Visitas");
            sb.AppendLine();
            sb.AppendLine("| # | Resultado | Duração | FPS médio | Quadros perdidos | Memória do processo | Memória Unity | Estado térmico | Bateria |");
            sb.AppendLine("| ---: | --- | ---: | ---: | ---: | ---: | ---: | --- | ---: |");
            foreach (var v in visitas)
            {
                var media = v.quadros.Count > 0 ? v.quadros.Average() : 0f;
                sb.AppendLine($"| {v.numero} | {(v.completa ? "completa" : "interrompida")} | {Formatar(v.duracao)} | {(media > 0 ? 1f / media : 0):0} | " +
                              $"{(XRStartup.XRAtivo ? v.perdidos.ToString() : "—")} | {v.memoriaProcesso.ToString("N0", ci)} MB | " +
                              $"{v.memoriaUnity.ToString("N0", ci)} MB | {(v.termico >= 0 ? NomesTermicos[Mathf.Min(v.termico, 6)] : "—")} | " +
                              $"{(v.bateria >= 0 ? $"{v.bateria * 100f:0}%" : "—")} |");
            }

            sb.AppendLine();
            var completas = visitas.Where(v => v.completa).ToList();
            if (completas.Count >= 2)
            {
                // A primeira visita inclui o aquecimento (shaders, cache); com 3+, compara a partir da segunda.
                var baseVisita = completas.Count >= 3 ? completas[1] : completas[0];
                var ultima = completas[completas.Count - 1];
                var cresceu = ultima.memoriaProcesso - baseVisita.memoriaProcesso;
                var fpsBase = 1f / Mathf.Max(0.0001f, baseVisita.quadros.Average());
                var fpsUltima = 1f / Mathf.Max(0.0001f, ultima.quadros.Average());
                sb.AppendLine($"Da visita {baseVisita.numero} à {ultima.numero}: memória do processo {(cresceu >= 0 ? "+" : "")}{cresceu:0} MB, " +
                              $"FPS médio {fpsBase:0} → {fpsUltima:0}.");
                sb.AppendLine();
                if (cresceu > 40f && cresceu > baseVisita.memoriaProcesso * 0.1f)
                    problemasGerais.Add($"A memória cresceu {cresceu:0} MB entre as visitas {baseVisita.numero} e {ultima.numero}: possível vazamento.");
                if (fpsUltima < fpsBase * 0.9f)
                    problemasGerais.Add($"O FPS médio caiu de {fpsBase:0} para {fpsUltima:0} ao longo das visitas (aquecimento ou vazamento).");
            }

            var quente = visitas.Where(v => v.termico >= 3).Select(v => v.numero).ToList();
            if (quente.Count > 0) problemasGerais.Add($"Estado térmico severo ou pior nas visitas {string.Join(", ", quente)}: o Quest vai reduzir o desempenho.");
        }

        static string Formatar(float segundos) => $"{(int)(segundos / 60)}:{(int)(segundos % 60):00}";
    }
}
