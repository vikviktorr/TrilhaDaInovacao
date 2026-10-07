using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>Gira em torno de um eixo local (radar, protótipo, hélices).</summary>
    public class Girar : MonoBehaviour
    {
        public Vector3 eixo = Vector3.up;
        public float grausPorSegundo = 90f;
        public bool ativo = true;

        void Update()
        {
            if (ativo) transform.Rotate(eixo, grausPorSegundo * Time.deltaTime, Space.Self);
        }
    }
}
