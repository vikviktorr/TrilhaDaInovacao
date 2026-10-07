using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>Cena 1 — 1986, central Trópico-R da TELMA (0:25–1:00).</summary>
    public class Cena1Telma : EraSequence
    {
        public PokeTrigger telefone;
        public Pulsar brilhoTelefone;
        public AudioSource somTelefone;
        public PulsoNoCaminho ligacao;
        public Pulsar brilhoTelefoneDestino;
        public AudioSource somTelefoneDestino;
        public AudioSource ambiente;

        protected override IEnumerator Rodar()
        {
            Tocar(ambiente, "zumbido", true, 0.5f);
            brilhoTelefone.Definir(false);
            brilhoTelefoneDestino.Definir(false);

            yield return Dizer("intro");

            Tocar(somTelefone, "toque", true, 0.8f);
            brilhoTelefone.Definir(true);
            ctx.Enquadrar(telefone.transform.position);
            yield return Dizer("primeira");

            ctx.Capturar("1986");
            yield return EsperarToque(telefone, "dica");
            somTelefone.Stop();
            brilhoTelefone.Definir(false);
            somTelefone.PlayOneShot(ProceduralAudio.Obter("discagem"), 0.7f);

            var percurso = StartCoroutine(ligacao.Percorrer());
            StartCoroutine(CapturarDepois(1.2f, "1986-ligacao"));
            yield return Dizer("toque");
            yield return percurso;

            Tocar(somTelefoneDestino, "toque", true, 0.8f);
            brilhoTelefoneDestino.Definir(true);
            ctx.Enquadrar(brilhoTelefoneDestino.transform.position);
            yield return Dizer("fim");
            somTelefoneDestino.Stop();
            brilhoTelefoneDestino.Definir(false);
        }

        IEnumerator CapturarDepois(float segundos, string nome)
        {
            yield return new WaitForSeconds(segundos);
            ctx.Enquadrar(ligacao.bola.position);
            yield return null;
            ctx.Capturar(nome);
        }

        internal static void Tocar(AudioSource fonte, string som, bool loop, float volume)
        {
            if (fonte == null) return;
            fonte.clip = ProceduralAudio.Obter(som);
            fonte.loop = loop;
            fonte.volume = volume;
            fonte.Play();
        }
    }
}
