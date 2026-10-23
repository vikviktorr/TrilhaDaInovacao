using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Mapa RTI do radar CUPRI (1994): altura × hora, com a cor dando a força
    /// do eco. Como na tela do radar, o mapa vai sendo desenhado da esquerda
    /// para a direita: o pivô (na borda esquerda) cresce em x e a textura
    /// acompanha, sem esticar. Um cursor claro marca a coluna que está sendo
    /// medida. Este objeto tem a escala da tela (1 × 1 local = a tela toda);
    /// o pivô fica em x = -0,5 e o cursor é filho deste objeto, não do pivô.
    /// </summary>
    public class RevelarRTI : MonoBehaviour
    {
        public Transform pivo;
        public Renderer mapa;
        public Transform cursor;
        public float duracao = 20f;
        public bool ativo;

        static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");

        MaterialPropertyBlock bloco;
        float tempo;

        void Awake()
        {
            bloco = new MaterialPropertyBlock();
            Aplicar(0f);
        }

        public void Iniciar()
        {
            tempo = 0f;
            ativo = true;
        }

        void Update()
        {
            if (!ativo) return;
            tempo += Time.deltaTime;
            Aplicar(Mathf.Clamp01(tempo / Mathf.Max(0.01f, duracao)));
            if (tempo >= duracao) ativo = false;
        }

        void Aplicar(float fracao)
        {
            var f = Mathf.Max(0.001f, fracao);
            if (pivo != null) pivo.localScale = new Vector3(f, 1f, 1f);
            if (mapa != null)
            {
                mapa.GetPropertyBlock(bloco);
                bloco.SetVector(BaseMapST, new Vector4(f, 1f, 0f, 0f));
                mapa.SetPropertyBlock(bloco);
            }

            if (cursor != null)
            {
                var p = cursor.localPosition;
                p.x = fracao - 0.5f;
                cursor.localPosition = p;
                cursor.gameObject.SetActive(fracao > 0f && fracao < 1f);
            }
        }
    }
}
