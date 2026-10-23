using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Fabricação simulada (2018) numa impressora cartesiana como a Ender-3:
    /// o cabeçote corre em X no pórtico, a mesa anda em Y por baixo dele e o
    /// pórtico sobe uma camada de cada vez (Z). O bico contorna a peça, que
    /// cresce presa à mesa. Os eixos são os deste transform: right = X,
    /// forward = Y, up = Z.
    /// </summary>
    public class ImpressaoEmCamadas : MonoBehaviour
    {
        public Transform peca;
        public Transform mesa;
        public Transform portico;
        public Transform cabecote;
        public int camadas = 12;
        public float alturaFinal = 0.06f;
        [Tooltip("Meia largura do contorno que o bico percorre em cada camada (m).")]
        public float raioContorno = 0.03f;

        Vector3 escalaFinal;
        Vector3 mesaInicial, porticoInicial, cabecoteInicial;

        void Awake()
        {
            if (peca != null)
            {
                // A peça é impressa sobre a mesa e anda com ela.
                if (mesa != null) peca.SetParent(mesa, true);
                escalaFinal = peca.localScale;
                peca.gameObject.SetActive(false);
            }

            if (mesa != null) mesaInicial = mesa.position;
            if (portico != null) porticoInicial = portico.position;
            if (cabecote != null) cabecoteInicial = cabecote.position;
        }

        public IEnumerator Imprimir(float duracao)
        {
            peca.gameObject.SetActive(true);
            var porCamada = duracao / camadas;
            // Contorno da camada em coordenadas da mesa (x, y): quadrado com cantos.
            var r = raioContorno;
            var caminho = new[] { new Vector2(-r, -r), new Vector2(r, -r), new Vector2(r, r), new Vector2(-r, r), new Vector2(-r, -r) };
            for (var i = 1; i <= camadas; i++)
            {
                var k = i / (float)camadas;
                peca.localScale = new Vector3(escalaFinal.x, escalaFinal.y * k, escalaFinal.z);
                var z = alturaFinal * k;
                for (var t = 0f; t < porCamada; t += Time.deltaTime)
                {
                    var s = t / porCamada * (caminho.Length - 1);
                    var a = Mathf.Min((int)s, caminho.Length - 2);
                    Posicionar(Vector2.Lerp(caminho[a], caminho[a + 1], s - a), z);
                    yield return null;
                }
            }

            // Terminou: o pórtico sobe e a mesa vem para a frente, como na máquina real.
            yield return Mover(new Vector2(0f, 0f), alturaFinal, alturaFinal + 0.04f, 0.6f);
        }

        IEnumerator Mover(Vector2 xy, float zDe, float zPara, float duracao)
        {
            for (var t = 0f; t < duracao; t += Time.deltaTime)
            {
                var k = Mathf.SmoothStep(0f, 1f, t / duracao);
                Posicionar(new Vector2(xy.x, Mathf.Lerp(xy.y, 0.08f, k)), Mathf.Lerp(zDe, zPara, k));
                yield return null;
            }
        }

        /// <summary>Bico no ponto (x, y) da mesa, a z acima da primeira camada.</summary>
        void Posicionar(Vector2 xy, float z)
        {
            var subir = transform.up * z;
            // A mesa se move ao contrário: para o bico ir a +y na peça, a mesa recua.
            if (mesa != null) mesa.position = mesaInicial - transform.forward * xy.y;
            if (portico != null) portico.position = porticoInicial + subir;
            if (cabecote != null) cabecote.position = cabecoteInicial + subir + transform.right * xy.x;
        }

        public void Esconder()
        {
            if (peca != null) peca.gameObject.SetActive(false);
            Posicionar(Vector2.zero, 0f);
        }
    }
}
