using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Painel central da feira. Começa vazio (as seis décadas apagadas) e vai
    /// se preenchendo conforme o usuário ouve cada NPC — é o feedback de
    /// progresso da jornada e o gatilho do encerramento.
    /// </summary>
    public class MuralDoConhecimento : MonoBehaviour
    {
        [SerializeField] List<SetorMural> setores = new List<SetorMural>();
        [SerializeField] TMP_Text contador;
        [SerializeField] CelebracaoFinal celebracao;

        ProgressoExperiencia progresso;

        void OnEnable()
        {
            progresso = ProgressoExperiencia.Instancia;
            if (progresso == null) return;

            progresso.AoMudarEstado += AoMudarEstado;
            progresso.AoConcluirTudo += AoConcluirTudo;
            Sincronizar();
        }

        void OnDisable()
        {
            if (progresso == null) return;
            progresso.AoMudarEstado -= AoMudarEstado;
            progresso.AoConcluirTudo -= AoConcluirTudo;
        }

        void Start()
        {
            // O progresso pode ser criado depois do OnEnable do Mural.
            if (progresso == null) OnEnable();
        }

        void Sincronizar()
        {
            foreach (var setor in setores)
            {
                if (setor == null) continue;
                setor.DefinirEstado(progresso.Estado(setor.Estande));
            }

            AtualizarContador();
        }

        void AoMudarEstado(EstandeData estande, EstadoEstande estado)
        {
            foreach (var setor in setores)
            {
                if (setor == null || setor.Estande != estande) continue;
                setor.DefinirEstado(estado);
            }

            AtualizarContador();
        }

        void AtualizarContador()
        {
            if (contador == null || progresso == null) return;
            contador.text = $"{progresso.TotalConcluidos} / {progresso.Total} décadas descobertas";
        }

        void AoConcluirTudo()
        {
            if (celebracao != null) celebracao.Disparar();
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(List<SetorMural> lista, TMP_Text textoContador, CelebracaoFinal final)
        {
            setores = lista;
            contador = textoContador;
            celebracao = final;
        }
#endif
    }
}
