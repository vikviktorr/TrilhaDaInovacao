using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>Gira só no eixo vertical para ficar de frente para o visitante.</summary>
    public class EncararUsuario : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var direcao = transform.position - cam.transform.position;
            direcao.y = 0f;
            if (direcao.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direcao.normalized, Vector3.up);
        }
    }
}
