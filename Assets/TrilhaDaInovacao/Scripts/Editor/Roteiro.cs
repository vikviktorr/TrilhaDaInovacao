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
        public const string PastaVozes = "Assets/TrilhaDaInovacao/Voz";

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
                "O lançamento aconteceu, mas uma anomalia levou à perda da missão."),
            ("contagem", "Five, four, three, two, one", "Cinco, quatro, três, dois, um"),
            ("siga", "Siga a luz no chão até a primeira marca.", "Um portal vai se abrir. Ele leva para 1986."),
            ("dica", "Dê um passo até a marca do radar no chão.", "Toque no botão vermelho do console para ligar o radar."),
            // O foguete não foi destruído no ar: caiu em pedaços e explodiu no solo (Innospace, FAB).
            // A rede metropolitana tinha 10 Gbit/s; 1 Gbps era a meta do link do IFMA para 2010 (IFMA, 2009).
            ("numeros", "São 43 quilômetros de fibra óptica ligando seis instituições a 1 Gbps.",
                "São 43 quilômetros de fibra óptica, a 10 Gbps, ligando seis instituições."),
            // Telebrasil (nov./dez. 1986): operação comercial em 13 de novembro, com 1.008 terminais.
            ("intro", "São Luís, 1986. A TELMA acaba de ativar a central Trópico-R.",
                "São Luís, novembro de 1986. A TELMA põe em operação a central Trópico-R, com 1.008 terminais."),
            // A lâmpada saiu da bancada: o desenho começa pela tela do computador.
            ("dica", "Encoste a mão na lâmpada sobre a bancada.", "Toque na tela do computador para desenhar a peça."),
            ("causa3", "O sistema de segurança destruiu o foguete no ar, para proteger quem estava em solo.", "Sem empuxo, o foguete se partiu em pedaços, que explodiram ao cair dentro do CLA.")
        };

        public static Dictionary<string, SceneScript> CriarOuAtualizar()
        {
            Directory.CreateDirectory(Pasta);
            var roteiros = new Dictionary<string, SceneScript>
            {
                [Geral] = Garantir(Geral, "Geral", "", 0f, "", new[]
                {
                    ("dica-andar", "Caminhe até a marca que está brilhando no chão."),
                    ("explorar", "Olhe tudo com calma. Quando quiser seguir, toque no botão Próximo."),
                    ("dica-proximo", "Quando quiser seguir viagem, toque no botão Próximo."),
                    ("dica-portal", "Atravesse o portal para viajar no tempo.")
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
                    "central de comutação totalmente digitalizada do Brasil (HOLANDA, 1996). Trópico R da PHT, 1.008 terminais, " +
                    "operação comercial em 13/11/1986 (Telebrasil, nov./dez. 1986); estação Telma-1, prefixo 232 (Telebrasil, jan./fev. 1987).", new[]
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
                        ("dica", "Toque no botão vermelho do console para ligar o radar."),
                        ("ionosfera", "Essa camada se chama ionosfera. Foguetes e instrumentos em terra a estudaram."),
                        ("fim", "O INPE e a NASA trabalharam juntos nessa missão. Próxima parada: 2009.")
                    }),

                [Nomes[3]] = Garantir(Nomes[3], "Rede Metropolitana de São Luís", "2009", 35f,
                    "RNP/Redecomep: em operação desde 23/10/2009, 43 km de fibra óptica a 10 Gbit/s ligando seis " +
                    "instituições: CLA, FAPEMA, IFMA, SEATI, UEMA e UFMA (IFMA, 2009; RNP, 2009).", new[]
                    {
                        ("intro", "São Luís, 2009. A RNP inaugura a Rede Metropolitana da cidade."),
                        ("numeros", "São 43 quilômetros de fibra óptica ligando seis instituições a 1 Gbps."),
                        ("dica", "Toque no ponto grande que pulsa no mapa."),
                        ("veja", "Veja os centros de pesquisa se conectarem pela fibra óptica."),
                        ("fim", "Com a rede pronta, as ideias circulam. Vamos ver uma delas virar objeto.")
                    }),

                [Nomes[4]] = Garantir(Nomes[4], "Laboratório Aberto do SENAI", "2018", 45f,
                    "SENAI inaugurou o Laboratório Aberto em São Luís em 2 de março de 2018 (FIEMA, 2018).", new[]
                    {
                        ("intro", "2 de março de 2018. O SENAI inaugura seu primeiro Laboratório Aberto no Maranhão."),
                        ("juntos", "Estudantes, pesquisadores e empreendedores trabalham juntos aqui."),
                        ("estudante", "Oi! Cada post-it nessa parede é uma ideia da turma."),
                        ("dica", "Encoste a mão na lâmpada sobre a bancada."),
                        ("ideia", "Aqui, uma ideia pode ganhar forma de protótipo."),
                        ("pesquisador", "A impressora 3D monta a peça camada por camada."),
                        ("dica-impressora", "O desenho está pronto. Aperte o botão no painel da impressora para imprimir."),
                        ("empreendedora", "Com o protótipo pronto, dá para testar e virar negócio!"),
                        ("fim", "Pronto! Agora, vamos acelerar a conexão da cidade.")
                    }),

                [Nomes[5]] = Garantir(Nomes[5], "5G em São Luís", "2022", 35f,
                    "Anatel autorizou estações na faixa de 3,5 GHz em São Luís a partir de 19 set. 2022 " +
                    "(ANATEL, 2022). Bairros com sinal no lançamento: TIM 25 (43,9% da população), Claro 9, Vivo 7 " +
                    "(Jornal Pequeno, 19 set. 2022).", new[]
                    {
                        ("intro", "São Luís, setembro de 2022. O 5G chega à cidade."),
                        ("dica", "Aproxime-se da torre."),
                        ("anatel", "A Anatel liberou a faixa de 3,5 GHz a partir de 19 de setembro."),
                        ("expansao", "A nova faixa permitiu a expansão das redes móveis na cidade."),
                        ("fim", "Agora, a última parada: de volta a Alcântara.")
                    }),

                [Nomes[6]] = Garantir(Nomes[6], "Lançamentos em Alcântara", "2023", 125f,
                    "HANBIT-TLV (Innospace) levou o SISNAV em 2023 (AEB, 2024). HANBIT-Nano, 22/12/2025, 22h13: primeiro " +
                    "lançamento comercial do Brasil, com o PION-BR2 (UFMA). Subida normal por ~30 s; aos 33 s, vazamento na vedação " +
                    "da câmara de combustão do 1º estágio, que rompeu (Innospace, AEB e CENIPA, 2026); sem empuxo, o foguete se partiu " +
                    "em 3 ou 4 partes e explodiu ao cair na zona de segurança do CLA (Innospace; FAB).", new[]
                    {
                        ("intro2023", "2023. O foguete HANBIT-TLV, da Innospace, sai de Alcântara."),
                        ("sisnav", "Dentro dele, o SISNAV, sistema brasileiro de navegação, foi levado para testes."),
                        ("intro2025", "2025. O HANBIT-Nano fará o primeiro lançamento comercial a partir do Brasil."),
                        ("pion", "Entre as cargas está o PION-BR2, ligado à UFMA..."),
                        ("decolar", "Nossa, o foguete vai decolar agora!"),
                        ("contagem", "Cinco, quatro, três, dois, um"),
                        ("anomalia", "Aos 33 segundos, um clarão dentro das nuvens. Algo deu errado!"),
                        ("queda", "Os pedaços estão caindo na zona de segurança, longe das pessoas."),
                        ("resultado", "O lançamento aconteceu, mas uma anomalia levou à perda da missão."),
                        ("causa", "Aos 33 segundos de voo, uma vedação do motor do primeiro estágio falhou."),
                        ("causa2", "O gás quente vazou e a câmara de combustão rompeu."),
                        ("causa3", "O sistema de segurança destruiu o foguete no ar, para proteger quem estava em solo."),
                        ("investigacao", "A investigação com o CENIPA achou vedações deformadas na montagem."),
                        ("licao", "Agora, a montagem do motor tem mais controle e testes."),
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

            foreach (var roteiro in roteiros.Values) LigarVozes(roteiro);
            AssetDatabase.SaveAssets();
            return roteiros;
        }

        /// <summary>
        /// Liga cada fala à voz gerada por Ferramentas/voz/gerar_vozes.py
        /// (Voz/&lt;roteiro&gt;/&lt;id&gt;.ogg). Uma voz gravada pela equipe e
        /// arrastada para o campo "audio" de outra pasta nunca é trocada.
        /// </summary>
        static void LigarVozes(SceneScript roteiro)
        {
            foreach (var fala in roteiro.falas)
            {
                if (fala == null) continue;
                var atual = fala.audio != null ? AssetDatabase.GetAssetPath(fala.audio) : null;
                if (atual != null && !atual.StartsWith(PastaVozes)) continue;
                var gerada = AssetDatabase.LoadAssetAtPath<AudioClip>($"{PastaVozes}/{roteiro.name}/{fala.id}.ogg");
                if (gerada == fala.audio) continue;
                fala.audio = gerada;
                EditorUtility.SetDirty(roteiro);
            }
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
