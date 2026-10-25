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
    /// abertura no hub → botão PRÓXIMO → portal até a marca i → transição com o
    /// ano → cena da época → ... → encerramento → reinício para o próximo visitante.
    /// Cada época é uma cena carregada de forma aditiva sobre o Bootstrap.
    /// </summary>
    public class VisitDirector : MonoBehaviour
    {
        [Header("Referências do Bootstrap")]
        [Tooltip("Marcas 0 a 6 da trilha e, por último, a da saída.")]
        [SerializeField] FloorMarker[] marcas;
        [SerializeField] LineRenderer trilhaDeLuz;
        [SerializeField] ScreenFader fader;
        [SerializeField] SubtitleController legenda;
        [SerializeField] GuideCharacter guia;
        [SerializeField] SessionLogger registro;
        [SerializeField] DesktopRig desktop;
        [SerializeField] OperatorPanel operador;
        [SerializeField] AudioSource efeitos;
        [SerializeField] PortalEpoca portal;
        [SerializeField] TunelDoTempo tunel;
        [SerializeField, Tooltip("Falas comuns a todas as cenas (dica de caminhar).")]
        SceneScript roteiroGeral;

        [Header("Plano B (IdleHint)")]
        [SerializeField, Tooltip("Segundos sem ação até o guia repetir a instrução.")]
        float tempoDica = 6f;
        [SerializeField, Tooltip("Segundos sem ação até a cena avançar sozinha.")]
        float tempoAuto = 10f;
        [SerializeField, Tooltip("Segundos parado fora da marca até o guia lembrar de caminhar.")]
        float tempoLembrarCaminhada = 15f;
        [SerializeField, Tooltip("Segundos explorando a cena até o guia lembrar do botão Próximo.")]
        float tempoLembrarProximo = 30f;
        [SerializeField, Tooltip("Segundos sem tocar em Próximo até o portal abrir sozinho.")]
        float tempoAbrirPortal = 90f;
        [SerializeField, Tooltip("Espera no fim antes de recomeçar sozinho para o próximo visitante.")]
        float esperaReinicio = 12f;
        [SerializeField, Range(0f, 1f), Tooltip("Volume dos efeitos e ambientes enquanto alguém fala.")]
        float efeitosDuranteFala = 0.35f;

        Coroutine visita;
        Coroutine cenaEmCurso;
        EraSequence sequenciaEmCurso;
        bool pularSolicitado;
        int falaAtual;
        Figurante falanteAtual;
        string cenaCarregada;
        AsyncOperation cargaPendente;
        bool tour;
        bool qa;
        string pastaTour;
        int capturas;
        QARelatorio relatorio;
        PausaVisita pausa;
        int visitasQA = 1;
        int visitasFeitas;
        string anoAtual;

        public GuideCharacter Guia => guia;
        public SubtitleController Legenda => legenda;
        public ScreenFader Fader => fader;
        public TunelDoTempo Tunel => tunel;
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

            tunel.Rapido = tour && !qa;
            pausa = gameObject.AddComponent<PausaVisita>();
            if (qa)
            {
                relatorio = gameObject.AddComponent<QARelatorio>();
                // -visitas N: teste de longa duração, N visitas seguidas no mesmo processo.
                if (int.TryParse(XRStartup.ValorDoArgumento("-visitas"), out var n)) visitasQA = Mathf.Max(1, n);
            }

            // Sons do próprio Bootstrap (portal, túnel, botões) e a voz do guia.
            AbafarEfeitos(gameObject.scene);

            if (XRStartup.XRAtivo)
            {
                operador.CarregarCalibracao();
                // No VR o corpo não pode ser empurrado: a visão escurece dentro de um móvel.
                Cabeca.gameObject.AddComponent<CabecaNoCenario>().Configurar(fader);
            }
            else desktop.Ativar();

            visita = StartCoroutine(Laco());
        }

        void OnApplicationQuit() => XRStartup.Encerrar();

        void Update()
        {
            Abafador.Alvo = guia.Falando || falanteAtual != null ? efeitosDuranteFala : 1f;
        }

        /// <summary>Todo som da cena, menos as vozes, abaixa quando alguém fala.</summary>
        void AbafarEfeitos(Scene cena)
        {
            var vozes = new HashSet<AudioSource> { guia.Voz };
            foreach (var raiz in cena.GetRootGameObjects())
            foreach (var figurante in raiz.GetComponentsInChildren<Figurante>(true))
                vozes.Add(figurante.Voz);

            foreach (var raiz in cena.GetRootGameObjects())
            foreach (var fonte in raiz.GetComponentsInChildren<AudioSource>(true))
                if (!vozes.Contains(fonte) && fonte.GetComponent<GanhoVoz>() == null) Abafar(fonte);

            foreach (var voz in vozes)
                if (voz != null && voz.GetComponent<GanhoVoz>() == null) voz.gameObject.AddComponent<GanhoVoz>();
        }

        static void Abafar(AudioSource fonte)
        {
            if (fonte != null && fonte.GetComponent<Abafador>() == null) fonte.gameObject.AddComponent<Abafador>();
        }

        IEnumerator Laco()
        {
            while (true)
            {
                if (relatorio != null) relatorio.InicioVisita();
                yield return RodarVisita();
                if (relatorio != null) relatorio.FimVisita(true);
                if (tour)
                {
                    if (qa && ++visitasFeitas < visitasQA) continue;
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
            if (relatorio != null)
            {
                relatorio.FimVisita(false);
                visitasFeitas++;
            }

            guia.Falando = false;
            legenda.Ocultar();
            legenda.SeguirInclinacao = false;
            portal.Esconder();
            tunel.Esconder();
            HandPoints.PontoSimulado = null;
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
            legenda.SeguirInclinacao = false;
            guia.Esconder();
            foreach (var marca in marcas) marca.Definir(EstadoMarca.Apagada);
            MostrarTrilha(-1);

            // Recomeça sempre de cenas recém-carregadas (estado limpo).
            yield return DescarregarEpocas();

            registro.IniciarVisita(XRStartup.XRAtivo ? "vr" : tour ? "tour" : "desktop");
            anoAtual = AnoPresente;

            yield return TrocarCena(LayoutTrilha.Inicio, null);
            if (!XRStartup.XRAtivo) desktop.Posicionar(LayoutTrilha.Marcas[0], LayoutTrilha.Foco(0));
            marcas[0].Definir(EstadoMarca.Visitada);
            yield return fader.Abrir(1f);
            yield return Executar(0);

            // Depois de cada cena o visitante explora à vontade e decide quando
            // seguir: toca em PRÓXIMO e atravessa o portal até a marca seguinte.
            // Na abertura o botão Começar já fez esse papel.
            for (var i = 1; i <= LayoutTrilha.TotalEpocas; i++)
            {
                portal.Posicionar(LayoutTrilha.Marcas[i - 1], LayoutTrilha.Marcas[i], LayoutTrilha.Foco(i - 1),
                    LayoutTrilha.Anos[i], LayoutTrilha.Titulos[i], LugarDoBotao());
                marcas[i].Definir(EstadoMarca.Proxima);
                yield return Passagem(i - 1, LayoutTrilha.Marcas[i], i > 1);
                MostrarTrilha(-1);
                marcas[i].Definir(EstadoMarca.Visitada);

                yield return TrocarCena(i, LayoutTrilha.Anos[i]);
                portal.Esconder();
                yield return fader.Abrir();
                yield return Executar(i);
            }

            // Encerramento: o último portal leva de volta ao hub, junto da saída.
            portal.Posicionar(LayoutTrilha.Marcas[LayoutTrilha.TotalEpocas], LayoutTrilha.Saida,
                LayoutTrilha.Foco(LayoutTrilha.TotalEpocas), "", "De volta ao Sebraelab", LugarDoBotao());
            var saida = marcas.Length > LayoutTrilha.TotalEpocas + 1 ? marcas[LayoutTrilha.TotalEpocas + 1] : null;
            if (saida != null) saida.Definir(EstadoMarca.Proxima);
            yield return Passagem(LayoutTrilha.TotalEpocas, LayoutTrilha.Saida, true);
            if (saida != null) saida.Definir(EstadoMarca.Visitada);
            yield return TrocarCena(LayoutTrilha.Inicio, AnoPresente);
            portal.Esconder();
            yield return fader.Abrir();
            yield return Executar(LayoutTrilha.TotalEpocas + 1);
            yield return fader.Fechar(Color.black, 1.5f);
            registro.EncerrarVisita(true);

            if (tour) yield break;
            for (var t = 0f; t < esperaReinicio && !pularSolicitado; t += Time.deltaTime) yield return null;
        }

        /// <summary>
        /// Botão PRÓXIMO (se <paramref name="comBotao"/>) e travessia do portal que
        /// leva da marca <paramref name="origem"/> até <paramref name="destino"/>.
        /// Plano B: o guia lembra do botão e, sem toque, o portal abre sozinho.
        /// </summary>
        IEnumerator Passagem(int origem, Vector3 destino, bool comBotao)
        {
            if (comBotao)
            {
                var tocou = false;
                Action marcar = () => tocou = true;
                portal.Botao.Tocado += marcar;
                portal.MostrarBotao();
                if (tour)
                {
                    Enquadrar(portal.Botao.transform.position + Vector3.up * 0.1f);
                    yield return null;
                    Capturar($"proximo-{origem}");
                }

                if (origem == 1) StartCoroutine(Dizer(roteiroGeral.Obter("explorar")));
                var simulacao = qa ? StartCoroutine(SimularToque(portal.Botao, 3f)) : null;

                var t = 0f;
                var lembrou = false;
                while (!tocou && !pularSolicitado)
                {
                    t += Time.deltaTime;
                    if (tour && !qa && t > 0.5f) portal.Botao.Tocar();
                    if (!lembrou && t >= tempoLembrarProximo)
                    {
                        lembrou = true;
                        StartCoroutine(Dizer(roteiroGeral.Obter("dica-proximo")));
                    }

                    if (t >= tempoAbrirPortal) break;
                    yield return null;
                }

                if (simulacao != null) StopCoroutine(simulacao);
                HandPoints.PontoSimulado = null;
                portal.Botao.Tocado -= marcar;
                var gatilho = tocou ? "proximo" : pularSolicitado ? "pulado" : "auto";
                registro.Gatilho(gatilho);
                if (relatorio != null) relatorio.Gatilho(tocou ? $"próximo ({t:0.0} s)" : gatilho);
                if (ConsumirPulo()) yield break;
            }

            efeitos.PlayOneShot(ProceduralAudio.Obter("portal"), 0.7f);
            yield return portal.Abrir();
            Enquadrar(portal.Centro + Vector3.up * 1.1f);
            Capturar($"portal-{origem}");
            if (origem <= 1) StartCoroutine(Dizer(roteiroGeral.Obter("dica-portal")));

            var parado = 0f;
            var tourEspera = 0.6f;
            while (!portal.Atravessou())
            {
                if (ConsumirPulo())
                {
                    registro.Gatilho("pulado");
                    yield break;
                }

                // QA: o visitante simulado caminha de verdade, através do portal.
                if (qa)
                {
                    yield return desktop.Caminhar(destino, 0.7f);
                    if (!portal.Atravessou())
                        relatorio.Problema($"O portal depois da marca {origem} não detectou a travessia do visitante.");
                    desktop.Posicionar(destino, DirecaoDepois(destino));
                    yield break;
                }

                if (tour && (tourEspera -= Time.deltaTime) <= 0f)
                    desktop.Posicionar(destino, DirecaoDepois(destino));

                parado += Time.deltaTime;
                if (parado >= tempoLembrarCaminhada)
                {
                    parado = 0f;
                    StartCoroutine(Dizer(roteiroGeral.Obter("dica-portal")));
                }

                yield return null;
            }
        }

        /// <summary>Lugar do botão PRÓXIMO reservado pela cena carregada (ou null).</summary>
        LugarDoProximo LugarDoBotao()
        {
            var cena = SceneManager.GetSceneByName(cenaCarregada ?? string.Empty);
            if (!cena.IsValid()) return null;
            foreach (var raiz in cena.GetRootGameObjects())
            {
                var lugar = raiz.GetComponentInChildren<LugarDoProximo>(true);
                if (lugar != null) return lugar;
            }

            return null;
        }

        /// <summary>Para onde olhar ao chegar: o foco da marca, ou da saída no fim.</summary>
        static Vector3 DirecaoDepois(Vector3 ponto)
        {
            for (var i = 0; i < LayoutTrilha.Marcas.Length; i++)
                if ((LayoutTrilha.Marcas[i] - ponto).sqrMagnitude < 0.01f) return LayoutTrilha.Foco(i);
            return LayoutTrilha.Foco(LayoutTrilha.TotalEpocas);
        }

        /// <summary>
        /// QA: um dedo virtual sai da altura do peito e vai até o alvo, como um
        /// visitante faria. Testa a detecção do PokeTrigger, não um atalho.
        /// </summary>
        public IEnumerator SimularToque(PokeTrigger alvo, float espera = 1.5f)
        {
            yield return new WaitForSeconds(espera);
            var cabeca = Cabeca;
            var frente = cabeca.forward;
            frente.y = 0f;
            var inicio = cabeca.position + Vector3.down * 0.45f + frente.normalized * 0.2f;
            var fim = alvo.GetComponent<Collider>().bounds.center;
            Enquadrar(fim);
            for (var t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                HandPoints.PontoSimulado = Vector3.Lerp(inicio, fim, Mathf.SmoothStep(0f, 1f, t / 1.2f));
                yield return null;
            }

            HandPoints.PontoSimulado = fim;
        }

        /// <summary>O ano da volta ao Sebraelab: a visita começa e termina no presente.</summary>
        static string AnoPresente => DateTime.Now.Year.ToString();

        /// <summary>
        /// Troca a cena aditiva. Com ano, a troca acontece dentro do túnel do tempo;
        /// sem ano (início da visita), atrás do véu preto. O caller abre o véu.
        /// </summary>
        IEnumerator TrocarCena(int marca, string ano)
        {
            var temAno = !string.IsNullOrEmpty(ano);
            if (temAno) yield return EntrarNoTunel(ano);
            else yield return fader.Fechar(Color.black);

            legenda.Ocultar();
            legenda.SeguirInclinacao = false;
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
                    AbafarEfeitos(cena);
                    // O cenário novo pode ter um móvel bem onde o visitante está.
                    if (!XRStartup.XRAtivo) desktop.Desentalar();
                    cenaCarregada = nova;
                }
            }

            if (temAno) yield return SairDoTunel();
        }

        /// <summary>Clarão e túnel do tempo, do ano atual da visita até <paramref name="ano"/>.</summary>
        public IEnumerator EntrarNoTunel(string ano)
        {
            efeitos.PlayOneShot(ProceduralAudio.Obter("whoosh"), 0.6f);
            var partida = anoAtual;
            anoAtual = ano;
            yield return tunel.Entrar(partida, ano);
        }

        /// <summary>Espera o ano travar e sai do túnel num clarão branco (o caller abre o véu).</summary>
        public IEnumerator SairDoTunel()
        {
            while (tunel.Ativo && !tunel.Chegou) yield return null;
            Capturar($"tunel-{anoAtual}");
            yield return tunel.Sair();
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
                // No óculos a câmera segue a cabeça: sem panorama.
                if (!XRStartup.XRAtivo) yield return Panorama(sequencia.name, epoca > LayoutTrilha.TotalEpocas ? LayoutTrilha.TotalEpocas : epoca);
            }

            // QA: a pausa ao tirar o óculos em toda visita (cena 1986) e, no teste
            // longo, o óculos largado na 2ª visita (cena 1994). Rodam no relatório,
            // que sobrevive ao Reiniciar.
            if (relatorio != null && epoca == 1) relatorio.StartCoroutine(relatorio.TestarPausa(pausa, sequencia.name));
            if (relatorio != null && epoca == 2 && visitasQA > 1 && visitasFeitas == 1)
                relatorio.StartCoroutine(relatorio.TestarAbandono(pausa, sequencia.name));

            var terminou = false;
            sequenciaEmCurso = sequencia;
            cenaEmCurso = StartCoroutine(Envolver(sequencia.Executar(this), () => terminou = true));

            while (!terminou)
            {
                if (ConsumirPulo())
                {
                    registro.Gatilho("pulado");
                    PararCena();
                    // Pulo no meio de uma viagem dentro da cena (2023 → 2025).
                    if (tunel.Ativo)
                    {
                        tunel.Esconder();
                        yield return fader.Abrir();
                    }

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
            if (falanteAtual != null)
            {
                falanteAtual.Falando = false;
                if (falanteAtual.Voz != null) falanteAtual.Voz.Stop();
                falanteAtual = null;
            }
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

        /// <summary>
        /// O guia (ou, com <paramref name="quem"/>, um figurante) fala: voz (se
        /// gravada) + legenda pelo tempo da fala. A legenda de um figurante leva o nome dele.
        /// </summary>
        public IEnumerator Dizer(Fala fala, Figurante quem = null)
        {
            if (fala == null || string.IsNullOrEmpty(fala.texto)) yield break;

            var minha = ++falaAtual;
            var fonte = quem != null ? quem.Voz : guia.Voz;
            legenda.Definir(quem != null && !string.IsNullOrEmpty(quem.papel) ? $"<b>{quem.papel}:</b> {fala.texto}" : fala.texto);
            if (quem != null)
            {
                falanteAtual = quem;
                quem.Falando = true;
                guia.Falando = false;
            }
            else guia.Falando = true;

            if (fala.audio != null && fonte != null)
            {
                fonte.clip = fala.audio;
                fonte.Play();
            }

            var duracao = fala.Duracao * FatorFala;
            for (var t = 0f; t < duracao; t += Time.deltaTime) yield return null;

            if (quem != null)
            {
                quem.Falando = false;
                if (falanteAtual == quem) falanteAtual = null;
            }

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
            if (!tour || XRStartup.XRAtivo) return;
            capturas++;
            ScreenCapture.CaptureScreenshot(Path.Combine(pastaTour, $"{capturas:00}_{nome}.png"));
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(FloorMarker[] m, LineRenderer trilha, ScreenFader f, SubtitleController l,
            GuideCharacter g, SessionLogger r, DesktopRig d, OperatorPanel o, AudioSource e, SceneScript geral, PortalEpoca p,
            TunelDoTempo t)
        {
            portal = p;
            tunel = t;
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
