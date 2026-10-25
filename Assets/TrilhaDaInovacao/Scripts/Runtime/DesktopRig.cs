using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Modo sem headset, para testar a visita no PC e apresentar no
    /// computador: WASD anda, botão direito (ou Tab para travar o cursor) olha,
    /// clique esquerdo toca o objeto sob a mira. A altura dos olhos é fixa.
    /// − e = ajustam o volume das vozes; [ e ] o dos efeitos (<see cref="ConfigAudio"/>).
    /// O corpo é uma cápsula que desliza nas paredes e móveis do cenário
    /// (camada <see cref="LayoutTrilha.CamadaCenario"/>) em vez de atravessá-los.
    /// Depois de cada teleporte (início, troca de cena, tour), <see cref="Desentalar"/>
    /// tira o corpo de dentro de um móvel, se ele tiver nascido encostado em um.
    /// </summary>
    public class DesktopRig : MonoBehaviour
    {
        [SerializeField] Transform cameraRig;
        [SerializeField] Transform cameraTransform;
        [SerializeField, Min(0.1f)] float alturaOlhos = 1.6f;
        [SerializeField, Min(0.1f)] float velocidade = 1.4f;
        [SerializeField, Min(0.01f)] float sensibilidade = 0.12f;
        [SerializeField, Min(0.05f)] float raioCorpo = 0.25f;

        const float Pele = 0.02f;
        const float AlturaJoelho = 0.3f;
        const int MascaraCenario = 1 << LayoutTrilha.CamadaCenario;

        float yaw, pitch;
        bool cursorTravado;
        bool mostrarAjuda = true;
        GUIStyle estilo;
        float avisoVolumeAte;

        public bool Ativo { get; private set; }

        public void Ativar()
        {
            Ativo = true;
            var driver = cameraTransform.GetComponent<TrackedPoseDriver>();
            if (driver != null) driver.enabled = false;
            cameraTransform.localPosition = Vector3.up * alturaOlhos;
            cameraTransform.localRotation = Quaternion.identity;
            Posicionar(LayoutTrilha.Marcas[LayoutTrilha.Inicio], LayoutTrilha.Foco(LayoutTrilha.Inicio));
        }

        /// <summary>Coloca o visitante numa marca olhando numa direção (usado no tour).</summary>
        public void Posicionar(Vector3 pontoNoChao, Vector3 olhar, float inclinacao = 0f)
        {
            var origem = cameraRig.position;
            var cabecaLocal = cameraTransform.position - origem;
            cabecaLocal.y = 0f;
            cameraRig.position = new Vector3(pontoNoChao.x, origem.y, pontoNoChao.z) - cabecaLocal;
            yaw = Quaternion.LookRotation(olhar).eulerAngles.y - cameraRig.eulerAngles.y;
            pitch = inclinacao;
            AplicarRotacao();
            Desentalar();
        }

        /// <summary>
        /// Se o corpo está dentro de um móvel ou parede, leva-o ao ponto livre mais
        /// próximo (anéis de 10 em 10 cm até 2 m, 16 direções). Devolve true se moveu.
        /// </summary>
        public bool Desentalar()
        {
            if (!Ativo) return false;
            Physics.SyncTransforms();
            var cabeca = cameraTransform.position;
            if (!Ocupado(cabeca)) return false;

            for (var raio = 0.1f; raio <= 2.001f; raio += 0.1f)
            for (var i = 0; i < 16; i++)
            {
                var a = i * Mathf.PI / 8f;
                var desvio = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * raio;
                // Só aceita um ponto alcançável a pé: nada de atravessar a parede para o outro lado.
                if (Ocupado(cabeca + desvio) || Bloqueado(cabeca, desvio)) continue;
                cameraRig.position += desvio;
                var aviso = $"Visitante nasceu dentro do cenário em ({cabeca.x:0.00}, {cabeca.z:0.00}); movido {raio * 100f:0} cm.";
                Debug.Log($"[Trilha da Inovação] {aviso}");
                if (QARelatorio.Instancia != null) QARelatorio.Instancia.Problema(aviso);
                return true;
            }

            Debug.LogWarning($"[Trilha da Inovação] Nenhum ponto livre perto de ({cabeca.x:0.00}, {cabeca.z:0.00}).");
            return false;
        }

        void Capsula(Vector3 cabeca, out Vector3 baixo, out Vector3 alto)
        {
            var chao = cameraRig.position.y;
            baixo = new Vector3(cabeca.x, chao + AlturaJoelho + raioCorpo, cabeca.z);
            alto = new Vector3(cabeca.x, Mathf.Max(baixo.y, cabeca.y - raioCorpo), cabeca.z);
        }

        bool Ocupado(Vector3 cabeca)
        {
            Capsula(cabeca, out var baixo, out var alto);
            return Physics.CheckCapsule(baixo, alto, raioCorpo, MascaraCenario, QueryTriggerInteraction.Ignore);
        }

        bool Bloqueado(Vector3 cabeca, Vector3 desvio)
        {
            // Raio fino na altura do peito, só contra faces da frente: sair do móvel em que
            // o corpo nasceu (visto por dentro) vale; atravessar outra parede, não.
            var peito = new Vector3(cabeca.x, cameraRig.position.y + 1.1f, cabeca.z);
            var antes = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = false;
            var bateu = Physics.Raycast(peito, desvio.normalized, desvio.magnitude, MascaraCenario, QueryTriggerInteraction.Ignore);
            Physics.queriesHitBackfaces = antes;
            return bateu;
        }

        /// <summary>QA: anda até um ponto do chão na velocidade de um visitante.</summary>
        public System.Collections.IEnumerator Caminhar(Vector3 destino, float metrosPorSegundo)
        {
            while (true)
            {
                var cabeca = cameraTransform.position;
                var falta = new Vector3(destino.x - cabeca.x, 0f, destino.z - cabeca.z);
                if (falta.magnitude < 0.05f) yield break;
                var passo = Mathf.Min(falta.magnitude, metrosPorSegundo * Time.deltaTime);
                cameraRig.position += falta.normalized * passo;
                OlharPara(cameraTransform.position + falta.normalized * 2f + Vector3.down * 0.5f);
                yield return null;
            }
        }

        /// <summary>Vira a câmera para um ponto do mundo, sem mover o visitante.</summary>
        public void OlharPara(Vector3 alvo)
        {
            var direcao = alvo - cameraTransform.position;
            if (direcao.sqrMagnitude < 0.0001f) return;
            var rotacao = Quaternion.LookRotation(direcao.normalized).eulerAngles;
            yaw = rotacao.y - cameraRig.eulerAngles.y;
            pitch = rotacao.x > 180f ? rotacao.x - 360f : rotacao.x;
            AplicarRotacao();
        }

        void Update()
        {
            if (!Ativo) return;
            var teclado = Keyboard.current;
            var mouse = Mouse.current;
            if (teclado == null || mouse == null) return;

            if (teclado.tabKey.wasPressedThisFrame) TravarCursor(!cursorTravado);
            if (teclado.escapeKey.wasPressedThisFrame) TravarCursor(false);
            if (teclado.hKey.wasPressedThisFrame) mostrarAjuda = !mostrarAjuda;
            AjustarVolume(teclado);

            if (cursorTravado || mouse.rightButton.isPressed)
            {
                var delta = mouse.delta.ReadValue() * sensibilidade;
                yaw += delta.x;
                pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
                AplicarRotacao();
            }

            var entrada = Vector2.zero;
            if (teclado.wKey.isPressed || teclado.upArrowKey.isPressed) entrada.y += 1f;
            if (teclado.sKey.isPressed || teclado.downArrowKey.isPressed) entrada.y -= 1f;
            if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) entrada.x += 1f;
            if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed) entrada.x -= 1f;

            if (entrada.sqrMagnitude > 0f)
            {
                var frente = cameraTransform.forward;
                frente.y = 0f;
                frente.Normalize();
                var direita = new Vector3(frente.z, 0f, -frente.x);
                Andar((frente * entrada.y + direita * entrada.x).normalized * velocidade * Time.deltaTime);
            }

            if (mouse.leftButton.wasPressedThisFrame) Tocar(mouse);
        }

        /// <summary>Move o corpo; ao bater no cenário, desliza ao longo da superfície.</summary>
        void Andar(Vector3 deslocamento)
        {
            const int mascara = MascaraCenario;
            for (var i = 0; i < 3 && deslocamento.sqrMagnitude > 1e-8f; i++)
            {
                var cabeca = cameraTransform.position;
                var chao = cameraRig.position.y;
                var baixo = new Vector3(cabeca.x, chao + AlturaJoelho + raioCorpo, cabeca.z);
                var alto = new Vector3(cabeca.x, Mathf.Max(baixo.y, cabeca.y - raioCorpo), cabeca.z);
                var distancia = deslocamento.magnitude;
                var direcao = deslocamento / distancia;

                if (!Physics.CapsuleCast(baixo, alto, raioCorpo, direcao, out var acerto, distancia + Pele, mascara,
                        QueryTriggerInteraction.Ignore))
                {
                    cameraRig.position += deslocamento;
                    return;
                }

                var livre = Mathf.Max(0f, acerto.distance - Pele);
                cameraRig.position += direcao * livre;

                var normal = acerto.normal;
                normal.y = 0f;
                if (normal.sqrMagnitude < 1e-6f) return;
                normal.Normalize();
                var resto = direcao * (distancia - livre);
                deslocamento = resto - Vector3.Dot(resto, normal) * normal;
            }
        }

        void AjustarVolume(Keyboard teclado)
        {
            var vozes = (teclado.equalsKey.wasPressedThisFrame || teclado.numpadPlusKey.wasPressedThisFrame ? 1 : 0)
                        - (teclado.minusKey.wasPressedThisFrame || teclado.numpadMinusKey.wasPressedThisFrame ? 1 : 0);
            var efeitos = (teclado.rightBracketKey.wasPressedThisFrame ? 1 : 0) - (teclado.leftBracketKey.wasPressedThisFrame ? 1 : 0);
            if (vozes != 0) ConfigAudio.AjustarVozes(vozes);
            if (efeitos != 0) ConfigAudio.AjustarEfeitos(efeitos);
            if (vozes != 0 || efeitos != 0) avisoVolumeAte = Time.unscaledTime + 2f;
        }

        void Tocar(Mouse mouse)
        {
            var cam = cameraTransform.GetComponent<Camera>();
            var raio = cursorTravado
                ? new Ray(cameraTransform.position, cameraTransform.forward)
                : cam.ScreenPointToRay(mouse.position.ReadValue());

            const int semCenario = ~(1 << LayoutTrilha.CamadaCenario);
            if (!Physics.Raycast(raio, out var acerto, 6f, semCenario, QueryTriggerInteraction.Collide)) return;
            var alvo = acerto.collider.GetComponentInParent<PokeTrigger>();
            if (alvo != null) alvo.Tocar();
        }

        void AplicarRotacao() => cameraTransform.localRotation = Quaternion.Euler(pitch, yaw, 0f);

        void TravarCursor(bool travar)
        {
            cursorTravado = travar;
            Cursor.lockState = travar ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !travar;
        }

        void OnGUI()
        {
            if (!Ativo || XRStartup.TemArgumento("-tour") || XRStartup.TemArgumento("-qa")) return;

            estilo ??= new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };

            if (cursorTravado)
            {
                var c = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                GUI.Label(new Rect(c.x - 6, c.y - 12, 20, 24), "<b>+</b>", estilo);
            }

            if (Time.unscaledTime < avisoVolumeAte)
                GUI.Label(new Rect(16, 40, 600, 30),
                    $"<b>Volume</b>  vozes {Mathf.RoundToInt(ConfigAudio.Vozes * 100f)}% · efeitos {Mathf.RoundToInt(ConfigAudio.Efeitos * 100f)}%", estilo);

            if (!mostrarAjuda) return;
            GUI.Label(new Rect(16, 12, 1100, 60),
                "<b>Modo desktop</b>  WASD andar · botão direito ou Tab olhar · clique tocar · " +
                "N pular cena · R reiniciar · − = volume das vozes · [ ] dos efeitos · H esconder ajuda", estilo);
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(Transform rig, Transform cam)
        {
            cameraRig = rig;
            cameraTransform = cam;
        }
#endif
    }
}
