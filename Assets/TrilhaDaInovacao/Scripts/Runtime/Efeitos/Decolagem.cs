using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Reconstituição da decolagem do HANBIT-Nano (2025): ignição com clarão,
    /// fogo e fumaça na base, e subida acelerada. A câmera do visitante nunca
    /// se move; o "tremor" vem só do som grave.
    /// </summary>
    public class Decolagem : MonoBehaviour
    {
        public Transform foguete;
        public ParticleSystem fogo;
        public ParticleSystem fumacaBase;
        public ParticleSystem rastro;
        public Light clarao;
        public AudioSource motor;
        public float aceleracao = 7f;

        Vector3 posicaoInicial;
        bool pronto;

        void Awake()
        {
            posicaoInicial = foguete.position;
            pronto = true;
            Parar();
        }

        public void Parar()
        {
            if (!pronto) return;
            foguete.position = posicaoInicial;
            foguete.gameObject.SetActive(true);
            foreach (var p in new[] { fogo, fumacaBase, rastro })
            {
                if (p != null) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (clarao != null) clarao.intensity = 0f;
            if (motor != null) motor.Stop();
        }

        public IEnumerator Ignicao(float duracao)
        {
            if (motor != null)
            {
                motor.clip = ProceduralAudio.Obter("motor");
                motor.loop = true;
                motor.volume = 0f;
                motor.Play();
            }

            fogo?.Play();
            fumacaBase?.Play();

            for (var t = 0f; t < duracao; t += Time.deltaTime)
            {
                var k = t / duracao;
                if (clarao != null) clarao.intensity = Mathf.Lerp(0f, 12f, k);
                if (motor != null) motor.volume = Mathf.Lerp(0f, 0.7f, k);
                yield return null;
            }
        }

        public IEnumerator Subir(float duracao)
        {
            rastro?.Play();
            for (var t = 0f; t < duracao; t += Time.deltaTime)
            {
                foguete.position = posicaoInicial + Vector3.up * (0.5f * aceleracao * t * t);
                var k = t / duracao;
                if (clarao != null) clarao.intensity = Mathf.Lerp(12f, 2f, k);
                if (motor != null) motor.volume = Mathf.Lerp(0.7f, 0.15f, k * k);
                if (t > 2.5f) fumacaBase?.Stop();
                yield return null;
            }

            fogo?.Stop();
            rastro?.Stop();
            if (motor != null) motor.Stop();
            if (clarao != null) clarao.intensity = 0f;
            foguete.gameObject.SetActive(false);
        }
    }
}
