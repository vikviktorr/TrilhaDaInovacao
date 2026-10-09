using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Relatório do modo -qa: para cada cena mede tempo (contra o roteiro),
    /// desempenho, peso da cena (orçamento do Quest 2) e checa a ergonomia
    /// pedida no documento — alvos de toque grandes e alcançáveis, guia fora
    /// do caminho, legendas de até 2 linhas e nada no corredor até a próxima
    /// marca. Erros e avisos do log também entram no relatório.
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
            public float previsto;
            public float inicio;
            public float duracao;
            public readonly List<float> quadros = new List<float>();
            public int renderers, triangulos, luzes, particulas, textos;
            public readonly List<long> drawCalls = new List<long>();
            public readonly List<long> setPass = new List<long>();
            public readonly List<long> triangulosQuadro = new List<long>();
            public readonly List<string> gatilhos = new List<string>();
            public readonly List<string> problemas = new List<string>();
        }

        public static QARelatorio Instancia { get; private set; }

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
            if (atual == null) return;
            atual.quadros.Add(Time.unscaledDeltaTime);
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

        public void InicioCena(string nome, int marca, float previsto, Scene cena, EraSequence sequencia)
        {
            atual = new Cena { nome = nome, marca = marca, previsto = previsto, inicio = Time.time };
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
            var previsto = cenas.Sum(c => c.previsto);

            sb.AppendLine("# Relatório de QA — Trilha da Inovação");
            sb.AppendLine();
            sb.AppendLine($"Gerado em {System.DateTime.Now.ToString("dd/MM/yyyy HH:mm", ci)} · Unity {Application.unityVersion} · " +
                          $"{SystemInfo.graphicsDeviceName} · {Screen.width}×{Screen.height}");
            sb.AppendLine();
            sb.AppendLine($"Visita completa: **{Formatar(total)}** (meta do roteiro: 4:45; soma prevista das cenas: {Formatar(previsto)}).");
            sb.AppendLine("O tempo inclui caminhadas simuladas a 0,7 m/s entre as marcas.");
            sb.AppendLine();
            sb.AppendLine("| Cena | Previsto | Real | Gatilhos | FPS médio | Quadro p95 | Draw calls (méd/p95/máx) | SetPass máx | Triângulos/quadro máx | Renderers | Luzes |");
            sb.AppendLine("| --- | ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
            foreach (var c in cenas)
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
                sb.AppendLine($"| {c.nome} | {c.previsto:0} s | {c.duracao:0.0} s | {string.Join(", ", c.gatilhos)} | " +
                              $"{(media > 0 ? 1f / media : 0):0} | {p95 * 1000f:0.0} ms | {dcMedia:0}/{dcP95Tab}/{dcMax} | {spMax} | " +
                              $"{triMax.ToString("N0", ci)} | {c.renderers} | {c.luzes} |");
            }

            sb.AppendLine();
            sb.AppendLine("Draw calls e triângulos medidos pelo ProfilerRecorder neste PC (pipeline PC); no Quest, com multiview, a contagem de draw calls é semelhante.");
            sb.AppendLine($"Orçamento Quest 2 usado na checagem: ≤ {LimiteRenderers} draw calls, " +
                          $"≤ {LimiteTriangulos.ToString("N0", ci)} triângulos, {LimiteLuzesTempoReal} luz em tempo real (+ luzes pontuais pequenas).");
            sb.AppendLine();
            sb.AppendLine("## Problemas encontrados");
            sb.AppendLine();
            var algum = false;
            foreach (var p in problemasGerais)
            {
                sb.AppendLine($"- [ ] Geral: {p}");
                algum = true;
            }

            foreach (var c in cenas)
            {
                // p95: um quadro isolado (aquecimento de shader ao abrir a cena) não reprova.
                var dcOrdenados = c.drawCalls.OrderBy(x => x).ToList();
                var dcP95 = dcOrdenados.Count > 0 ? dcOrdenados[(int)(dcOrdenados.Count * 0.95f)] : c.renderers;
                if (dcP95 > LimiteRenderers) c.problemas.Add($"{dcP95} draw calls em 5% dos quadros (meta ≤ {LimiteRenderers}).");
                var triPico = c.triangulosQuadro.Count > 0 ? c.triangulosQuadro.Max() : c.triangulos;
                if (triPico > LimiteTriangulos) c.problemas.Add($"{triPico:N0} triângulos por quadro (limite {LimiteTriangulos:N0}).");
                if (c.gatilhos.Contains("auto")) c.problemas.Add("A interação simulada não disparou o gatilho; a cena avançou pelo plano B.");
                foreach (var p in c.problemas)
                {
                    sb.AppendLine($"- [ ] {c.nome}: {p}");
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

        static string Formatar(float segundos) => $"{(int)(segundos / 60)}:{(int)(segundos % 60):00}";
    }
}
