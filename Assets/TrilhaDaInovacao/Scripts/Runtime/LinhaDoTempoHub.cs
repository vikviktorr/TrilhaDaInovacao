using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Linha do tempo 1986 → 2025 do hub: acende na abertura e termina com os
    /// seis pontos acesos no encerramento.
    /// </summary>
    public class LinhaDoTempoHub : MonoBehaviour
    {
        public Renderer barra;
        public Renderer[] pontos;
        public Color cor = new Color(0.3f, 0.75f, 1f);

        static readonly int Emissao = Shader.PropertyToID("_EmissionColor");

        public void ApagarTodos()
        {
            Pintar(barra, 0.05f);
            foreach (var p in pontos) Pintar(p, 0.05f);
        }

        public IEnumerator AcenderBarra(float duracao)
        {
            for (var t = 0f; t < duracao; t += Time.deltaTime)
            {
                Pintar(barra, Mathf.Lerp(0.05f, 1.2f, t / duracao));
                yield return null;
            }
        }

        public IEnumerator AcenderPontos(float intervalo, AudioSource som)
        {
            Pintar(barra, 1.2f);
            foreach (var p in pontos)
            {
                Pintar(p, 2.5f);
                if (som != null) som.PlayOneShot(ProceduralAudio.Obter("chime"), 0.5f);
                yield return new WaitForSeconds(intervalo);
            }
        }

        void Pintar(Renderer r, float k)
        {
            if (r == null) return;
            r.material.EnableKeyword("_EMISSION");
            r.material.SetColor(Emissao, cor * k);
        }
    }
}
