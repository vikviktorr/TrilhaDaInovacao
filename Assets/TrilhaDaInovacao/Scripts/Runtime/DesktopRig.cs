using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Modo sem headset, para testar a visita no PC e apresentar no
    /// computador: WASD anda, botão direito (ou Tab para travar o cursor) olha,
    /// clique esquerdo toca o objeto sob a mira. A altura dos olhos é fixa.
    /// O corpo é uma cápsula que desliza nas paredes e móveis do cenário
    /// (camada <see cref="LayoutTrilha.CamadaCenario"/>) em vez de atravessá-los.
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

        float yaw, pitch;
        bool cursorTravado;
        bool mostrarAjuda = true;
        GUIStyle estilo;

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
            const int mascara = 1 << LayoutTrilha.CamadaCenario;
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

            if (!mostrarAjuda) return;
            GUI.Label(new Rect(16, 12, 900, 60),
                "<b>Modo desktop</b>  WASD andar · botão direito ou Tab olhar · clique tocar · " +
                "N pular cena · R reiniciar · H esconder ajuda", estilo);
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
