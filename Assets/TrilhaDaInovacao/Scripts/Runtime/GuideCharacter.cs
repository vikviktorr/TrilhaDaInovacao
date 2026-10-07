using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// O guia que acompanha a visita. No whitebox é feito de primitivas: flutua
    /// de leve, vira para o visitante e "fala" pulsando a cabeça e a luz do peito.
    /// Troca de lugar a cada cena (aparece ao lado da marca, nunca no caminho).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GuideCharacter : MonoBehaviour
    {
        [SerializeField] Transform corpo;
        [SerializeField] Transform cabeca;
        [SerializeField] Renderer luzPeito;
        [SerializeField] Color corLuz = new Color(0.3f, 0.75f, 1f);
        [SerializeField] float amplitude = 0.03f;

        static readonly int Emissao = Shader.PropertyToID("_EmissionColor");

        AudioSource voz;
        Material materialLuz;
        Transform alvoOlhar;
        Vector3 cabecaEscalaBase = Vector3.one;
        float alturaBase;

        public bool Falando { get; set; }
        public AudioSource Voz => voz;

        void Awake()
        {
            voz = GetComponent<AudioSource>();
            voz.playOnAwake = false;
            voz.spatialBlend = 0.8f;
            voz.minDistance = 1.5f;
            if (cabeca != null) cabecaEscalaBase = cabeca.localScale;
            if (corpo != null) alturaBase = corpo.localPosition.y;
            if (luzPeito != null)
            {
                materialLuz = luzPeito.material;
                materialLuz.EnableKeyword("_EMISSION");
            }
        }

        void Update()
        {
            if (alvoOlhar == null && Camera.main != null) alvoOlhar = Camera.main.transform;

            var t = Time.time;
            if (corpo != null)
            {
                var p = corpo.localPosition;
                p.y = alturaBase + Mathf.Sin(t * 1.6f) * amplitude;
                corpo.localPosition = p;
            }

            if (alvoOlhar != null)
            {
                var direcao = alvoOlhar.position - transform.position;
                direcao.y = 0f;
                if (direcao.sqrMagnitude > 0.01f)
                {
                    var desejada = Quaternion.LookRotation(direcao.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, desejada, 1f - Mathf.Exp(-3f * Time.deltaTime));
                }
            }

            var fala = Falando ? 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(t * 9f)) : 0f;
            if (cabeca != null) cabeca.localScale = cabecaEscalaBase * (1f + 0.04f * fala);
            if (materialLuz != null) materialLuz.SetColor(Emissao, corLuz * (0.6f + 2.4f * fala));
        }

        public void IrPara(Vector3 posicao, Quaternion rotacao)
        {
            transform.SetPositionAndRotation(posicao, rotacao);
            gameObject.SetActive(true);
        }

        public void Esconder() => gameObject.SetActive(false);

#if UNITY_EDITOR
        public void ConfigurarNoEditor(Transform corpoGuia, Transform cabecaGuia, Renderer peito)
        {
            corpo = corpoGuia;
            cabeca = cabecaGuia;
            luzPeito = peito;
        }
#endif
    }
}
