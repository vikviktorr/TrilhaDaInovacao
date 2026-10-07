using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Linha que se desenha aos poucos: trajetória do foguete (1994), feixes
    /// de fibra (2009) e o desenho digital da ideia (2018). Os pontos ficam
    /// guardados e são revelados do primeiro ao último.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LinhaProgressiva : MonoBehaviour
    {
        public Vector3[] pontos;
        public bool espacoLocal = true;

        LineRenderer linha;

        void Awake()
        {
            linha = GetComponent<LineRenderer>();
            linha.useWorldSpace = !espacoLocal;
            Esconder();
        }

        public void Esconder()
        {
            if (linha == null) linha = GetComponent<LineRenderer>();
            linha.positionCount = 0;
        }

        public void Completa()
        {
            linha.positionCount = pontos.Length;
            linha.SetPositions(pontos);
        }

        public IEnumerator Desenhar(float duracao)
        {
            if (pontos == null || pontos.Length < 2) yield break;

            var comprimentos = new float[pontos.Length];
            for (var i = 1; i < pontos.Length; i++)
                comprimentos[i] = comprimentos[i - 1] + Vector3.Distance(pontos[i - 1], pontos[i]);
            var total = comprimentos[pontos.Length - 1];

            for (var t = 0f; t < duracao; t += Time.deltaTime)
            {
                AtePercurso(total * Mathf.SmoothStep(0f, 1f, t / duracao), comprimentos);
                yield return null;
            }

            Completa();
        }

        void AtePercurso(float distancia, float[] comprimentos)
        {
            var n = 1;
            while (n < pontos.Length && comprimentos[n] < distancia) n++;
            if (n >= pontos.Length)
            {
                Completa();
                return;
            }

            linha.positionCount = n + 1;
            for (var i = 0; i < n; i++) linha.SetPosition(i, pontos[i]);
            var trecho = comprimentos[n] - comprimentos[n - 1];
            var k = trecho > 0f ? (distancia - comprimentos[n - 1]) / trecho : 1f;
            linha.SetPosition(n, Vector3.Lerp(pontos[n - 1], pontos[n], k));
        }
    }
}
