using TMPro;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Painel do hub para o visitante ajustar, com o dedo, o volume das vozes e
    /// dos efeitos (botões − e +, alvos de 15 cm como os demais). Cada toque mostra
    /// o nível numa barra de 10 segmentos e toca uma amostra no volume novo: um
    /// trecho da fala do guia ou o som de toque. O valor fica salvo (<see cref="ConfigAudio"/>).
    /// </summary>
    public class PainelVolume : MonoBehaviour
    {
        [SerializeField] PokeTrigger vozMenos, vozMais, efeitosMenos, efeitosMais;
        [SerializeField] Renderer[] barraVozes, barraEfeitos;
        [SerializeField] TMP_Text valorVozes, valorEfeitos;
        [SerializeField] Material aceso, apagado;
        [SerializeField, Tooltip("Fonte da amostra de voz (com GanhoVoz).")] AudioSource fonteVoz;
        [SerializeField, Tooltip("Fonte da amostra de efeito (o diretor põe o Abafador).")] AudioSource fonteEfeito;
        [SerializeField, Tooltip("Trecho de fala do guia tocado ao ajustar as vozes.")] AudioClip amostraVoz;

        void OnEnable()
        {
            vozMenos.Tocado += MenosVoz;
            vozMais.Tocado += MaisVoz;
            efeitosMenos.Tocado += MenosEfeitos;
            efeitosMais.Tocado += MaisEfeitos;
            ConfigAudio.Mudou += Atualizar;
            foreach (var b in new[] { vozMenos, vozMais, efeitosMenos, efeitosMais }) b.Ativar();
            Atualizar();
        }

        void OnDisable()
        {
            vozMenos.Tocado -= MenosVoz;
            vozMais.Tocado -= MaisVoz;
            efeitosMenos.Tocado -= MenosEfeitos;
            efeitosMais.Tocado -= MaisEfeitos;
            ConfigAudio.Mudou -= Atualizar;
        }

        void MenosVoz() => AjustarVozes(-1);
        void MaisVoz() => AjustarVozes(1);
        void MenosEfeitos() => AjustarEfeitos(-1);
        void MaisEfeitos() => AjustarEfeitos(1);

        void AjustarVozes(int passos)
        {
            ConfigAudio.AjustarVozes(passos);
            // Amostra curta: não atropela o guia se ele estiver falando.
            if (fonteVoz != null && amostraVoz != null && !fonteVoz.isPlaying) StartCoroutine(Amostra());
        }

        System.Collections.IEnumerator Amostra()
        {
            fonteVoz.clip = amostraVoz;
            fonteVoz.time = 0f;
            fonteVoz.Play();
            yield return new WaitForSeconds(Mathf.Min(1.8f, amostraVoz.length));
            fonteVoz.Stop();
        }

        void AjustarEfeitos(int passos)
        {
            ConfigAudio.AjustarEfeitos(passos);
            if (fonteEfeito != null) fonteEfeito.PlayOneShot(ProceduralAudio.Obter("bipe"), 0.8f);
        }

        void Atualizar()
        {
            Preencher(barraVozes, valorVozes, ConfigAudio.Vozes);
            Preencher(barraEfeitos, valorEfeitos, ConfigAudio.Efeitos);
        }

        void Preencher(Renderer[] barra, TMP_Text valor, float volume)
        {
            var acesos = Mathf.RoundToInt(volume * barra.Length);
            for (var i = 0; i < barra.Length; i++) barra[i].sharedMaterial = i < acesos ? aceso : apagado;
            if (valor != null) valor.text = volume <= 0f ? "mudo" : $"{Mathf.RoundToInt(volume * 100f)}%";
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(PokeTrigger vm, PokeTrigger vM, PokeTrigger em, PokeTrigger eM, Renderer[] bv, Renderer[] be,
            TMP_Text tv, TMP_Text te, Material ligado, Material desligado, AudioSource fv, AudioSource fe, AudioClip amostra)
        {
            vozMenos = vm;
            vozMais = vM;
            efeitosMenos = em;
            efeitosMais = eM;
            barraVozes = bv;
            barraEfeitos = be;
            valorVozes = tv;
            valorEfeitos = te;
            aceso = ligado;
            apagado = desligado;
            fonteVoz = fv;
            fonteEfeito = fe;
            amostraVoz = amostra;
        }
#endif
    }
}
