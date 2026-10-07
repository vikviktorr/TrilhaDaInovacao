using System;
using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Base das cenas do roteiro. Cada subclasse é a "ficha" executável de
    /// uma cena: falas, gatilho do visitante e efeitos, na ordem do roteiro.
    /// Todo gatilho tem o plano B do documento: a instrução é repetida em
    /// 6 s e a cena avança sozinha em 10 s, para a visita nunca travar.
    /// </summary>
    public abstract class EraSequence : MonoBehaviour
    {
        [SerializeField, Tooltip("0 = abertura, 1 a 6 = épocas, 7 = encerramento.")]
        int epoca;
        [SerializeField] protected SceneScript roteiro;
        [SerializeField, Tooltip("Onde o guia aparece nesta cena (ao lado da marca, fora do caminho).")]
        protected Transform posicaoGuia;

        protected VisitDirector ctx;

        public int Epoca => epoca;
        public SceneScript Roteiro => roteiro;

        public IEnumerator Executar(VisitDirector diretor)
        {
            ctx = diretor;
            PosicionarGuia(posicaoGuia);
            yield return Rodar();
        }

        protected abstract IEnumerator Rodar();

        /// <summary>Chamado quando o operador pula ou reinicia no meio da cena.</summary>
        public virtual void AoInterromper()
        {
            StopAllCoroutines();
        }

        protected void PosicionarGuia(Transform ponto)
        {
            if (ponto != null) ctx.Guia.IrPara(ponto.position, ponto.rotation);
            else ctx.Guia.Esconder();
        }

        protected IEnumerator Dizer(string id) => ctx.Dizer(roteiro.Obter(id));

        /// <summary>Fala sem bloquear a sequência (efeito acontecendo junto).</summary>
        protected Coroutine DizerJunto(string id) => StartCoroutine(Dizer(id));

        protected static IEnumerator Esperar(float segundos)
        {
            yield return new WaitForSeconds(segundos);
        }

        protected IEnumerator EsperarToque(PokeTrigger alvo, string dica)
        {
            var feito = false;
            Action marcar = () => feito = true;
            alvo.Tocado += marcar;
            alvo.Ativar();

            yield return EsperarCom(() => feito, dica);

            alvo.Tocado -= marcar;
            alvo.Desativar();
        }

        protected IEnumerator EsperarProximidade(ProximityTrigger alvo, string dica)
        {
            var feito = false;
            Action marcar = () => feito = true;
            alvo.Alcancado += marcar;
            alvo.Ativar();

            yield return EsperarCom(() => feito, dica);

            alvo.Alcancado -= marcar;
            alvo.Desativar();
        }

        IEnumerator EsperarCom(Func<bool> concluido, string dica)
        {
            var t = 0f;
            var repetiu = false;

            while (!concluido())
            {
                t += Time.deltaTime;

                if (!repetiu && t >= ctx.TempoDica && !string.IsNullOrEmpty(dica))
                {
                    repetiu = true;
                    DizerJunto(dica);
                }

                if (t >= ctx.TempoAuto)
                {
                    ctx.Registro.Gatilho("auto");
                    yield break;
                }

                yield return null;
            }

            ctx.Registro.Gatilho("visitante");
        }

        /// <summary>Faz um objeto surgir crescendo a partir do zero.</summary>
        protected static IEnumerator Surgir(Transform alvo, float duracao)
        {
            if (alvo == null) yield break;
            var escala = alvo.localScale;
            alvo.gameObject.SetActive(true);
            for (var t = 0f; t < duracao; t += Time.deltaTime)
            {
                var k = Mathf.SmoothStep(0f, 1f, t / duracao);
                alvo.localScale = escala * k;
                yield return null;
            }

            alvo.localScale = escala;
        }

        protected static void Mostrar(GameObject alvo, bool visivel)
        {
            if (alvo != null) alvo.SetActive(visivel);
        }
    }
}
