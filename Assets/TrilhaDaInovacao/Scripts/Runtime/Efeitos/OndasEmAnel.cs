using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Anéis que nascem no centro e se espalham, sumindo na borda — as ondas
    /// do 5G (2022) sobre o mapa. Cada anel é um LineRenderer em círculo.
    /// </summary>
    public class OndasEmAnel : MonoBehaviour
    {
        public LineRenderer[] aneis;
        public float raioMaximo = 3f;
        public float periodo = 2.4f;
        public int segmentos = 72;
        public Color cor = new Color(0.8f, 0.3f, 1f);
        public bool ativo;
        public AudioSource pulso;

        float tempo;
        int ultimoCiclo = -1;

        void Awake()
        {
            if (pulso != null && pulso.clip == null) pulso.clip = ProceduralAudio.Obter("pulso-grave");
            foreach (var anel in aneis)
            {
                anel.positionCount = segmentos;
                anel.loop = true;
                anel.useWorldSpace = false;
                anel.enabled = false;
            }
        }

        public void Iniciar()
        {
            ativo = true;
            tempo = 0f;
        }

        void Update()
        {
            if (!ativo) return;
            tempo += Time.deltaTime;

            var ciclo = Mathf.FloorToInt(tempo / (periodo / aneis.Length));
            if (ciclo != ultimoCiclo && pulso != null) pulso.Play();
            ultimoCiclo = ciclo;

            for (var i = 0; i < aneis.Length; i++)
            {
                var fase = Mathf.Repeat(tempo / periodo + i / (float)aneis.Length, 1f);
                var raio = Mathf.Lerp(0.05f, raioMaximo, fase);
                var anel = aneis[i];
                anel.enabled = true;

                for (var s = 0; s < segmentos; s++)
                {
                    var a = s / (float)segmentos * Mathf.PI * 2f;
                    anel.SetPosition(s, new Vector3(Mathf.Cos(a) * raio, 0f, Mathf.Sin(a) * raio));
                }

                var c = cor;
                c.a = 1f - fase;
                anel.startColor = anel.endColor = c * (1.5f - fase);
                anel.widthMultiplier = Mathf.Lerp(0.04f, 0.015f, fase) * Mathf.Max(1f, raioMaximo / 3f);
            }
        }
    }
}
