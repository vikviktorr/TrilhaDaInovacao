using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Pulsos do radar CUPRI (1994) subindo das antenas até a ionosfera. Cada
    /// pulso é um disco achatado que sobe pelo eixo y local, cresce com a
    /// abertura do feixe e some no alto; os discos saem um depois do outro.
    /// O bipe toca a cada pulso que sai.
    /// </summary>
    public class PulsosRadar : MonoBehaviour
    {
        public Transform[] pulsos;
        public float altura = 44f;
        public float periodo = 2.4f;
        public float raioBase = 1f;
        public float raioTopo = 4f;
        public AudioSource bipe;
        public bool ativo;

        float tempo;
        int ultimoCiclo = -1;
        Vector3[] escalas;

        void Awake()
        {
            escalas = new Vector3[pulsos.Length];
            for (var i = 0; i < pulsos.Length; i++)
            {
                escalas[i] = pulsos[i].localScale;
                pulsos[i].gameObject.SetActive(false);
            }
        }

        public void Iniciar()
        {
            tempo = 0f;
            ultimoCiclo = -1;
            ativo = true;
        }

        void Update()
        {
            if (!ativo || pulsos.Length == 0) return;
            tempo += Time.deltaTime;

            var intervalo = periodo / pulsos.Length;
            var ciclo = Mathf.FloorToInt(tempo / intervalo);
            if (ciclo != ultimoCiclo && bipe != null) bipe.PlayOneShot(ProceduralAudio.Obter("bipe"), 0.6f);
            ultimoCiclo = ciclo;

            for (var i = 0; i < pulsos.Length; i++)
            {
                // O pulso i só sai depois do seu primeiro intervalo.
                var desde = tempo - i * intervalo;
                var pulso = pulsos[i];
                pulso.gameObject.SetActive(desde >= 0f);
                if (desde < 0f) continue;

                var fase = Mathf.Repeat(desde / periodo, 1f);
                var raio = Mathf.Lerp(raioBase, raioTopo, fase);
                pulso.localPosition = new Vector3(0f, fase * altura, 0f);
                pulso.localScale = new Vector3(escalas[i].x * raio, escalas[i].y, escalas[i].z * raio) * (1f - 0.4f * fase);
            }
        }
    }
}
