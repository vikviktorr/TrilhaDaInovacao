using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace TrilhaDaInovacao.EditorTools
{
    /// <summary>
    /// Ícone do jogo desenhado por código: a trilha em U com as seis marcas,
    /// como no mapa do documento. Vai para o Player Settings e para os
    /// instaladores (Ferramentas/icone.png).
    /// </summary>
    public static class Icone
    {
        const int Tamanho = 512;

        public static void GerarEAplicar(string caminho)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(caminho) ?? "Assets");
            var png = Desenhar();
            File.WriteAllBytes(caminho, png);
            Directory.CreateDirectory("Ferramentas");
            File.WriteAllBytes("Ferramentas/icone.png", png);

            AssetDatabase.ImportAsset(caminho);
            var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { textura }, IconKind.Any);
        }

        static byte[] Desenhar()
        {
            var tex = new Texture2D(Tamanho, Tamanho, TextureFormat.RGBA32, false);
            var fundoA = new Color(0.04f, 0.09f, 0.22f);
            var fundoB = new Color(0.08f, 0.28f, 0.55f);
            var trilha = new Color(0.35f, 0.85f, 1f);

            // Pontos do U, na mesma ordem do LayoutTrilha (visto de cima).
            var pontos = new Vector2[]
            {
                new Vector2(110, 150), new Vector2(210, 150), new Vector2(310, 150), new Vector2(410, 150),
                new Vector2(410, 330), new Vector2(310, 330), new Vector2(210, 330)
            };

            for (var y = 0; y < Tamanho; y++)
            for (var x = 0; x < Tamanho; x++)
            {
                var p = new Vector2(x, y);
                var cor = Color.Lerp(fundoA, fundoB, y / (float)Tamanho);

                // Cantos arredondados.
                var canto = Vector2.Max(Vector2.zero, new Vector2(Mathf.Abs(x - 255.5f), Mathf.Abs(y - 255.5f)) - Vector2.one * 176f);
                var alfa = Mathf.Clamp01(80f - canto.magnitude);

                var dTrilha = float.MaxValue;
                for (var i = 1; i < pontos.Length; i++) dTrilha = Mathf.Min(dTrilha, DistSegmento(p, pontos[i - 1], pontos[i]));
                if (dTrilha < 14f) cor = Color.Lerp(cor, trilha * 0.75f, Mathf.Clamp01(14f - dTrilha));

                for (var i = 0; i < pontos.Length; i++)
                {
                    var d = Vector2.Distance(p, pontos[i]);
                    var raio = i == 0 ? 26f : 34f;
                    if (d < raio + 1f)
                    {
                        var dentro = i == 0 ? new Color(0.6f, 0.65f, 0.75f) : (i == pontos.Length - 1 ? new Color(1f, 0.6f, 0.2f) : Color.white);
                        cor = Color.Lerp(cor, dentro, Mathf.Clamp01(raio + 1f - d));
                    }
                }

                cor.a = alfa;
                tex.SetPixel(x, y, cor);
            }

            tex.Apply();
            var png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return png;
        }

        static float DistSegmento(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
