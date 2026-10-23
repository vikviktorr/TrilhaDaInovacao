using System.Collections.Generic;
using System.Linq;
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
            // Salão amplo (10 × 9 m, pé-direito de 3,6 m): os bastidores ficam ao fundo,
            // a 4 m do visitante, e os móveis encostados nas paredes.
            SalaFechada(q, new Vector3(0.6f, 0f, 2f), new Vector3(10f, 3.6f, 9f),
                Opaco("1986Piso", new Color(0.72f, 0.66f, 0.52f), smooth: 0.35f),
                Opaco("1986Parede", new Color(0.62f, 0.64f, 0.52f)),
                Opaco("1986Teto", new Color(0.85f, 0.85f, 0.8f)));

            var fluorescente = Opaco("1986Fluorescente", Color.white, new Color(0.9f, 1f, 0.95f) * 1.6f);
            for (var i = 0; i < 4; i++)
            for (var j = 0; j < 2; j++)
                Bloco($"Fluorescente {i}-{j}", q, new Vector3(-2.4f + i * 2f, 3.56f, 0.2f + j * 3.4f), new Vector3(0.18f, 0.03f, 2.4f), fluorescente);
            Luz("Luz fluorescente A", q, new Vector3(-1.2f, 3.1f, 0.6f), LightType.Point, new Color(0.92f, 1f, 0.95f), 1.4f, 8f);
            Luz("Luz fluorescente B", q, new Vector3(2.4f, 3.1f, 0.6f), LightType.Point, new Color(0.92f, 1f, 0.95f), 1.2f, 8f);
            Luz("Luz fluorescente C", q, new Vector3(0.6f, 3.1f, 3.6f), LightType.Point, new Color(0.92f, 1f, 0.95f), 1.3f, 8f);

            // Bastidores da central, com LEDs.
            var rackMat = Opaco("1986Rack", new Color(0.42f, 0.48f, 0.42f), smooth: 0.4f, metal: 0.3f);
            var ledMat = Opaco("1986Led", new Color(0.05f, 0.08f, 0.05f), new Color(0.2f, 1f, 0.35f));
            var bastidores = new List<PiscaLeds>();
            for (var i = 0; i < 5; i++)
            {
                var x = -2f + i * 1f;
                var rack = Bloco($"Bastidor {i + 1}", q, new Vector3(x, 1f, 4.2f), new Vector3(0.6f, 2f, 0.5f), rackMat);
                var grupos = new[] { new List<GameObject>(), new List<GameObject>() };
                for (var c = 0; c < 3; c++)
                for (var l = 0; l < 8; l++)
                {
                    grupos[(c + l) % 2].Add(Bloco($"LED {c}-{l}", q, new Vector3(x - 0.15f + c * 0.15f, 0.7f + l * 0.16f, 3.945f),
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

            Bloco("Calha de cabos", q, new Vector3(0f, 2.45f, 4.2f), new Vector3(4.8f, 0.08f, 0.3f), Opaco("1986Calha", new Color(0.3f, 0.3f, 0.3f)));
            Bloco("Placa", q, new Vector3(0f, 2.3f, 3.94f), new Vector3(2.4f, 0.26f, 0.02f), Opaco("1986Placa", new Color(0.08f, 0.15f, 0.35f)));
            Texto("Texto da placa", q, new Vector3(0f, 2.3f, 3.925f), "TELMA · Central Trópico-R\n<size=40%>Projeto CPqD/Telebrás · até 10 mil assinantes · funciona sem ventilação forçada</size>",
                0.13f, 2.3f, Color.white);

            // Balcão com o telefone de origem.
            Bloco("Balcão", q, new Vector3(-0.1f, 0.44f, 0.78f), new Vector3(0.9f, 0.88f, 0.5f), Opaco("1986Madeira", new Color(0.45f, 0.3f, 0.18f)));
            var (telefone, baseTelefone) = Telefone("Telefone", q, new Vector3(-0.1f, 0.88f, 0.62f));
            var col = telefone.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.08f, 0f);
            col.size = new Vector3(0.3f, 0.22f, 0.26f);
            var poke = telefone.AddComponent<PokeTrigger>();
            poke.ConfigurarNoEditor(telefone.GetComponentsInChildren<Renderer>(), new Color(1f, 0.85f, 0.4f));
            var brilho = AdicionarPulsar(baseTelefone, new Color(1f, 0.7f, 0.2f), 1.2f, 2.5f);
            // Em um filho: o PokeTrigger já põe uma AudioSource no telefone, e o
            // Abafador (OnAudioFilterRead) só aceita uma por objeto.
            var somTelefone = new GameObject("Som do telefone").AddComponent<AudioSource>();
            somTelefone.transform.SetParent(telefone.transform, false);
            somTelefone.playOnAwake = false;
            somTelefone.spatialBlend = 1f;

            // Painel com o mapa do Maranhão: a ligação sai de São Luís e chega a Imperatriz.
            var (pontoA, pontoB) = MapaMaranhao(q, new Vector3(3.2f, 1.62f, 4.2f));

            // Telefone de destino, numa mesa à direita.
            Bloco("Mesa", q, new Vector3(2.1f, 0.4f, 1f), new Vector3(0.6f, 0.8f, 0.5f), Opaco("1986Madeira", Color.white));
            var (_, baseDestino) = Telefone("Telefone de destino", q, new Vector3(2.1f, 0.8f, 0.95f));
            var brilhoDestino = AdicionarPulsar(baseDestino, new Color(1f, 0.7f, 0.2f), 1.2f, 2.5f);
            var somDestino = Fonte("Som do destino", q, q.TransformPoint(new Vector3(2.1f, 0.9f, 0.95f)), true);

            // O pulso da ligação: telefone → bastidores → mapa → telefone de destino.
            var caminhoLocal = new[]
            {
                new Vector3(-0.1f, 1f, 0.62f), new Vector3(-0.1f, 1.4f, 3.85f), new Vector3(-2f, 1.4f, 3.85f),
                new Vector3(2f, 1.4f, 3.85f), pontoA + new Vector3(0f, 0f, -0.05f), pontoB + new Vector3(0f, 0f, -0.05f),
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

            var ambiente = Fonte("Zumbido da central", q, q.TransformPoint(new Vector3(0f, 1.5f, 3.9f)), true);

            Decorar1986(q);

            var seq = Sequencia<Cena1Telma>("Cena 1 · 1986 TELMA", 1, 1);
            seq.telefone = poke;
            seq.brilhoTelefone = brilho;
            seq.somTelefone = somTelefone;
            seq.ligacao = pulso;
            seq.brilhoTelefoneDestino = brilhoDestino;
            seq.somTelefoneDestino = somDestino;
            seq.ambiente = ambiente;

            LugarDoProximo(q, new Vector3(-0.45f, 0.88f, 0.9f), true);   // no balcão, à esquerda do telefone (o rádio fica à direita)
            return Salvar(cena, LayoutTrilha.Cenas[1]);
        }

        [System.Serializable]
        class AnotacaoMapa
        {
            public float proporcao;
            public PontoMapa[] pontos;
        }

        [System.Serializable]
        class PontoMapa
        {
            public string nome;
            public string tipo;
            public float u;
            public float v;
        }

        /// <summary>
        /// Ficha "Na vida real" num pedestal inclinado, atrás do visitante: o que a
        /// pesquisa mostrou sobre o lugar e a época, para ler com calma antes de
        /// tocar em Próximo. Estática (vira parte da malha mesclada da cena).
        /// </summary>
        /// <summary>Piso elevado, ar-condicionado de precisão, calhas de fibra e NOC da sala de 2009.</summary>
        static void SalaDeEquipamentos2009(Transform q)
        {
            // Juntas das placas de 60 × 60 cm do piso elevado.
            var junta = Opaco("2009Junta", new Color(0.14f, 0.15f, 0.18f));
            for (var i = 0; i <= 16; i++)
                Bloco($"Junta x {i}", q, new Vector3(-4.8f + i * 0.6f, 0.002f, 2f), new Vector3(0.015f, 0.004f, 9f), junta);
            for (var i = 0; i <= 15; i++)
                Bloco($"Junta z {i}", q, new Vector3(0f, 0.002f, -2.4f + i * 0.6f), new Vector3(10f, 0.004f, 0.015f), junta);
            var perfurada = Opaco("2009Perfurada", new Color(0.18f, 0.2f, 0.24f));
            for (var r = 0; r < 12; r++)
                Bloco($"Placa perfurada {r}", q, new Vector3(-3.3f + r * 0.6f, 0.003f, 2.4f), new Vector3(0.56f, 0.004f, 0.56f), perfurada);

            // Ar-condicionado de precisão nas pontas do corredor, e calhas de fibra no alto.
            var crac = Opaco("2009Crac", new Color(0.78f, 0.8f, 0.82f));
            foreach (var x in new[] { -4.4f, 4.4f })
            {
                Bloco("Ar-condicionado de precisão", q, new Vector3(x, 0.95f, 4.4f), new Vector3(0.9f, 1.9f, 0.9f), crac);
                Bloco("Grade do ar", q, new Vector3(x - Mathf.Sign(x) * 0.455f, 1.3f, 4.4f), new Vector3(0.01f, 0.8f, 0.7f), junta);
            }

            var fibra = Opaco("2009Fibra", new Color(0.95f, 0.78f, 0.1f));
            foreach (var z in new[] { 3.4f, 5.4f })
            {
                Bloco("Calha de fibra", q, new Vector3(0f, 2.55f, z), new Vector3(7.4f, 0.1f, 0.22f), fibra);
                for (var r = 0; r < 4; r++)
                    Bloco("Descida de fibra", q, new Vector3(-2.7f + r * 1.8f, 2.33f, z), new Vector3(0.12f, 0.35f, 0.12f), fibra);
            }
            Bloco("Calha de fibra (tronco)", q, new Vector3(0f, 2.55f, 4.4f), new Vector3(0.22f, 0.1f, 2f), fibra);

            // NOC: telão e duas estações de monitoramento junto à parede esquerda.
            var telao = Opaco("2009Telao", new Color(0.02f, 0.05f, 0.12f), new Color(0.05f, 0.18f, 0.4f));
            var grafico = Opaco("2009Grafico", Color.black, new Color(0.2f, 0.9f, 0.5f));
            for (var i = 0; i < 3; i++)
            for (var j = 0; j < 2; j++)
            {
                var c = new Vector3(-4.93f, 1.45f + j * 0.62f, 0.2f + i * 1f);
                Bloco($"Tela do telão {i}-{j}", q, c, new Vector3(0.02f, 0.58f, 0.97f), telao);
                for (var k = 0; k < 5; k++)
                {
                    var altura = 0.08f + 0.05f * ((i * 7 + j * 3 + k * 5) % 6);
                    Bloco("Barra do gráfico", q, c + new Vector3(0.012f, -0.22f + altura / 2f, -0.3f + k * 0.15f), new Vector3(0.005f, altura, 0.08f), grafico);
                }
            }
        }

        /// <summary>Ponto fixo e livre do cenário onde o botão PRÓXIMO aparece.</summary>
        static void LugarDoProximo(Transform q, Vector3 posicao, bool montado)
        {
            var go = new GameObject("Lugar do botão Próximo");
            go.transform.SetParent(q, false);
            go.transform.localPosition = posicao;
            go.AddComponent<LugarDoProximo>().montado = montado;
        }

        static void FichaReal(Transform q, Vector3 posicao, string titulo, string texto)
        {
            var raiz = new GameObject($"Na vida real · {titulo}").transform;
            raiz.SetParent(q, false);
            raiz.localPosition = posicao;
            var frente = new Vector3(posicao.x, 0f, posicao.z);
            raiz.localRotation = Quaternion.LookRotation(frente.normalized, Vector3.up);

            var escuro = Opaco("FichaPainel", new Color(0.06f, 0.1f, 0.2f));
            Bloco("Poste", raiz, new Vector3(0f, 0.5f, 0f), new Vector3(0.06f, 0.5f, 0.06f), escuro, PrimitiveType.Cylinder);
            var tampo = new GameObject("Tampo").transform;
            tampo.SetParent(raiz, false);
            tampo.localPosition = new Vector3(0f, 1.12f, 0f);
            tampo.localRotation = Quaternion.Euler(25f, 0f, 0f);
            Bloco("Painel", tampo, new Vector3(0f, 0f, 0.015f), new Vector3(0.66f, 0.44f, 0.025f), escuro);
            var corpo = Texto("Texto", tampo, Vector3.zero,
                $"<size=70%><color=#7FD4FF>NA VIDA REAL</color></size>\n<b>{titulo}</b>\n<size=78%>{texto}</size>",
                0.033f, 0.6f, Color.white);
            corpo.rectTransform.sizeDelta = new Vector2(0.6f, 0.4f);
        }

        /// <summary>
        /// Painel de parede com o mapa do Maranhão em relevo (Ferramentas/blender/mapa_maranhao.py,
        /// contorno do IBGE), os nomes das mesorregiões e as duas cidades da ligação.
        /// Devolve, no quadro da cena, os pontos de São Luís e de Imperatriz.
        /// </summary>
        static (Vector3 saoLuis, Vector3 imperatriz) MapaMaranhao(Transform q, Vector3 centroPainel)
        {
            const float alturaMapa = 0.98f;
            var painel = new Vector3(0.88f, 1.12f, 0.05f);
            Bloco("Painel mapa", q, centroPainel, painel, Opaco("1986Mapa", new Color(0.12f, 0.2f, 0.3f)));
            var frente = centroPainel.z - painel.z / 2f;

            var pontoMat = Opaco("1986PontoMapa", Color.yellow, new Color(1f, 0.85f, 0.2f));
            var pe = centroPainel.y - alturaMapa / 2f;
            var anotacao = AssetDatabase.LoadAssetAtPath<TextAsset>(PastaModelos + "Mapa/mapa-maranhao.json");
            var mapa = Modelo("Mapa/mapa-maranhao", q, new Vector3(centroPainel.x, pe, frente - 0.011f), 180f, alturaMapa);
            if (mapa == null || anotacao == null)
            {
                // Sem o modelo: só os dois pontos, onde ficariam no mapa.
                var a = centroPainel + new Vector3(0.1f, 0.33f, -0.04f);
                var b = centroPainel + new Vector3(-0.31f, 0.01f, -0.04f);
                Bloco("Ponto São Luís", q, a, Vector3.one * 0.04f, pontoMat, PrimitiveType.Sphere);
                Bloco("Ponto Imperatriz", q, b, Vector3.one * 0.04f, pontoMat, PrimitiveType.Sphere);
                return (a, b);
            }

            var dados = JsonUtility.FromJson<AnotacaoMapa>(anotacao.text);
            var largura = alturaMapa * dados.proporcao;
            var frenteRelevo = frente - 0.025f;
            var vermelho = new Color(0.62f, 0.08f, 0.12f);
            Vector3 saoLuis = default, imperatriz = default;
            foreach (var p in dados.pontos)
            {
                var pos = new Vector3(centroPainel.x + (p.u - 0.5f) * largura, pe + p.v * alturaMapa, frenteRelevo);
                if (p.tipo == "regiao")
                {
                    Texto($"Região {p.nome.Replace('\n', ' ')}", q, pos, p.nome, 0.026f, 0.2f, vermelho);
                    continue;
                }

                Bloco($"Ponto {p.nome}", q, pos, Vector3.one * 0.035f, pontoMat, PrimitiveType.Sphere);
                Texto($"Cidade {p.nome}", q, pos + new Vector3(0.075f, 0.022f, 0f), p.nome, 0.028f, 0.16f, new Color(0.08f, 0.08f, 0.1f));
                if (p.nome == "São Luís") saoLuis = pos;
                else if (p.nome == "Imperatriz") imperatriz = pos;
            }

            return (saoLuis, imperatriz);
        }

        /// <summary>
        /// Telefone de mesa de baquelite com disco (Ferramentas/blender/telefone_1986.py).
        /// O brilho vai no "Corpo" do modelo. Sem o FBX, volta ao telefone de primitivas.
        /// </summary>
        static (GameObject raiz, GameObject baseTelefone) Telefone(string nome, Transform pai, Vector3 posicaoLocal)
        {
            var raiz = new GameObject(nome);
            raiz.transform.SetParent(pai, false);
            raiz.transform.localPosition = posicaoLocal;

            // Altura real do modelo (18 cm): escala 1:1. A frente do Blender (-Y) chega
            // ao Unity como +Z; o giro de 180° deixa o disco virado para o visitante.
            var modelo = Modelo("Telefone/telefone-1986", raiz.transform, Vector3.zero, 180f, 0.18f);
            var corpo = modelo != null ? modelo.transform.Find("Corpo") : null;
            if (corpo != null) return (raiz, corpo.gameObject);
            if (modelo != null) Object.DestroyImmediate(modelo);

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
        /// <summary>
        /// Console de radar dos anos 1970–90: tela PPI redonda (o radar visto de cima,
        /// com a varredura girando junto da antena), A-scope, medidores e lâmpadas. O
        /// botão de ligar, a tela acesa e a varredura são peças do Unity sobre o modelo.
        /// Cotas do modelo no Blender (x, y, z) viram (x, z, y) aqui, com o giro de 180°.
        /// </summary>
        static (PokeTrigger botao, Pulsar brilho, GameObject tela, Girar varredura) ConsoleRadar1994(Transform q, Vector3 posicao)
        {
            var raiz = new GameObject("Console do radar").transform;
            raiz.SetParent(q, false);
            raiz.localPosition = posicao;
            var modelo = Modelo("Guara/console-radar", raiz, Vector3.zero, 180f, 1.54f);
            // Modelo() centra o bbox; as cotas abaixo partem da origem do Blender.
            var origem = modelo != null ? modelo.transform.localPosition : Vector3.zero;
            if (modelo == null)
            {
                var cinza = Opaco("1994Console", new Color(0.6f, 0.62f, 0.58f));
                Bloco("Gabinete", raiz, new Vector3(0f, 0.39f, 0f), new Vector3(1.24f, 0.78f, 0.7f), cinza);
                Bloco("Painel", raiz, new Vector3(0f, 1.14f, 0.2f), new Vector3(1.2f, 0.72f, 0.3f), cinza);
            }

            // Painel inclinado: de (z 0,02; y 0,78) a (z 0,25; y 1,50), normal para o visitante.
            Vector3 NoPainel(float u, float s) => origem + new Vector3(u, 0.78f + 0.72f * s, 0.02f + 0.23f * s);
            var normal = new Vector3(0f, 0.23f, -0.72f).normalized;
            var deitado = Quaternion.FromToRotation(Vector3.up, normal);

            // Botão de ligar o transmissor, no aro amarelo e preto da caixa de comando.
            var alvo = new GameObject("Botão ligar").transform;
            alvo.SetParent(raiz, false);
            alvo.localPosition = origem + new Vector3(0.42f, 0.91f, -0.24f);
            var tampa = Bloco("Tampa", alvo, new Vector3(0f, 0.015f, 0f), new Vector3(0.14f, 0.015f, 0.14f),
                Opaco("1994BotaoRadar", new Color(0.85f, 0.12f, 0.1f), new Color(0.3f, 0.03f, 0.02f)), PrimitiveType.Cylinder);
            var colisor = alvo.gameObject.AddComponent<BoxCollider>();
            colisor.center = new Vector3(0f, 0.04f, 0f);
            colisor.size = new Vector3(0.2f, 0.12f, 0.2f);
            var botao = alvo.gameObject.AddComponent<PokeTrigger>();
            botao.ConfigurarNoEditor(new[] { tampa.GetComponent<Renderer>() }, new Color(1f, 0.5f, 0.4f));
            var brilho = AdicionarPulsar(tampa, new Color(1f, 0.35f, 0.2f), 1.6f, 2f);
            var rotulo = Texto("Rótulo ligar", raiz, origem + new Vector3(0.42f, 1.07f, -0.26f), "LIGAR RADAR", 0.045f, 0.4f, Color.white);
            rotulo.fontStyle = FontStyles.Bold;
            rotulo.gameObject.AddComponent<EncararUsuario>();

            // Tela acesa: o fósforo da PPI, a varredura e um eco; o traço da A-scope.
            var tela = new GameObject("Tela ligada").transform;
            tela.SetParent(raiz, false);
            var fosforo = Opaco("1994Fosforo", new Color(0.05f, 0.25f, 0.1f), new Color(0.05f, 0.35f, 0.12f));
            var verde = Opaco("1994Varredura", new Color(0.4f, 1f, 0.5f), new Color(0.35f, 1f, 0.45f));
            var ppi = NoPainel(-0.27f, 0.55f);
            Bloco("Fósforo PPI", tela, ppi + normal * 0.007f, new Vector3(0.33f, 0.0005f, 0.33f), fosforo,
                PrimitiveType.Cylinder, deitado.eulerAngles);
            var pivo = new GameObject("Varredura").transform;
            pivo.SetParent(tela, false);
            pivo.SetLocalPositionAndRotation(ppi + normal * 0.012f, deitado);
            Bloco("Raio", pivo, new Vector3(0f, 0f, 0.08f), new Vector3(0.008f, 0.002f, 0.16f), verde);
            var varredura = pivo.gameObject.AddComponent<Girar>();
            varredura.grausPorSegundo = 60f;   // junto com a antena (10 rpm)
            varredura.ativo = false;
            Bloco("Eco", tela, ppi + normal * 0.012f + deitado * new Vector3(0.07f, 0f, 0.05f), Vector3.one * 0.014f,
                verde, PrimitiveType.Sphere);

            var traco = new GameObject("Traço A-scope").transform;
            traco.SetParent(tela, false);
            traco.SetLocalPositionAndRotation(NoPainel(0.22f, 0.62f) + normal * 0.027f, deitado);
            var pontos = new Vector3[12];
            for (var i = 0; i < pontos.Length; i++)
            {
                var x = -0.13f + 0.26f * i / (pontos.Length - 1f);
                var pulso = i == 1 ? 0.06f : i == 7 ? 0.035f : 0f;   // pulso transmitido e o eco
                pontos[i] = new Vector3(x, 0f, -0.05f + pulso);
            }
            Linha("Traço", traco, pontos, 0.004f, new Color(0.4f, 1f, 0.5f), true);
            tela.gameObject.SetActive(false);

            return (botao, brilho, tela.gameObject, varredura);
        }

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

            // Console do operador de radar (Ferramentas/blender/console_radar.py): o
            // visitante liga o radar no botão da mesa e a tela PPI mostra a varredura.
            var console = ConsoleRadar1994(q, new Vector3(0.05f, 0f, 0.8f));

            // Antena do radar com o feixe que varre o céu.
            var antenaBase = new GameObject("Radar").transform;
            antenaBase.SetParent(q, false);
            antenaBase.localPosition = PosicaoRadar1994;
            var metal = Opaco("1994Metal", new Color(0.75f, 0.76f, 0.78f), smooth: 0.6f, metal: 0.4f);
            // Como num radar de verdade, o pedestal fica fixo no chão e só a cabeça
            // (garfo + refletor parabólico) gira em azimute sobre o rolamento do
            // topo do pedestal. 60°/s = 10 rpm, típico de radar de vigilância.
            var radar = Modelo("Kenney/space-kit/satelliteDish_large", antenaBase, Vector3.zero, 180f, 1.9f, Medida.MaiorLado);
            var giro = new GameObject("Giro").transform;
            giro.SetParent(antenaBase, false);
            var centroPrato = antenaBase.position + Vector3.up * 1.2f;
            var eixoPrato = antenaBase.rotation * Quaternion.Euler(-45f, 180f, 0f) * Vector3.forward;
            if (radar != null) SepararCabecaDoRadar(radar, giro, out centroPrato, out eixoPrato);
            var girar = giro.gameObject.AddComponent<Girar>();
            girar.grausPorSegundo = 60f;
            girar.ativo = false;
            // O feixe sai do centro do prato, para onde o refletor aponta.
            var inclinacao = new GameObject("Inclinação").transform;
            inclinacao.SetParent(giro, false);
            inclinacao.SetPositionAndRotation(centroPrato, Quaternion.LookRotation(eixoPrato));
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

            // Foguete de sondagem Nike-Orion no lançador de trilho, ao longe, e a trajetória
            // até a ionosfera (Ferramentas/blender/nike_orion.py). Na Campanha Guará, a NASA
            // lançou de Alcântara Nike-Orion, Nike-Tomahawk e Black Brant (ago.–out. 1994).
            var rampa = new Vector3(-5f, 0f, 16f);
            if (Modelo("Guara/nike-orion-lancador", q, rampa, 180f, 11.8f) == null)
            {
                Bloco("Trilho de lançamento", q, rampa + new Vector3(0.35f, 3f, 0f), new Vector3(0.2f, 6f, 0.2f), metal, rotacao: new Vector3(-8f, 0f, 0f));
                Bloco("Foguete de sondagem", q, rampa + new Vector3(0f, 2.8f, 0f), new Vector3(0.35f, 2.4f, 0.35f),
                    Opaco("FogueteBranco", new Color(0.95f, 0.95f, 0.95f)), PrimitiveType.Capsule, new Vector3(-8f, 0f, 0f));
            }

            var trajetoriaGo = new GameObject("Trajetória");
            trajetoriaGo.transform.SetParent(q, false);
            var pontos = new Vector3[32];
            var inicio = rampa + new Vector3(0f, 12f, 1.6f);
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
            FichaReal(q, new Vector3(-0.55f, 0f, -0.85f), "Campanha Guará · ago.–out. 1994",
                "A NASA e o INPE lançaram de Alcântara foguetes Nike-Orion, Nike-Tomahawk e Black Brant; " +
                "um Black Brant chegou a 956 km de altitude. Radares e ionossondas em terra, perto de São Luís, " +
                "acompanharam a ionosfera sobre o equador magnético.");

            var ambiente = Fonte("Vento e mar", q, q.TransformPoint(new Vector3(0f, 2f, 3f)), false);

            var seq = Sequencia<Cena2Guara>("Cena 2 · 1994 Guará", 2, 2);
            seq.botaoRadar = console.botao;
            seq.brilhoBotao = console.brilho;
            seq.telaLigada = console.tela;
            seq.varredura = console.varredura;
            seq.antena = girar;
            seq.feixe = feixe;
            seq.ionosfera = ionosfera.transform;
            seq.rotuloIonosfera = rotuloIonosfera;
            seq.trajetoria = trajetoria;
            seq.pontoNoCeu = pontoNoCeu;
            seq.ambiente = ambiente;
            seq.bipe = bipe;

            LugarDoProximo(q, new Vector3(0.45f, 0f, -0.85f), false);   // atrás do visitante, ao lado da ficha
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
            // Sala de equipamentos do ponto de presença (PoP) da RNP, como num data
            // center: piso elevado de placas (com placas perfuradas no corredor frio,
            // por onde sai o ar do ar-condicionado de precisão), fileiras de racks de
            // 44U (2,1 m), calhas amarelas de fibra óptica no alto e a central de
            // monitoramento (NOC) com o telão. 10 × 9 m, pé-direito de 3,2 m.
            var escuro = Opaco("2009Parede", new Color(0.07f, 0.09f, 0.13f));
            SalaFechada(q, new Vector3(0f, 0f, 2f), new Vector3(10f, 3.2f, 9f), Opaco("2009Piso", new Color(0.3f, 0.32f, 0.36f), smooth: 0.5f), escuro, escuro);
            Luz("Luz da mesa", q, new Vector3(0f, 2.4f, 1f), LightType.Point, new Color(0.5f, 0.75f, 1f), 1.6f, 5f);
            Luz("Luz dos racks", q, new Vector3(0f, 3f, 3.2f), LightType.Point, new Color(0.75f, 0.85f, 1f), 1.1f, 8f);
            SalaDeEquipamentos2009(q);

            // Mesa-mapa com a ilha de São Luís.
            var tampo = Opaco("2009Tampo", new Color(0.03f, 0.06f, 0.12f), new Color(0.01f, 0.03f, 0.08f), smooth: 0.8f);
            Bloco("Tampo", q, new Vector3(0f, 0.85f, 1.15f), new Vector3(2f, 0.05f, 1.2f), tampo);
            var perna = Opaco("2009Perna", new Color(0.2f, 0.22f, 0.26f));
            foreach (var x in new[] { -0.9f, 0.9f })
            foreach (var z in new[] { 0.65f, 1.65f })
                Bloco("Perna", q, new Vector3(x, 0.42f, z), new Vector3(0.06f, 0.84f, 0.06f), perna);

            // Mapa da Ilha de São Luís em relevo (Ferramentas/blender/mapa_sao_luis.py).
            var centroIlha = new Vector3(0f, 0.875f, 1.2f);
            var basesPinos = MapaSaoLuis(q, centroIlha, 1.05f);

            // Os seis pinos (instituições) e o ponto grande para tocar.
            var ponto = new Vector3(0f, 0.96f, 0.62f);
            var cabecaPino = Opaco("2009Pino", new Color(0.6f, 0.7f, 0.8f), new Color(0.05f, 0.08f, 0.1f));
            var hasteMat = Opaco("2009Haste", new Color(0.7f, 0.7f, 0.75f));
            var pinos = new List<Renderer>();
            var feixes = new List<LinhaProgressiva>();
            for (var i = 0; i < basesPinos.Length; i++)
            {
                var baseP = basesPinos[i];
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
            FichaReal(q, new Vector3(-0.85f, 0f, -0.45f), "Rede Metropolitana · São Luís",
                "Projeto Redecomep, da RNP, com recursos da Finep. Começou com 43 km de fibra óptica ligando " +
                "seis instituições de ensino e pesquisa, com investimento de R$ 668 mil: a 16ª rede " +
                "metropolitana em operação no país.");
            var contador = Texto("Contador", q, new Vector3(0f, 1.6f, 1.7f), "0 km", 0.13f, 2f, Color.white);
            contador.fontStyle = FontStyles.Bold;

            // Duas fileiras de 12 racks; a da frente mostra os LEDs para o visitante.
            var rackMat = Opaco("2009Rack", new Color(0.12f, 0.12f, 0.14f), metal: 0.5f);
            var ledAzul = Opaco("2009Led", Color.black, new Color(0.2f, 0.6f, 1f));
            var gruposRede = new[] { new List<GameObject>(), new List<GameObject>(), new List<GameObject>() };
            foreach (var zFila in new[] { 3.4f, 5.4f })
            for (var r = 0; r < 12; r++)
            {
                var x = -3.3f + r * 0.6f;
                Bloco($"Rack {zFila}-{r}", q, new Vector3(x, 1.05f, zFila), new Vector3(0.58f, 2.1f, 1.07f), rackMat);
                if (zFila > 4f) continue;
                for (var l = 0; l < 8; l++)
                for (var c = 0; c < 3; c++)
                    gruposRede[(c + l + r) % 3].Add(Bloco("LED", q, new Vector3(x - 0.15f + c * 0.15f, 0.45f + l * 0.2f, 2.86f),
                        new Vector3(0.04f, 0.02f, 0.01f), ledAzul));
            }

            var controleRede = new GameObject("Controle dos LEDs");
            controleRede.transform.SetParent(q, false);
            controleRede.transform.localPosition = new Vector3(0f, 1f, 3.4f);
            var pisca = controleRede.AddComponent<PiscaLeds>();
            pisca.leds = new[]
            {
                Combinar("LEDs A", q, gruposRede[0], ledAzul),
                Combinar("LEDs B", q, gruposRede[1], ledAzul),
                Combinar("LEDs C", q, gruposRede[2], ledAzul)
            };
            pisca.cor = new Color(0.3f, 0.7f, 1f);

            var ambiente = Fonte("Servidores", q, q.TransformPoint(new Vector3(0f, 1f, 3f)), true);
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

            LugarDoProximo(q, new Vector3(0.8f, 0.875f, 0.68f), true);   // na borda da mesa-mapa (atrás da marca já é a parede real)
            return Salvar(cena, LayoutTrilha.Cenas[3]);
        }

        /// <summary>
        /// Mapa da Ilha de São Luís deitado na mesa, com os nomes dos municípios.
        /// Devolve onde ficam os pinos das instituições (pontos "rede" do JSON).
        /// Sem o modelo, os pinos ficam numa elipse em volta do centro.
        /// </summary>
        static Vector3[] MapaSaoLuis(Transform q, Vector3 centro, float profundidade)
        {
            var anotacao = AssetDatabase.LoadAssetAtPath<TextAsset>(PastaModelos + "Mapa/mapa-sao-luis.json");
            var mapa = Modelo("Mapa/mapa-sao-luis", q, centro, 180f, profundidade, Medida.Profundidade);
            if (mapa == null || anotacao == null)
            {
                return Enumerable.Range(0, 6).Select(i =>
                {
                    var a = (i * 60f + 20f) * Mathf.Deg2Rad;
                    return centro + new Vector3(Mathf.Cos(a) * 0.48f, 0f, Mathf.Sin(a) * 0.27f);
                }).ToArray();
            }

            var dados = JsonUtility.FromJson<AnotacaoMapa>(anotacao.text);
            var largura = profundidade * dados.proporcao;
            // Acima do relevo mais alto (São Luís, ~4,5 cm depois da escala).
            var topo = centro.y + 0.048f;
            var pinos = new List<Vector3>();
            foreach (var p in dados.pontos)
            {
                var pos = new Vector3(centro.x + (p.u - 0.5f) * largura, topo, centro.z + (p.v - 0.5f) * profundidade);
                if (p.tipo == "rede")
                {
                    pinos.Add(pos);
                    continue;
                }

                // Nome do município deitado na mesa, inclinado para o visitante ler.
                var rotulo = Texto($"Município {p.nome.Replace('\n', ' ')}", q, pos + Vector3.up * 0.004f, p.nome, 0.032f, 0.3f,
                    new Color(0.12f, 0.12f, 0.25f));
                rotulo.fontStyle = FontStyles.Bold;
                rotulo.transform.localRotation = Quaternion.Euler(85f, 0f, 0f);
            }

            return pinos.ToArray();
        }

        /// <summary>
        /// Céu estrelado: quadradinhos virados para o visitante numa cúpula (um draw
        /// call só). Posições fixas (semente) para a cena sair igual a cada construção.
        /// </summary>
        static Mesh MalhaEstrelas(float raio, int quantidade)
        {
            const string caminho = PastaGerado + "/Estrelas.asset";
            var rng = new System.Random(1225);
            var vertices = new Vector3[quantidade * 4];
            var triangulos = new int[quantidade * 6];
            for (var i = 0; i < quantidade; i++)
            {
                // Mais estrelas perto do zênite que no horizonte (a neblina apaga as baixas).
                var elevacao = Mathf.Asin(Mathf.Lerp(0.12f, 1f, (float)rng.NextDouble()));
                var azimute = (float)rng.NextDouble() * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(elevacao) * Mathf.Sin(azimute), Mathf.Sin(elevacao), Mathf.Cos(elevacao) * Mathf.Cos(azimute));
                var tamanho = raio * (0.0009f + 0.0022f * (float)(rng.NextDouble() * rng.NextDouble() * rng.NextDouble()));
                var direita = Vector3.Cross(Vector3.up, dir).normalized;
                if (direita == Vector3.zero) direita = Vector3.right;
                var cima = Vector3.Cross(dir, direita);
                var centro = dir * raio;
                vertices[i * 4] = centro + (-direita - cima) * tamanho;
                vertices[i * 4 + 1] = centro + (direita - cima) * tamanho;
                vertices[i * 4 + 2] = centro + (direita + cima) * tamanho;
                vertices[i * 4 + 3] = centro + (-direita + cima) * tamanho;
                triangulos[i * 6] = i * 4;
                triangulos[i * 6 + 1] = i * 4 + 2;
                triangulos[i * 6 + 2] = i * 4 + 1;
                triangulos[i * 6 + 3] = i * 4;
                triangulos[i * 6 + 4] = i * 4 + 3;
                triangulos[i * 6 + 5] = i * 4 + 2;
            }

            var malha = AssetDatabase.LoadAssetAtPath<Mesh>(caminho);
            if (malha == null)
            {
                malha = new Mesh { name = "Estrelas" };
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
            // Laboratório Aberto como os espaços maker do SENAI: um salão amplo e claro
            // (12 × 10 m, pé-direito de 3,6 m), com a bancada de prototipagem no meio,
            // o quadro de ideias, as máquinas (cortadora a laser, fresadora CNC,
            // impressoras 3D) junto às paredes e ilhas de trabalho ao fundo.
            var parede = Opaco("2018Parede", new Color(0.93f, 0.92f, 0.9f));
            SalaFechada(q, new Vector3(0.5f, 0f, 2.5f), new Vector3(12f, 3.6f, 10f), Opaco("2018Piso", new Color(0.75f, 0.6f, 0.42f), smooth: 0.35f), parede, parede);
            var laranja = Opaco("SenaiLaranja", new Color(0.95f, 0.45f, 0.1f));
            var azul = Opaco("SenaiAzul", new Color(0.05f, 0.3f, 0.6f));
            Bloco("Faixa laranja", q, new Vector3(0.5f, 1.2f, 7.44f), new Vector3(12f, 0.15f, 0.02f), laranja);
            Bloco("Faixa azul", q, new Vector3(0.5f, 1.05f, 7.44f), new Vector3(12f, 0.08f, 0.02f), azul);
            Texto("Placa", q, new Vector3(0.5f, 2.75f, 7.42f), "SENAI · Laboratório Aberto · 2 mar. 2018", 0.26f, 8f, new Color(0.12f, 0.18f, 0.3f));
            var luminaria = Opaco("2018Luminaria", Color.white, Color.white * 1.4f);
            for (var i = 0; i < 3; i++)
            for (var j = 0; j < 3; j++)
                Bloco($"Luminária {i}-{j}", q, new Vector3(-3f + i * 3.5f, 3.57f, -0.5f + j * 3.3f), new Vector3(1.4f, 0.03f, 0.3f), luminaria);
            Luz("Luz do fundo", q, new Vector3(1f, 3.1f, 5f), LightType.Point, new Color(1f, 0.95f, 0.88f), 1.3f, 9f);
            FichaReal(q, new Vector3(-0.55f, 0f, -0.85f), "Laboratório Aberto · SENAI",
                "Espaço maker aberto a estudantes, pesquisadores e empreendedores, com impressão 3D, " +
                "corte a laser e eletrônica para criar protótipos e testar ideias antes de virarem produto.");
            Luz("Luz da oficina", q, new Vector3(0f, 3.1f, 1f), LightType.Point, new Color(1f, 0.93f, 0.82f), 1.7f, 9f);
            var janela = Opaco("2018Janela", new Color(0.8f, 0.9f, 1f), new Color(0.8f, 0.9f, 1f) * 1.2f);
            for (var i = 0; i < 5; i++)
                Bloco($"Janela {i}", q, new Vector3(-5.44f, 1.9f, -1.5f + i * 2f), new Vector3(0.02f, 1.4f, 1.3f), janela);

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

            Bloco("Suporte do monitor", q, new Vector3(0.52f, 1.0f, 1.05f), new Vector3(0.05f, 0.2f, 0.05f), Opaco("Metal", Color.white));
            Bloco("Monitor", q, new Vector3(0.52f, 1.25f, 1.05f), new Vector3(0.62f, 0.4f, 0.03f), Opaco("2018Monitor", new Color(0.1f, 0.1f, 0.12f)));
            Bloco("Tela CAD", q, new Vector3(0.52f, 1.25f, 1.033f), new Vector3(0.57f, 0.35f, 0.005f), Opaco("2018Tela", Color.black, new Color(0.04f, 0.08f, 0.2f)));
            var cadGo = new GameObject("Desenho digital (CAD)");
            cadGo.transform.SetParent(q, false);
            var cadLinha = cadGo.AddComponent<LineRenderer>();
            cadLinha.widthMultiplier = 0.005f;
            cadLinha.sharedMaterial = LinhaSolida("Linha");
            cadLinha.startColor = cadLinha.endColor = new Color(0.4f, 0.9f, 1f);
            var cad = cadGo.AddComponent<LinhaProgressiva>();
            cad.pontos = DesenhoLampada(new Vector3(0.52f, 1.28f, 1.028f), 0.085f);

            // Impressora FDM cartesiana (Ender-3) em tamanho real, feita no Blender
            // (Ferramentas/blender/impressora_3d.py) com as partes de cada eixo separadas.
            var impressoraRaiz = new GameObject("Impressora 3D").transform;
            impressoraRaiz.SetParent(q, false);
            impressoraRaiz.localPosition = new Vector3(-0.08f, 0.905f, 0.86f);
            var impressora = impressoraRaiz.gameObject.AddComponent<ImpressaoEmCamadas>();
            var modeloImpressora = Modelo("Impressora/impressora-3d", impressoraRaiz, Vector3.zero, 180f, 0.675f);
            var pontoBico = new Vector3(0f, 0.025f, 0f);
            if (modeloImpressora != null)
            {
                var m = modeloImpressora.transform;
                impressora.mesa = m.Find("Mesa");
                impressora.portico = m.Find("Portico");
                impressora.cabecote = m.Find("Cabecote");
                // A mesa foi modelada centrada sob o bico: o topo dela no centro é
                // onde sai a primeira camada.
                var b = impressora.mesa.GetComponent<Renderer>().bounds;
                pontoBico = impressoraRaiz.InverseTransformPoint(new Vector3(b.center.x, b.max.y, b.center.z));
            }
            else
            {
                Bloco("Mesa", impressoraRaiz, new Vector3(0f, 0.02f, 0f), new Vector3(0.24f, 0.01f, 0.24f), Opaco("2018Mesa", new Color(0.2f, 0.2f, 0.25f)));
            }

            var pecaPivo = new GameObject("Peça (pivô)").transform;
            pecaPivo.SetParent(impressoraRaiz, false);
            pecaPivo.localPosition = pontoBico;
            Bloco("Peça", pecaPivo, new Vector3(0f, 0.5f, 0f), Vector3.one, Opaco("2018Filamento", new Color(0.22f, 0.87f, 0.93f), new Color(0.02f, 0.12f, 0.14f)));
            pecaPivo.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            impressora.peca = pecaPivo;
            var somImpressora = Fonte("Som da impressora", q, impressoraRaiz.position, true);

            // Protótipo pronto: um pequeno drone que gira na frente da bancada.
            var drone = new GameObject("Protótipo (drone)").transform;
            drone.SetParent(q, false);
            drone.localPosition = new Vector3(0.4f, 1.15f, 0.6f);
            // Quadricóptero de PLA ciano feito na impressora (Ferramentas/blender/drone.py).
            var modeloDrone = Modelo("Drone/drone", drone, new Vector3(0f, -0.03f, 0f), 0f, 0.44f, Medida.MaiorLado);
            if (modeloDrone != null)
            {
                foreach (Transform parte in modeloDrone.transform)
                {
                    if (!parte.name.StartsWith("Helice")) continue;
                    var giro = parte.gameObject.AddComponent<Girar>();
                    giro.eixo = parte.InverseTransformDirection(drone.up);
                    giro.grausPorSegundo = parte.name.EndsWith("1") || parte.name.EndsWith("3") ? 1440f : -1440f;
                }
            }
            else
            {
                var corpo = Opaco("2018Drone", new Color(0.2f, 0.22f, 0.26f), smooth: 0.6f);
                Bloco("Corpo", drone, Vector3.zero, new Vector3(0.12f, 0.045f, 0.12f), corpo);
                Bloco("Braço 1", drone, Vector3.zero, new Vector3(0.36f, 0.015f, 0.025f), corpo, rotacao: new Vector3(0f, 45f, 0f));
                Bloco("Braço 2", drone, Vector3.zero, new Vector3(0.36f, 0.015f, 0.025f), corpo, rotacao: new Vector3(0f, -45f, 0f));
            }

            drone.gameObject.AddComponent<Girar>().grausPorSegundo = 45f;

            // Quadro de ideias no meio do salão, de frente para o visitante, com post-its.
            var cores = new[] { new Color(1f, 0.9f, 0.3f), new Color(1f, 0.5f, 0.6f), new Color(0.5f, 0.9f, 0.6f), new Color(0.5f, 0.8f, 1f) };
            var quadroIdeias = new Vector3(-2.5f, 1.45f, 3.7f);
            Bloco("Quadro de ideias", q, quadroIdeias, new Vector3(2.2f, 1.3f, 0.04f), parede);
            Bloco("Moldura do quadro", q, quadroIdeias + new Vector3(0f, 0f, 0.025f), new Vector3(2.3f, 1.4f, 0.02f), azul);
            foreach (var x in new[] { -1.05f, 1.05f })
                Bloco("Pé do quadro", q, quadroIdeias + new Vector3(x, -0.73f, 0f), new Vector3(0.05f, 1.45f, 0.3f), azul);
            var rngPostit = new System.Random(2018);
            for (var i = 0; i < 28; i++)
            {
                var p = quadroIdeias + new Vector3(-0.9f + (i % 7) * 0.3f + (float)rngPostit.NextDouble() * 0.06f,
                    -0.42f + (i / 7) * 0.28f + (float)rngPostit.NextDouble() * 0.04f, -0.025f);
                Bloco($"Post-it {i}", q, p, new Vector3(0.14f, 0.14f, 0.005f), Opaco($"PostIt{i % 4}", cores[i % 4]));
            }

            // Área de máquinas, à direita: cortadora a laser, fresadora CNC e estante
            // com impressoras 3D; quadro de ferramentas na parede do fundo.
            var maquina = Opaco("2018Laser", new Color(0.85f, 0.85f, 0.88f));
            var metalEscuro = Opaco("Metal", new Color(0.6f, 0.6f, 0.62f), metal: 0.6f);
            Bloco("Quadro de ferramentas", q, new Vector3(3.6f, 1.7f, 7.43f), new Vector3(2f, 1f, 0.03f), Opaco("2018Ferramentas", new Color(0.3f, 0.32f, 0.3f)));
            Bloco("Cortadora a laser", q, new Vector3(3.7f, 0.45f, 3.3f), new Vector3(0.8f, 0.9f, 1.3f), maquina);
            Bloco("Janela da cortadora", q, new Vector3(3.7f, 0.91f, 3.3f), new Vector3(0.6f, 0.02f, 1.05f), Opaco("2018LaserJanela", new Color(0.4f, 0.1f, 0.05f), new Color(0.3f, 0.05f, 0f)));
            Bloco("Mesa da CNC", q, new Vector3(4.9f, 0.4f, 5.4f), new Vector3(1.4f, 0.8f, 2f), maquina);
            Bloco("Pórtico da CNC", q, new Vector3(4.9f, 1.05f, 5.1f), new Vector3(1.5f, 0.12f, 0.15f), metalEscuro);
            foreach (var x in new[] { -0.7f, 0.7f })
                Bloco("Coluna da CNC", q, new Vector3(4.9f + x, 0.95f, 5.1f), new Vector3(0.1f, 0.3f, 0.15f), metalEscuro);
            Bloco("Cabeçote da CNC", q, new Vector3(4.7f, 0.93f, 5.0f), new Vector3(0.15f, 0.25f, 0.15f), laranja);
            Bloco("Placa na CNC", q, new Vector3(4.9f, 0.81f, 5.5f), new Vector3(1f, 0.02f, 1.2f), madeira);
            for (var i = 0; i < 3; i++)
            for (var j = 0; j < 2; j++)
                Bloco($"Impressora na estante {i}-{j}", q, new Vector3(6.15f, 0.95f + j * 0.75f, 1.6f + i * 0.6f), new Vector3(0.4f, 0.42f, 0.42f), azul);
            for (var j = 0; j < 3; j++)
                Bloco($"Prateleira {j}", q, new Vector3(6.15f, 0.72f + j * 0.75f, 2.2f), new Vector3(0.55f, 0.04f, 2.1f), madeira);

            var ambiente = Fonte("Oficina", q, q.TransformPoint(new Vector3(0f, 2f, 2.5f)), false);

            var (estudante, pesquisador, empreendedora) = Decorar2018(q);

            var seq = Sequencia<Cena4Senai>("Cena 4 · 2018 SENAI", 4, 4);
            seq.estudante = estudante;
            seq.pesquisador = pesquisador;
            seq.empreendedora = empreendedora;
            seq.lampada = poke;
            seq.brilhoLampada = brilhoLampada;
            seq.desenhoDigital = cad;
            seq.impressora = impressora;
            seq.prototipo = drone;
            seq.somImpressora = somImpressora;
            seq.ambiente = ambiente;

            LugarDoProximo(q, new Vector3(0.45f, 0f, -0.85f), false);   // atrás do visitante, ao lado da ficha
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

            FichaReal(q, new Vector3(-0.55f, 0f, -0.85f), "5G em São Luís · 19 set. 2022",
                "A Anatel liberou a faixa de 3,5 GHz na cidade. Claro, TIM e Vivo tinham de ativar ao menos " +
                "14 antenas cada. Para usar, bastava um celular compatível, sem trocar chip nem plano.");
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

            LugarDoProximo(q, new Vector3(0.45f, 0f, -0.85f), false);   // atrás do visitante, ao lado da ficha
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
            // Noite: o HANBIT-Nano decolou às 22h13 de 22 de dezembro de 2025 (FAB).
            // O "sol" fica abaixo do horizonte para o céu procedural escurecer; quem
            // ilumina são o luar fraco (ambiente), os refletores da plataforma e a lua.
            Ambiente(new Color(0.24f, 0.27f, 0.38f),
                Ceu("CeuNoite", new Color(0.1f, 0.18f, 0.5f), 1.1f, 0.9f, new Color(0.03f, 0.04f, 0.06f), 0f),
                new Color(0.55f, 0.62f, 0.9f), 0.12f, new Vector3(-7f, 160f, 0f), true, new Color(0.06f, 0.08f, 0.14f), 0.0025f);

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
            // Centro de Lançamento de Alcântara: planalto no litoral, vegetação baixa,
            // chão de laterita avermelhada e as plataformas espalhadas pela área.
            Bloco("Vegetação", parteB, new Vector3(0f, -0.08f, 20f), new Vector3(240f, 0.1f, 240f), Opaco("2025Vegetacao", new Color(0.24f, 0.32f, 0.18f)));
            Bloco("Mar", parteB, new Vector3(0f, -0.06f, 110f), new Vector3(400f, 0.1f, 100f), Opaco("Mar", Color.white));
            var laterita = Opaco("2025Laterita", new Color(0.55f, 0.3f, 0.18f));
            Bloco("Área da plataforma (laterita)", parteB, new Vector3(0f, -0.055f, 25f), new Vector3(46f, 0.1f, 40f), laterita);
            Bloco("Mirante", parteB, new Vector3(0f, -0.05f, 1.5f), new Vector3(12f, 0.1f, 8f), Opaco("2025Patio", new Color(0.58f, 0.58f, 0.57f)));
            var asfalto = Opaco("2025Asfalto", new Color(0.16f, 0.16f, 0.17f));
            Bloco("Estrada da plataforma", parteB, new Vector3(0f, -0.045f, 10.5f), new Vector3(5f, 0.1f, 10f), asfalto);
            Bloco("Estrada da casamata", parteB, new Vector3(-9f, -0.045f, 7f), new Vector3(14f, 0.1f, 4f), asfalto, rotacao: new Vector3(0f, -20f, 0f));
            Bloco("Estrada da TMI", parteB, new Vector3(-24f, -0.045f, 55f), new Vector3(5f, 0.1f, 60f), asfalto, rotacao: new Vector3(0f, -28f, 0f));
            var gradil = Opaco("2025Gradil", new Color(0.8f, 0.6f, 0.1f));
            Bloco("Gradil", parteB, new Vector3(0f, 1f, 2.5f), new Vector3(4f, 0.05f, 0.05f), gradil);
            for (var i = 0; i <= 8; i++)
                Bloco($"Poste {i}", parteB, new Vector3(-2f + i * 0.5f, 0.5f, 2.5f), new Vector3(0.04f, 1f, 0.04f), gradil);

            // Plataforma do HANBIT-Nano (Ferramentas/blender/alcantara_cla.py): mesa de
            // lançamento, torre umbilical, tanques de LOX e metano, refletores e para-raios.
            var plataforma = new Vector3(0f, 0f, 25f);
            const float topoMesa = 2f;
            var modeloPlataforma = Modelo("Alcantara/plataforma-hanbit", parteB, plataforma, 180f, 32f);
            if (modeloPlataforma == null)
                Bloco("Plataforma", parteB, plataforma + new Vector3(0f, topoMesa / 2f, 0f), new Vector3(7f, topoMesa, 7f), Opaco("2025Plataforma", new Color(0.35f, 0.35f, 0.36f)));

            // Refletores acesos no alto das quatro torres de iluminação, virados para o foguete.
            var refletor = Opaco("2025Refletor", new Color(1f, 0.97f, 0.9f), new Color(2.2f, 2.1f, 1.8f));
            foreach (var (sx, sz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            {
                var poste = plataforma + new Vector3(sx * 9.5f, 15.2f, sz * 9.5f);
                var paraFoguete = Quaternion.LookRotation(plataforma + Vector3.up * 10f - poste);
                Bloco("Refletores", parteB, poste + paraFoguete * Vector3.forward * 0.2f, new Vector3(2f, 0.8f, 0.12f), refletor,
                    rotacao: paraFoguete.eulerAngles);
            }

            var holofote = Luz("Refletores da plataforma", parteB, plataforma + new Vector3(-9.5f, 15.2f, -9.5f), LightType.Spot,
                new Color(1f, 0.95f, 0.85f), 9f, 45f);
            holofote.spotAngle = 55f;
            holofote.transform.localRotation = Quaternion.LookRotation(plataforma + Vector3.up * 11f - holofote.transform.localPosition);

            // Luzes vermelhas de obstáculo aéreo no topo da torre e dos para-raios.
            var obstaculo = Opaco("2025Obstaculo", new Color(0.8f, 0.05f, 0.05f), new Color(1.6f, 0.05f, 0.03f));
            foreach (var topo in new[] { new Vector3(4.6f, 25.4f, 0f), new Vector3(-11.5f, 32.2f, 6.5f), new Vector3(11.5f, 32.2f, 6.5f) })
                AdicionarPulsar(Bloco("Luz de obstáculo", parteB, plataforma + topo, Vector3.one * 0.45f, obstaculo, PrimitiveType.Sphere),
                    new Color(1f, 0.1f, 0.05f), 2f, 0.5f);

            // HANBIT-Nano: dois estágios, 21,8 m x 1,4 m, apoiado na mesa de lançamento.
            var hanbit = new GameObject("HANBIT-Nano").transform;
            hanbit.SetParent(parteB, false);
            hanbit.localPosition = plataforma + new Vector3(0f, topoMesa, 0f);
            if (Modelo("Alcantara/foguete-hanbit-nano", hanbit, Vector3.zero, 180f, 21.8f) == null)
                Bloco("Corpo", hanbit, new Vector3(0f, 10.9f, 0f), new Vector3(1.4f, 10.9f, 1.4f), Opaco("2025Foguete", new Color(0.96f, 0.96f, 0.97f)), PrimitiveType.Cylinder);

            var nome = Texto("Nome no foguete", hanbit, new Vector3(0f, 8.5f, -0.72f), "HANBIT-Nano", 0.55f, 6f, new Color(0.1f, 0.15f, 0.35f));
            nome.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var marca = Texto("Innospace no foguete", hanbit, new Vector3(0f, 15.3f, -0.72f), "INNOSPACE", 0.38f, 3.5f, new Color(0.08f, 0.08f, 0.1f));
            marca.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            // Prédios do CLA: a Torre Móvel de Integração na plataforma do VLS, ao
            // fundo, e a casamata (controle avançado) à esquerda do mirante.
            Modelo("Alcantara/torre-movel-integracao", parteB, new Vector3(-46f, 0f, 82f), 152f, 34.9f);
            AdicionarPulsar(Bloco("Luz de obstáculo", parteB, new Vector3(-46f, 35.2f, 82f), Vector3.one * 0.7f, obstaculo, PrimitiveType.Sphere),
                new Color(1f, 0.1f, 0.05f), 2f, 0.4f);
            Modelo("Alcantara/casamata", parteB, new Vector3(-17f, 0f, 9f), 125f, 11f);
            Bloco("Janelas acesas", parteB, new Vector3(-13.6f, 2.2f, 7.4f), new Vector3(0.06f, 0.35f, 4f),
                Opaco("2025JanelaAcesa", new Color(1f, 0.85f, 0.55f), new Color(1.2f, 0.95f, 0.55f)), rotacao: new Vector3(0f, 35f, 0f));

            // Céu de dezembro: estrelas e lua.
            var estrelas = new GameObject("Estrelas");
            estrelas.transform.SetParent(parteB, false);
            estrelas.AddComponent<MeshFilter>().sharedMesh = MalhaEstrelas(140f, 320);
            var estrelasR = estrelas.AddComponent<MeshRenderer>();
            estrelasR.sharedMaterial = Opaco("2025Estrela", Color.black, new Color(1.6f, 1.6f, 1.7f));
            estrelasR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Bloco("Lua", parteB, new Vector3(-55f, 60f, 110f), Vector3.one * 5f, Opaco("2025Lua", Color.black, new Color(1.5f, 1.45f, 1.3f)), PrimitiveType.Sphere);

            // Relógio da contagem no mirante, como nos painéis de transmissão do lançamento.
            var relogio = new GameObject("Relógio da contagem").transform;
            relogio.SetParent(parteB, false);
            relogio.localPosition = new Vector3(-2.3f, 0f, 3.4f);
            relogio.localRotation = Quaternion.Euler(0f, -25f, 0f);
            var suporte = Opaco("2025Suporte", new Color(0.2f, 0.2f, 0.22f));
            foreach (var x in new[] { -0.6f, 0.6f })
                Bloco("Pé", relogio, new Vector3(x, 1f, 0.03f), new Vector3(0.06f, 2f, 0.06f), suporte);
            Bloco("Placar", relogio, new Vector3(0f, 2.1f, 0.03f), new Vector3(1.5f, 0.75f, 0.06f), Opaco("2025Placar", new Color(0.02f, 0.02f, 0.03f)));
            Texto("Missão", relogio, new Vector3(0f, 2.36f, -0.01f), "HANBIT-Nano · SPACEWARD · 22/12/2025", 0.07f, 1.4f, new Color(0.8f, 0.85f, 0.95f));
            var placar = Texto("Contagem", relogio, new Vector3(0f, 2.04f, -0.01f), "T-00:10", 0.32f, 1.4f, new Color(1f, 0.2f, 0.12f));
            placar.fontStyle = FontStyles.Bold;

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

            // Explosão no ar: bola de fogo, nuvem de fumaça e destroços em chamas caindo.
            var bola = Particulas("Bola de fogo", parteB, Vector3.zero, fogoMat, 1.8f, 13f, 16f, 0f, new Color(1f, 0.6f, 0.2f), 180f, maximo: 120);
            var fumacaExplosao = Particulas("Fumaça da explosão", parteB, Vector3.zero, fumacaMat, 8f, 6f, 22f, 0f,
                new Color(0.35f, 0.33f, 0.32f, 0.85f), 180f, gravidade: -0.02f, maximo: 80);
            var destrocos = Particulas("Destroços", parteB, Vector3.zero, fogoMat, 5f, 20f, 1.8f, 0f, new Color(1f, 0.75f, 0.35f), 180f,
                gravidade: 0.9f, maximo: 120);
            foreach (var ps in new[] { bola, fumacaExplosao, destrocos })
            {
                var forma = ps.shape;
                forma.shapeType = ParticleSystemShapeType.Sphere;
                forma.radius = 3f;
                var principal = ps.main;
                principal.loop = false;
            }

            decolagem.bolaDeFogo = bola;
            decolagem.fumacaExplosao = fumacaExplosao;
            decolagem.destrocos = destrocos;

            Decorar2025(parteB);

            // Engenheiro da equipe de lançamento, no mirante, que explica a falha.
            var engenheiro = Personagem("Kenney/mini-characters/character-male-d", parteB, new Vector3(2.9f, 0f, 1.0f), -110f, 1.75f,
                "emote-yes", "interact-right");
            DarVoz(engenheiro, "Engenheiro");

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
            Bloco("Fundo", resultado, new Vector3(0f, 0f, 0.02f), new Vector3(1.1f, 0.82f, 0.02f), Opaco("2025Resultado", new Color(0.3f, 0.06f, 0.06f)));
            Texto("Texto", resultado, Vector3.zero,
                "<b>Resultado da missão</b>\n<size=80%>Uma anomalia levou à perda da missão.\nO PION-BR2 não chegou à órbita.\n\n" +
                "Causa: vazamento na vedação da câmara de\ncombustão do 1º estágio, aos 33 s de voo.\n\n" +
                "<size=70%>Fontes: AEB, Relatório de gestão 2025;\nInnospace e CENIPA, 2026</size></size>",
                0.05f, 1.05f, Color.white).rectTransform.sizeDelta = new Vector2(1.05f, 0.78f);

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
            seq.engenheiro = engenheiro;
            seq.painelResultado = resultado;
            seq.avisoReconstituicao = aviso;
            seq.ambiente = ambiente;
            seq.relogio = placar;
            seq.vozContagem = Enumerable.Range(1, 5)
                .Select(i => AssetDatabase.LoadAssetAtPath<AudioClip>($"{Roteiro.PastaVozes}/{Roteiro.Nomes[6]}/contagem-{i}.ogg"))
                .Where(c => c != null).ToArray();

            // A cena abre em 2023; a parte de 2025 fica escondida até a virada.
            parteB.gameObject.SetActive(false);
            resultado.gameObject.SetActive(false);

            LugarDoProximo(q, new Vector3(0.45f, 0f, -0.85f), false);   // no mirante, atrás do visitante
            return Salvar(cena, LayoutTrilha.Cenas[6]);
        }
    }
}
