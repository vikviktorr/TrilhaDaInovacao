using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrilhaDaInovacao.EditorTools
{
    /// <summary>
    /// Cria os roteiros (SceneScript) com as falas do documento "Trilha da
    /// Inovação VR — Cenários e Roteiro de Produção". Se o asset já existe,
    /// só acrescenta as falas que faltam: o texto revisado pela equipe no
    /// Inspector nunca é sobrescrito.
    /// </summary>
    public static class Roteiro
    {
        const string Pasta = "Assets/TrilhaDaInovacao/Roteiro";

        public const string Geral = "00_Geral";

        public static readonly string[] Nomes =
        {
            "Cena0_Abertura", "Cena1_1986_Telma", "Cena2_1994_Guara", "Cena3_2009_Fibra",
            "Cena4_2018_Senai", "Cena5_2022_5G", "Cena6_2023-2025_Alcantara", "Cena7_Encerramento"
        };

        /// <summary>Correções vindas do QA (id, texto antigo, texto novo).</summary>
        static readonly (string, string, string)[] Revisoes =
        {
            ("resultado",
                "O lançamento aconteceu, mas uma anomalia levou à perda da missão. O PION-BR2 não chegou à órbita.",
                "O lançamento aconteceu, mas uma anomalia levou à perda da missão.")
        };

        public static Dictionary<string, SceneScript> CriarOuAtualizar()
        {
            Directory.CreateDirectory(Pasta);
            var roteiros = new Dictionary<string, SceneScript>
            {
                [Geral] = Garantir(Geral, "Geral", "", 0f, "", new[]
                {
                    ("dica-andar", "Caminhe até a marca que está brilhando no chão.")
                }),

                [Nomes[0]] = Garantir(Nomes[0], "Abertura", "", 25f,
                    "Hub do Sebraelab: apresentação do guia e calibração das mãos.", new[]
                    {
                        ("ola", "Olá! Bem-vindo à Trilha da Inovação."),
                        ("maos", "Levante as mãos. É com elas que você vai tocar a história."),
                        ("seis", "Vamos visitar seis momentos em que o Maranhão fez história na tecnologia."),
                        ("dica-comecar", "Toque no botão Começar, à sua frente."),
                        ("siga", "Siga a luz no chão até a primeira marca.")
                    }),

                [Nomes[1]] = Garantir(Nomes[1], "Central Trópico-R da TELMA", "1986", 35f,
                    "TELMA ativou a central Trópico-R em São Luís em 1986, apontada como a primeira " +
                    "central de comutação totalmente digitalizada do Brasil (HOLANDA, 1996).", new[]
                    {
                        ("intro", "São Luís, 1986. A TELMA acaba de ativar a central Trópico-R."),
                        ("primeira", "Ela foi apontada como a primeira central de comutação totalmente digitalizada do Brasil."),
                        ("dica", "Toque no telefone que está brilhando."),
                        ("toque", "Ao tocar neste telefone, você vê como a central encaminhava uma ligação por sinais digitais."),
                        ("fim", "Ligação completa! Agora vamos ao céu de Alcântara.")
                    }),

                [Nomes[2]] = Garantir(Nomes[2], "Campanha Guará", "1994", 35f,
                    "Campanha Guará em Alcântara: pesquisadores brasileiros e da NASA estudaram a " +
                    "ionosfera com foguetes e instrumentos em terra (INPE, s. d.).", new[]
                    {
                        ("intro", "Alcântara, 1994. Aqui acontece a campanha Guará."),
                        ("pesquisadores", "Pesquisadores do Brasil e dos Estados Unidos estudaram juntos essa região da atmosfera."),
                        ("dica", "Dê um passo até a marca do radar no chão."),
                        ("ionosfera", "Essa camada se chama ionosfera. Foguetes e instrumentos em terra a estudaram."),
                        ("fim", "O INPE e a NASA trabalharam juntos nessa missão. Próxima parada: 2009.")
                    }),

                [Nomes[3]] = Garantir(Nomes[3], "Rede Metropolitana de São Luís", "2009", 35f,
                    "RNP: rede de fibra óptica de 43 km conectando seis instituições a 1 Gbps (RNP, 2009). " +
                    "PENDENTE: nomes das seis instituições para rotular os pinos.", new[]
                    {
                        ("intro", "São Luís, 2009. A RNP inaugura a Rede Metropolitana da cidade."),
                        ("numeros", "São 43 quilômetros de fibra óptica ligando seis instituições a 1 Gbps."),
                        ("dica", "Toque no ponto grande que pulsa no mapa."),
                        ("veja", "Veja os centros de pesquisa se conectarem pela fibra óptica."),
                        ("fim", "Com a rede pronta, as ideias circulam. Vamos ver uma delas virar objeto.")
                    }),

                [Nomes[4]] = Garantir(Nomes[4], "Laboratório Aberto do SENAI", "2018", 35f,
                    "SENAI inaugurou o Laboratório Aberto em São Luís em 2 de março de 2018 (FIEMA, 2018).", new[]
                    {
                        ("intro", "2 de março de 2018. O SENAI inaugura seu primeiro Laboratório Aberto no Maranhão."),
                        ("juntos", "Estudantes, pesquisadores e empreendedores trabalham juntos aqui."),
                        ("dica", "Encoste a mão na lâmpada sobre a bancada."),
                        ("ideia", "Aqui, uma ideia pode ganhar forma de protótipo."),
                        ("fim", "Pronto! Agora, vamos acelerar a conexão da cidade.")
                    }),

                [Nomes[5]] = Garantir(Nomes[5], "5G em São Luís", "2022", 35f,
                    "Anatel autorizou estações na faixa de 3,5 GHz em São Luís a partir de 19 set. 2022 " +
                    "(ANATEL, 2022). PENDENTE: valores e fonte da comparação 3G/4G/5G.", new[]
                    {
                        ("intro", "São Luís, setembro de 2022. O 5G chega à cidade."),
                        ("dica", "Aproxime-se da torre."),
                        ("anatel", "A Anatel liberou a faixa de 3,5 GHz a partir de 19 de setembro."),
                        ("expansao", "A nova faixa permitiu a expansão das redes móveis na cidade."),
                        ("fim", "Agora, a última parada: de volta a Alcântara.")
                    }),

                [Nomes[6]] = Garantir(Nomes[6], "Lançamentos em Alcântara", "2023", 60f,
                    "HANBIT-TLV (Innospace) levou o SISNAV em 2023 (AEB, 2024). HANBIT-Nano, 2025: primeiro " +
                    "lançamento comercial do Brasil, com o PION-BR2 (UFMA); anomalia e perda da missão (AEB, 2026).", new[]
                    {
                        ("intro2023", "2023. O foguete HANBIT-TLV, da Innospace, sai de Alcântara."),
                        ("sisnav", "Dentro dele, o SISNAV, sistema brasileiro de navegação, foi levado para testes."),
                        ("intro2025", "2025. O HANBIT-Nano fará o primeiro lançamento comercial a partir do Brasil."),
                        ("pion", "Entre as cargas está o PION-BR2, ligado à UFMA..."),
                        ("decolar", "Nossa, o foguete vai decolar agora!"),
                        ("contagem", "Five, four, three, two, one"),
                        ("resultado", "O lançamento aconteceu, mas uma anomalia levou à perda da missão."),
                        ("resultado2", "O PION-BR2 não chegou à órbita."),
                        ("reconstituicao", "Reconstituição feita para este projeto.")
                    }),

                [Nomes[7]] = Garantir(Nomes[7], "Encerramento", "", 25f,
                    "Volta ao hub com a linha do tempo completa e créditos.", new[]
                    {
                        ("historia", "De uma central telefônica a foguetes, essa história também aconteceu no Maranhão."),
                        ("obrigado", "Obrigado por percorrer a Trilha da Inovação!"),
                        ("retirar", "Você já pode retirar o óculos.")
                    })
            };

            AssetDatabase.SaveAssets();
            return roteiros;
        }

        static SceneScript Garantir(string arquivo, string titulo, string ano, float previsto, string fonte,
            (string id, string texto)[] falas)
        {
            var caminho = $"{Pasta}/{arquivo}.asset";
            var roteiro = AssetDatabase.LoadAssetAtPath<SceneScript>(caminho);

            if (roteiro == null)
            {
                roteiro = ScriptableObject.CreateInstance<SceneScript>();
                roteiro.titulo = titulo;
                roteiro.ano = ano;
                roteiro.tempoPrevisto = previsto;
                roteiro.fonte = fonte;
                AssetDatabase.CreateAsset(roteiro, caminho);
            }

            foreach (var (id, texto) in falas)
            {
                if (!roteiro.Contem(id)) roteiro.falas.Add(new Fala { id = id, texto = texto });
            }

            // Revisões do roteiro: só troca se o texto ainda for o original gerado.
            foreach (var (id, antigo, novo) in Revisoes)
            {
                var fala = roteiro.falas.Find(f => f.id == id);
                if (fala != null && fala.texto == antigo) fala.texto = novo;
            }

            EditorUtility.SetDirty(roteiro);
            return roteiro;
        }
    }
}
