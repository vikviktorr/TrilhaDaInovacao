using System.Collections;
using TMPro;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Passagem de uma época para a seguinte: um pedestal com o botão PRÓXIMO
    /// ao lado da marca e um portal vertical no caminho até a próxima marca.
    /// O visitante explora a cena o quanto quiser, toca no botão, o portal abre
    /// em frente a ele e a troca de cena acontece quando a cabeça o atravessa.
    /// Como o portal fica no meio do caminho físico entre as marcas, atravessá-lo
    /// é o mesmo passo que o visitante já daria na sala do Sebraelab.
    /// </summary>
    public class PortalEpoca : MonoBehaviour
    {
        [SerializeField] Transform pedestal;
        [SerializeField] PokeTrigger botao;
        [SerializeField] Transform portal;
        [SerializeField, Tooltip("Moldura, véu e brilho: crescem juntos ao abrir.")]
        Transform abertura;
        [SerializeField] Transform veu;
        [SerializeField] ParticleSystem faiscas;
        [SerializeField] TMP_Text destino;
        [SerializeField, Tooltip("Meia largura do vão do portal (m).")]
        float meiaLargura = 0.5f;

        Vector3 escalaAbertura;
        Vector3 ultimaCabeca;
        bool aberto;

        public PokeTrigger Botao => botao;
        public bool Aberto => aberto;
        public Vector3 Centro => portal.position;

        GameObject[] colunas = { };

        void Awake()
        {
            var lista = new System.Collections.Generic.List<GameObject>();
            foreach (Transform filho in pedestal)
                if (filho.name == "Pedestal" || filho.name == "Base do botão") lista.Add(filho.gameObject);
            colunas = lista.ToArray();
            escalaAbertura = abertura.localScale;
            Esconder();
        }

        /// <summary>
        /// Põe o portal no meio do caminho de <paramref name="de"/> até <paramref name="para"/>,
        /// virado para lá, e o botão no <paramref name="lugar"/> que a cena reservou para ele.
        /// Sem lugar reservado, o pedestal fica atrás do visitante, à direita, longe do
        /// caminho (<paramref name="foco"/> é para onde ele olha na marca).
        /// </summary>
        public void Posicionar(Vector3 de, Vector3 para, Vector3 foco, string ano, string titulo, LugarDoProximo lugar)
        {
            var normal = (para - de).normalized;
            portal.SetPositionAndRotation((de + para) * 0.5f, Quaternion.LookRotation(normal, Vector3.up));

            var montado = lugar != null && lugar.montado;
            var ponto = lugar != null ? lugar.transform.position : de + normal * 0.45f - foco * 0.85f;
            var olhar = ponto - de;
            olhar.y = 0f;
            // O botão fica a 1,06 m do pé do pedestal; montado, ele pousa no móvel.
            if (montado) ponto.y -= 1.06f - 0.02f;
            else ponto.y = de.y;
            pedestal.SetPositionAndRotation(ponto, Quaternion.LookRotation(olhar.normalized, Vector3.up));
            foreach (var coluna in colunas) coluna.SetActive(!montado);

            destino.text = string.IsNullOrEmpty(ano) ? titulo : $"<b>{ano}</b>\n<size=50%>{titulo}</size>";
        }

        public void MostrarBotao()
        {
            pedestal.gameObject.SetActive(true);
            botao.Ativar();
        }

        /// <summary>Abre o portal crescendo a partir do centro.</summary>
        public IEnumerator Abrir()
        {
            botao.Desativar();
            pedestal.gameObject.SetActive(true);
            portal.gameObject.SetActive(true);
            aberto = true;
            ultimaCabeca = Cabeca();
            if (faiscas != null) faiscas.Play();
            for (var t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                var k = Mathf.SmoothStep(0f, 1f, t / 0.8f);
                abertura.localScale = new Vector3(escalaAbertura.x * k, escalaAbertura.y * Mathf.Lerp(0.15f, 1f, k), escalaAbertura.z);
                yield return null;
            }

            abertura.localScale = escalaAbertura;
        }

        public void Esconder()
        {
            aberto = false;
            if (botao != null) botao.Desativar();
            pedestal.gameObject.SetActive(false);
            portal.gameObject.SetActive(false);
        }

        /// <summary>
        /// True no quadro em que a cabeça passa pelo vão do portal, no sentido da
        /// próxima marca. Compara com a posição do quadro anterior: um salto (tour,
        /// teleporte) também conta, desde que o segmento passe pelo vão.
        /// </summary>
        public bool Atravessou()
        {
            if (!aberto) return false;
            var agora = Cabeca();
            var antes = ultimaCabeca;
            ultimaCabeca = agora;

            var normal = portal.forward;
            var da = Vector3.Dot(antes - portal.position, normal);
            var db = Vector3.Dot(agora - portal.position, normal);
            if (da > 0f || db < 0f) return false;

            var k = Mathf.Abs(da) < 1e-5f ? 0f : da / (da - db);
            var ponto = Vector3.Lerp(antes, agora, k);
            var lateral = Vector3.Dot(ponto - portal.position, portal.right);
            return Mathf.Abs(lateral) <= meiaLargura + 0.25f;
        }

        void Update()
        {
            if (!aberto || veu == null) return;
            veu.Rotate(0f, 0f, 40f * Time.deltaTime, Space.Self);
        }

        static Vector3 Cabeca() => Camera.main != null ? Camera.main.transform.position : Vector3.zero;

#if UNITY_EDITOR
        public void ConfigurarNoEditor(Transform p, PokeTrigger b, Transform port, Transform ab, Transform v,
            ParticleSystem f, TMP_Text d, float meia)
        {
            pedestal = p;
            botao = b;
            portal = port;
            abertura = ab;
            veu = v;
            faiscas = f;
            destino = d;
            meiaLargura = meia;
        }
#endif
    }
}
