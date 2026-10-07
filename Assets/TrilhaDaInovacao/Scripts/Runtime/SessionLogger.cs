using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Registra cada visita para os testes da metodologia (seção 8 do paper):
    /// tempo de cada cena e como ela avançou — toque do visitante, avanço
    /// automático do plano B ou pulo do operador. Um CSV por máquina em
    /// persistentDataPath/sessoes.csv.
    /// </summary>
    public class SessionLogger : MonoBehaviour
    {
        const string Cabecalho = "sessao,data_hora,modo,cena,duracao_s,previsto_s,gatilhos";

        string caminho;
        string sessao;
        string modo;
        string cenaAtual;
        float previstoAtual;
        float inicioCena;
        float inicioVisita;
        readonly List<string> gatilhos = new List<string>();

        public string Caminho => caminho;

        void Awake()
        {
            caminho = Path.Combine(Application.persistentDataPath, "sessoes.csv");
        }

        public void IniciarVisita(string modoVisita)
        {
            sessao = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            modo = modoVisita;
            inicioVisita = Time.time;
            cenaAtual = null;
        }

        public void IniciarCena(string nome, float previsto)
        {
            EncerrarCena();
            cenaAtual = nome;
            previstoAtual = previsto;
            inicioCena = Time.time;
            gatilhos.Clear();
        }

        public void Gatilho(string tipo)
        {
            if (cenaAtual != null) gatilhos.Add(tipo);
        }

        public void EncerrarCena()
        {
            if (cenaAtual == null) return;
            Escrever(cenaAtual, Time.time - inicioCena, previstoAtual, string.Join("|", gatilhos));
            cenaAtual = null;
        }

        public void EncerrarVisita(bool completa)
        {
            EncerrarCena();
            if (sessao == null) return;
            Escrever(completa ? "TOTAL" : "TOTAL_INTERROMPIDA", Time.time - inicioVisita, 285f, string.Empty);
            Debug.Log($"[Trilha da Inovação] Visita {sessao} registrada em {caminho}");
            sessao = null;
        }

        void Escrever(string cena, float duracao, float previsto, string tipos)
        {
            try
            {
                var novo = !File.Exists(caminho);
                using var arquivo = new StreamWriter(caminho, true);
                if (novo) arquivo.WriteLine(Cabecalho);
                arquivo.WriteLine(string.Join(",",
                    sessao,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    modo,
                    $"\"{cena}\"",
                    duracao.ToString("0.0", CultureInfo.InvariantCulture),
                    previsto.ToString("0", CultureInfo.InvariantCulture),
                    tipos));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Trilha da Inovação] Não foi possível gravar o log: {e.Message}");
            }
        }
    }
}
