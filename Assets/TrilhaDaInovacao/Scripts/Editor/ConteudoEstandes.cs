using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrilhaDaInovacao.EditorTools
{
    /// <summary>
    /// Gera os assets dos 6 estandes com a estrutura cronológica da seção 5.1
    /// do documento da P1. As legendas são um roteiro de trabalho (whitebox):
    /// serão substituídas pelo roteiro definitivo e pela dublagem da equipe.
    /// </summary>
    public static class ConteudoEstandes
    {
        public const string Pasta = "Assets/TrilhaDaInovacao/Data/Estandes";

        struct Rascunho
        {
            public int ordem;
            public string periodo;
            public string titulo;
            public string npc;
            public string papel;
            public string[] falas;
        }

        static readonly Rascunho[] Roteiro =
        {
            new Rascunho
            {
                ordem = 1, periodo = "Década de 1980", titulo = "Os Primeiros Passos",
                npc = "Pioneiro da informática local",
                papel = "Trabalhou nos primeiros centros de processamento de dados do estado.",
                falas = new[]
                {
                    "Seja bem-vindo aos anos 80! Naquela época, um computador ocupava uma sala inteira.",
                    "Os primeiros centros de processamento de dados chegaram às instituições e empresas daqui.",
                    "Era o começo de tudo: poucos sabiam programar, e cada máquina nova era um acontecimento."
                }
            },
            new Rascunho
            {
                ordem = 2, periodo = "Década de 1990", titulo = "O Maranhão Entra na Rede",
                npc = "Um dos primeiros usuários de internet do estado",
                papel = "Acompanhou a chegada das redes acadêmicas e dos primeiros provedores comerciais.",
                falas = new[]
                {
                    "Nos anos 90 o Maranhão entrou na rede. Primeiro pelas universidades, depois pelos provedores.",
                    "O som do modem discando virou a trilha sonora de uma geração inteira.",
                    "De repente, quem estava aqui podia conversar com o mundo todo."
                }
            },
            new Rascunho
            {
                ordem = 3, periodo = "Início dos anos 2000", titulo = "O Nascimento do Empreendedorismo Tech",
                npc = "Consultora do Sebrae",
                papel = "Explica o surgimento das incubadoras e do apoio institucional à inovação.",
                falas = new[]
                {
                    "Nos anos 2000 surgiram as primeiras incubadoras de empresas de tecnologia no estado.",
                    "O Sebrae passou a apoiar quem tinha uma boa ideia, mas não sabia como transformá-la em negócio.",
                    "Foi aí que inovar deixou de ser hobby e virou profissão por aqui."
                }
            },
            new Rascunho
            {
                ordem = 4, periodo = "Década de 2010", titulo = "A Onda das Startups",
                npc = "Fundador de startup maranhense",
                papel = "Relata os desafios e conquistas de empreender em tecnologia no estado.",
                falas = new[]
                {
                    "Na década de 2010 veio a onda das startups, e o Maranhão entrou nela.",
                    "Montamos equipes pequenas, com pouco dinheiro e muita vontade de resolver problemas daqui.",
                    "A OPS Game Studio é um bom exemplo dessa safra: gente local vivendo de tecnologia."
                }
            },
            new Rascunho
            {
                ordem = 5, periodo = "Década de 2010/2020", titulo = "OPS Game Studio e a Indústria de Jogos",
                npc = "Desenvolvedor de jogos",
                papel = "Conta a trajetória da OPS Game Studio e o peso da economia criativa digital.",
                falas = new[]
                {
                    "Jogo também é indústria, e aqui no Maranhão ela começou com times como a OPS Game Studio.",
                    "Economia criativa digital gera renda, emprego qualificado e leva o nome do estado longe.",
                    "Cada jogo publicado é uma vitrine do que a gente consegue fazer daqui."
                }
            },
            new Rascunho
            {
                ordem = 6, periodo = "Atualidade", titulo = "Sebraelab e o Futuro Imersivo",
                npc = "Guia do Sebraelab",
                papel = "Conecta o passado ao presente e fecha o arco da experiência.",
                falas = new[]
                {
                    "Você acabou de percorrer quarenta anos de inovação maranhense em poucos minutos.",
                    "Hoje, no Sebraelab, a Realidade Virtual é mais uma ferramenta para quem quer inovar.",
                    "Essa experiência que você está vivendo agora é o próximo passo dessa mesma trilha."
                }
            }
        };

        public static List<EstandeData> CriarOuAtualizar()
        {
            if (!Directory.Exists(Pasta)) Directory.CreateDirectory(Pasta);

            var lista = new List<EstandeData>();

            foreach (var rascunho in Roteiro)
            {
                var caminho = $"{Pasta}/Estande_{rascunho.ordem}_{Sanear(rascunho.titulo)}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<EstandeData>(caminho);

                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<EstandeData>();
                    AssetDatabase.CreateAsset(asset, caminho);
                }

                asset.ordem = rascunho.ordem;
                asset.periodo = rascunho.periodo;
                asset.titulo = rascunho.titulo;
                asset.nomeNpc = rascunho.npc;
                asset.papelNpc = rascunho.papel;

                var falas = new LinhaDeDialogo[rascunho.falas.Length];
                for (var i = 0; i < falas.Length; i++)
                {
                    falas[i] = new LinhaDeDialogo
                    {
                        legenda = rascunho.falas[i],
                        // ~0,33 s por palavra: estimativa de leitura em voz alta
                        // usada só enquanto a dublagem não está gravada.
                        duracaoSemAudio = Mathf.Max(2.5f, rascunho.falas[i].Split(' ').Length * 0.33f)
                    };
                }

                asset.falas = falas;
                EditorUtility.SetDirty(asset);
                lista.Add(asset);
            }

            AssetDatabase.SaveAssets();
            return lista;
        }

        static string Sanear(string texto)
        {
            var limpo = texto
                .Replace("á", "a").Replace("ã", "a").Replace("â", "a")
                .Replace("é", "e").Replace("ê", "e")
                .Replace("í", "i").Replace("ó", "o").Replace("õ", "o")
                .Replace("ú", "u").Replace("ç", "c")
                .Replace(" ", "");

            return limpo;
        }
    }
}
