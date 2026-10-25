using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Reconstituição do voo do HANBIT-Nano em 22/12/2025, às 22h13, na sequência
    /// e nos tempos do acidente real:
    /// <list type="bullet">
    /// <item>T+0: ignição do motor híbrido de 245 kN e subida vertical, como previsto (FAB);</item>
    /// <item>~T+12: o foguete entra na camada de nuvens, onde o contato com o solo se perdeu (Innospace);</item>
    /// <item>T+30: passa a barreira do som e o Max-Q (transmissão ao vivo);</item>
    /// <item>T+33: o gás vaza pela vedação, a câmara de combustão do 1º estágio rompe e o foguete
    ///   fica envolto em chamas (Innospace, AEB e CENIPA, 2026);</item>
    /// <item>sem empuxo, se parte em três ou quatro pedaços, que caem em queda livre dentro da
    ///   zona de segurança terrestre do CLA e explodem ao atingir o chão (Innospace, FAB).</item>
    /// </list>
    /// A subida até as nuvens está em escala real; acima delas o foguete só aparece
    /// como brilho, e as distâncias são encurtadas para caber no céu da cena. A queda
    /// dos pedaços também é encurtada (no voo real levou cerca de um minuto).
    /// A câmera do visitante nunca se move; o "tremor" vem só do som grave.
    /// </summary>
    public class Decolagem : MonoBehaviour
    {
        public enum Fase { Parado, Subindo, Anomalia, Caindo, Impacto, Fim }

        public Transform foguete;
        public ParticleSystem fogo;
        public ParticleSystem fumacaBase;
        public ParticleSystem rastro;
        public Light clarao;
        public AudioSource motor;

        [Header("Céu")]
        [Tooltip("Partículas grandes e escuras da camada de nuvens (emitidas uma vez, paradas).")]
        public ParticleSystem nuvens;
        [Tooltip("Brilho do motor e do fogo visto através das nuvens (quad aditivo).")]
        public ParticleSystem brilho;
        [Tooltip("Altura da base e do topo da camada de nuvens, em metros.")]
        public Vector2 camadaNuvens = new Vector2(380f, 480f);
        [Tooltip("Direção da trajetória no chão (para o mar, a nordeste).")]
        public Vector3 rumo = new Vector3(0.45f, 0f, 1f);

        [Header("Falha e queda")]
        public float tempoMaxQ = 30f;
        public float tempoRuptura = 33f;
        [Tooltip("Segundos de chamas e perda de empuxo antes de o foguete se partir.")]
        public float tempoAtePartir = 2.5f;
        [Tooltip("Segundos de queda dos pedaços até o chão (encurtado).")]
        public float tempoQueda = 22f;
        [Tooltip("Pedaços do foguete (coifa e 2º estágio, interestágio, 1º estágio em duas partes).")]
        public Transform[] pedacos;
        [Tooltip("Onde cada pedaço cai, na zona de segurança terrestre (espaço do pai do foguete).")]
        public Vector3[] impactos;

        [Header("Explosão no solo")]
        public ParticleSystem bolaDeFogo;
        public ParticleSystem fumacaExplosao;
        public ParticleSystem destrocos;
        [Tooltip("Fogo que continua no chão depois do impacto.")]
        public ParticleSystem incendio;

        /// <summary>Tempo de voo (T+), em segundos de cena.</summary>
        public float TempoDeVoo { get; private set; }
        public Fase Estado { get; private set; }
        /// <summary>Para onde olhar agora: o foguete, o brilho nas nuvens ou os pedaços.</summary>
        public Vector3 PontoAtual { get; private set; }
        /// <summary>Onde o pedaço principal explodiu ao cair (para enquadrar e falar dele depois).</summary>
        public Vector3 PontoDaExplosao { get; private set; }
        /// <summary>1 = tempo real; o tour automático acelera.</summary>
        public float Escala { get; set; } = 1f;

        const float VelocidadeDoSom = 343f;
        const float Passo = 0.05f;
        static readonly int CorBase = Shader.PropertyToID("_BaseColor");

        Vector3 posicaoInicial;
        Quaternion rotacaoInicial;
        Vector3 posicaoClarao, posicaoMotor;
        float alcanceClarao;
        readonly ParticleSystem.Particle[] particulaBrilho = new ParticleSystem.Particle[1];
        Material materialNuvens;
        Renderer[] corpo;
        Vector3[] trajetoria;
        bool pronto;

        void Awake()
        {
            posicaoInicial = foguete.localPosition;
            rotacaoInicial = foguete.localRotation;
            if (clarao != null)
            {
                posicaoClarao = clarao.transform.position;
                alcanceClarao = clarao.range;
            }

            if (motor != null) posicaoMotor = motor.transform.position;
            // Só a malha do foguete some na nuvem: desativar o objeto pararia as corrotinas
            // deste componente, que fica nele.
            corpo = System.Array.FindAll(foguete.GetComponentsInChildren<Renderer>(true), x => !(x is ParticleSystemRenderer));
            if (nuvens != null) materialNuvens = nuvens.GetComponent<ParticleSystemRenderer>().material;
            trajetoria = CalcularTrajetoria();
            EmitirNuvens();
            pronto = true;
            Parar();
        }

        public void Parar()
        {
            if (!pronto) return;
            StopAllCoroutines();
            Estado = Fase.Parado;
            TempoDeVoo = 0f;
            foguete.localPosition = posicaoInicial;
            foguete.localRotation = rotacaoInicial;
            MostrarCorpo(true);
            PontoAtual = foguete.position + Vector3.up * 11f;
            foreach (var p in new[] { fogo, fumacaBase, rastro, bolaDeFogo, fumacaExplosao, destrocos, incendio })
            {
                if (p != null) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (pedacos != null)
                foreach (var p in pedacos)
                    if (p != null) p.gameObject.SetActive(false);

            Brilho(Vector3.zero, 0f, 0f, Color.black);
            if (clarao != null)
            {
                clarao.intensity = 0f;
                clarao.range = alcanceClarao;
                clarao.transform.position = posicaoClarao;
            }

            if (motor != null)
            {
                motor.Stop();
                motor.transform.position = posicaoMotor;
            }
        }

        public IEnumerator Ignicao(float duracao)
        {
            if (motor != null)
            {
                motor.clip = ProceduralAudio.Obter("motor");
                motor.loop = true;
                motor.volume = 0f;
                motor.Play();
            }

            fogo?.Play();
            fumacaBase?.Play();

            for (var t = 0f; t < duracao; t += Time.deltaTime * Escala)
            {
                var k = t / duracao;
                if (clarao != null) clarao.intensity = Mathf.Lerp(0f, 12f, k);
                if (motor != null) motor.volume = Mathf.Lerp(0f, 0.8f, k);
                yield return null;
            }
        }

        /// <summary>
        /// Trajetória real até a ruptura, em passos de 0,05 s: aceleração que cresce
        /// conforme o propelente queima (4 m/s² na saída, ~15 m/s² aos 33 s, perto
        /// de Mach 1 e ~4 km de altura), subida vertical até passar a torre e depois
        /// a inclinação gradual para o mar.
        /// </summary>
        Vector3[] CalcularTrajetoria()
        {
            var n = Mathf.CeilToInt(tempoRuptura / Passo) + 1;
            var pontos = new Vector3[n];
            var direcao = rumo.normalized;
            var p = Vector3.zero;
            var v = 0f;
            for (var i = 0; i < n; i++)
            {
                pontos[i] = p;
                var t = i * Passo;
                v += (4f + 0.35f * t) * Passo;
                var inclinacao = Mathf.Min(35f, Mathf.Max(0f, t - 6f) * 1.6f) * Mathf.Deg2Rad;
                p += (Vector3.up * Mathf.Cos(inclinacao) + direcao * Mathf.Sin(inclinacao)) * v * Passo;
            }

            return pontos;
        }

        /// <summary>Posição no espaço do pai, com as distâncias acima das nuvens encurtadas.</summary>
        Vector3 Posicao(float t)
        {
            var f = Mathf.Clamp(t / Passo, 0f, trajetoria.Length - 1.001f);
            var i = Mathf.FloorToInt(f);
            var real = Vector3.Lerp(trajetoria[i], trajetoria[i + 1], f - i);
            var acima = real.y - camadaNuvens.x;
            if (acima > 0f)
            {
                // Acima da base das nuvens: só o brilho aparece; cabe no céu da cena (1,5 km).
                var fator = Mathf.Lerp(1f, 0.15f, Mathf.Clamp01(acima / 300f));
                real = new Vector3(real.x * fator, camadaNuvens.x + acima * fator, real.z * fator);
            }

            return posicaoInicial + real;
        }

        /// <summary>
        /// O voo inteiro: subida, ruptura aos 33 s, queda dos pedaços e explosão no solo.
        /// <paramref name="aoMudar"/> avisa a cena a cada fase (para o placar e o guia).
        /// </summary>
        public IEnumerator Voar(System.Action<Fase> aoMudar = null)
        {
            void Mudar(Fase f)
            {
                Estado = f;
                aoMudar?.Invoke(f);
            }

            var pai = foguete.parent;
            Mudar(Fase.Subindo);
            rastro?.Play();
            if (motor != null) motor.volume = 0.8f;

            // --- Subida até a ruptura ---------------------------------------------
            var anterior = foguete.localPosition;
            for (TempoDeVoo = 0f; TempoDeVoo < tempoRuptura; TempoDeVoo += Time.deltaTime * Escala)
            {
                var local = Posicao(TempoDeVoo);
                foguete.localPosition = local;
                var direcao = local - anterior;
                if (direcao.sqrMagnitude > 1e-6f) foguete.localRotation = Quaternion.FromToRotation(Vector3.up, direcao.normalized) * rotacaoInicial;
                anterior = local;

                var mundo = pai.TransformPoint(local);
                var altura = local.y - posicaoInicial.y;
                PontoAtual = mundo + foguete.up * 11f;
                // O motor vai junto, com o som caindo pela distância.
                if (motor != null) motor.transform.position = mundo;
                if (clarao != null)
                {
                    clarao.transform.position = mundo;
                    clarao.intensity = Mathf.Lerp(12f, 0f, altura / camadaNuvens.x);
                }

                if (TempoDeVoo > 3f) fumacaBase?.Stop();
                // Na nuvem, o foguete some e fica só o brilho difuso do motor.
                var dentro = Mathf.InverseLerp(camadaNuvens.x - 20f, camadaNuvens.x + 40f, altura);
                MostrarCorpo(dentro < 0.95f);
                // Como na transmissão: de longe o motor é uma bola de luz com halo, que vira só um
                // clarão difuso dentro das nuvens.
                Brilho(mundo, Mathf.Lerp(30f, 220f, dentro), Mathf.Lerp(1f, 0.6f, dentro), new Color(1f, 0.7f, 0.35f));
                yield return null;
            }

            // --- Ruptura: vazamento, câmara rompe, chamas e perda de empuxo ---------
            Mudar(Fase.Anomalia);
            var ponto = pai.TransformPoint(Posicao(tempoRuptura));
            PontoAtual = ponto;
            MostrarCorpo(false);
            fogo?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            rastro?.Stop();
            if (clarao != null) clarao.intensity = 0f;
            StartCoroutine(SomAtrasado(ponto, 0.45f));
            // A transmissão mostra o foguete envolto em chamas ("Passing Max Q") e uma bola de
            // fogo vermelho-alaranjada no céu, soltando fagulhas: emitida grande, porque fica a ~1 km.
            EmitirPerto(bolaDeFogo, ponto, 40, 70f, 25f);
            EmitirPerto(destrocos, ponto, 80, 6f, 60f);

            for (var t = 0f; t < tempoAtePartir; t += Time.deltaTime * Escala)
            {
                TempoDeVoo = tempoRuptura + t;
                // Envolto em chamas: o clarão toma a nuvem e tremula. A ~1 km de quem
                // assiste, só um brilho de centenas de metros aparece como na transmissão.
                var k = t / tempoAtePartir;
                var tremor = 0.75f + 0.25f * Mathf.PerlinNoise(t * 9f, 0.3f);
                Brilho(ponto, Mathf.Lerp(320f, 700f, Mathf.Sqrt(k)), 2f * tremor * (1f - 0.4f * k), new Color(1f, 0.38f, 0.14f));
                // Sem empuxo, o ronco do motor some (o som chega com atraso, pela distância).
                if (motor != null) motor.volume = Mathf.Lerp(0.8f, 0f, k);
                yield return null;
            }

            if (motor != null) motor.Stop();

            // --- Queda livre dos pedaços até a zona de segurança -------------------
            Mudar(Fase.Caindo);
            var n = pedacos != null ? Mathf.Min(pedacos.Length, impactos?.Length ?? 0) : 0;
            var giros = new Vector3[n];
            var inicio = new float[n];
            var caiu = new bool[n];
            var rng = new System.Random(2025);
            for (var i = 0; i < n; i++)
            {
                pedacos[i].position = ponto + new Vector3((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f) * 25f;
                pedacos[i].gameObject.SetActive(true);
                foreach (var ps in pedacos[i].GetComponentsInChildren<ParticleSystem>()) ps.Play();
                giros[i] = new Vector3((float)rng.NextDouble() * 120f - 60f, (float)rng.NextDouble() * 200f - 100f, (float)rng.NextDouble() * 120f - 60f);
                // Os pedaços maiores caem primeiro.
                inicio[i] = 0.06f * i;
            }

            var restantes = n;
            for (var t = 0f; restantes > 0; t += Time.deltaTime * Escala)
            {
                TempoDeVoo = tempoRuptura + tempoAtePartir + t;
                var maisBaixo = ponto;
                for (var i = 0; i < n; i++)
                {
                    if (caiu[i]) continue;
                    var s = Mathf.Clamp01(t / (tempoQueda * (1f + inicio[i])));
                    var alvo = pai.TransformPoint(impactos[i]);
                    // Começa quase parado e acelera para baixo (queda livre com arrasto).
                    var horizontal = Vector3.Lerp(ponto, alvo, Mathf.SmoothStep(0f, 1f, s));
                    var y = Mathf.Lerp(ponto.y, alvo.y, s * s) + 25f * Mathf.Sin(Mathf.PI * s) * (1f - s);
                    pedacos[i].position = new Vector3(horizontal.x, y, horizontal.z);
                    pedacos[i].Rotate(giros[i] * (Time.deltaTime * Escala), Space.Self);
                    if (pedacos[i].position.y < maisBaixo.y) maisBaixo = pedacos[i].position;
                    if (s < 1f) continue;

                    caiu[i] = true;
                    restantes--;
                    Explodir(alvo, i == 0);
                }

                PontoAtual = maisBaixo;
                var brilhoNuvem = Mathf.Clamp01(1f - t / 6f);
                Brilho(ponto, 450f, 0.7f * brilhoNuvem, new Color(1f, 0.5f, 0.2f));
                yield return null;
            }

            // --- Impacto: explosão e fogo no chão ------------------------------------
            Mudar(Fase.Impacto);
            PontoAtual = PontoDaExplosao;
            for (var t = 0f; t < 4f; t += Time.deltaTime * Escala)
            {
                if (clarao != null) clarao.intensity = 3f + 37f * Mathf.Exp(-t * 2.2f);
                yield return null;
            }

            Mudar(Fase.Fim);
        }

        void MostrarCorpo(bool visivel)
        {
            foreach (var r in corpo) r.enabled = visivel;
        }

        static void EmitirEm(ParticleSystem ps, Vector3 ponto, int quantidade, float tamanho)
        {
            if (ps == null) return;
            ps.Emit(new ParticleSystem.EmitParams { position = ponto, startSize = tamanho, applyShapeToPosition = true }, quantidade);
        }

        /// <summary>
        /// A neblina da cena (exponencial ao quadrado) apaga o que está a ~1 km. O que precisa ser
        /// visto lá no alto é desenhado na mesma direção, mais perto da câmera e menor na mesma
        /// proporção: o tamanho na tela não muda e a neblina quase não age.
        /// </summary>
        static Vector3 Perto(Vector3 ponto, out float proporcao)
        {
            proporcao = 1f;
            var cam = Camera.main;
            if (cam == null) return ponto;
            var origem = cam.transform.position;
            var distancia = Vector3.Distance(origem, ponto);
            if (distancia <= DistanciaSemNeblina) return ponto;
            proporcao = DistanciaSemNeblina / distancia;
            return Vector3.LerpUnclamped(origem, ponto, proporcao);
        }

        const float DistanciaSemNeblina = 150f;

        static void EmitirPerto(ParticleSystem ps, Vector3 ponto, int quantidade, float tamanho, float velocidade)
        {
            if (ps == null) return;
            var perto = Perto(ponto, out var k);
            for (var i = 0; i < quantidade; i++)
                ps.Emit(new ParticleSystem.EmitParams
                {
                    position = perto + Random.insideUnitSphere * (tamanho * 0.3f * k),
                    velocity = Random.insideUnitSphere * (velocidade * k),
                    startSize = tamanho * k * Random.Range(0.6f, 1f)
                }, 1);
        }

        void Explodir(Vector3 ponto, bool principal)
        {
            foreach (var p in pedacos)
            {
                if (p == null || !p.gameObject.activeSelf) continue;
                if ((p.position - ponto).sqrMagnitude < 4f) p.gameObject.SetActive(false);
            }

            var escala = principal ? 1f : 0.35f;
            foreach (var (ps, n) in new[] { (bolaDeFogo, 70), (fumacaExplosao, 45), (destrocos, 60) })
            {
                if (ps == null) continue;
                ps.transform.position = ponto + Vector3.up * 2f;
                ps.Emit(Mathf.RoundToInt(n * escala));
            }

            StartCoroutine(SomAtrasado(ponto, principal ? 1f : 0.5f));
            if (!principal) return;

            PontoDaExplosao = ponto;
            if (incendio != null)
            {
                incendio.transform.position = ponto;
                incendio.Play();
            }

            if (clarao != null)
            {
                clarao.transform.position = ponto + Vector3.up * 6f;
                clarao.range = 260f;
            }
        }

        /// <summary>O estrondo chega depois da luz: distância até o visitante ÷ velocidade do som.</summary>
        IEnumerator SomAtrasado(Vector3 ponto, float volume)
        {
            var cam = Camera.main;
            var distancia = cam != null ? Vector3.Distance(cam.transform.position, ponto) : 300f;
            for (var t = 0f; t < distancia / VelocidadeDoSom; t += Time.deltaTime * Escala) yield return null;
            if (motor == null) yield break;
            // Toca perto do visitante, na direção da explosão: a fonte 3D a 1,5 km quase não se ouviria.
            var origem = cam != null ? cam.transform.position : ponto;
            AudioSource.PlayClipAtPoint(ProceduralAudio.Obter("explosao"), origem + (ponto - origem).normalized * 8f,
                volume * Mathf.Clamp(300f / distancia, 0.35f, 1f));
        }

        void Brilho(Vector3 ponto, float tamanho, float intensidade, Color cor)
        {
            // A camada de nuvens inteira acende por dentro, como na transmissão do lançamento.
            if (materialNuvens != null) materialNuvens.SetColor(CorBase, Color.white + cor * (intensidade * 0.45f));
            if (brilho == null) return;
            if (intensidade <= 0.01f)
            {
                brilho.Clear();
                return;
            }

            // Uma partícula só, posta à mão a cada quadro (a cor da partícula vai até 1: o
            // excesso de intensidade vira tamanho).
            var perto = Perto(ponto, out var k);
            particulaBrilho[0] = new ParticleSystem.Particle
            {
                position = perto,
                startSize = tamanho * k * Mathf.Max(1f, Mathf.Sqrt(intensidade)),
                startColor = new Color(cor.r, cor.g, cor.b, Mathf.Clamp01(intensidade)),
                startLifetime = 1f,
                remainingLifetime = 1f
            };
            if (!brilho.isPlaying) brilho.Play();
            brilho.SetParticles(particulaBrilho, 1);
        }

        /// <summary>Camada de nuvens baixas e escuras da noite de 22/12 (sementes fixas: sempre igual).</summary>
        void EmitirNuvens()
        {
            if (nuvens == null) return;
            nuvens.Clear();
            var rng = new System.Random(2212);
            var centro = foguete.parent.TransformPoint(posicaoInicial);
            for (var i = 0; i < 26; i++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f;
                var r = Mathf.Sqrt((float)rng.NextDouble()) * 650f;
                var parametros = new ParticleSystem.EmitParams
                {
                    position = centro + new Vector3(Mathf.Cos(a) * r, Mathf.Lerp(camadaNuvens.x + 30f, camadaNuvens.y, (float)rng.NextDouble()), Mathf.Sin(a) * r),
                    velocity = Vector3.zero,
                    startSize = 180f + (float)rng.NextDouble() * 140f,
                    startLifetime = 1e6f,
                    rotation = (float)rng.NextDouble() * 360f,
                    applyShapeToPosition = false
                };
                nuvens.Emit(parametros, 1);
            }
        }
    }
}
