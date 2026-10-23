using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Reconstituição da decolagem do HANBIT-Nano (2025): ignição com clarão,
    /// fogo e fumaça na base, subida acelerada e a perda do foguete no ar.
    /// No voo real, aos 33 s a vedação da câmara de combustão do 1º estágio
    /// falhou, a câmara rompeu e o sistema de terminação de voo destruiu o
    /// foguete (Innospace e CENIPA, 2026); aqui o tempo é encurtado. A câmera
    /// do visitante nunca se move; o "tremor" vem só do som grave.
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

        [Header("Explosão")]
        public ParticleSystem bolaDeFogo;
        public ParticleSystem fumacaExplosao;
        public ParticleSystem destrocos;

        /// <summary>Onde o foguete explodiu (para enquadrar e falar dele depois).</summary>
        public Vector3 PontoDaExplosao { get; private set; }

        Vector3 posicaoInicial;
        Vector3 posicaoClarao, posicaoMotor;
        float alcanceClarao;
        bool pronto;

        void Awake()
        {
            posicaoInicial = foguete.position;
            if (clarao != null)
            {
                posicaoClarao = clarao.transform.position;
                alcanceClarao = clarao.range;
            }

            if (motor != null) posicaoMotor = motor.transform.position;
            pronto = true;
            Parar();
        }

        public void Parar()
        {
            if (!pronto) return;
            foguete.position = posicaoInicial;
            foguete.gameObject.SetActive(true);
            foreach (var p in new[] { fogo, fumacaBase, rastro, bolaDeFogo, fumacaExplosao, destrocos })
            {
                if (p != null) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (clarao != null)
            {
                clarao.intensity = 0f;
                clarao.range = alcanceClarao;
                clarao.transform.position = posicaoClarao;
            }

            if (motor != null)
            {
                motor.Stop();
                motor.transform.position = posicaoMotor;
            }
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

        /// <summary>
        /// Sobe como em <see cref="Subir"/> e, depois de <paramref name="ateFalha"/>
        /// segundos, o foguete se desfaz numa bola de fogo, com destroços em chamas
        /// caindo e o estrondo chegando um pouco depois do clarão (o som é mais lento).
        /// </summary>
        public IEnumerator SubirEExplodir(float ateFalha, float depois)
        {
            rastro?.Play();
            for (var t = 0f; t < ateFalha; t += Time.deltaTime)
            {
                foguete.position = posicaoInicial + Vector3.up * (0.5f * aceleracao * t * t);
                var k = t / ateFalha;
                if (clarao != null) clarao.intensity = Mathf.Lerp(12f, 3f, k);
                if (motor != null) motor.volume = Mathf.Lerp(0.7f, 0.3f, k);
                if (t > 2.5f) fumacaBase?.Stop();
                yield return null;
            }

            // A câmara rompe: o motor some e o foguete vira bola de fogo.
            var ponto = foguete.position + Vector3.up * 10f;
            PontoDaExplosao = ponto;
            fogo?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            rastro?.Stop();
            foguete.gameObject.SetActive(false);
            foreach (var (p, n) in new[] { (bolaDeFogo, 70), (fumacaExplosao, 45), (destrocos, 60) })
            {
                if (p == null) continue;
                p.transform.position = ponto;
                p.Emit(n);
            }

            if (motor != null) motor.Stop();
            if (clarao != null)
            {
                clarao.transform.position = ponto;
                clarao.range = 220f;
            }

            var somTocou = false;
            for (var t = 0f; t < depois; t += Time.deltaTime)
            {
                if (clarao != null) clarao.intensity = 40f * Mathf.Exp(-t * 2.2f);
                // ~120 m até o mirante: o estrondo chega uns 0,35 s depois da luz.
                if (!somTocou && t > 0.35f && motor != null)
                {
                    motor.transform.position = ponto;
                    motor.PlayOneShot(ProceduralAudio.Obter("explosao"), 1f);
                    somTocou = true;
                }

                yield return null;
            }

            if (clarao != null) clarao.intensity = 0f;
        }
    }
}
