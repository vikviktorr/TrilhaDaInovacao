using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Hands;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Fonte única das "pontas de dedo" que tocam a cena. Com rastreamento de
    /// mãos (XR Hands), usa a ponta do indicador e desenha as juntas como
    /// esferas brilhantes. Sem mãos, cai para os controles Touch. Os
    /// <see cref="PokeTrigger"/> só leem <see cref="Pontas"/>.
    /// </summary>
    public class HandPoints : MonoBehaviour
    {
        public static HandPoints Instancia { get; private set; }

        [SerializeField, Tooltip("Camera Offset do XR Origin: as poses das mãos vêm nesse espaço.")]
        Transform espacoRastreado;
        [SerializeField] Material materialJunta;
        [SerializeField] Material materialPonta;
        [SerializeField, Min(0.001f)] float raioJunta = 0.009f;

        readonly List<Vector3> pontas = new List<Vector3>(2);
        readonly List<XRHandSubsystem> subsistemas = new List<XRHandSubsystem>();

        XRHandSubsystem maos;
        Transform[] juntasEsquerda, juntasDireita;
        Transform pontaControleEsquerdo, pontaControleDireito;

        InputAction posEsquerda, rotEsquerda, rastEsquerda;
        InputAction posDireita, rotDireita, rastDireita;

        /// <summary>Posições, no mundo, de tudo que pode tocar um objeto neste quadro.</summary>
        public IReadOnlyList<Vector3> Pontas => pontas;

        /// <summary>Alguma mão está sendo rastreada (usado na calibração da Cena 0).</summary>
        public bool MaosVisiveis { get; private set; }

        public bool Ativo { get; set; } = true;

        /// <summary>QA: ponta de dedo simulada (visitante virtual), somada às reais.</summary>
        public static Vector3? PontoSimulado { get; set; }

        void Awake()
        {
            Instancia = this;

            posEsquerda = Acao("<XRController>{LeftHand}/devicePosition", "Vector3");
            rotEsquerda = Acao("<XRController>{LeftHand}/deviceRotation", "Quaternion");
            rastEsquerda = Acao("<XRController>{LeftHand}/isTracked", "Button");
            posDireita = Acao("<XRController>{RightHand}/devicePosition", "Vector3");
            rotDireita = Acao("<XRController>{RightHand}/deviceRotation", "Quaternion");
            rastDireita = Acao("<XRController>{RightHand}/isTracked", "Button");

            var total = XRHandJointID.EndMarker.ToIndex();
            juntasEsquerda = CriarJuntas("Mão esquerda", total);
            juntasDireita = CriarJuntas("Mão direita", total);
            pontaControleEsquerdo = CriarEsfera("Controle esquerdo", transform, 0.022f, materialPonta);
            pontaControleDireito = CriarEsfera("Controle direito", transform, 0.022f, materialPonta);
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
            foreach (var acao in new[] { posEsquerda, rotEsquerda, rastEsquerda, posDireita, rotDireita, rastDireita })
                acao?.Dispose();
        }

        static InputAction Acao(string caminho, string tipo)
        {
            var acao = new InputAction(binding: caminho, expectedControlType: tipo);
            acao.Enable();
            return acao;
        }

        Transform[] CriarJuntas(string nome, int total)
        {
            var raiz = new GameObject(nome).transform;
            raiz.SetParent(transform, false);
            var juntas = new Transform[total];
            for (var i = 0; i < total; i++)
            {
                var ponta = XRHandJointIDUtility.FromIndex(i) == XRHandJointID.IndexTip;
                juntas[i] = CriarEsfera($"Junta {i}", raiz, ponta ? raioJunta * 2.2f : raioJunta,
                    ponta ? materialPonta : materialJunta);
            }

            raiz.gameObject.SetActive(false);
            return juntas;
        }

        static Transform CriarEsfera(string nome, Transform pai, float raio, Material material)
        {
            var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            esfera.name = nome;
            Destroy(esfera.GetComponent<Collider>());
            esfera.transform.SetParent(pai, false);
            esfera.transform.localScale = Vector3.one * raio * 2f;
            var r = esfera.GetComponent<Renderer>();
            if (material != null) r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            esfera.SetActive(false);
            return esfera.transform;
        }

        void Update()
        {
            pontas.Clear();
            MaosVisiveis = false;

            // Óculos fora ou menu do sistema aberto: mãos escondidas e sem toque (VRC.Quest.Input.4).
            if (PausaVisita.Pausada)
            {
                EsconderTudo();
                return;
            }

            if (PontoSimulado.HasValue) pontas.Add(PontoSimulado.Value);

            if (!Ativo || !XRStartup.XRAtivo)
            {
                EsconderTudo();
                return;
            }

            if (maos == null || !maos.running)
            {
                subsistemas.Clear();
                SubsystemManager.GetSubsystems(subsistemas);
                maos = subsistemas.Find(s => s.running);
            }

            var esquerdaPorMao = AtualizarMao(maos != null ? maos.leftHand : default, juntasEsquerda);
            var direitaPorMao = AtualizarMao(maos != null ? maos.rightHand : default, juntasDireita);

            AtualizarControle(!esquerdaPorMao, posEsquerda, rotEsquerda, rastEsquerda, pontaControleEsquerdo);
            AtualizarControle(!direitaPorMao, posDireita, rotDireita, rastDireita, pontaControleDireito);
        }

        bool AtualizarMao(XRHand mao, Transform[] juntas)
        {
            var raiz = juntas[0].parent.gameObject;
            if (maos == null || !mao.isTracked)
            {
                raiz.SetActive(false);
                return false;
            }

            raiz.SetActive(true);
            MaosVisiveis = true;

            for (var i = 0; i < juntas.Length; i++)
            {
                var id = XRHandJointIDUtility.FromIndex(i);
                var junta = mao.GetJoint(id);
                if (!junta.TryGetPose(out var pose))
                {
                    juntas[i].gameObject.SetActive(false);
                    continue;
                }

                var mundo = Mundo(pose.position);
                juntas[i].position = mundo;
                juntas[i].gameObject.SetActive(true);
                if (id == XRHandJointID.IndexTip) pontas.Add(mundo);
            }

            return true;
        }

        void AtualizarControle(bool permitido, InputAction pos, InputAction rot, InputAction rastreado, Transform esfera)
        {
            var ativo = permitido && rastreado.IsPressed();
            esfera.gameObject.SetActive(ativo);
            if (!ativo) return;

            var rotacao = rot.ReadValue<Quaternion>();
            // A ponta fica 6 cm à frente do controle, onde o dedo estaria.
            var local = pos.ReadValue<Vector3>() + rotacao * new Vector3(0f, -0.01f, 0.06f);
            var mundo = Mundo(local);
            esfera.position = mundo;
            pontas.Add(mundo);
        }

        Vector3 Mundo(Vector3 rastreado) => espacoRastreado != null ? espacoRastreado.TransformPoint(rastreado) : rastreado;

        void EsconderTudo()
        {
            juntasEsquerda[0].parent.gameObject.SetActive(false);
            juntasDireita[0].parent.gameObject.SetActive(false);
            pontaControleEsquerdo.gameObject.SetActive(false);
            pontaControleDireito.gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(Transform espaco, Material junta, Material ponta)
        {
            espacoRastreado = espaco;
            materialJunta = junta;
            materialPonta = ponta;
        }
#endif
    }
}
