using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// NPC de um estande. O usuário caminha fisicamente até ele (room-scale,
    /// sem teleporte); ao entrar no raio de ativação, a dublagem começa e as
    /// legendas aparecem. Terminada a fala, o setor acende no Mural.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class NpcEstande : MonoBehaviour
    {
        [Header("Conteúdo")]
        [SerializeField] EstandeData estande;

        [Header("Ativação por proximidade")]
        [SerializeField, Min(0.3f), Tooltip("Distância em metros para o NPC começar a falar.")]
        float raioAtivacao = 1.6f;
        [SerializeField, Tooltip("Margem extra antes de considerar que o usuário saiu.")]
        float histerese = 0.4f;
        [SerializeField, Tooltip("Repete a fala se o usuário voltar depois de concluir.")]
        bool repetirAoRetornar = true;

        [Header("Saída")]
        [SerializeField] AudioSource fonteDeVoz;
        [SerializeField] PainelLegenda painelLegenda;
        [SerializeField, Tooltip("Aceso enquanto o NPC fala, para guiar o olhar.")]
        Light luzDeDestaque;

        Transform cabecaUsuario;
        Coroutine falaEmCurso;
        bool usuarioPorPerto;

        public EstandeData Estande => estande;
        public bool Falando => falaEmCurso != null;

        void Awake()
        {
            if (fonteDeVoz == null) fonteDeVoz = GetComponent<AudioSource>();
            ConfigurarAudioEspacial();
            if (luzDeDestaque != null) luzDeDestaque.enabled = false;
        }

        void Update()
        {
            var cabeca = ObterCabeca();
            if (cabeca == null) return;

            var distancia = Vector3.Distance(
                new Vector3(cabeca.position.x, 0f, cabeca.position.z),
                new Vector3(transform.position.x, 0f, transform.position.z));

            var limite = usuarioPorPerto ? raioAtivacao + histerese : raioAtivacao;
            var dentro = distancia <= limite;

            if (dentro == usuarioPorPerto) return;
            usuarioPorPerto = dentro;

            if (dentro) AoEntrar();
            else AoSair();
        }

        void AoEntrar()
        {
            var progresso = ProgressoExperiencia.Instancia;
            if (progresso != null) progresso.MarcarVisitado(estande);

            if (Falando) return;

            var jaConcluido = progresso != null && progresso.Estado(estande) == EstadoEstande.Concluido;
            if (jaConcluido && !repetirAoRetornar) return;

            falaEmCurso = StartCoroutine(Falar());
        }

        void AoSair()
        {
            // A fala continua até o fim mesmo se o usuário se afastar um pouco:
            // interromper no meio quebraria a narrativa e o progresso do Mural.
        }

        IEnumerator Falar()
        {
            if (estande == null || estande.falas == null || estande.falas.Length == 0)
            {
                falaEmCurso = null;
                yield break;
            }

            if (luzDeDestaque != null) luzDeDestaque.enabled = true;

            foreach (var fala in estande.falas)
            {
                if (fala == null) continue;

                if (painelLegenda != null) painelLegenda.Mostrar(fala.legenda);

                if (fala.dublagem != null && fonteDeVoz != null)
                {
                    fonteDeVoz.clip = fala.dublagem;
                    fonteDeVoz.Play();
                }

                yield return new WaitForSeconds(fala.Duracao);
            }

            if (painelLegenda != null) painelLegenda.Ocultar();
            if (luzDeDestaque != null) luzDeDestaque.enabled = false;

            var progresso = ProgressoExperiencia.Instancia;
            if (progresso != null) progresso.MarcarConcluido(estande);

            falaEmCurso = null;
        }

        void ConfigurarAudioEspacial()
        {
            if (fonteDeVoz == null) return;
            // Áudio posicional guia o usuário entre os estandes sem HUD extra.
            fonteDeVoz.playOnAwake = false;
            fonteDeVoz.loop = false;
            fonteDeVoz.spatialBlend = 1f;
            fonteDeVoz.rolloffMode = AudioRolloffMode.Linear;
            fonteDeVoz.minDistance = 1f;
            fonteDeVoz.maxDistance = 8f;
            fonteDeVoz.dopplerLevel = 0f;
        }

        Transform ObterCabeca()
        {
            if (cabecaUsuario != null) return cabecaUsuario;
            var camera = Camera.main;
            if (camera != null) cabecaUsuario = camera.transform;
            return cabecaUsuario;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, raioAtivacao);
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(EstandeData dados, PainelLegenda painel, Light luz)
        {
            estande = dados;
            painelLegenda = painel;
            luzDeDestaque = luz;
            fonteDeVoz = GetComponent<AudioSource>();
        }
#endif
    }
}
