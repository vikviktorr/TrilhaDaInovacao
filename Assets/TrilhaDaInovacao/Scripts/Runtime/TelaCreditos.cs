using System.Collections;
using TMPro;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Créditos finais com o convite para conhecer o Sebraelab fisicamente.
    /// Depois reinicia sozinha, para atender o próximo visitante da fila.
    /// </summary>
    public class TelaCreditos : MonoBehaviour
    {
        [SerializeField] TMP_Text texto;
        [SerializeField, Min(1f), Tooltip("Tempo de leitura antes de voltar ao início.")]
        float segundosAteReiniciar = 20f;
        [SerializeField] bool reiniciarSozinho = true;
        [SerializeField] FadeVR fade;

        [SerializeField, TextArea(5, 12)]
        string conteudo =
            "TRILHA DA INOVAÇÃO\n" +
            "A Feira Tecnológica do Maranhão\n\n" +
            "Fofucho's Games Group\n" +
            "Pedro Victor Santos Veloso · Gabriel Araujo Menezes\n" +
            "Kauã Carneiro de Morais · Davi Chaves de Oliveira\n" +
            "Lucas Gomes Antunes · Kalil Ramos\n\n" +
            "UNDB — Engenharia de Software\n" +
            "Tecnologias Emergentes\n\n" +
            "Venha conhecer o Sebraelab pessoalmente.";

        void Start()
        {
            if (texto != null) texto.text = conteudo;
            if (reiniciarSozinho) StartCoroutine(VoltarAoInicio());
        }

        IEnumerator VoltarAoInicio()
        {
            yield return new WaitForSeconds(segundosAteReiniciar);

            if (fade != null)
            {
                var pronto = false;
                fade.Escurecer(() => pronto = true);
                while (!pronto) yield return null;
            }

            FluxoExperiencia.Reiniciar();
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(TMP_Text alvo, FadeVR transicao)
        {
            texto = alvo;
            fade = transicao;
        }
#endif
    }
}
