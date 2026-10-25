using System;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Volume das vozes (guia e figurantes) e dos efeitos (sons, ambientes),
    /// ajustado pelo visitante no painel do hub (<see cref="PainelVolume"/>) ou
    /// pelas teclas do modo desktop, e salvo entre as visitas. Os campos são lidos
    /// na thread de áudio por <see cref="GanhoVoz"/> e <see cref="Abafador"/>.
    /// </summary>
    public static class ConfigAudio
    {
        const string ChaveVozes = "volume-vozes";
        const string ChaveEfeitos = "volume-efeitos";

        public const float Passo = 0.1f;
        public const float PadraoVozes = 1f;
        // Efeitos um pouco abaixo das vozes: o que o guia diz é o principal.
        public const float PadraoEfeitos = 0.8f;

        public static volatile float Vozes = PadraoVozes;
        public static volatile float Efeitos = PadraoEfeitos;

        /// <summary>Disparado na thread principal quando um dos volumes muda.</summary>
        public static event Action Mudou;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Carregar()
        {
            Vozes = Mathf.Clamp01(PlayerPrefs.GetFloat(ChaveVozes, PadraoVozes));
            Efeitos = Mathf.Clamp01(PlayerPrefs.GetFloat(ChaveEfeitos, PadraoEfeitos));
        }

        public static void AjustarVozes(int passos) => DefinirVozes(Vozes + passos * Passo);

        public static void AjustarEfeitos(int passos) => DefinirEfeitos(Efeitos + passos * Passo);

        public static void DefinirVozes(float valor)
        {
            Vozes = Salvar(ChaveVozes, valor);
            Mudou?.Invoke();
        }

        public static void DefinirEfeitos(float valor)
        {
            Efeitos = Salvar(ChaveEfeitos, valor);
            Mudou?.Invoke();
        }

        static float Salvar(string chave, float valor)
        {
            // Em décimos exatos, para a barra e o "%" não acumularem erro de arredondamento.
            var v = Mathf.Round(Mathf.Clamp01(valor) / Passo) * Passo;
            PlayerPrefs.SetFloat(chave, v);
            PlayerPrefs.Save();
            return v;
        }
    }
}
