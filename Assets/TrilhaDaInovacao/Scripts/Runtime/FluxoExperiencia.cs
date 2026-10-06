using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Fluxo entre as três cenas do escopo: Boas-vindas/Tutorial, Feira
    /// principal (hub único, sem load entre estandes) e Encerramento/Créditos.
    /// </summary>
    public static class FluxoExperiencia
    {
        public const string CenaBoasVindas = "01_BoasVindas_Tutorial";
        public const string CenaFeira = "02_Feira_TrilhaDaInovacao";
        public const string CenaCreditos = "03_Encerramento_Creditos";

        public static void IrParaBoasVindas() => Carregar(CenaBoasVindas);

        public static void IrParaFeira() => Carregar(CenaFeira);

        public static void IrParaCreditos() => Carregar(CenaCreditos);

        /// <summary>Recomeça a experiência para o próximo visitante da fila.</summary>
        public static void Reiniciar()
        {
            var progresso = ProgressoExperiencia.Instancia;
            if (progresso != null)
            {
                progresso.Reiniciar();
                Object.Destroy(progresso.gameObject);
            }

            Carregar(CenaBoasVindas);
        }

        static void Carregar(string cena)
        {
            if (Application.CanStreamedLevelBeLoaded(cena))
            {
                SceneManager.LoadScene(cena);
                return;
            }

            Debug.LogError(
                $"[Trilha da Inovação] Cena '{cena}' não está em File > Build Profiles > Scene List.");
        }
    }
}
