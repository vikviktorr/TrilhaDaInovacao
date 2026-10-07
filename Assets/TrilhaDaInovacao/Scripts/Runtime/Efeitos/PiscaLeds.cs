using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// LEDs de um bastidor (1986) ou rack de rede (2009): piscam ao acaso e
    /// acendem todos de uma vez quando o pulso da ligação passa por perto.
    /// </summary>
    public class PiscaLeds : MonoBehaviour
    {
        public Renderer[] leds;
        public Color cor = new Color(0.2f, 1f, 0.35f);
        public float chanceTroca = 3f;

        static readonly int Emissao = Shader.PropertyToID("_EmissionColor");

        Material[] materiais;
        bool[] aceso;
        float flash;

        void Awake()
        {
            materiais = new Material[leds.Length];
            aceso = new bool[leds.Length];
            for (var i = 0; i < leds.Length; i++)
            {
                materiais[i] = leds[i].material;
                materiais[i].EnableKeyword("_EMISSION");
                aceso[i] = Random.value > 0.5f;
            }
        }

        void Update()
        {
            flash = Mathf.Max(0f, flash - Time.deltaTime * 2.5f);
            for (var i = 0; i < materiais.Length; i++)
            {
                if (Random.value < chanceTroca * Time.deltaTime / Mathf.Max(1, leds.Length) * 4f) aceso[i] = !aceso[i];
                var k = Mathf.Max(aceso[i] ? 0.8f : 0.05f, flash * 3f);
                materiais[i].SetColor(Emissao, cor * k);
            }
        }

        public void Flash() => flash = 1f;
    }
}
