using TMPro;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Legenda em World Space que segue o olhar com atraso ("lazy follow"):
    /// fica um pouco abaixo da linha dos olhos e só reposiciona quando o
    /// visitante vira a cabeça além de um limite, para não tremer.
    /// </summary>
    public class SubtitleController : MonoBehaviour
    {
        [SerializeField] TMP_Text texto;
        [SerializeField] GameObject fundo;
        [SerializeField, Min(0.5f)] float distancia = 1.6f;
        [SerializeField] float abaixoDosOlhos = 0.38f;
        [SerializeField, Tooltip("Graus de diferença antes de a legenda acompanhar o olhar.")]
        float limiteAngulo = 25f;
        [SerializeField, Min(0.1f)] float suavidade = 4f;

        Transform cabeca;
        Vector3 direcaoAlvo = Vector3.forward;
        bool visivel;
        bool seguirInclinacao;

        public bool Visivel => visivel;

        /// <summary>
        /// Também acompanha a inclinação da cabeça, para quando o visitante olha
        /// para o céu (decolagem). Ao desligar, a legenda volta para a altura normal.
        /// </summary>
        public bool SeguirInclinacao
        {
            get => seguirInclinacao;
            set
            {
                seguirInclinacao = value;
                if (!value) direcaoAlvo = Plana(direcaoAlvo);
            }
        }

        void Awake() => Definir(string.Empty);

        void LateUpdate()
        {
            if (cabeca == null)
            {
                var cam = Camera.main;
                if (cam == null) return;
                cabeca = cam.transform;
                direcaoAlvo = Plana(cabeca.forward);
                Posicionar(1f);
            }

            var olhar = seguirInclinacao ? cabeca.forward : Plana(cabeca.forward);
            // Olhando para cima, 25° já tiram a legenda do campo de visão.
            var limite = seguirInclinacao ? limiteAngulo * 0.4f : limiteAngulo;
            if (Vector3.Angle(olhar, direcaoAlvo) > limite) direcaoAlvo = olhar;
            Posicionar(1f - Mathf.Exp(-suavidade * Time.deltaTime));
        }

        void Posicionar(float fator)
        {
            var baixo = seguirInclinacao ? Quaternion.LookRotation(direcaoAlvo) * Vector3.down : Vector3.down;
            var alvo = cabeca.position + direcaoAlvo * distancia + baixo * abaixoDosOlhos;
            transform.position = Vector3.Lerp(transform.position, alvo, fator);
            var paraFora = transform.position - cabeca.position;
            if (!seguirInclinacao) paraFora.y = 0f;
            if (paraFora.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(paraFora.normalized, Vector3.up);
        }

        static Vector3 Plana(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 0.0001f ? Vector3.forward : v.normalized;
        }

        public void Definir(string legenda)
        {
            visivel = !string.IsNullOrEmpty(legenda);
            if (texto != null) texto.text = legenda ?? string.Empty;
            if (texto != null) texto.gameObject.SetActive(visivel);
            if (fundo != null) fundo.SetActive(visivel);

            if (visivel && QARelatorio.Instancia != null && texto != null)
            {
                texto.ForceMeshUpdate();
                if (texto.textInfo.lineCount > 2)
                    QARelatorio.Instancia.Problema($"Legenda com {texto.textInfo.lineCount} linhas: \"{legenda}\"");
                if (texto.isTextOverflowing)
                    QARelatorio.Instancia.Problema($"Legenda transborda o painel: \"{legenda}\"");
            }
        }

        public void Ocultar() => Definir(string.Empty);

#if UNITY_EDITOR
        public void ConfigurarNoEditor(TMP_Text alvo, GameObject painelFundo)
        {
            texto = alvo;
            fundo = painelFundo;
        }
#endif
    }
}
