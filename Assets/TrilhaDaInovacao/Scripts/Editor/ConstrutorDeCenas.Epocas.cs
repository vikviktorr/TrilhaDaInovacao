using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TrilhaDaInovacao.EditorTools
{
    /// <summary>
    /// Cenários das seis épocas (seção "Cenários" do documento de produção).
    /// Tudo é montado no referencial da marca: x = direita (lado da próxima
    /// marca), y = altura, z = frente (para dentro do U). O visitante está na
    /// origem; o que é interativo fica a até ~0,6 m, entre 0,9 e 1,4 m de altura.
    /// </summary>
    public static partial class ConstrutorDeCenas
    {
        // ==================================================================
        // Cena 1 — 1986, Central Trópico-R da TELMA
        // ==================================================================
        static string Construir1986()
        {
            var cena = NovaCena();
            Ambiente(new Color(0.42f, 0.42f, 0.38f), Ceu("CeuInterno", new Color(0.4f, 0.4f, 0.4f), 0.6f, 0.5f),
                new Color(0.95f, 1f, 0.95f), 0.45f, new Vector3(70f, 20f, 0f));

            var q = Quadro(1, "Central TELMA · 1986");
            SalaFechada(q, new Vector3(0.6f, 0f, 1f), new Vector3(6f, 2.9f, 6f),
                Opaco("1986Piso", new Color(0.72f, 0.66f, 0.52f), smooth: 0.35f),
                Opaco("1986Parede", new Color(0.62f, 0.64f, 0.52f)),
                Opaco("1986Teto", new Color(0.85f, 0.85f, 0.8f)));

            var fluorescente = Opaco("1986Fluorescente", Color.white, new Color(0.9f, 1f, 0.95f) * 1.6f);
            for (var i = 0; i < 3; i++)
                Bloco($"Fluorescente {i}", q, new Vector3(-1.2f + i * 1.4f, 2.86f, 1.2f), new Vector3(0.18f, 0.03f, 2.4f), fluorescente);
            Luz("Luz fluorescente A", q, new Vector3(-0.4f, 2.5f, 0.6f), LightType.Point, new Color(0.92f, 1f, 0.95f), 1.4f, 6f);
            Luz("Luz fluorescente B", q, new Vector3(0.8f, 2.5f, 2.2f), LightType.Point, new Color(0.92f, 1f, 0.95f), 1.2f, 6f);

            // Bastidores da central, com LEDs.
            var rackMat = Opaco("1986Rack", new Color(0.42f, 0.48f, 0.42f), smooth: 0.4f, metal: 0.3f);
            var ledMat = Opaco("1986Led", new Color(0.05f, 0.08f, 0.05f), new Color(0.2f, 1f, 0.35f));
            var bastidores = new List<PiscaLeds>();
            for (var i = 0; i < 5; i++)
            {
                var x = -1.6f + i * 0.8f;
                var rack = Bloco($"Bastidor {i + 1}", q, new Vector3(x, 1f, 2.3f), new Vector3(0.6f, 2f, 0.5f), rackMat);
                var grupos = new[] { new List<GameObject>(), new List<GameObject>() };
                for (var c = 0; c < 3; c++)
                for (var l = 0; l < 8; l++)
                {
                    grupos[(c + l) % 2].Add(Bloco($"LED {c}-{l}", q, new Vector3(x - 0.15f + c * 0.15f, 0.7f + l * 0.16f, 2.045f),
                        new Vector3(0.05f, 0.03f, 0.01f), ledMat));
                }

                // Cada grupo vira uma malha só (2 draw calls por bastidor em vez de 24).
                // O controle fica num objeto à parte para o bastidor poder ser mesclado.
                var controle = new GameObject($"Controle dos LEDs {i + 1}");
                controle.transform.SetParent(q, false);
                controle.transform.localPosition = rack.transform.localPosition;
                var pisca = controle.AddComponent<PiscaLeds>();
                pisca.leds = new[]
                {
                    Combinar($"LEDs {i + 1}A", q, grupos[0], ledMat),
                    Combinar($"LEDs {i + 1}B", q, grupos[1], ledMat)
                };
                bastidores.Add(pisca);
            }

            Bloco("Calha de cabos", q, new Vector3(0f, 2.45f, 2.3f), new Vector3(4f, 0.08f, 0.3f), Opaco("1986Calha", new Color(0.3f, 0.3f, 0.3f)));
            Bloco("Placa", q, new Vector3(0f, 2.22f, 2.04f), new Vector3(2.4f, 0.26f, 0.02f), Opaco("1986Placa", new Color(0.08f, 0.15f, 0.35f)));
            Texto("Texto da placa", q, new Vector3(0f, 2.22f, 2.025f), "TELMA · Central Trópico-R", 0.13f, 2.3f, Color.white);

            // Balcão com o telefone de origem.
            Bloco("Balcão", q, new Vector3(-0.1f, 0.44f, 0.78f), new Vector3(0.9f, 0.88f, 0.5f), Opaco("1986Madeira", new Color(0.45f, 0.3f, 0.18f)));
            var (telefone, baseTelefone) = Telefone("Telefone", q, new Vector3(-0.1f, 0.88f, 0.62f));
            var col = telefone.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.08f, 0f);
            col.size = new Vector3(0.3f, 0.22f, 0.26f);
            var poke = telefone.AddComponent<PokeTrigger>();
            poke.ConfigurarNoEditor(telefone.GetComponentsInChildren<Renderer>(), new Color(1f, 0.85f, 0.4f));
            var brilho = AdicionarPulsar(baseTelefone, new Color(1f, 0.7f, 0.2f), 1.2f, 2.5f);
            var somTelefone = telefone.AddComponent<AudioSource>();
            somTelefone.playOnAwake = false;
            somTelefone.spatialBlend = 1f;

            // Painel com o mapa simplificado de São Luís.
            Bloco("Painel mapa", q, new Vector3(2.5f, 1.55f, 2.3f), new Vector3(0.9f, 0.7f, 0.05f), Opaco("1986Mapa", new Color(0.12f, 0.2f, 0.3f)));
            Texto("Título mapa", q, new Vector3(2.5f, 1.97f, 2.27f), "São Luís", 0.07f, 0.9f, Color.white);
            var pontoA = new Vector3(2.28f, 1.42f, 2.26f);
            var pontoB = new Vector3(2.7f, 1.7f, 2.26f);
            var pontoMat = Opaco("1986PontoMapa", Color.yellow, new Color(1f, 0.85f, 0.2f));
            Bloco("Ponto A", q, pontoA, Vector3.one * 0.05f, pontoMat, PrimitiveType.Sphere);
            Bloco("Ponto B", q, pontoB, Vector3.one * 0.05f, pontoMat, PrimitiveType.Sphere);

            // Telefone de destino, numa mesa à direita.
            Bloco("Mesa", q, new Vector3(2.1f, 0.4f, 1f), new Vector3(0.6f, 0.8f, 0.5f), Opaco("1986Madeira", Color.white));
            var (_, baseDestino) = Telefone("Telefone de destino", q, new Vector3(2.1f, 0.8f, 0.95f));
            var brilhoDestino = AdicionarPulsar(baseDestino, new Color(1f, 0.7f, 0.2f), 1.2f, 2.5f);
            var somDestino = Fonte("Som do destino", q, q.TransformPoint(new Vector3(2.1f, 0.9f, 0.95f)), true);

            // O pulso da ligação: telefone → bastidores → mapa → telefone de destino.
            var caminhoLocal = new[]
            {
                new Vector3(-0.1f, 1f, 0.62f), new Vector3(-0.1f, 1.4f, 1.95f), new Vector3(-1.6f, 1.4f, 1.95f),
                new Vector3(1.6f, 1.4f, 1.95f), pontoA + new Vector3(0f, 0f, -0.05f), pontoB + new Vector3(0f, 0f, -0.05f),
                new Vector3(2.1f, 1.3f, 0.95f), new Vector3(2.1f, 0.95f, 0.95f)
            };
            var caminho = new Vector3[caminhoLocal.Length];
            for (var i = 0; i < caminho.Length; i++) caminho[i] = q.TransformPoint(caminhoLocal[i]);

            var pulsoGo = new GameObject("Pulso da ligação");
            pulsoGo.transform.SetParent(q, false);
            var bola = Bloco("Pulso", pulsoGo.transform, Vector3.zero, Vector3.one * 0.06f,
                Opaco("Pulso", Color.white, new Color(0.3f, 0.9f, 1f) * 4f), PrimitiveType.Sphere);
            var rastro = bola.AddComponent<TrailRenderer>();
            rastro.time = 0.7f;
            rastro.widthMultiplier = 0.04f;
            rastro.sharedMaterial = LinhaSolida("Linha");
            rastro.startColor = new Color(0.4f, 0.95f, 1f);
            rastro.endColor = new Color(0.1f, 0.3f, 0.5f, 0f);
            Luz("Brilho do pulso", bola.transform, Vector3.zero, LightType.Point, new Color(0.4f, 0.9f, 1f), 2f, 1.2f);
            var pulso = pulsoGo.AddComponent<PulsoNoCaminho>();
            pulso.bola = bola.transform;
            pulso.rastro = rastro;
            pulso.caminho = caminho;
            pulso.bastidores = bastidores.ToArray();

            var ambiente = Fonte("Zumbido da central", q, q.TransformPoint(new Vector3(0f, 1.5f, 2f)), true);

            Decorar1986(q);

            var seq = Sequencia<Cena1Telma>("Cena 1 · 1986 TELMA", 1, 1);
            seq.telefone = poke;
            seq.brilhoTelefone = brilho;
            seq.somTelefone = somTelefone;
            seq.ligacao = pulso;
            seq.brilhoTelefoneDestino = brilhoDestino;
            seq.somTelefoneDestino = somDestino;
            seq.ambiente = ambiente;

            return Salvar(cena, LayoutTrilha.Cenas[1]);
        }

        /// <summary>Telefone de mesa dos anos 80: base, disco e fone.</summary>
        static (GameObject raiz, GameObject baseTelefone) Telefone(string nome, Transform pai, Vector3 posicaoLocal)
        {
            var raiz = new GameObject(nome);
            raiz.transform.SetParent(pai, false);
            raiz.transform.localPosition = posicaoLocal;
            var bege = Opaco("1986Telefone", new Color(0.82f, 0.74f, 0.58f), smooth: 0.5f);
            var escuro = Opaco("1986TelefoneEscuro", new Color(0.25f, 0.2f, 0.16f), smooth: 0.5f);
            var baseT = Bloco("Base", raiz.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.24f, 0.08f, 0.2f), bege);
            Bloco("Disco", raiz.transform, new Vector3(0f, 0.085f, -0.03f), new Vector3(0.12f, 0.006f, 0.12f), escuro,
                PrimitiveType.Cylinder, new Vector3(-15f, 0f, 0f));
            Bloco("Fone", raiz.transform, new Vector3(0f, 0.11f, 0.05f), new Vector3(0.06f, 0.12f, 0.06f), bege,
                PrimitiveType.Capsule, new Vector3(0f, 0f, 90f));
            return (raiz, baseT);
        }

        // ==================================================================
        // Cena 2 — 1994, Campanha Guará em Alcântara
        // ==================================================================
        static string Construir1994()
        {
            var cena = NovaCena();
            Ambiente(new Color(0.55f, 0.42f, 0.4f),
                Ceu("CeuEntardecer", new Color(1f, 0.55f, 0.35f), 1.3f, 1.7f, new Color(0.3f, 0.24f, 0.2f), 0.07f),
                new Color(1f, 0.62f, 0.38f), 1.1f, new Vector3(9f, 160f, 0f), true, new Color(0.85f, 0.58f, 0.45f), 0.006f);

            var q = Quadro(2, "Alcântara · 1994");
            Bloco("Terreno", q, new Vector3(0f, -0.06f, 0f), new Vector3(160f, 0.1f, 160f), Opaco("1994Terreno", new Color(0.46f, 0.47f, 0.3f)));
            Bloco("Mar", q, new Vector3(0f, -0.04f, 80f), new Vector3(260f, 0.1f, 80f), Opaco("Mar", new Color(0.14f, 0.28f, 0.42f), smooth: 0.9f));
            var madeira = Opaco("1994Deck", new Color(0.5f, 0.36f, 0.24f));
            Bloco("Deck de observação", q, new Vector3(0f, -0.02f, 0.8f), new Vector3(4f, 0.04f, 3.6f), madeira);
            for (var i = 0; i <= 8; i++)
                Bloco($"Poste {i}", q, new Vector3(-2f + i * 0.5f, 0.5f, 2.55f), new Vector3(0.05f, 1f, 0.05f), madeira);
            Bloco("Corrimão", q, new Vector3(0f, 1f, 2.55f), new Vector3(4.05f, 0.05f, 0.07f), madeira);

            // Restinga, palmeiras e antenas (modelos Kenney).
            Decorar1994(q);

            // Marca do radar no piso.
            var marcaGo = new GameObject("Marca do radar");
            marcaGo.transform.SetParent(q, false);
            marcaGo.transform.localPosition = new Vector3(0f, 0.01f, 0.6f);
            var discoRadar = Bloco("Disco", marcaGo.transform, Vector3.zero, new Vector3(0.6f, 0.004f, 0.6f),
                Opaco("1994MarcaRadar", new Color(0.3f, 0.2f, 0.1f), new Color(0.6f, 0.3f, 0.05f)), PrimitiveType.Cylinder);
            var anel = Linha("Anel", marcaGo.transform, Circulo(0.3f, 48, 0.01f), 0.025f, new Color(1f, 0.6f, 0.2f), true);
            anel.loop = true;
            var rotuloMarca = Texto("RADAR", marcaGo.transform, new Vector3(0f, 0.012f, 0f), "RADAR", 0.08f, 0.6f, Color.white);
            rotuloMarca.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var proximidade = marcaGo.AddComponent<ProximityTrigger>();
            proximidade.ConfigurarNoEditor(0.3f);
            var brilhoMarca = AdicionarPulsar(discoRadar, new Color(1f, 0.55f, 0.15f), 2f, 1.5f);

            // Antena do radar com o feixe que varre o céu.
            var antenaBase = new GameObject("Radar").transform;
            antenaBase.SetParent(q, false);
            antenaBase.localPosition = new Vector3(2.4f, 0f, 2.9f);
            var metal = Opaco("1994Metal", new Color(0.75f, 0.76f, 0.78f), smooth: 0.6f, metal: 0.4f);
            // O radar inteiro gira sobre a base (modelo satelliteDish_large).
            var giro = new GameObject("Giro").transform;
            giro.SetParent(antenaBase, false);
            Modelo("Kenney/space-kit/satelliteDish_large", giro, Vector3.zero, 180f, 1.9f, Medida.MaiorLado);
            var girar = giro.gameObject.AddComponent<Girar>();
            girar.grausPorSegundo = 60f;
            girar.ativo = false;
            var inclinacao = new GameObject("Inclinação").transform;
            inclinacao.SetParent(giro, false);
            inclinacao.localPosition = new Vector3(0f, 1.5f, 0f);
            inclinacao.localRotation = Quaternion.Euler(-55f, 0f, 0f);
            var feixe = Bloco("Feixe", inclinacao, new Vector3(0f, 0f, 12f), new Vector3(0.6f, 12f, 0.6f),
                Transparente("1994Feixe", new Color(0.4f, 1f, 0.6f, 0.12f), true), PrimitiveType.Cylinder, new Vector3(90f, 0f, 0f));
            var bipe = Fonte("Bipe do radar", q, antenaBase.position + Vector3.up * 2f, true);
            feixe.SetActive(false);

            // Ionosfera: faixa translúcida alta no céu.
            var ionosfera = Bloco("Ionosfera", q, new Vector3(0f, 45f, 40f), new Vector3(320f, 320f, 1f),
                Transparente("1994Ionosfera", new Color(0.65f, 0.45f, 1f, 0.22f), true), PrimitiveType.Quad, new Vector3(90f, 0f, 0f));
            var rotuloIonosfera = Texto("Rótulo ionosfera", q, new Vector3(0f, 28f, 32f), "IONOSFERA", 2.6f, 40f,
                new Color(0.9f, 0.8f, 1f)).gameObject;
            rotuloIonosfera.AddComponent<EncararUsuario>();
            ionosfera.SetActive(false);
            rotuloIonosfera.SetActive(false);

            // Foguete de sondagem na rampa, ao longe, e a trajetória até a ionosfera.
            var rampa = new Vector3(-5f, 0f, 16f);
            Bloco("Trilho de lançamento", q, rampa + new Vector3(0.35f, 3f, 0f), new Vector3(0.2f, 6f, 0.2f), metal, rotacao: new Vector3(-8f, 0f, 0f));
            Bloco("Foguete de sondagem", q, rampa + new Vector3(0f, 2.8f, 0f), new Vector3(0.35f, 2.4f, 0.35f),
                Opaco("FogueteBranco", new Color(0.95f, 0.95f, 0.95f)), PrimitiveType.Capsule, new Vector3(-8f, 0f, 0f));
            var trajetoriaGo = new GameObject("Trajetória");
            trajetoriaGo.transform.SetParent(q, false);
            var pontos = new Vector3[32];
            var inicio = rampa + new Vector3(0f, 5.2f, 0.7f);
            var controle = new Vector3(-5f, 38f, 22f);
            var fim = new Vector3(-1f, 45f, 46f);
            for (var i = 0; i < pontos.Length; i++)
            {
                var t = i / (pontos.Length - 1f);
                pontos[i] = (1 - t) * (1 - t) * inicio + 2 * (1 - t) * t * controle + t * t * fim;
            }

            var linhaTrajetoria = trajetoriaGo.AddComponent<LineRenderer>();
            linhaTrajetoria.widthMultiplier = 0.35f;
            linhaTrajetoria.sharedMaterial = LinhaSolida("Linha");
            linhaTrajetoria.startColor = new Color(1f, 0.9f, 0.4f);
            linhaTrajetoria.endColor = new Color(1f, 0.5f, 0.9f);
            var trajetoria = trajetoriaGo.AddComponent<LinhaProgressiva>();
            trajetoria.pontos = pontos;
            var pontoNoCeu = new GameObject("Ponto no céu").transform;
            pontoNoCeu.SetParent(q, false);
            pontoNoCeu.localPosition = new Vector3(-2f, 22f, 30f);

            // Mesa de instrumentos e bandeiras INPE / NASA.
            Modelo("Kenney/space-kit/desk_computer", q, new Vector3(-1.7f, 0f, 2.0f), 180f, 1.0f, Medida.MaiorLado);

            Bandeira(q, new Vector3(-2.25f, 0f, 2.35f), true);
            Bandeira(q, new Vector3(-1.45f, 0f, 2.35f), false);

            var ambiente = Fonte("Vento e mar", q, q.TransformPoint(new Vector3(0f, 2f, 3f)), false);

            var seq = Sequencia<Cena2Guara>("Cena 2 · 1994 Guará", 2, 2);
            seq.marcaRadar = proximidade;
            seq.brilhoMarca = brilhoMarca;
            seq.antena = girar;
            seq.feixe = feixe;
            seq.ionosfera = ionosfera.transform;
            seq.rotuloIonosfera = rotuloIonosfera;
            seq.trajetoria = trajetoria;
            seq.pontoNoCeu = pontoNoCeu;
            seq.ambiente = ambiente;
            seq.bipe = bipe;

            return Salvar(cena, LayoutTrilha.Cenas[2]);
        }

        static void Bandeira(Transform pai, Vector3 posicao, bool brasil)
        {
            var raiz = new GameObject(brasil ? "Bandeira do Brasil (INPE)" : "Bandeira dos EUA (NASA)").transform;
            raiz.SetParent(pai, false);
            raiz.localPosition = posicao;
            Bloco("Mastro", raiz, new Vector3(-0.35f, 1.1f, 0f), new Vector3(0.03f, 1.1f, 0.03f), Opaco("Mastro", new Color(0.8f, 0.8f, 0.8f)), PrimitiveType.Cylinder);

            if (brasil)
            {
                Bloco("Verde", raiz, new Vector3(0f, 1.9f, 0f), new Vector3(0.66f, 0.46f, 1f), Opaco("BRVerde", new Color(0f, 0.6f, 0.2f)), PrimitiveType.Quad);
                Bloco("Losango", raiz, new Vector3(0f, 1.9f, -0.002f), new Vector3(0.38f, 0.38f, 1f), Opaco("BRAmarelo", new Color(1f, 0.85f, 0f)),
                    PrimitiveType.Quad, new Vector3(0f, 0f, 45f)).transform.localScale = new Vector3(0.3f, 0.3f, 1f);
                Bloco("Círculo", raiz, new Vector3(0f, 1.9f, -0.004f), new Vector3(0.2f, 0.001f, 0.2f), Opaco("BRAzul", new Color(0.1f, 0.15f, 0.5f)),
                    PrimitiveType.Cylinder, new Vector3(90f, 0f, 0f));
            }
            else
            {
                var vermelho = Opaco("USVermelho", new Color(0.75f, 0.1f, 0.15f));
                var branco = Opaco("USBranco", Color.white);
                for (var i = 0; i < 7; i++)
                {
                    Bloco($"Listra {i}", raiz, new Vector3(0f, 2.1f - i * 0.066f, 0f), new Vector3(0.66f, 0.066f, 1f),
                        i % 2 == 0 ? vermelho : branco, PrimitiveType.Quad);
                }

                Bloco("Cantão", raiz, new Vector3(-0.18f, 2.02f, -0.002f), new Vector3(0.3f, 0.23f, 1f), Opaco("USAzul", new Color(0.1f, 0.15f, 0.4f)), PrimitiveType.Quad);
            }

            Texto("Legenda", raiz, new Vector3(0f, 1.52f, 0f), brasil ? "INPE · Brasil" : "NASA · EUA", 0.06f, 0.8f, Color.white);
        }

        // ==================================================================
        // Cena 3 — 2009, Rede Metropolitana de São Luís
        // ==================================================================
        static string Construir2009()
        {
            var cena = NovaCena();
            Ambiente(new Color(0.1f, 0.12f, 0.18f), Ceu("CeuInterno", Color.gray, 0.6f, 0.5f),
                new Color(0.5f, 0.6f, 0.9f), 0.25f, new Vector3(70f, 0f, 0f));

            var q = Quadro(3, "Rede Metropolitana · 2009");
            var escuro = Opaco("2009Parede", new Color(0.07f, 0.09f, 0.13f));
            SalaFechada(q, new Vector3(0f, 0f, 1f), new Vector3(6f, 2.8f, 5.5f), Opaco("2009Piso", new Color(0.1f, 0.12f, 0.16f), smooth: 0.5f), escuro, escuro);
            Luz("Luz da mesa", q, new Vector3(0f, 2.4f, 1f), LightType.Point, new Color(0.5f, 0.75f, 1f), 1.6f, 5f);

            // Mesa-mapa com a ilha de São Luís.
            var tampo = Opaco("2009Tampo", new Color(0.03f, 0.06f, 0.12f), new Color(0.01f, 0.03f, 0.08f), smooth: 0.8f);
            Bloco("Tampo", q, new Vector3(0f, 0.85f, 1.15f), new Vector3(2f, 0.05f, 1.2f), tampo);
            var perna = Opaco("2009Perna", new Color(0.2f, 0.22f, 0.26f));
            foreach (var x in new[] { -0.9f, 0.9f })
            foreach (var z in new[] { 0.65f, 1.65f })
                Bloco("Perna", q, new Vector3(x, 0.42f, z), new Vector3(0.06f, 0.84f, 0.06f), perna);

            var centroIlha = new Vector3(0f, 0.878f, 1.22f);
            var ilha = new GameObject("Ilha de São Luís (estilizada)");
            ilha.transform.SetParent(q, false);
            ilha.transform.localPosition = centroIlha;
            ilha.AddComponent<MeshFilter>().sharedMesh = MalhaIlha(0.78f, 0.46f);
            var ilhaR = ilha.AddComponent<MeshRenderer>();
            ilhaR.sharedMaterial = Opaco("2009Ilha", new Color(0.12f, 0.32f, 0.36f), new Color(0.02f, 0.12f, 0.14f));
            ilhaR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Os seis pinos (instituições) e o ponto grande para tocar.
            var ponto = new Vector3(0f, 0.96f, 0.62f);
            var cabecaPino = Opaco("2009Pino", new Color(0.6f, 0.7f, 0.8f), new Color(0.05f, 0.08f, 0.1f));
            var hasteMat = Opaco("2009Haste", new Color(0.7f, 0.7f, 0.75f));
            var pinos = new List<Renderer>();
            var feixes = new List<LinhaProgressiva>();
            for (var i = 0; i < 6; i++)
            {
                var a = (i * 60f + 20f) * Mathf.Deg2Rad;
                var baseP = centroIlha + new Vector3(Mathf.Cos(a) * 0.48f, 0f, Mathf.Sin(a) * 0.27f);
                Bloco($"Haste {i + 1}", q, baseP + Vector3.up * 0.06f, new Vector3(0.01f, 0.06f, 0.01f), hasteMat, PrimitiveType.Cylinder);
                var cabeca = Bloco($"Pino {i + 1}", q, baseP + Vector3.up * 0.13f, Vector3.one * 0.045f, cabecaPino, PrimitiveType.Sphere);
                pinos.Add(cabeca.GetComponent<Renderer>());

                var feixeGo = new GameObject($"Feixe {i + 1}");
                feixeGo.transform.SetParent(q, false);
                var lr = feixeGo.AddComponent<LineRenderer>();
                lr.widthMultiplier = 0.012f;
                lr.sharedMaterial = LinhaSolida("Linha");
                lr.startColor = new Color(0.6f, 1f, 1f);
                lr.endColor = new Color(0.2f, 0.8f, 1f);
                var alvo = baseP + Vector3.up * 0.13f;
                var arco = new Vector3[14];
                for (var k = 0; k < arco.Length; k++)
                {
                    var t = k / (arco.Length - 1f);
                    arco[k] = Vector3.Lerp(ponto, alvo, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.12f);
                }

                var progressiva = feixeGo.AddComponent<LinhaProgressiva>();
                progressiva.pontos = arco;
                feixes.Add(progressiva);
            }

            var pontoGo = Bloco("Ponto da rede", q, ponto, Vector3.one * 0.16f,
                Opaco("2009Ponto", new Color(0.3f, 0.9f, 1f), new Color(0.1f, 0.5f, 0.6f)), PrimitiveType.Sphere);
            pontoGo.AddComponent<SphereCollider>().radius = 0.65f;
            var poke = pontoGo.AddComponent<PokeTrigger>();
            poke.ConfigurarNoEditor(new[] { pontoGo.GetComponent<Renderer>() }, new Color(0.5f, 1f, 1f));
            var brilhoPonto = AdicionarPulsar(pontoGo, new Color(0.4f, 1f, 1f), 1.5f, 1.2f, 0.12f);

            Texto("Título", q, new Vector3(0f, 1.78f, 1.7f), "Rede Metropolitana de São Luís · RNP", 0.065f, 2f, new Color(0.7f, 0.9f, 1f));
            var contador = Texto("Contador", q, new Vector3(0f, 1.6f, 1.7f), "0 km", 0.13f, 2f, Color.white);
            contador.fontStyle = FontStyles.Bold;

            // Rack de rede ao fundo.
            var rack = Bloco("Rack de rede", q, new Vector3(1.9f, 1f, 3.3f), new Vector3(0.6f, 2f, 0.6f), Opaco("2009Rack", new Color(0.12f, 0.12f, 0.14f), metal: 0.5f));
            var ledAzul = Opaco("2009Led", Color.black, new Color(0.2f, 0.6f, 1f));
            var gruposRede = new[] { new List<GameObject>(), new List<GameObject>(), new List<GameObject>() };
            for (var l = 0; l < 10; l++)
            for (var c = 0; c < 4; c++)
                gruposRede[(c + l) % 3].Add(Bloco("LED", q, new Vector3(1.75f + c * 0.1f, 0.5f + l * 0.14f, 2.995f), new Vector3(0.04f, 0.02f, 0.01f), ledAzul));
            var controleRede = new GameObject("Controle dos LEDs");
            controleRede.transform.SetParent(q, false);
            controleRede.transform.localPosition = rack.transform.localPosition;
            var pisca = controleRede.AddComponent<PiscaLeds>();
            pisca.leds = new[]
            {
                Combinar("LEDs A", q, gruposRede[0], ledAzul),
                Combinar("LEDs B", q, gruposRede[1], ledAzul),
                Combinar("LEDs C", q, gruposRede[2], ledAzul)
            };
            pisca.cor = new Color(0.3f, 0.7f, 1f);

            var ambiente = Fonte("Servidores", q, q.TransformPoint(new Vector3(1.9f, 1f, 3f)), true);
            var sons = Fonte("Conexões", q, q.TransformPoint(ponto), false);

            Decorar2009(q);

            var seq = Sequencia<Cena3Fibra>("Cena 3 · 2009 Fibra", 3, 3);
            seq.ponto = poke;
            seq.brilhoPonto = brilhoPonto;
            seq.feixes = feixes.ToArray();
            seq.pinos = pinos.ToArray();
            seq.contador = contador;
            seq.ambiente = ambiente;
            seq.sons = sons;

            return Salvar(cena, LayoutTrilha.Cenas[3]);
        }

        /// <summary>
        /// Contorno estilizado da ilha: elipse com recortes suaves (não é um mapa
        /// cartográfico). Salvo como asset para a cena referenciar.
        /// </summary>
        static Mesh MalhaIlha(float rx, float rz)
        {
            const string caminho = PastaGerado + "/IlhaSaoLuis.asset";
            const int n = 72;
            var vertices = new Vector3[n + 1];
            var triangulos = new int[n * 3];
            vertices[0] = Vector3.zero;
            for (var i = 0; i < n; i++)
            {
                var a = i / (float)n * Mathf.PI * 2f;
                var r = 1f + 0.12f * Mathf.Sin(3f * a + 1f) + 0.08f * Mathf.Sin(5f * a + 2f) + 0.05f * Mathf.Sin(9f * a);
                vertices[i + 1] = new Vector3(Mathf.Cos(a) * rx * r, 0f, Mathf.Sin(a) * rz * r);
                triangulos[i * 3] = 0;
                triangulos[i * 3 + 1] = i == n - 1 ? 1 : i + 2;
                triangulos[i * 3 + 2] = i + 1;
            }

            var malha = AssetDatabase.LoadAssetAtPath<Mesh>(caminho);
            if (malha == null)
            {
                malha = new Mesh { name = "Ilha de São Luís" };
                AssetDatabase.CreateAsset(malha, caminho);
            }

            malha.Clear();
            malha.vertices = vertices;
            malha.triangles = triangulos;
            malha.RecalculateNormals();
            malha.RecalculateBounds();
            EditorUtility.SetDirty(malha);
            return malha;
        }

        // ==================================================================
        // Cena 4 — 2018, Laboratório Aberto do SENAI
        // ==================================================================
        static string Construir2018()
        {
            var cena = NovaCena();
            Ambiente(new Color(0.55f, 0.5f, 0.45f), Ceu("CeuInterno", Color.gray, 0.6f, 0.5f),
                new Color(1f, 0.92f, 0.8f), 0.9f, new Vector3(50f, 30f, 0f));

            var q = Quadro(4, "Laboratório Aberto SENAI · 2018");
            var parede = Opaco("2018Parede", new Color(0.93f, 0.92f, 0.9f));
            SalaFechada(q, new Vector3(0f, 0f, 1f), new Vector3(6f, 2.9f, 6f), Opaco("2018Piso", new Color(0.75f, 0.6f, 0.42f), smooth: 0.35f), parede, parede);
            var laranja = Opaco("SenaiLaranja", new Color(0.95f, 0.45f, 0.1f));
            var azul = Opaco("SenaiAzul", new Color(0.05f, 0.3f, 0.6f));
            Bloco("Faixa laranja", q, new Vector3(0f, 1.2f, 3.94f), new Vector3(6f, 0.15f, 0.02f), laranja);
            Bloco("Faixa azul", q, new Vector3(0f, 1.05f, 3.94f), new Vector3(6f, 0.08f, 0.02f), azul);
            Texto("Placa", q, new Vector3(0f, 2.3f, 3.92f), "SENAI · Laboratório Aberto · 2 mar. 2018", 0.14f, 4.5f, new Color(0.12f, 0.18f, 0.3f));
            Luz("Luz da oficina", q, new Vector3(0f, 2.5f, 1f), LightType.Point, new Color(1f, 0.93f, 0.82f), 1.6f, 6f);
            var janela = Opaco("2018Janela", new Color(0.8f, 0.9f, 1f), new Color(0.8f, 0.9f, 1f) * 1.2f);
            for (var i = 0; i < 3; i++)
                Bloco($"Janela {i}", q, new Vector3(-2.94f, 1.7f, -0.5f + i * 1.6f), new Vector3(0.02f, 1.1f, 1.1f), janela);

            // Bancada com a ideia (lâmpada), CAD, impressora 3D.
            var madeira = Opaco("2018Bancada", new Color(0.6f, 0.42f, 0.25f));
            Bloco("Tampo da bancada", q, new Vector3(0f, 0.88f, 0.82f), new Vector3(1.6f, 0.05f, 0.75f), madeira);
            foreach (var x in new[] { -0.75f, 0.75f })
            foreach (var z in new[] { 0.5f, 1.14f })
                Bloco("Perna", q, new Vector3(x, 0.43f, z), new Vector3(0.05f, 0.86f, 0.05f), madeira);

            var lampadaRaiz = new GameObject("Lâmpada (ideia)").transform;
            lampadaRaiz.SetParent(q, false);
            lampadaRaiz.localPosition = new Vector3(-0.42f, 0.905f, 0.62f);
            Bloco("Soquete", lampadaRaiz, new Vector3(0f, 0.03f, 0f), new Vector3(0.07f, 0.03f, 0.07f), Opaco("Metal", new Color(0.6f, 0.6f, 0.62f), metal: 0.6f), PrimitiveType.Cylinder);
            var bulbo = Bloco("Bulbo", lampadaRaiz, new Vector3(0f, 0.15f, 0f), Vector3.one * 0.18f,
                Opaco("2018Bulbo", new Color(1f, 0.92f, 0.55f), new Color(0.5f, 0.42f, 0.1f)), PrimitiveType.Sphere);
            bulbo.AddComponent<SphereCollider>().radius = 0.6f;
            var poke = bulbo.AddComponent<PokeTrigger>();
            poke.ConfigurarNoEditor(new[] { bulbo.GetComponent<Renderer>() }, new Color(1f, 0.9f, 0.4f));
            var brilhoLampada = AdicionarPulsar(bulbo, new Color(1f, 0.85f, 0.3f), 1.5f, 1.2f, 0.08f);

            Bloco("Suporte do monitor", q, new Vector3(0.48f, 1.0f, 1.05f), new Vector3(0.05f, 0.2f, 0.05f), Opaco("Metal", Color.white));
            Bloco("Monitor", q, new Vector3(0.48f, 1.25f, 1.05f), new Vector3(0.62f, 0.4f, 0.03f), Opaco("2018Monitor", new Color(0.1f, 0.1f, 0.12f)));
            Bloco("Tela CAD", q, new Vector3(0.48f, 1.25f, 1.033f), new Vector3(0.57f, 0.35f, 0.005f), Opaco("2018Tela", Color.black, new Color(0.04f, 0.08f, 0.2f)));
            var cadGo = new GameObject("Desenho digital (CAD)");
            cadGo.transform.SetParent(q, false);
            var cadLinha = cadGo.AddComponent<LineRenderer>();
            cadLinha.widthMultiplier = 0.005f;
            cadLinha.sharedMaterial = LinhaSolida("Linha");
            cadLinha.startColor = cadLinha.endColor = new Color(0.4f, 0.9f, 1f);
            var cad = cadGo.AddComponent<LinhaProgressiva>();
            cad.pontos = DesenhoLampada(new Vector3(0.48f, 1.28f, 1.028f), 0.085f);

            var impressoraRaiz = new GameObject("Impressora 3D").transform;
            impressoraRaiz.SetParent(q, false);
            impressoraRaiz.localPosition = new Vector3(0.02f, 0.905f, 0.92f);
            var quadroMat = Opaco("2018Impressora", new Color(0.15f, 0.15f, 0.17f), metal: 0.4f);
            foreach (var x in new[] { -0.15f, 0.15f })
            foreach (var z in new[] { -0.15f, 0.15f })
                Bloco("Coluna", impressoraRaiz, new Vector3(x, 0.17f, z), new Vector3(0.02f, 0.34f, 0.02f), quadroMat);
            Bloco("Topo", impressoraRaiz, new Vector3(0f, 0.34f, 0f), new Vector3(0.32f, 0.02f, 0.32f), quadroMat);
            Bloco("Mesa", impressoraRaiz, new Vector3(0f, 0.02f, 0f), new Vector3(0.26f, 0.01f, 0.26f), Opaco("2018Mesa", new Color(0.2f, 0.2f, 0.25f)));
            var pecaPivo = new GameObject("Peça (pivô)").transform;
            pecaPivo.SetParent(impressoraRaiz, false);
            pecaPivo.localPosition = new Vector3(0f, 0.025f, 0f);
            Bloco("Peça", pecaPivo, new Vector3(0f, 0.06f, 0f), new Vector3(0.12f, 0.12f, 0.12f), Opaco("2018Filamento", new Color(0.95f, 0.5f, 0.15f), new Color(0.2f, 0.08f, 0f)));
            var bico = Bloco("Bico", impressoraRaiz, new Vector3(0f, 0.06f, 0f), new Vector3(0.035f, 0.04f, 0.035f), laranja);
            var impressora = impressoraRaiz.gameObject.AddComponent<ImpressaoEmCamadas>();
            impressora.peca = pecaPivo;
            impressora.bico = bico.transform;
            var somImpressora = Fonte("Som da impressora", q, impressoraRaiz.position, true);

            // Protótipo pronto: um pequeno drone que gira na frente da bancada.
            var drone = new GameObject("Protótipo (drone)").transform;
            drone.SetParent(q, false);
            drone.localPosition = new Vector3(0f, 1.08f, 0.6f);
            var corpo = Opaco("2018Drone", new Color(0.2f, 0.22f, 0.26f), smooth: 0.6f);
            Bloco("Corpo", drone, Vector3.zero, new Vector3(0.12f, 0.045f, 0.12f), corpo);
            Bloco("Braço 1", drone, Vector3.zero, new Vector3(0.36f, 0.015f, 0.025f), corpo, rotacao: new Vector3(0f, 45f, 0f));
            Bloco("Braço 2", drone, Vector3.zero, new Vector3(0.36f, 0.015f, 0.025f), corpo, rotacao: new Vector3(0f, -45f, 0f));
            var helice = Transparente("2018Helice", new Color(0.6f, 0.85f, 1f, 0.45f));
            for (var i = 0; i < 4; i++)
            {
                var a = (45f + i * 90f) * Mathf.Deg2Rad;
                Bloco($"Hélice {i + 1}", drone, new Vector3(Mathf.Cos(a) * 0.13f, 0.02f, Mathf.Sin(a) * 0.13f), new Vector3(0.1f, 0.003f, 0.1f), helice, PrimitiveType.Cylinder);
            }

            drone.gameObject.AddComponent<Girar>().grausPorSegundo = 45f;

            // Parede de ideias, ferramentas e cortadora a laser.
            var cores = new[] { new Color(1f, 0.9f, 0.3f), new Color(1f, 0.5f, 0.6f), new Color(0.5f, 0.9f, 0.6f), new Color(0.5f, 0.8f, 1f) };
            var rngPostit = new System.Random(2018);
            for (var i = 0; i < 16; i++)
            {
                var p = new Vector3(-2.4f + (i % 4) * 0.2f + (float)rngPostit.NextDouble() * 0.05f, 1.5f + (i / 4) * 0.18f, 3.93f);
                Bloco($"Post-it {i}", q, p, new Vector3(0.14f, 0.14f, 0.005f), Opaco($"PostIt{i % 4}", cores[i % 4]));
            }

            Bloco("Quadro de ferramentas", q, new Vector3(1.6f, 1.6f, 3.93f), new Vector3(1.4f, 0.9f, 0.03f), Opaco("2018Ferramentas", new Color(0.3f, 0.32f, 0.3f)));
            Bloco("Cortadora a laser", q, new Vector3(2.2f, 0.45f, 2.6f), new Vector3(1f, 0.9f, 0.7f), Opaco("2018Laser", new Color(0.85f, 0.85f, 0.88f)));
            Bloco("Janela da cortadora", q, new Vector3(2.2f, 0.91f, 2.6f), new Vector3(0.8f, 0.02f, 0.5f), Opaco("2018LaserJanela", new Color(0.4f, 0.1f, 0.05f), new Color(0.3f, 0.05f, 0f)));

            var ambiente = Fonte("Oficina", q, q.TransformPoint(new Vector3(0f, 2f, 2.5f)), false);

            Decorar2018(q);

            var seq = Sequencia<Cena4Senai>("Cena 4 · 2018 SENAI", 4, 4);
            seq.lampada = poke;
            seq.brilhoLampada = brilhoLampada;
            seq.desenhoDigital = cad;
            seq.impressora = impressora;
            seq.prototipo = drone;
            seq.somImpressora = somImpressora;
            seq.ambiente = ambiente;

            return Salvar(cena, LayoutTrilha.Cenas[4]);
        }

        /// <summary>Contorno de uma lâmpada em linhas, desenhado na tela do CAD.</summary>
        static Vector3[] DesenhoLampada(Vector3 centro, float raio)
        {
            var pontos = new List<Vector3>();
            for (var i = 0; i <= 40; i++)
            {
                var a = Mathf.Lerp(-40f, 220f, i / 40f) * Mathf.Deg2Rad;
                pontos.Add(centro + new Vector3(Mathf.Cos(a) * raio, Mathf.Sin(a) * raio, 0f));
            }

            var baseY = centro.y - raio * 1.05f;
            var esq = centro.x - raio * 0.45f;
            var dir = centro.x + raio * 0.45f;
            pontos.Add(new Vector3(esq, baseY, centro.z));
            pontos.Add(new Vector3(esq, baseY - raio * 0.5f, centro.z));
            pontos.Add(new Vector3(dir, baseY - raio * 0.5f, centro.z));
            pontos.Add(new Vector3(dir, baseY, centro.z));
            pontos.Add(centro + new Vector3(Mathf.Cos(-40f * Mathf.Deg2Rad) * raio, Mathf.Sin(-40f * Mathf.Deg2Rad) * raio, 0f));
            return pontos.ToArray();
        }

        // ==================================================================
        // Cena 5 — 2022, 5G em São Luís
        // ==================================================================
        static string Construir2022()
        {
            var cena = NovaCena();
            Ambiente(new Color(0.14f, 0.15f, 0.25f),
                Ceu("CeuNoite", new Color(0.1f, 0.12f, 0.3f), 0.2f, 0.6f, new Color(0.02f, 0.02f, 0.04f), 0.02f),
                new Color(0.6f, 0.7f, 1f), 0.35f, new Vector3(40f, 160f, 0f), true, new Color(0.05f, 0.06f, 0.12f), 0.004f);

            var q = Quadro(5, "Terraço · 2022");
            var concreto = Opaco("2022Concreto", new Color(0.35f, 0.35f, 0.38f));
            Bloco("Laje", q, new Vector3(0f, -0.05f, 1f), new Vector3(7f, 0.1f, 7f), concreto);
            Bloco("Parapeito N", q, new Vector3(0f, 0.45f, 4.5f), new Vector3(7f, 0.9f, 0.15f), concreto);
            Bloco("Parapeito S", q, new Vector3(0f, 0.45f, -2.5f), new Vector3(7f, 0.9f, 0.15f), concreto);
            Bloco("Parapeito L", q, new Vector3(3.5f, 0.45f, 1f), new Vector3(0.15f, 0.9f, 7f), concreto);
            Bloco("Parapeito O", q, new Vector3(-3.5f, 0.45f, 1f), new Vector3(0.15f, 0.9f, 7f), concreto);

            // A cidade como maquete luminosa lá embaixo.
            var abaixo = -25f;
            Bloco("Chão da cidade", q, new Vector3(0f, abaixo, 1f), new Vector3(320f, 0.1f, 320f), Opaco("2022Cidade", new Color(0.03f, 0.03f, 0.05f)));
            CidadeKenney(q, abaixo);

            // Mapa da cidade no chão do terraço, em volta da torre.
            var centroTorre = new Vector3(0f, 0f, 1.4f);
            Bloco("Mapa no chão", q, centroTorre + new Vector3(0f, 0.003f, 0f), new Vector3(4.4f, 0.002f, 4.4f),
                Opaco("2022MapaChao", new Color(0.05f, 0.07f, 0.15f), new Color(0.03f, 0.04f, 0.1f)), PrimitiveType.Cylinder);
            for (var i = -2; i <= 2; i++)
            {
                Linha($"Rua NS {i}", q, new[] { centroTorre + new Vector3(i * 0.8f, 0.01f, -2f), centroTorre + new Vector3(i * 0.8f, 0.01f, 2f) },
                    0.01f, new Color(0.12f, 0.15f, 0.3f), true);
                Linha($"Rua LO {i}", q, new[] { centroTorre + new Vector3(-2f, 0.01f, i * 0.8f), centroTorre + new Vector3(2f, 0.01f, i * 0.8f) },
                    0.01f, new Color(0.12f, 0.15f, 0.3f), true);
            }

            // Torre 5G.
            var torreRaiz = new GameObject("Torre 5G").transform;
            torreRaiz.SetParent(q, false);
            torreRaiz.localPosition = centroTorre;
            var aco = Opaco("2022Aco", new Color(0.7f, 0.72f, 0.75f), smooth: 0.5f, metal: 0.6f);
            foreach (var x in new[] { -0.14f, 0.14f })
            foreach (var z in new[] { -0.14f, 0.14f })
                Bloco("Perna", torreRaiz, new Vector3(x, 1.2f, z), new Vector3(0.04f, 2.4f, 0.04f), aco);
            for (var i = 1; i <= 4; i++)
                Bloco($"Travessa {i}", torreRaiz, new Vector3(0f, i * 0.5f, 0f), new Vector3(0.32f, 0.03f, 0.32f), aco);
            for (var i = 0; i < 3; i++)
            {
                var a = i * 120f;
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Bloco($"Antena {i + 1}", torreRaiz, new Vector3(0f, 2.2f, 0f) + dir * 0.2f, new Vector3(0.14f, 0.55f, 0.05f),
                    Opaco("2022Antena", new Color(0.92f, 0.92f, 0.95f)), rotacao: new Vector3(0f, a, 0f));
            }

            var farol = Bloco("Farol", torreRaiz, new Vector3(0f, 2.62f, 0f), Vector3.one * 0.08f, Opaco("2022Farol", Color.red, new Color(0.5f, 0f, 0f)), PrimitiveType.Sphere);
            AdicionarPulsar(farol, Color.red, 3f, 0.8f).ativo = true;
            var proximidade = torreRaiz.gameObject.AddComponent<ProximityTrigger>();
            proximidade.ConfigurarNoEditor(0.85f);

            var rotulo = Texto("Faixa", q, centroTorre + new Vector3(-0.55f, 2.45f, 0f), "3,5 GHz · 19 set. 2022", 0.09f, 1.6f, new Color(0.9f, 0.75f, 1f));
            rotulo.gameObject.AddComponent<EncararUsuario>();

            var pulso = Fonte("Pulso das ondas", q, torreRaiz.position + Vector3.up, true);
            pulso.clip = null;
            var ondasMapa = Ondas("Ondas no mapa", q, centroTorre + new Vector3(0f, 0.02f, 0f), 2.1f, pulso);
            var ondasCidade = Ondas("Ondas na cidade", q, new Vector3(0f, abaixo + 0.2f, 1.4f), 95f, null);

            // Painel comparativo (valores pendentes no documento: só ilustrativo).
            var painel = new GameObject("Painel comparativo").transform;
            painel.SetParent(q, false);
            painel.localPosition = new Vector3(1.15f, 1.45f, 1.45f);
            painel.localRotation = Quaternion.Euler(0f, 25f, 0f);
            Bloco("Fundo", painel, new Vector3(0f, 0f, 0.02f), new Vector3(0.95f, 0.78f, 0.02f), Opaco("2022Painel", new Color(0.05f, 0.06f, 0.14f)));
            Texto("Título", painel, new Vector3(0f, 0.3f, 0f), "Gerações de rede móvel", 0.055f, 0.9f, Color.white);
            var barras = new List<Transform>();
            var nomes = new[] { "3G", "4G", "5G" };
            var alturas = new[] { 0.12f, 0.24f, 0.42f };
            var coresBarra = new[] { new Color(0.5f, 0.55f, 0.6f), new Color(0.3f, 0.55f, 1f), new Color(0.85f, 0.3f, 1f) };
            for (var i = 0; i < 3; i++)
            {
                var x = -0.25f + i * 0.25f;
                var pivo = new GameObject($"Barra {nomes[i]}").transform;
                pivo.SetParent(painel, false);
                pivo.localPosition = new Vector3(x, -0.22f, 0f);
                Bloco("Barra", pivo, new Vector3(0f, 0.5f, 0f), new Vector3(0.14f, 1f, 0.02f), Opaco("2022Barra" + nomes[i], coresBarra[i], coresBarra[i] * 0.4f));
                pivo.localScale = new Vector3(1f, alturas[i], 1f);
                barras.Add(pivo);
                Texto($"Rótulo {nomes[i]}", painel, new Vector3(x, -0.27f, 0f), nomes[i], 0.05f, 0.2f, Color.white);
            }

            Texto("Nota", painel, new Vector3(0f, -0.35f, 0f), "Comparação ilustrativa · valores a definir", 0.026f, 0.9f, new Color(0.7f, 0.7f, 0.8f));

            var ambiente = Fonte("Cidade", q, q.TransformPoint(new Vector3(0f, 1f, 3f)), false);

            var seq = Sequencia<Cena5Rede5G>("Cena 5 · 2022 5G", 5, 5);
            seq.torre = proximidade;
            seq.ondasNoMapa = ondasMapa;
            seq.ondasNaCidade = ondasCidade;
            seq.painelComparacao = painel.gameObject;
            seq.barras = barras.ToArray();
            seq.ambiente = ambiente;

            return Salvar(cena, LayoutTrilha.Cenas[5]);
        }

        static OndasEmAnel Ondas(string nome, Transform pai, Vector3 posicaoLocal, float raio, AudioSource pulso)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            go.transform.localPosition = posicaoLocal;
            var aneis = new LineRenderer[4];
            for (var i = 0; i < aneis.Length; i++)
                aneis[i] = Linha($"Anel {i + 1}", go.transform, new[] { Vector3.zero, Vector3.right }, 0.03f, Color.white, true);
            var ondas = go.AddComponent<OndasEmAnel>();
            ondas.aneis = aneis;
            ondas.raioMaximo = raio;
            ondas.pulso = pulso;
            return ondas;
        }

        // ==================================================================
        // Cena 6 — 2023 a 2025, lançamentos em Alcântara
        // ==================================================================
        static string Construir2025()
        {
            var cena = NovaCena();
            Ambiente(new Color(0.55f, 0.58f, 0.62f),
                Ceu("CeuDia", new Color(0.45f, 0.6f, 0.9f), 1.25f, 1f, new Color(0.35f, 0.33f, 0.3f)),
                new Color(1f, 0.96f, 0.88f), 1.2f, new Vector3(45f, 200f, 0f), true, new Color(0.7f, 0.78f, 0.88f), 0.004f);

            var q = Quadro(6, "Alcântara · 2023–2025");

            // ---------------- Parte A: 2023, HANBIT-TLV por dentro ----------------
            var parteA = new GameObject("Parte A · 2023 · HANBIT-TLV").transform;
            parteA.SetParent(q, false);
            var hangar = Opaco("2023Hangar", new Color(0.16f, 0.17f, 0.2f));
            SalaFechada(parteA, new Vector3(0f, 0f, 2f), new Vector3(9f, 7.5f, 9f), Opaco("2023Piso", new Color(0.22f, 0.23f, 0.26f), smooth: 0.6f), hangar, hangar);
            Luz("Refletor A", parteA, new Vector3(-1.5f, 4f, 0.5f), LightType.Point, new Color(1f, 0.97f, 0.9f), 2.5f, 9f);
            Luz("Refletor B", parteA, new Vector3(1.5f, 2f, 1f), LightType.Point, new Color(0.8f, 0.9f, 1f), 1.5f, 6f);

            var foguete = new GameObject("Foguete em corte").transform;
            foguete.SetParent(parteA, false);
            foguete.localPosition = new Vector3(0f, 0f, 1.9f);
            Bloco("Casco (transparente)", foguete, new Vector3(0f, 3.4f, 0f), new Vector3(1.25f, 3.2f, 1.25f),
                Transparente("2023Casco", new Color(0.85f, 0.9f, 1f, 0.16f)), PrimitiveType.Cylinder);
            var escuro = Opaco("2023Motor", new Color(0.2f, 0.2f, 0.22f), smooth: 0.5f, metal: 0.7f);
            Bloco("Tubeira", foguete, new Vector3(0f, 0.38f, 0f), new Vector3(0.7f, 0.22f, 0.7f), escuro, PrimitiveType.Cylinder);
            Bloco("Motor", foguete, new Vector3(0f, 0.85f, 0f), new Vector3(0.55f, 0.25f, 0.55f), escuro, PrimitiveType.Cylinder);
            Bloco("Anel 1", foguete, new Vector3(0f, 1.15f, 0f), new Vector3(1.2f, 0.03f, 1.2f), escuro, PrimitiveType.Cylinder);
            var sisnav = Bloco("SISNAV", foguete, new Vector3(0f, 1.5f, 0f), new Vector3(0.42f, 0.28f, 0.42f),
                Opaco("2023Sisnav", new Color(0.15f, 0.5f, 0.6f), new Color(0.05f, 0.25f, 0.3f)));
            var brilhoSisnav = AdicionarPulsar(sisnav, new Color(0.3f, 1f, 1f), 2f, 1.2f);
            Bloco("Anel 2", foguete, new Vector3(0f, 1.85f, 0f), new Vector3(1.2f, 0.03f, 1.2f), escuro, PrimitiveType.Cylinder);
            Bloco("Tanque", foguete, new Vector3(0f, 3.2f, 0f), new Vector3(1.0f, 1.25f, 1.0f), Opaco("2023Tanque", new Color(0.85f, 0.45f, 0.2f), smooth: 0.5f), PrimitiveType.Cylinder);
            Bloco("Coifa", foguete, new Vector3(0f, 5.4f, 0f), new Vector3(1.15f, 1.6f, 1.15f), Transparente("2023Coifa", new Color(0.9f, 0.9f, 0.95f, 0.3f)), PrimitiveType.Sphere);

            var rotuloSisnav = Texto("Rótulo SISNAV", parteA, new Vector3(0.95f, 1.7f, 1.55f),
                "<b>SISNAV</b>\n<size=75%>sistema brasileiro de navegação\nmissão de 2023</size>", 0.06f, 0.75f, Color.white);
            rotuloSisnav.rectTransform.sizeDelta = new Vector2(0.75f, 0.3f);
            Linha("Indicador", parteA, new[] { new Vector3(0.6f, 1.62f, 1.6f), new Vector3(0.22f, 1.5f, 1.85f) }, 0.006f, new Color(0.4f, 1f, 1f), true);
            Texto("Placa", parteA, new Vector3(0.95f, 2.15f, 1.55f), "HANBIT-TLV · Innospace · 2023", 0.08f, 1.2f, new Color(0.85f, 0.9f, 1f));

            Decorar2023(parteA);

            // ---------------- Parte B: 2025, plataforma do HANBIT-Nano ----------------
            var parteB = new GameObject("Parte B · 2025 · HANBIT-Nano").transform;
            parteB.SetParent(q, false);
            Bloco("Vegetação", parteB, new Vector3(0f, -0.08f, 20f), new Vector3(240f, 0.1f, 240f), Opaco("2025Vegetacao", new Color(0.35f, 0.42f, 0.25f)));
            Bloco("Mar", parteB, new Vector3(0f, -0.06f, 110f), new Vector3(400f, 0.1f, 100f), Opaco("Mar", Color.white));
            Bloco("Pátio", parteB, new Vector3(0f, -0.05f, 14f), new Vector3(40f, 0.1f, 34f), Opaco("2025Patio", new Color(0.58f, 0.58f, 0.57f)));
            var gradil = Opaco("2025Gradil", new Color(0.8f, 0.6f, 0.1f));
            Bloco("Gradil", parteB, new Vector3(0f, 1f, 2.5f), new Vector3(4f, 0.05f, 0.05f), gradil);
            for (var i = 0; i <= 8; i++)
                Bloco($"Poste {i}", parteB, new Vector3(-2f + i * 0.5f, 0.5f, 2.5f), new Vector3(0.04f, 1f, 0.04f), gradil);

            var plataforma = new Vector3(0f, 0f, 25f);
            var concretoEscuro = Opaco("2025Plataforma", new Color(0.35f, 0.35f, 0.36f));
            Bloco("Plataforma", parteB, plataforma + new Vector3(0f, 0.3f, 0f), new Vector3(7f, 0.6f, 7f), concretoEscuro);
            var torreMat = Opaco("2025Torre", new Color(0.75f, 0.2f, 0.15f), metal: 0.3f);
            Bloco("Torre de serviço", parteB, plataforma + new Vector3(3.4f, 11f, 0f), new Vector3(1.4f, 22f, 1.4f), torreMat);
            Bloco("Braço superior", parteB, plataforma + new Vector3(2f, 14f, 0f), new Vector3(2f, 0.25f, 0.35f), torreMat);
            Bloco("Braço inferior", parteB, plataforma + new Vector3(2f, 7f, 0f), new Vector3(2f, 0.25f, 0.35f), torreMat);

            var hanbit = new GameObject("HANBIT-Nano").transform;
            hanbit.SetParent(parteB, false);
            hanbit.localPosition = plataforma + new Vector3(0f, 0.6f, 0f);
            var branco = Opaco("2025Foguete", new Color(0.96f, 0.96f, 0.97f), smooth: 0.6f);
            var preto = Opaco("2025Faixa", new Color(0.08f, 0.08f, 0.1f));
            Bloco("Corpo", hanbit, new Vector3(0f, 8f, 0f), new Vector3(1.4f, 8f, 1.4f), branco, PrimitiveType.Cylinder);
            Bloco("Faixa 1", hanbit, new Vector3(0f, 4.5f, 0f), new Vector3(1.43f, 0.2f, 1.43f), preto, PrimitiveType.Cylinder);
            Bloco("Faixa 2", hanbit, new Vector3(0f, 12f, 0f), new Vector3(1.43f, 0.2f, 1.43f), preto, PrimitiveType.Cylinder);
            Bloco("Coifa", hanbit, new Vector3(0f, 16.2f, 0f), new Vector3(1.4f, 2.6f, 1.4f), branco, PrimitiveType.Sphere);
            for (var i = 0; i < 4; i++)
            {
                var a = i * 90f + 45f;
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Bloco($"Aleta {i + 1}", hanbit, new Vector3(0f, 0.9f, 0f) + dir * 0.85f, new Vector3(0.08f, 1.6f, 0.9f), preto, rotacao: new Vector3(0f, a, 0f));
            }

            var nome = Texto("Nome no foguete", hanbit, new Vector3(0f, 9f, -0.72f), "HANBIT-Nano", 0.55f, 6f, new Color(0.1f, 0.15f, 0.35f));
            nome.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            var fogoMat = Particula("Fogo", true);
            var fumacaMat = Particula("Fumaca", false);
            var fogo = Particulas("Fogo", hanbit, new Vector3(0f, -0.1f, 0f), fogoMat, 0.5f, 14f, 2.6f, 220f, new Color(1f, 0.65f, 0.25f), 12f);
            fogo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var rastro = Particulas("Rastro", hanbit, new Vector3(0f, -0.5f, 0f), fumacaMat, 6f, 1f, 3f, 45f, new Color(0.9f, 0.9f, 0.9f, 0.7f), 8f);
            rastro.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var fumacaBase = Particulas("Fumaça da base", parteB, plataforma + new Vector3(0f, 0.8f, 0f), fumacaMat, 4f, 7f, 6f, 70f,
                new Color(0.85f, 0.85f, 0.85f, 0.75f), 75f, gravidade: -0.03f);
            fumacaBase.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var clarao = Luz("Clarão", parteB, plataforma + new Vector3(0f, 2f, -2f), LightType.Point, new Color(1f, 0.65f, 0.3f), 0f, 70f);
            var motor = Fonte("Motor", parteB, q.TransformPoint(plataforma), true);
            motor.minDistance = 12f;
            motor.maxDistance = 400f;

            var decolagem = hanbit.gameObject.AddComponent<Decolagem>();
            decolagem.foguete = hanbit;
            decolagem.fogo = fogo;
            decolagem.fumacaBase = fumacaBase;
            decolagem.rastro = rastro;
            decolagem.clarao = clarao;
            decolagem.motor = motor;

            Decorar2025(parteB, plataforma);

            var cargas = new GameObject("Painel de cargas").transform;
            cargas.SetParent(parteB, false);
            cargas.localPosition = new Vector3(1.2f, 1.5f, 1.4f);
            cargas.localRotation = Quaternion.Euler(0f, 25f, 0f);
            Bloco("Fundo", cargas, new Vector3(0f, 0f, 0.02f), new Vector3(0.9f, 0.6f, 0.02f), Opaco("2025Painel", new Color(0.06f, 0.1f, 0.2f)));
            Texto("Texto", cargas, Vector3.zero,
                "<b>HANBIT-Nano · Innospace</b>\n<size=80%>Primeiro lançamento comercial\na partir do Brasil\n\nCarga a bordo: PION-BR2 (UFMA)\ne outras cargas</size>",
                0.05f, 0.85f, Color.white).rectTransform.sizeDelta = new Vector2(0.85f, 0.55f);

            var resultado = new GameObject("Painel de resultado").transform;
            resultado.SetParent(parteB, false);
            resultado.localPosition = new Vector3(0.1f, 1.6f, 1.7f);
            resultado.gameObject.AddComponent<EncararUsuario>();
            Bloco("Fundo", resultado, new Vector3(0f, 0f, 0.02f), new Vector3(1f, 0.6f, 0.02f), Opaco("2025Resultado", new Color(0.3f, 0.06f, 0.06f)));
            Texto("Texto", resultado, Vector3.zero,
                "<b>Resultado da missão</b>\n<size=80%>Uma anomalia levou à perda da missão.\nO PION-BR2 não chegou à órbita.\n\n<size=70%>Fonte: AEB, Relatório de gestão 2025</size></size>",
                0.05f, 0.95f, Color.white).rectTransform.sizeDelta = new Vector2(0.95f, 0.55f);

            var aviso = Texto("Aviso de reconstituição", parteB, new Vector3(-0.3f, 1.12f, 2.45f), "Reconstituição feita para este projeto",
                0.04f, 1.2f, new Color(1f, 0.95f, 0.8f)).gameObject;

            var guia2025 = new GameObject("Posição do guia 2025").transform;
            guia2025.SetParent(parteB, true);
            guia2025.SetPositionAndRotation(PosicaoGuia(6), Quaternion.LookRotation(-LayoutTrilha.Foco(6)));

            var ambiente = Fonte("Vento", q, q.TransformPoint(new Vector3(0f, 2f, 3f)), false);

            var seq = Sequencia<Cena6Alcantara>("Cena 6 · 2023–2025 Alcântara", 6, 6);
            seq.parte2023 = parteA.gameObject;
            seq.parte2025 = parteB.gameObject;
            seq.sisnav = brilhoSisnav;
            seq.posicaoGuia2025 = guia2025;
            seq.decolagem = decolagem;
            seq.painelResultado = resultado;
            seq.avisoReconstituicao = aviso;
            seq.ambiente = ambiente;

            // A cena abre em 2023; a parte de 2025 fica escondida até a virada.
            parteB.gameObject.SetActive(false);
            resultado.gameObject.SetActive(false);

            return Salvar(cena, LayoutTrilha.Cenas[6]);
        }
    }
}
