using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Um pulso de luz que percorre um caminho com rastro (a ligação de 1986
    /// passando pela central). Ao passar perto de um bastidor, acende os LEDs.
    /// </summary>
    public class PulsoNoCaminho : MonoBehaviour
    {
        public Transform bola;
        public TrailRenderer rastro;
        public Vector3[] caminho;
        public float velocidade = 1.6f;
        public PiscaLeds[] bastidores;
        public float raioFlash = 0.5f;

        void Awake() => Mostrar(false);

        void Mostrar(bool visivel)
        {
            if (bola != null) bola.gameObject.SetActive(visivel);
        }

        public IEnumerator Percorrer()
        {
            if (caminho == null || caminho.Length < 2) yield break;
            bola.position = caminho[0];
            if (rastro != null) rastro.Clear();
            Mostrar(true);

            for (var i = 1; i < caminho.Length; i++)
            {
                var de = caminho[i - 1];
                var para = caminho[i];
                var duracao = Vector3.Distance(de, para) / velocidade;
                for (var t = 0f; t < duracao; t += Time.deltaTime)
                {
                    bola.position = Vector3.Lerp(de, para, t / duracao);
                    AcenderProximos();
                    yield return null;
                }
            }

            bola.position = caminho[caminho.Length - 1];
            yield return new WaitForSeconds(0.3f);
            Mostrar(false);
        }

        void AcenderProximos()
        {
            if (bastidores == null) return;
            foreach (var b in bastidores)
            {
                if (b != null && Vector3.Distance(b.transform.position, bola.position) < raioFlash) b.Flash();
            }
        }
    }
}
