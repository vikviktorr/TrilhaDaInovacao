using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Máquina de estados da visita (seção "Roteiro cena a cena"):
    /// abertura no hub → caminhar até a marca i → transição com o ano → cena
    /// da época → ... → encerramento → reinício para o próximo visitante.
    /// Cada época é uma cena carregada de forma aditiva sobre o Bootstrap.
    /// </summary>
    public class VisitDirector : MonoBehaviour
    {
        [Header("Referências do Bootstrap")]
        [SerializeField] FloorMarker[] marcas;
        [SerializeField] LineRenderer trilhaDeLuz;
        [SerializeField] ScreenFader fader;
        [SerializeField] SubtitleController legenda;
        [SerializeField] GuideCharacter guia;
        [SerializeField] SessionLogger registro;
        [SerializeField] DesktopRig desktop;
        [SerializeField] OperatorPanel operador;
        [SerializeField] AudioSource efeitos;
        [SerializeField, Tooltip("Falas comuns a todas as cenas (dica de caminhar).")]
        SceneScript roteiroGeral;

        [Header("Plano B (IdleHint)")]
        [SerializeField, Tooltip("Segundos sem ação até o guia repetir a instrução.")]
        float tempoDica = 6f;
        [SerializeField, Tooltip("Segundos sem ação até a cena avançar sozinha.")]
        float tempoAuto = 10f;
        [SerializeField, Tooltip("Segundos parado fora da marca até o guia lembrar de caminhar.")]
        float tempoLembrarCaminhada = 15f;
        [SerializeField, Tooltip("Espera no fim antes de recomeçar sozinho para o próximo visitante.")]
        float esperaReinicio = 12f;

        Coroutine visita;
        Coroutine cenaEmCurso;
        EraSequence sequenciaEmCurso;
        bool pularSolicitado;
        int falaAtual;
        string cenaCarregada;
        AsyncOperation cargaPendente;
        bool tour;
        bool qa;
        string pastaTour;
        int capturas;
        QARelatorio relatorio;

        public GuideCharacter Guia => guia;
        public SubtitleController Legenda => legenda;
        public ScreenFader Fader => fader;
        public SessionLogger Registro => registro;
        public AudioSource Efeitos => efeitos;
        public bool Tour => tour;

        /// <summary>Modo -qa: visita em tempo real com visitante simulado e relatório.</summary>
        public bool QA => qa;
        public DesktopRig Desktop => desktop;
        public float TempoDica => tour && !qa ? float.MaxValue : tempoDica;
        public float TempoAuto => qa ? tempoAuto + 2f : tour ? 1.2f : tempoAuto;

        /// <summary>Encurta falas no tour automático, que só verifica a montagem.</summary>
        float FatorFala => tour && !qa ? 0.35f : 1f;

        Transform Cabeca => Camera.main != null ? Camera.main.transform : transform;

        IEnumerator Start()
        {
            fader.FecharImediato(Color.black);
            yield return XRStartup.Inicializar();

            qa = XRStartup.TemArgumento("-qa");
            tour = qa || XRStartup.TemArgumento("-tour");
            if (tour)
            {
                pastaTour = XRStartup.ValorDoArgumento(qa ? "-qa" : "-tour");
                if (string.IsNullOrEmpty(pastaTour) || pastaTour.StartsWith("-"))
                    pastaTour = Path.Combine(Application.persistentDataPath, "tour");
                Directory.CreateDirectory(pastaTour);
            }

            if (qa) relatorio = gameObject.AddComponent<QARelatorio>();

            if (XRStartup.XRAtivo) operador.CarregarCalibracao();
            else desktop.Ativar();

            visita = StartCoroutine(Laco());
        }

        void OnApplicationQuit() => XRStartup.Encerrar();

        IEnumerator Laco()
        {
            while (true)
            {
                yield return RodarVisita();
                if (tour)
                {
                    if (relatorio != null) Debug.Log($"[Trilha da Inovação] Relatório de QA: {relatorio.Escrever(pastaTour)}");
                    Debug.Log($"[Trilha da Inovação] Tour concluído: {capturas} capturas em {pastaTour}");
                    yield return new WaitForSeconds(0.5f);
                    Application.Quit();
                    yield break;
                }
            }
        }

        // ------------------------------------------------------------------
        // Comandos do operador
        // ------------------------------------------------------------------
        public void Pular() => pularSolicitado = true;

        public void Reiniciar()
        {
            // Antes do Start terminar (XR ainda iniciando) não há visita a reiniciar.
            if (visita == null) return;

            // Para também as falas soltas (dica de caminhar) e o laço da visita.
            StopAllCoroutines();
            PararCena();
            registro.EncerrarVisita(false);
            guia.Falando = false;
            legenda.Ocultar();
            visita = StartCoroutine(Laco());
        }

        // ------------------------------------------------------------------
        // Visita
        // ------------------------------------------------------------------
        IEnumerator RodarVisita()
        {
            pularSolicitado = false;
            fader.FecharImediato(Color.black);
            legenda.Ocultar();
            guia.Esconder();
            foreach (var marca in marcas) marca.Definir(EstadoMarca.Apagada);
            MostrarTrilha(-1);

            // Recomeça sempre de cenas recém-carregadas (estado limpo).
            yield return DescarregarEpocas();

            registro.IniciarVisita(XRStartup.XRAtivo ? "vr" : tour ? "tour" : "desktop");

            yield return TrocarCena(LayoutTrilha.Inicio, null);
            if (!XRStartup.XRAtivo) desktop.Posicionar(LayoutTrilha.Marcas[0], LayoutTrilha.Foco(0));
            marcas[0].Definir(EstadoMarca.Visitada);
            yield return fader.Abrir(1f);
            yield return Executar(0);

            for (var i = 1; i <= LayoutTrilha.TotalEpocas; i++)
            {
                marcas[i].Definir(EstadoMarca.Proxima);
                MostrarTrilha(i);
                yield return EsperarMarca(i);
                MostrarTrilha(-1);
                marcas[i].Definir(EstadoMarca.Visitada);

                yield return TrocarCena(i, LayoutTrilha.Anos[i]);
                yield return fader.Abrir();
                yield return Executar(i);
            }

            // Encerramento: volta ao hub, com o visitante ainda na marca 6.
            yield return TrocarCena(LayoutTrilha.Inicio, null);
            yield return fader.Abrir();
            yield return Executar(LayoutTrilha.TotalEpocas + 1);
            yield return fader.Fechar(Color.black, 1.5f);
            registro.EncerrarVisita(true);

            if (tour) yield break;
            for (var t = 0f; t < esperaReinicio && !pularSolicitado; t += Time.deltaTime) yield return null;
        }

        IEnumerator EsperarMarca(int i)
        {
            var parado = 0f;
            var tourEspera = 0.6f;

            while (!marcas[i].ContemCabeca(Cabeca.position))
            {
                if (ConsumirPulo())
                {
                    registro.Gatilho("pulado");
                    yield break;
                }

                // QA: o visitante simulado caminha de verdade até a marca.
                if (qa)
                {
                    yield return desktop.Caminhar(LayoutTrilha.Marcas[i], 0.7f);
                    if (!marcas[i].ContemCabeca(Cabeca.position))
                        relatorio.Problema($"A marca {i} não detectou a cabeça do visitante ao chegar.");
                    desktop.Posicionar(LayoutTrilha.Marcas[i], LayoutTrilha.Foco(i));
                    continue;
                }

                if (tour && (tourEspera -= Time.deltaTime) <= 0f)
                    desktop.Posicionar(LayoutTrilha.Marcas[i], LayoutTrilha.Foco(i));

                parado += Time.deltaTime;
                if (parado >= tempoLembrarCaminhada && roteiroGeral != null)
                {
                    parado = 0f;
                    StartCoroutine(Dizer(roteiroGeral.Obter("dica-andar")));
                }

                yield return null;
            }
        }

        /// <summary>Fecha o véu, troca a cena aditiva e mostra o ano (o caller abre).</summary>
        IEnumerator TrocarCena(int marca, string ano)
        {
            var temAno = !string.IsNullOrEmpty(ano);
            efeitos.PlayOneShot(ProceduralAudio.Obter("whoosh"), 0.6f);
            yield return fader.Fechar(temAno ? Color.white : Color.black);
            var inicio = Time.time;
            if (temAno) fader.MostrarAno(ano);

            legenda.Ocultar();
            guia.Falando = false;
            guia.Esconder();

            var nova = LayoutTrilha.Cenas[marca];
            if (cenaCarregada != nova)
            {
                yield return DescarregarEpocas();
                var carga = SceneManager.LoadSceneAsync(nova, LoadSceneMode.Additive);
                cargaPendente = carga;
                if (carga == null)
                {
                    Debug.LogError($"[Trilha da Inovação] Cena '{nova}' fora da Scene List.");
                }
                else
                {
                    while (!carga.isDone) yield return null;
                    var cena = SceneManager.GetSceneByName(nova);
                    SceneManager.SetActiveScene(cena);
                    DynamicGI.UpdateEnvironment();
                    cenaCarregada = nova;
                }
            }

            while (temAno && Time.time - inicio < fader.TempoDoAno) yield return null;
        }

        IEnumerator DescarregarEpocas()
        {
            // Um reinício no meio de uma carga deixaria a cena chegar depois e
            // ficar sobreposta à seguinte: espera ela terminar para descarregá-la.
            while (cargaPendente != null && !cargaPendente.isDone) yield return null;
            cargaPendente = null;

            var descarregar = new List<Scene>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var cena = SceneManager.GetSceneAt(i);
                if (cena.isLoaded && cena.name != LayoutTrilha.CenaBootstrap) descarregar.Add(cena);
            }

            foreach (var cena in descarregar)
            {
                var op = SceneManager.UnloadSceneAsync(cena);
                while (op != null && !op.isDone) yield return null;
            }

            cenaCarregada = null;
        }

        IEnumerator Executar(int epoca)
        {
            var sequencia = EncontrarSequencia(epoca);
            if (sequencia == null)
            {
                Debug.LogError($"[Trilha da Inovação] Nenhuma sequência para a época {epoca}.");
                yield break;
            }

            var previsto = sequencia.Roteiro != null ? sequencia.Roteiro.tempoPrevisto : 0f;
            registro.IniciarCena(sequencia.name, previsto);
            pularSolicitado = false;

            var cenaAtiva = SceneManager.GetSceneByName(cenaCarregada);
            if (relatorio != null)
            {
                relatorio.InicioCena(sequencia.name, epoca > LayoutTrilha.TotalEpocas ? LayoutTrilha.TotalEpocas : epoca, previsto, cenaAtiva, sequencia);
                yield return Panorama(sequencia.name, epoca > LayoutTrilha.TotalEpocas ? LayoutTrilha.TotalEpocas : epoca);
            }

            var terminou = false;
            sequenciaEmCurso = sequencia;
            cenaEmCurso = StartCoroutine(Envolver(sequencia.Executar(this), () => terminou = true));

            while (!terminou)
            {
                if (ConsumirPulo())
                {
                    registro.Gatilho("pulado");
                    PararCena();
                    break;
                }

                yield return null;
            }

            cenaEmCurso = null;
            sequenciaEmCurso = null;
            registro.EncerrarCena();
            if (relatorio != null) relatorio.FimCena(cenaAtiva);
        }

        /// <summary>QA: quatro fotos (frente, direita, trás, esquerda) ao abrir a cena.</summary>
        IEnumerator Panorama(string nome, int marca)
        {
            var foco = LayoutTrilha.Foco(marca);
            var cabeca = Cabeca.position;
            var direcoes = new[] { ("frente", foco), ("direita", Vector3.Cross(Vector3.up, foco)), ("tras", -foco), ("esquerda", -Vector3.Cross(Vector3.up, foco)) };
            foreach (var (rotulo, d) in direcoes)
            {
                desktop.OlharPara(cabeca + d * 2f + Vector3.down * 0.35f);
                yield return null;
                Capturar($"{Arquivo(nome)}-{rotulo}");
                yield return null;
            }

            desktop.OlharPara(cabeca + foco * 2f + Vector3.down * 0.4f);
        }

        static string Arquivo(string nome)
        {
            var limpo = new System.Text.StringBuilder();
            foreach (var c in nome.Normalize(System.Text.NormalizationForm.FormD))
            {
                if (char.IsLetterOrDigit(c) && c < 128) limpo.Append(char.ToLowerInvariant(c));
                else if (c == ' ' && limpo.Length > 0 && limpo[limpo.Length - 1] != '-') limpo.Append('-');
            }

            return limpo.ToString().Trim('-');
        }

        static IEnumerator Envolver(IEnumerator rotina, Action fim)
        {
            yield return rotina;
            fim();
        }

        void PararCena()
        {
            if (cenaEmCurso != null) StopCoroutine(cenaEmCurso);
            if (sequenciaEmCurso != null) sequenciaEmCurso.AoInterromper();
            cenaEmCurso = null;
            sequenciaEmCurso = null;
            falaAtual++;
            guia.Falando = false;
            if (guia.Voz != null) guia.Voz.Stop();
            legenda.Ocultar();
        }

        EraSequence EncontrarSequencia(int epoca)
        {
            var cena = SceneManager.GetSceneByName(cenaCarregada ?? string.Empty);
            if (!cena.IsValid()) return null;

            foreach (var raiz in cena.GetRootGameObjects())
            {
                foreach (var seq in raiz.GetComponentsInChildren<EraSequence>(true))
                {
                    if (seq.Epoca == epoca) return seq;
                }
            }

            return null;
        }

        bool ConsumirPulo()
        {
            if (!pularSolicitado) return false;
            pularSolicitado = false;
            return true;
        }

        void MostrarTrilha(int destino)
        {
            if (trilhaDeLuz == null) return;
            if (destino <= 0)
            {
                trilhaDeLuz.enabled = false;
                return;
            }

            var de = LayoutTrilha.Marcas[destino - 1];
            var para = LayoutTrilha.Marcas[destino];
            var direcao = (para - de).normalized;
            trilhaDeLuz.positionCount = 2;
            trilhaDeLuz.SetPosition(0, de + direcao * LayoutTrilha.RaioMarca + Vector3.up * 0.006f);
            trilhaDeLuz.SetPosition(1, para - direcao * LayoutTrilha.RaioMarca + Vector3.up * 0.006f);
            trilhaDeLuz.enabled = true;
        }

        // ------------------------------------------------------------------
        // Serviços para as sequências
        // ------------------------------------------------------------------

        /// <summary>O guia fala: voz (se gravada) + legenda pelo tempo da fala.</summary>
        public IEnumerator Dizer(Fala fala)
        {
            if (fala == null || string.IsNullOrEmpty(fala.texto)) yield break;

            var minha = ++falaAtual;
            legenda.Definir(fala.texto);
            guia.Falando = true;
            if (fala.audio != null)
            {
                guia.Voz.clip = fala.audio;
                guia.Voz.Play();
            }

            var duracao = fala.Duracao * FatorFala;
            for (var t = 0f; t < duracao; t += Time.deltaTime) yield return null;

            if (minha != falaAtual) yield break;
            guia.Falando = false;
            legenda.Ocultar();
            yield return new WaitForSeconds(0.25f * FatorFala);
        }

        /// <summary>Mostra um texto curto na legenda sem o guia falar (contagem, avisos).</summary>
        public void Legendar(string texto)
        {
            falaAtual++;
            legenda.Definir(texto);
        }

        public void Som(string nome, float volume = 1f)
        {
            var clip = ProceduralAudio.Obter(nome);
            if (clip != null) efeitos.PlayOneShot(clip, volume);
        }

        /// <summary>No tour automático, vira a câmera para um ponto de interesse.</summary>
        public void Enquadrar(Vector3 alvo)
        {
            if (tour) desktop.OlharPara(alvo);
        }

        /// <summary>No tour automático, salva um screenshot com este nome.</summary>
        public void Capturar(string nome)
        {
            if (!tour) return;
            capturas++;
            ScreenCapture.CaptureScreenshot(Path.Combine(pastaTour, $"{capturas:00}_{nome}.png"));
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(FloorMarker[] m, LineRenderer trilha, ScreenFader f, SubtitleController l,
            GuideCharacter g, SessionLogger r, DesktopRig d, OperatorPanel o, AudioSource e, SceneScript geral)
        {
            marcas = m;
            trilhaDeLuz = trilha;
            fader = f;
            legenda = l;
            guia = g;
            registro = r;
            desktop = d;
            operador = o;
            efeitos = e;
            roteiroGeral = geral;
        }
#endif
    }
}
