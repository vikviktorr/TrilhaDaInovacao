using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Viagem entre as épocas: um clarão branco (De Volta para o Futuro), o
    /// visitante dentro de um vórtice em espiral (o túnel do tempo de Doctor Who
    /// e do Portal do Tempo de Chrono Trigger), riscos de luz passando como no
    /// salto para o hiperespaço e anéis que vêm ao encontro dele. À frente, o ano
    /// em algarismos 3D gira como o painel de uma máquina do tempo, do ano de
    /// partida até o destino, e trava com um pulso. Outro clarão e a época nova.
    ///
    /// Em VR o túnel fica parado no espaço do rig (não preso à cabeça): o
    /// visitante pode olhar em volta e dar um passo sem o mundo arrastar junto.
    /// Enquanto ele dura, a câmera só desenha a camada do túnel, então a cena
    /// seguinte carrega por trás sem aparecer.
    /// </summary>
    public class TunelDoTempo : MonoBehaviour
    {
        [SerializeField, Tooltip("Tudo o que aparece na viagem, na camada do túnel.")]
        Transform conteudo;
        [SerializeField] ScreenFader fader;
        [SerializeField] MeshFilter paredes;
        [SerializeField] MeshFilter brilho;
        [SerializeField] ParticleSystem riscos;
        [SerializeField] LineRenderer[] aneis = { };
        [SerializeField, Tooltip("Pai dos algarismos: fica de frente para o visitante.")]
        Transform ano;
        [SerializeField] MeshFilter[] casas = { };
        [SerializeField, Tooltip("Malhas dos algarismos 0 a 9 (Ferramentas/blender/digitos_3d.py).")]
        Mesh[] algarismos = new Mesh[10];
        [SerializeField] Renderer halo;
        [SerializeField] AudioSource som;

        [Header("Viagem")]
        [SerializeField, Min(0.5f), Tooltip("Tempo dentro do túnel (s), sem contar os clarões.")]
        float duracao = 3.6f;
        [SerializeField] float raio = 2.6f;
        [SerializeField] float comprimento = 40f;
        [SerializeField, Tooltip("Cor do vórtice indo para o passado.")]
        Color corPassado = new Color(1f, 0.55f, 0.2f);
        [SerializeField, Tooltip("Cor do vórtice indo para o futuro.")]
        Color corFuturo = new Color(0.3f, 0.6f, 1f);
        [SerializeField] Color corBrilho = new Color(0.75f, 0.35f, 1f);

        static readonly int MapaBase = Shader.PropertyToID("_BaseMap");
        static readonly int CorBase = Shader.PropertyToID("_BaseColor");

        Material matParedes, matBrilho;
        Camera cameraAtiva;
        int mascaraOriginal;
        CameraClearFlags fundoOriginal;
        Color corFundoOriginal;
        Scene cenaSemNeblina;
        float tempo;
        int de, para, mostrado = -1;
        float sentido = 1f;
        Vector3 escalaHalo;
        Color[] corAneis = { };

        /// <summary>Tour automático: a viagem fica curta (só confere a montagem).</summary>
        public bool Rapido { get; set; }
        public bool Ativo => conteudo != null && conteudo.gameObject.activeSelf;
        float Duracao => Rapido ? 1.2f : duracao;

        /// <summary>O ano já travou no destino.</summary>
        public bool Chegou => tempo >= Duracao * 0.8f + 0.25f;

        void Awake()
        {
            var textura = TexturaEspiral();
            paredes.sharedMesh = Tubo("Paredes do túnel", raio, 12, false);
            brilho.sharedMesh = Tubo("Brilho do túnel", raio * 0.82f, 6, true);
            matParedes = paredes.GetComponent<Renderer>().material;
            matBrilho = brilho.GetComponent<Renderer>().material;
            matParedes.SetTexture(MapaBase, textura);
            matBrilho.SetTexture(MapaBase, textura);
            matBrilho.SetColor(CorBase, corBrilho);
            if (halo != null) escalaHalo = halo.transform.localScale;
            corAneis = System.Array.ConvertAll(aneis, a => a.startColor);
            conteudo.gameObject.SetActive(false);
        }

        /// <summary>
        /// Clarão branco e o visitante já dentro do túnel, com o ano partindo de
        /// <paramref name="anoDe"/> (vazio: começa no próprio destino).
        /// O caller troca a cena enquanto isso e depois chama <see cref="Sair"/>.
        /// </summary>
        public IEnumerator Entrar(string anoDe, string anoPara)
        {
            int.TryParse(anoPara, out para);
            if (!int.TryParse(anoDe, out de)) de = para;
            sentido = para < de ? -1f : 1f;

            yield return fader.Fechar(Color.white, 0.25f);

            cameraAtiva = Camera.main;
            if (cameraAtiva != null)
            {
                mascaraOriginal = cameraAtiva.cullingMask;
                fundoOriginal = cameraAtiva.clearFlags;
                corFundoOriginal = cameraAtiva.backgroundColor;
                cameraAtiva.cullingMask = 1 << LayoutTrilha.CamadaTunel;
                cameraAtiva.clearFlags = CameraClearFlags.SolidColor;
                cameraAtiva.backgroundColor = new Color(0.01f, 0.01f, 0.03f);

                // Parado no espaço do rig, na altura dos olhos e virado para onde ele olha.
                var olhos = cameraAtiva.transform;
                conteudo.SetParent(olhos.parent, false);
                conteudo.localPosition = olhos.localPosition;
                var frente = Vector3.ProjectOnPlane(olhos.localRotation * Vector3.forward, Vector3.up);
                conteudo.localRotation = frente.sqrMagnitude > 0.001f ? Quaternion.LookRotation(frente) : Quaternion.identity;
            }

            matParedes.SetColor(CorBase, sentido < 0f ? corPassado : corFuturo);
            foreach (var anel in aneis) anel.transform.localPosition = Vector3.forward * Random.Range(2f, comprimento * 0.7f);
            mostrado = -1;
            MostrarAno(de);
            tempo = 0f;
            conteudo.gameObject.SetActive(true);
            riscos.Simulate(1.5f, true, true);
            riscos.Play();
            if (som != null)
            {
                som.clip = ProceduralAudio.Obter("tunel");
                som.volume = 0.8f;
                som.Play();
            }

            yield return fader.Abrir(0.35f);
        }

        /// <summary>Espera o ano chegar ao destino, trava e fecha num clarão branco (o caller abre).</summary>
        public IEnumerator Sair()
        {
            while (Ativo && tempo < Duracao + (Rapido ? 0.2f : 0.7f)) yield return null;
            yield return fader.Fechar(Color.white, 0.3f);
            Esconder();
        }

        /// <summary>Some com o túnel e devolve a câmera (também no reinício e no pulo).</summary>
        public void Esconder()
        {
            if (!Ativo) return;
            conteudo.gameObject.SetActive(false);
            conteudo.SetParent(transform, false);
            riscos.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (som != null) som.Stop();
            if (cameraAtiva != null)
            {
                cameraAtiva.cullingMask = mascaraOriginal;
                cameraAtiva.clearFlags = fundoOriginal;
                cameraAtiva.backgroundColor = corFundoOriginal;
            }

            if (cenaSemNeblina.IsValid() && cenaSemNeblina == SceneManager.GetActiveScene()) RenderSettings.fog = true;
            cenaSemNeblina = default;
        }

        void LateUpdate()
        {
            if (!Ativo) return;
            tempo += Time.deltaTime;
            var k = Mathf.Clamp01(tempo / Duracao);

            // A neblina de uma época apagaria o túnel. A cena nova chega no meio
            // da viagem com a dela, por isso a checagem é a cada quadro.
            if (RenderSettings.fog)
            {
                RenderSettings.fog = false;
                cenaSemNeblina = SceneManager.GetActiveScene();
            }

            // Arranca rápido e freia ao chegar, como o contador do ano.
            var velocidade = Mathf.Lerp(1f, 0.25f, k * k);
            var dt = Time.deltaTime * velocidade;
            Rolar(matParedes, new Vector2(0.12f * sentido, 1.6f) * dt);
            Rolar(matBrilho, new Vector2(-0.3f * sentido, 2.6f) * dt);
            var simulacao = riscos.main;
            simulacao.simulationSpeed = velocidade;

            for (var i = 0; i < aneis.Length; i++)
            {
                var anel = aneis[i];
                var z = anel.transform.localPosition.z - 22f * dt;
                if (z < -1f) z += comprimento * 0.75f;
                anel.transform.localPosition = Vector3.forward * z;
                // Acende ao se aproximar e some antes de passar pelo rosto.
                var luz = Mathf.Clamp01((comprimento * 0.6f - z) / 12f) * Mathf.Clamp01((z - 0.5f) / 2.5f);
                var cor = corAneis[i] * luz;
                cor.a = luz;
                anel.startColor = anel.endColor = cor;
            }

            // O ano vem de longe girando os algarismos e para a 3 m, levemente abaixo dos olhos.
            var contagem = Mathf.Clamp01(tempo / (Duracao * 0.8f));
            var suave = 1f - Mathf.Pow(1f - contagem, 3f);
            MostrarAno(Mathf.RoundToInt(Mathf.Lerp(de, para, suave)));
            var chegada = Mathf.Max(0f, tempo - Duracao * 0.8f);
            var pulso = 1f + 0.18f * Mathf.Exp(-chegada * 7f) * (contagem >= 1f ? 1f : 0f);
            ano.localPosition = new Vector3(0f, -0.12f, Mathf.Lerp(9f, 3f, suave));
            // Balança forte enquanto conta e continua de leve depois, para as laterais 3D aparecerem.
            ano.localRotation = Quaternion.Euler(-6f, Mathf.Lerp(18f, 7f, contagem) * Mathf.Sin(tempo * 1.7f), 0f);
            ano.localScale = Vector3.one * (0.5f * pulso);
            if (halo != null) halo.transform.localScale = escalaHalo * (0.8f + 0.5f * pulso * suave);
        }

        static void Rolar(Material m, Vector2 passo)
        {
            var o = m.GetTextureOffset(MapaBase) + passo;
            m.SetTextureOffset(MapaBase, new Vector2(Mathf.Repeat(o.x, 1f), Mathf.Repeat(o.y, 1f)));
        }

        void MostrarAno(int valor)
        {
            if (valor == mostrado) return;
            if (mostrado >= 0 && valor == para && som != null) som.PlayOneShot(ProceduralAudio.Obter("pulso-grave"), 0.9f);
            mostrado = valor;
            var texto = Mathf.Abs(valor).ToString("0000");
            for (var i = 0; i < casas.Length; i++)
            {
                var digito = i < texto.Length ? texto[i] - '0' : 0;
                casas[i].sharedMesh = algarismos[Mathf.Clamp(digito, 0, 9)];
            }
        }

        /// <summary>
        /// Tubo visto por dentro, ao longo de +z (continua 10 m atrás do visitante). A textura dá voltas e é torcida
        /// ao longo do comprimento: rolar o offset vira uma espiral que corre.
        /// Com <paramref name="esmaecer"/>, a cor do vértice apaga nas pontas
        /// (para o material aditivo, que usa a cor do vértice).
        /// </summary>
        Mesh Tubo(string nome, float r, int repeticoes, bool esmaecer)
        {
            const int lados = 24, aneisMalha = 24;
            const float voltas = 3f, torcao = 1.5f;
            var vertices = new Vector3[(lados + 1) * (aneisMalha + 1)];
            var uvs = new Vector2[vertices.Length];
            var cores = new Color[vertices.Length];
            for (var j = 0; j <= aneisMalha; j++)
            {
                var v = j / (float)aneisMalha;
                var z = Mathf.Lerp(-10f, comprimento, v);
                // Afunila ao longe: o fundo parece mais distante do que é.
                var rz = r * Mathf.Lerp(1f, 0.55f, v);
                var fade = esmaecer ? Mathf.Clamp01((z + 1f) / 3f) * (1f - v) : 1f;
                for (var i = 0; i <= lados; i++)
                {
                    var a = i / (float)lados * Mathf.PI * 2f;
                    var n = j * (lados + 1) + i;
                    vertices[n] = new Vector3(Mathf.Cos(a) * rz, Mathf.Sin(a) * rz, z);
                    uvs[n] = new Vector2(i / (float)lados * voltas + v * torcao, v * repeticoes);
                    cores[n] = new Color(fade, fade, fade, fade);
                }
            }

            var triangulos = new int[lados * aneisMalha * 6];
            var t = 0;
            for (var j = 0; j < aneisMalha; j++)
            for (var i = 0; i < lados; i++)
            {
                var a = j * (lados + 1) + i;
                var b = a + lados + 1;
                // Ordem invertida: as faces apontam para dentro.
                triangulos[t++] = a; triangulos[t++] = a + 1; triangulos[t++] = b;
                triangulos[t++] = a + 1; triangulos[t++] = b + 1; triangulos[t++] = b;
            }

            var malha = new Mesh { name = nome, vertices = vertices, uv = uvs, colors = cores, triangles = triangulos };
            malha.RecalculateBounds();
            return malha;
        }

        /// <summary>Faixas em espiral com riscos finos, sem emenda nos dois sentidos.</summary>
        static Texture2D TexturaEspiral()
        {
            const int largura = 128, altura = 128;
            var textura = new Texture2D(largura, altura, TextureFormat.RGBA32, true) { name = "Espiral do túnel", wrapMode = TextureWrapMode.Repeat };
            var pixels = new Color32[largura * altura];
            var rng = new System.Random(1986);
            var riscoColuna = new float[largura];
            for (var x = 0; x < largura; x++) riscoColuna[x] = (float)rng.NextDouble();
            for (var y = 0; y < altura; y++)
            for (var x = 0; x < largura; x++)
            {
                float u = x / (float)largura, v = y / (float)altura;
                var onda = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (2f * u + v + 0.12f * Mathf.Sin(2f * Mathf.PI * 3f * v)));
                var faixa = Mathf.Pow(onda, 3f);
                var risco = Mathf.Pow(riscoColuna[x], 6f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (2f * v + riscoColuna[x])));
                var b = (byte)(255f * Mathf.Clamp01(0.05f + 0.75f * faixa + 0.9f * risco));
                pixels[y * largura + x] = new Color32(b, b, b, b);
            }

            textura.SetPixels32(pixels);
            textura.Apply(true, true);
            return textura;
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(Transform c, ScreenFader f, MeshFilter p, MeshFilter b, ParticleSystem r,
            LineRenderer[] a, Transform anoT, MeshFilter[] cs, Mesh[] alg, Renderer h, AudioSource s)
        {
            conteudo = c;
            fader = f;
            paredes = p;
            brilho = b;
            riscos = r;
            aneis = a;
            ano = anoT;
            casas = cs;
            algarismos = alg;
            halo = h;
            som = s;
        }
#endif
    }
}
