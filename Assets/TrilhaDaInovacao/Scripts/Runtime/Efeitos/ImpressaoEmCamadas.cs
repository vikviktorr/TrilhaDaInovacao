using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Fabricação simulada (2018): a peça cresce em degraus, camada por
    /// camada, a partir da mesa da impressora; o bico acompanha a altura.
    /// </summary>
    public class ImpressaoEmCamadas : MonoBehaviour
    {
        public Transform peca;
        public Transform bico;
        public int camadas = 18;
        public float alturaFinal = 0.12f;

        Vector3 escalaFinal;
        Vector3 bicoInicial;

        void Awake()
        {
            if (peca != null)
            {
                escalaFinal = peca.localScale;
                peca.gameObject.SetActive(false);
            }

            if (bico != null) bicoInicial = bico.localPosition;
        }

        public IEnumerator Imprimir(float duracao)
        {
            peca.gameObject.SetActive(true);
            var porCamada = duracao / camadas;
            for (var i = 1; i <= camadas; i++)
            {
                var k = i / (float)camadas;
                peca.localScale = new Vector3(escalaFinal.x, escalaFinal.y * k, escalaFinal.z);
                if (bico != null)
                {
                    var vai = (i % 2 == 0 ? 1f : -1f) * 0.05f;
                    bico.localPosition = bicoInicial + new Vector3(vai, alturaFinal * k, 0f);
                }

                yield return new WaitForSeconds(porCamada);
            }

            if (bico != null) bico.localPosition = bicoInicial + Vector3.up * (alturaFinal + 0.04f);
        }

        public void Esconder()
        {
            if (peca != null) peca.gameObject.SetActive(false);
            if (bico != null) bico.localPosition = bicoInicial;
        }
    }
}
