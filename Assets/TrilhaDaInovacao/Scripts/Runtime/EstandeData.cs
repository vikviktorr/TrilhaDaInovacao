using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Conteúdo de um estande da feira: a década que ele representa, o NPC
    /// responsável e o roteiro dublado. Um asset por estande (5 a 7, ref. 6).
    /// </summary>
    [CreateAssetMenu(fileName = "Estande", menuName = "Trilha da Inovação/Estande")]
    public class EstandeData : ScriptableObject
    {
        [Header("Identificação")]
        [Min(1)] public int ordem = 1;
        public string periodo = "1980";
        public string titulo = "Os Primeiros Passos";

        [Header("NPC")]
        public string nomeNpc = "Pioneiro da informática local";
        [TextArea(2, 4)] public string papelNpc;

        [Header("Roteiro dublado")]
        public LinhaDeDialogo[] falas = new LinhaDeDialogo[0];

        public string RotuloCurto => $"{ordem}. {periodo} — {titulo}";

        public float DuracaoTotal
        {
            get
            {
                var total = 0f;
                foreach (var fala in falas)
                    if (fala != null) total += fala.Duracao;
                return total;
            }
        }
    }
}
