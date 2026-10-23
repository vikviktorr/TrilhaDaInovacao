using System;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Gatilho por aproximação (torre na Cena 5):
    /// dispara quando a cabeça entra no raio, medido só no plano do chão.
    /// </summary>
    public class ProximityTrigger : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float raio = 0.5f;

        public event Action Alcancado;

        public bool Ativo { get; private set; }
        public float Raio => raio;

        public void Ativar() => Ativo = true;
        public void Desativar() => Ativo = false;

        void Update()
        {
            if (!Ativo) return;
            var cam = Camera.main;
            if (cam == null) return;

            var d = LayoutTrilha.PlanoXZ(cam.transform.position) - LayoutTrilha.PlanoXZ(transform.position);
            if (d.sqrMagnitude > raio * raio) return;

            Ativo = false;
            Alcancado?.Invoke();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, raio);
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(float r) => raio = r;
#endif
    }
}
