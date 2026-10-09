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

        public bool Visivel => visivel;

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

            var olhar = Plana(cabeca.forward);
            if (Vector3.Angle(olhar, direcaoAlvo) > limiteAngulo) direcaoAlvo = olhar;
            Posicionar(1f - Mathf.Exp(-suavidade * Time.deltaTime));
        }

        void Posicionar(float fator)
        {
            var alvo = cabeca.position + direcaoAlvo * distancia + Vector3.down * abaixoDosOlhos;
            transform.position = Vector3.Lerp(transform.position, alvo, fator);
            var paraFora = transform.position - cabeca.position;
            paraFora.y = 0f;
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
