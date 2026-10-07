using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>Cena 2 — 1994, campanha Guará em Alcântara (1:00–1:35).</summary>
    public class Cena2Guara : EraSequence
    {
        public ProximityTrigger marcaRadar;
        public Pulsar brilhoMarca;
        public Girar antena;
        public GameObject feixe;
        public Transform ionosfera;
        public GameObject rotuloIonosfera;
        public LinhaProgressiva trajetoria;
        public Transform pontoNoCeu;
        public AudioSource ambiente;
        public AudioSource bipe;

        protected override IEnumerator Rodar()
        {
            Cena1Telma.Tocar(ambiente, "vento", true, 0.6f);
            antena.ativo = false;
            Mostrar(feixe, false);
            Mostrar(ionosfera.gameObject, false);
            Mostrar(rotuloIonosfera, false);
            brilhoMarca.Definir(false);

            yield return Dizer("intro");
            brilhoMarca.Definir(true);
            ctx.Enquadrar(marcaRadar.transform.position);
            yield return Dizer("pesquisadores");

            yield return EsperarProximidade(marcaRadar, "dica");
            brilhoMarca.Definir(false);

            antena.ativo = true;
            Mostrar(feixe, true);
            StartCoroutine(Bipar());
            StartCoroutine(Surgir(ionosfera, 1.5f));
            Mostrar(rotuloIonosfera, true);
            StartCoroutine(trajetoria.Desenhar(5f));
            ctx.Enquadrar(pontoNoCeu.position);

            yield return Dizer("ionosfera");
            ctx.Capturar("1994");
            yield return Dizer("fim");
        }

        IEnumerator Bipar()
        {
            while (true)
            {
                bipe.PlayOneShot(ProceduralAudio.Obter("bipe"), 0.6f);
                yield return new WaitForSeconds(360f / Mathf.Max(1f, antena.grausPorSegundo));
            }
        }
    }
}
