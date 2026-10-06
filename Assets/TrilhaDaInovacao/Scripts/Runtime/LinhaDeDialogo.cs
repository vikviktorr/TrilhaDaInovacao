using System;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Uma fala dublada de NPC, acompanhada da legenda simultânea exibida
    /// no painel World Space (requisito de acessibilidade da P1).
    /// </summary>
    [Serializable]
    public class LinhaDeDialogo
    {
        [Tooltip("Dublagem gravada pela equipe. Enquanto não existe, a legenda usa 'duracaoSemAudio'.")]
        public AudioClip dublagem;

        [TextArea(2, 5)]
        public string legenda;

        [Tooltip("Tempo de exibição da legenda no whitebox, antes da dublagem definitiva.")]
        [Min(0.5f)]
        public float duracaoSemAudio = 4f;

        public float Duracao => dublagem != null ? dublagem.length : duracaoSemAudio;
    }
}
