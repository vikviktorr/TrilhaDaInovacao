using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>Estado de um estande dentro do Mural do Conhecimento.</summary>
    public enum EstadoEstande
    {
        Bloqueado,
        Visitado,
        Concluido
    }

    /// <summary>
    /// Máquina de estados da jornada (bloqueado > visitado > concluído).
    /// É o "sistema" exigido na avaliação: centraliza o progresso, notifica o
    /// Mural do Conhecimento e dispara o encerramento quando tudo é ouvido.
    /// Persiste entre a Feira e os Créditos.
    /// </summary>
    [DisallowMultipleComponent]
    public class ProgressoExperiencia : MonoBehaviour
    {
        public static ProgressoExperiencia Instancia { get; private set; }

        [SerializeField, Tooltip("Ordem cronológica dos estandes, de 1980 até a atualidade.")]
        List<EstandeData> estandes = new List<EstandeData>();

        readonly Dictionary<EstandeData, EstadoEstande> estados = new Dictionary<EstandeData, EstadoEstande>();

        /// <summary>Disparado sempre que um estande muda de estado.</summary>
        public event Action<EstandeData, EstadoEstande> AoMudarEstado;

        /// <summary>Disparado uma única vez, quando o último estande é concluído.</summary>
        public event Action AoConcluirTudo;

        bool jaAvisouConclusao;

        public IReadOnlyList<EstandeData> Estandes => estandes;
        public int Total => estandes.Count;
        public int TotalConcluidos { get; private set; }
        public bool TudoConcluido => estandes.Count > 0 && TotalConcluidos >= estandes.Count;

        void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            Instancia = this;
            DontDestroyOnLoad(gameObject);

            foreach (var estande in estandes)
                if (estande != null && !estados.ContainsKey(estande))
                    estados[estande] = EstadoEstande.Bloqueado;
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        public EstadoEstande Estado(EstandeData estande)
        {
            if (estande == null) return EstadoEstande.Bloqueado;
            return estados.TryGetValue(estande, out var estado) ? estado : EstadoEstande.Bloqueado;
        }

        /// <summary>Usuário chegou perto do NPC, mas ainda não ouviu a fala inteira.</summary>
        public void MarcarVisitado(EstandeData estande)
        {
            if (estande == null) return;
            if (Estado(estande) != EstadoEstande.Bloqueado) return;
            DefinirEstado(estande, EstadoEstande.Visitado);
        }

        /// <summary>Usuário ouviu a fala completa: o setor acende no Mural.</summary>
        public void MarcarConcluido(EstandeData estande)
        {
            if (estande == null) return;
            if (Estado(estande) == EstadoEstande.Concluido) return;

            DefinirEstado(estande, EstadoEstande.Concluido);
            TotalConcluidos++;

            if (TudoConcluido && !jaAvisouConclusao)
            {
                jaAvisouConclusao = true;
                AoConcluirTudo?.Invoke();
            }
        }

        public void Reiniciar()
        {
            TotalConcluidos = 0;
            jaAvisouConclusao = false;
            foreach (var estande in estandes)
                if (estande != null) DefinirEstado(estande, EstadoEstande.Bloqueado);
        }

        void DefinirEstado(EstandeData estande, EstadoEstande estado)
        {
            estados[estande] = estado;
            AoMudarEstado?.Invoke(estande, estado);
        }

#if UNITY_EDITOR
        public void DefinirEstandesNoEditor(List<EstandeData> lista) => estandes = lista;
#endif
    }
}
