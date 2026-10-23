using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Personagem de fundo (mini-characters da Kenney, animação Legacy). Em vez
    /// de repetir um clipe curto sem parar, respira em "idle" e, a intervalos
    /// aleatórios, faz um gesto (mexer na bancada, concordar...) com transição
    /// suave na ida e na volta. Início e velocidade variam por personagem, para
    /// que ninguém se mexa em sincronia. Quando o visitante chega perto, vira
    /// a cabeça devagar para ele, dentro de um limite confortável. Também
    /// pode falar (<see cref="VisitDirector.Dizer"/>): enquanto fala, para os
    /// gestos, olha o visitante e acena de leve com a cabeça.
    /// </summary>
    [RequireComponent(typeof(Animation))]
    public class Figurante : MonoBehaviour
    {
        public string repouso = "idle";
        public string[] gestos = { };
        [Tooltip("Segundos de repouso entre um gesto e outro (mín., máx.).")]
        public Vector2 intervalo = new Vector2(3f, 7f);
        [Tooltip("Vezes seguidas que um gesto se repete (mín., máx.).")]
        public Vector2Int repeticoes = new Vector2Int(1, 1);
        public float transicao = 0.35f;

        [Header("Fala")]
        [Tooltip("Nome mostrado na legenda quando o personagem fala.")]
        public string papel;
        [Tooltip("Fonte 3D da voz, no próprio personagem.")]
        public AudioSource voz;

        [Header("Olhar o visitante")]
        public bool olharVisitante = true;
        public float distanciaOlhar = 4f;
        public float anguloMaximo = 60f;

        Animation anim;
        Transform cabeca;
        float pesoOlhar;
        Quaternion giroCabeca = Quaternion.identity;
        float pesoFala;

        /// <summary>Ligado pelo diretor enquanto a fala deste personagem toca.</summary>
        public bool Falando { get; set; }
        public AudioSource Voz => voz;

        void Awake()
        {
            anim = GetComponent<Animation>();
            // O osso "head" (não a malha "head-mesh", que não é movida pela animação).
            foreach (var t in GetComponentsInChildren<Transform>())
                if (t.name.ToLowerInvariant() == "head") { cabeca = t; break; }
        }

        void OnEnable()
        {
            var idle = anim[repouso];
            if (idle == null) return;
            idle.wrapMode = WrapMode.Loop;
            idle.layer = 0;
            idle.speed = Random.Range(0.85f, 1.1f);
            idle.time = Random.Range(0f, idle.length);
            anim.Play(repouso);
            foreach (var g in gestos)
            {
                if (anim[g] == null) continue;
                // ClampForever: o gesto segura a última pose enquanto o idle volta,
                // em vez de saltar para a pose inicial ao acabar.
                anim[g].wrapMode = WrapMode.ClampForever;
            }

            StartCoroutine(Rotina());
        }

        IEnumerator Rotina()
        {
            // Primeiro gesto em momentos diferentes para cada personagem.
            yield return new WaitForSeconds(Random.Range(0.5f, intervalo.y));
            while (true)
            {
                // Quem está falando não começa um gesto novo.
                while (Falando) yield return null;

                if (gestos.Length > 0)
                {
                    var nome = gestos[Random.Range(0, gestos.Length)];
                    var gesto = anim[nome];
                    if (gesto != null)
                    {
                        var vezes = Random.Range(repeticoes.x, repeticoes.y + 1);
                        for (var i = 0; i < vezes; i++)
                        {
                            gesto.speed = Random.Range(0.8f, 1f);
                            gesto.time = 0f;
                            anim.CrossFade(nome, i == 0 ? transicao : 0.15f);
                            yield return new WaitForSeconds(Mathf.Max(0.1f, gesto.length / gesto.speed - transicao * 0.5f));
                        }

                        anim.CrossFade(repouso, transicao);
                    }
                }

                yield return new WaitForSeconds(Random.Range(intervalo.x, intervalo.y));
            }
        }

        void LateUpdate()
        {
            // Roda depois da animação: soma o giro da cabeça à pose do clipe.
            if (!olharVisitante || cabeca == null) return;
            var cam = Camera.main;
            var alvo = Quaternion.identity;
            var querOlhar = false;
            if (cam != null)
            {
                var direcao = cam.transform.position - cabeca.position;
                var plano = Vector3.ProjectOnPlane(direcao, Vector3.up);
                var angulo = Vector3.SignedAngle(Vector3.ProjectOnPlane(transform.forward, Vector3.up), plano, Vector3.up);
                var alcance = Falando ? distanciaOlhar * 2f : distanciaOlhar;
                if (direcao.magnitude < alcance && Mathf.Abs(angulo) < anguloMaximo + 30f)
                {
                    querOlhar = true;
                    var inclinacao = -Mathf.Atan2(direcao.y, plano.magnitude) * Mathf.Rad2Deg;
                    alvo = Quaternion.Euler(Mathf.Clamp(inclinacao, -20f, 20f), Mathf.Clamp(angulo, -anguloMaximo, anguloMaximo), 0f);
                }
            }

            pesoOlhar = Mathf.MoveTowards(pesoOlhar, querOlhar ? 1f : 0f, Time.deltaTime * 0.8f);

            // Falando: aceno leve da cabeça (os mini-characters não têm boca animada).
            pesoFala = Mathf.MoveTowards(pesoFala, Falando ? 1f : 0f, Time.deltaTime * 3f);
            if (pesoFala > 0f)
                alvo = Quaternion.Euler(Mathf.Sin(Time.time * 7f) * 4f * pesoFala, 0f, 0f) * alvo;
            var suave = 1f - Mathf.Exp(-4f * Time.deltaTime);
            giroCabeca = Quaternion.Slerp(giroCabeca, Quaternion.Slerp(Quaternion.identity, alvo, Mathf.SmoothStep(0f, 1f, pesoOlhar)), suave);
            // Giro no referencial do corpo, aplicado sobre a rotação animada da cabeça.
            var corpo = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up), Vector3.up);
            cabeca.rotation = corpo * giroCabeca * Quaternion.Inverse(corpo) * cabeca.rotation;
        }
    }
}
