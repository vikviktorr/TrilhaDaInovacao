using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Mapa físico da trilha (seção "Mapa geral" do roteiro de produção): um U
    /// com a marca de início e seis marcas de época no piso do Sebraelab.
    /// Runtime e construtor de cenas leem daqui, então mover uma marca aqui
    /// move a marca física, o gatilho e o cenário juntos.
    /// </summary>
    public static class LayoutTrilha
    {
        // PROVISÓRIO: medir a área livre do Sebraelab e ajustar.
        public const float LarguraSala = 5f;
        public const float ProfundidadeSala = 4f;

        /// <summary>
        /// Ponto do modelo 3D do Sebraelab (Assets/Sebraelab3D/SM_Combined, em
        /// metros no referencial do modelo importado) que fica no centro da área
        /// livre, ou seja, na origem da trilha. Hoje é o salão norte: o visitante
        /// começa de frente para a mesa de madeira, com a TV e as lousas ao fundo.
        /// O U ocupa x -2,2…2,8 e z 6,5…10,5 do modelo, livre entre a parede de
        /// ripas (oeste), o banco junto ao vidro (x ≥ 2,97) e a mesa (z ≥ 10,62).
        /// </summary>
        public static readonly Vector3 CentroNoSebraelab = new Vector3(0.3f, 0f, 8.5f);

        /// <summary>Onde o guia espera na abertura: atrás da mesa de madeira, entre ela e as lousas.</summary>
        public static readonly Vector3 GuiaNoSebraelab = new Vector3(-0.22f, 0f, 11.76f);

        /// <summary>Converte um ponto do modelo do Sebraelab para o referencial da trilha.</summary>
        public static Vector3 DoSebraelab(Vector3 pontoNoModelo) => pontoNoModelo - CentroNoSebraelab;

        /// <summary>Camada dos colisores do cenário (paredes, móveis), usados no modo desktop.</summary>
        public const int CamadaCenario = 6;

        /// <summary>Camada do túnel do tempo: durante a viagem a câmera desenha só ela.</summary>
        public const int CamadaTunel = 7;

        /// <summary>Raio em que a cabeça do visitante conta como "na marca".</summary>
        public const float RaioMarca = 0.6f;

        public const int Inicio = 0;
        public const int TotalEpocas = 6;

        /// <summary>0 = início; 1 a 6 = épocas em ordem cronológica.</summary>
        public static readonly Vector3[] Marcas =
        {
            new Vector3(-2.1f, 0f, -1f),   // início
            new Vector3(-0.7f, 0f, -1f),   // 1986
            new Vector3(0.7f, 0f, -1f),    // 1994
            new Vector3(2.1f, 0f, -1f),    // 2009
            new Vector3(2.1f, 0f, 0.75f),  // 2018
            new Vector3(0.7f, 0f, 0.75f),  // 2022
            new Vector3(-0.7f, 0f, 0.75f)  // 2023–2025 + encerramento
        };

        /// <summary>Onde o monitor da equipe recebe o visitante no fim.</summary>
        public static readonly Vector3 Saida = new Vector3(-2.1f, 0f, 0.75f);

        public static readonly string[] Anos = { "", "1986", "1994", "2009", "2018", "2022", "2023" };

        public static readonly string[] Titulos =
        {
            "Abertura",
            "Central TELMA",
            "Campanha Guará",
            "Rede Metropolitana",
            "Laboratório Aberto SENAI",
            "5G em São Luís",
            "Lançamentos em Alcântara"
        };

        public const string CenaBootstrap = "00_Bootstrap";
        public const string CenaHub = "01_Hub";

        /// <summary>Cena aditiva de cada marca (0 = hub).</summary>
        public static readonly string[] Cenas =
        {
            CenaHub,
            "02_1986_Telma",
            "03_1994_Guara",
            "04_2009_Fibra",
            "05_2018_Senai",
            "06_2022_5G",
            "07_2025_Alcantara"
        };

        /// <summary>
        /// Para onde o visitante olha em cada marca: sempre para dentro do U,
        /// área que ninguém percorre. Assim a próxima marca fica à direita e
        /// guia e objetos de toque nunca ficam no caminho.
        /// </summary>
        public static Vector3 Foco(int marca)
        {
            if (marca == 3) return Vector3.left;          // 2009: o caminho sobe para o norte
            return marca <= 2 ? Vector3.forward : Vector3.back;
        }

        public static Quaternion Orientacao(int marca) => Quaternion.LookRotation(Foco(marca), Vector3.up);

        /// <summary>
        /// Ponto no referencial da marca: x = direita (lado da próxima marca),
        /// y = altura, z = frente (foco).
        /// </summary>
        public static Vector3 Local(int marca, float direita, float altura, float frente)
            => Marcas[marca] + Orientacao(marca) * new Vector3(direita, altura, frente);

        public static Vector2 PlanoXZ(Vector3 v) => new Vector2(v.x, v.z);
    }
}
