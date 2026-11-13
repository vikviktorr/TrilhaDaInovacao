using UnityEngine;
using UnityEngine.InputSystem;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Locomoção pelos controles do Quest 2, somada ao caminhar real: o analógico
    /// esquerdo anda na direção do olhar e o direito gira o corpo em passos de
    /// <see cref="anguloGiro"/> graus (giro em degraus enjoa menos que o contínuo).
    /// O corpo desliza nas paredes e móveis como no modo desktop (<see cref="DesktopRig.Andar"/>).
    /// Fica no rig; o <see cref="VisitDirector"/> o liga só no VR.
    /// </summary>
    public class LocomocaoJoystick : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float velocidade = 1.4f;
        [SerializeField, Range(10f, 90f)] float anguloGiro = 30f;
        [SerializeField, Range(0.05f, 0.9f)] float zonaMorta = 0.15f;

        const float LimiarGiro = 0.7f;
        const float SolturaGiro = 0.3f;

        DesktopRig corpo;
        Transform cabeca;
        InputAction andar, girar;
        bool giroArmado = true;

        public void Configurar(DesktopRig rig, Transform camera)
        {
            corpo = rig;
            cabeca = camera;
        }

        void Awake()
        {
            andar = new InputAction("Andar", InputActionType.Value, expectedControlType: "Vector2");
            andar.AddBinding("<XRController>{LeftHand}/{Primary2DAxis}");
            andar.AddBinding("<Gamepad>/leftStick");
            girar = new InputAction("Girar", InputActionType.Value, expectedControlType: "Vector2");
            girar.AddBinding("<XRController>{RightHand}/{Primary2DAxis}");
            girar.AddBinding("<Gamepad>/rightStick");
        }

        void OnEnable()
        {
            andar.Enable();
            girar.Enable();
        }

        void OnDisable()
        {
            andar.Disable();
            girar.Disable();
        }

        void OnDestroy()
        {
            andar.Dispose();
            girar.Dispose();
        }

        void Update()
        {
            if (corpo == null || cabeca == null || Time.timeScale == 0f) return;
            Girar(girar.ReadValue<Vector2>().x);

            var entrada = andar.ReadValue<Vector2>();
            if (entrada.magnitude < zonaMorta) return;
            entrada = Vector2.ClampMagnitude(entrada, 1f);

            // Olhando para o chão (ou o céu), o forward fica quase vertical; o up da cabeça diz a frente.
            var frente = cabeca.forward.y < -0.95f ? cabeca.up : cabeca.forward.y > 0.95f ? -cabeca.up : cabeca.forward;
            frente.y = 0f;
            frente.Normalize();
            var direita = new Vector3(frente.z, 0f, -frente.x);
            corpo.Andar((frente * entrada.y + direita * entrada.x) * velocidade * Time.deltaTime);
        }

        /// <summary>Um degrau de giro por empurrão; precisa soltar o analógico para girar de novo.</summary>
        void Girar(float x)
        {
            if (Mathf.Abs(x) < SolturaGiro) giroArmado = true;
            if (!giroArmado || Mathf.Abs(x) < LimiarGiro) return;
            giroArmado = false;
            // Gira em volta da cabeça, não da origem do rig, para o visitante não ser arrastado.
            transform.RotateAround(cabeca.position, Vector3.up, Mathf.Sign(x) * anguloGiro);
        }
    }
}
