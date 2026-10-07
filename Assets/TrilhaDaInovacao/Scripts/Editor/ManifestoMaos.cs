using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace TrilhaDaInovacao.EditorTools
{
    /// <summary>
    /// O Quest só libera o rastreamento de mãos se o manifesto pedir. Este
    /// passo acrescenta a permissão, a feature e a frequência alta de
    /// rastreamento ao AndroidManifest gerado, sem duplicar o que já existir.
    /// </summary>
    public class ManifestoMaos : IPostGenerateGradleAndroidProject
    {
        const string Android = "http://schemas.android.com/apk/res/android";

        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string caminho)
        {
            var arquivo = Path.Combine(caminho, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(arquivo)) return;

            var doc = new XmlDocument();
            doc.Load(arquivo);
            var manifesto = doc.DocumentElement;
            var aplicacao = manifesto?.SelectSingleNode("application") as XmlElement;
            if (manifesto == null || aplicacao == null) return;

            Garantir(doc, manifesto, "uses-permission", "com.oculus.permission.HAND_TRACKING");
            var feature = Garantir(doc, manifesto, "uses-feature", "oculus.software.handtracking");
            feature.SetAttribute("required", Android, "false");

            var frequencia = Garantir(doc, aplicacao, "meta-data", "com.oculus.handtracking.frequency");
            frequencia.SetAttribute("value", Android, "HIGH");
            var versao = Garantir(doc, aplicacao, "meta-data", "com.oculus.handtracking.version");
            versao.SetAttribute("value", Android, "V2.0");

            doc.Save(arquivo);
        }

        static XmlElement Garantir(XmlDocument doc, XmlElement pai, string tag, string nome)
        {
            foreach (XmlNode no in pai.SelectNodes(tag))
            {
                if (no is XmlElement e && e.GetAttribute("name", Android) == nome) return e;
            }

            var novo = doc.CreateElement(tag);
            novo.SetAttribute("name", Android, nome);
            pai.AppendChild(novo);
            return novo;
        }
    }
}
