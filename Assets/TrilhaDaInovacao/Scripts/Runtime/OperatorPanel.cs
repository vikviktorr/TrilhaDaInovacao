using System.Globalization;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Atalhos do monitor da equipe. Teclado (PC): N pula a cena, R reinicia,
    /// C calibra. Controle Touch direito/esquerdo segurado: A (2 s) calibra,
    /// B (1 s) pula, Y (3 s) reinicia — o visitante usa as mãos, então os
    /// controles ficam com o monitor.
    ///
    /// Calibrar = o monitor fica de pé na marca de INÍCIO olhando para o
    /// centro do U; a cena virtual é alinhada ao piso real e o alinhamento
    /// fica salvo para as próximas visitas.
    /// </summary>
    public class OperatorPanel : MonoBehaviour
    {
        const string ChaveCalibracao = "trilha.calibracao";

        [SerializeField] VisitDirector diretor;
        [SerializeField] XROrigin origem;

        InputAction botaoA, botaoB, botaoY;
        float seguraA, seguraB, seguraY;

        void Awake()
        {
            botaoA = Acao("<XRController>{RightHand}/primaryButton");
            botaoB = Acao("<XRController>{RightHand}/secondaryButton");
            botaoY = Acao("<XRController>{LeftHand}/secondaryButton");
        }

        void OnDestroy()
        {
            botaoA?.Dispose();
            botaoB?.Dispose();
            botaoY?.Dispose();
        }

        static InputAction Acao(string caminho)
        {
            var acao = new InputAction(binding: caminho, type: InputActionType.Button);
            acao.Enable();
            return acao;
        }

        void Update()
        {
            var teclado = Keyboard.current;
            if (teclado != null)
            {
                if (teclado.nKey.wasPressedThisFrame) diretor.Pular();
                if (teclado.rKey.wasPressedThisFrame) diretor.Reiniciar();
                if (teclado.cKey.wasPressedThisFrame && XRStartup.XRAtivo) Calibrar();
            }

            if (Segurou(botaoA, ref seguraA, 2f)) Calibrar();
            if (Segurou(botaoB, ref seguraB, 1f)) diretor.Pular();
            if (Segurou(botaoY, ref seguraY, 3f)) diretor.Reiniciar();
        }

        static bool Segurou(InputAction acao, ref float tempo, float limite)
        {
            if (!acao.IsPressed())
            {
                tempo = 0f;
                return false;
            }

            var antes = tempo;
            tempo += Time.unscaledDeltaTime;
            return antes < limite && tempo >= limite;
        }

        public void Calibrar()
        {
            if (origem == null || origem.Camera == null) return;

            var marca = LayoutTrilha.Marcas[LayoutTrilha.Inicio];
            origem.MatchOriginUpCameraForward(Vector3.up, LayoutTrilha.Foco(LayoutTrilha.Inicio));
            var cabeca = origem.Camera.transform.position;
            origem.MoveCameraToWorldLocation(new Vector3(marca.x, cabeca.y, marca.z));

            var t = origem.transform;
            var valor = string.Join(";",
                t.position.x.ToString(CultureInfo.InvariantCulture),
                t.position.z.ToString(CultureInfo.InvariantCulture),
                t.eulerAngles.y.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetString(ChaveCalibracao, valor);
            PlayerPrefs.Save();
            Debug.Log($"[Trilha da Inovação] Sala calibrada: {valor}");
        }

        public void CarregarCalibracao()
        {
            if (origem == null || !PlayerPrefs.HasKey(ChaveCalibracao)) return;
            var partes = PlayerPrefs.GetString(ChaveCalibracao).Split(';');
            if (partes.Length != 3) return;

            var x = float.Parse(partes[0], CultureInfo.InvariantCulture);
            var z = float.Parse(partes[1], CultureInfo.InvariantCulture);
            var giro = float.Parse(partes[2], CultureInfo.InvariantCulture);
            var t = origem.transform;
            t.SetPositionAndRotation(new Vector3(x, t.position.y, z), Quaternion.Euler(0f, giro, 0f));
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(VisitDirector d, XROrigin o)
        {
            diretor = d;
            origem = o;
        }
#endif
    }
}
