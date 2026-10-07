using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>Uma fala do guia; o texto é também a legenda.</summary>
    [Serializable]
    public class Fala
    {
        [Tooltip("Identificador usado pela sequência da cena. Não mude sem mudar o código.")]
        public string id;

        [TextArea(2, 4)]
        public string texto;

        [Tooltip("Voz gravada. Enquanto não existe, a legenda fica na tela pelo tempo calculado.")]
        public AudioClip audio;

        [Tooltip("0 = calcula pelo número de palavras (cerca de 2,6 palavras por segundo).")]
        [Min(0f)]
        public float duracao;

        public float Duracao
        {
            get
            {
                if (audio != null) return audio.length + 0.3f;
                if (duracao > 0f) return duracao;
                var palavras = string.IsNullOrWhiteSpace(texto)
                    ? 0
                    : texto.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
                return Mathf.Max(2f, palavras / 2.6f + 0.8f);
            }
        }
    }

    /// <summary>
    /// Roteiro de uma cena: as falas do guia com suas legendas. Editável no
    /// Inspector, para a equipe revisar o texto sem tocar no código.
    /// </summary>
    [CreateAssetMenu(menuName = "Trilha da Inovação/Roteiro de cena", fileName = "Roteiro")]
    public class SceneScript : ScriptableObject
    {
        public string titulo;
        public string ano;

        [Tooltip("Tempo previsto no roteiro de produção, em segundos.")]
        public float tempoPrevisto = 35f;

        [Tooltip("Fato histórico e fonte, para a ficha da cena.")]
        [TextArea(2, 5)]
        public string fonte;

        public List<Fala> falas = new List<Fala>();

        public Fala Obter(string id)
        {
            foreach (var fala in falas)
            {
                if (fala != null && fala.id == id) return fala;
            }

            Debug.LogWarning($"[Trilha da Inovação] Fala '{id}' não existe no roteiro '{name}'.");
            return new Fala { id = id, texto = string.Empty, duracao = 0.1f };
        }

        public bool Contem(string id) => falas.Exists(f => f != null && f.id == id);
    }
}
