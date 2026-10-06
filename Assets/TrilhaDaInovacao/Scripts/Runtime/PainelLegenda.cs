using TMPro;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Painel de legenda em World Space. Fica junto ao estande e sempre
    /// encara o usuário — texto grande e curto, pensado no público leigo.
    /// </summary>
    public class PainelLegenda : MonoBehaviour
    {
        [SerializeField] TMP_Text texto;
        [SerializeField] GameObject corpo;
        [SerializeField, Tooltip("Gira o painel para encarar a cabeça do usuário.")]
        bool encararUsuario = true;
        [SerializeField, Tooltip("Mantém o painel na horizontal, sem inclinar para cima/baixo.")]
        bool travarInclinacao = true;

        Transform cabecaUsuario;

        void Awake()
        {
            if (corpo == null) corpo = gameObject;
            Ocultar();
        }

        void LateUpdate()
        {
            if (!encararUsuario) return;

            if (cabecaUsuario == null)
            {
                var camera = Camera.main;
                if (camera == null) return;
                cabecaUsuario = camera.transform;
            }

            var direcao = transform.position - cabecaUsuario.position;
            if (travarInclinacao) direcao.y = 0f;
            if (direcao.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(direcao.normalized, Vector3.up);
        }

        public void Mostrar(string legenda)
        {
            if (texto != null) texto.text = legenda;
            if (corpo != null) corpo.SetActive(true);
        }

        public void Ocultar()
        {
            if (texto != null) texto.text = string.Empty;
            if (corpo != null) corpo.SetActive(false);
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(TMP_Text alvo, GameObject raiz)
        {
            texto = alvo;
            corpo = raiz;
        }
#endif
    }
}
