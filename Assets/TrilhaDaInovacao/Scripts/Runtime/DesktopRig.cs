using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Modo sem headset, para testar a visita no PC e apresentar no
    /// computador: WASD anda, botão direito (ou Tab para travar o cursor) olha,
    /// clique esquerdo toca o objeto sob a mira. A altura dos olhos é fixa.
    /// </summary>
    public class DesktopRig : MonoBehaviour
    {
        [SerializeField] Transform cameraRig;
        [SerializeField] Transform cameraTransform;
        [SerializeField, Min(0.1f)] float alturaOlhos = 1.6f;
        [SerializeField, Min(0.1f)] float velocidade = 1.4f;
        [SerializeField, Min(0.01f)] float sensibilidade = 0.12f;

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
                cameraRig.position += (frente * entrada.y + direita * entrada.x).normalized * velocidade * Time.deltaTime;
            }

            if (mouse.leftButton.wasPressedThisFrame) Tocar(mouse);
        }

        void Tocar(Mouse mouse)
        {
            var cam = cameraTransform.GetComponent<Camera>();
            var raio = cursorTravado
                ? new Ray(cameraTransform.position, cameraTransform.forward)
                : cam.ScreenPointToRay(mouse.position.ReadValue());

            if (!Physics.Raycast(raio, out var acerto, 6f, ~0, QueryTriggerInteraction.Collide)) return;
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
            if (!Ativo || XRStartup.TemArgumento("-tour")) return;

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
