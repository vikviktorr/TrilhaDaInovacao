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
        PokeTrigger toqueEsperado;
        ProximityTrigger proximidadeEsperada;

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

            // As coroutines paradas não chegam à limpeza do fim da espera:
            // sem isto o alvo seguiria brilhando e aceitando toque.
            if (toqueEsperado != null) toqueEsperado.Desativar();
            if (proximidadeEsperada != null) proximidadeEsperada.Desativar();
            toqueEsperado = null;
            proximidadeEsperada = null;
            HandPoints.PontoSimulado = null;
        }

        protected void PosicionarGuia(Transform ponto)
        {
            if (ponto != null) ctx.Guia.IrPara(ponto.position, ponto.rotation);
            else ctx.Guia.Esconder();
        }

        protected IEnumerator Dizer(string id) => ctx.Dizer(roteiro.Obter(id));

        /// <summary>Um figurante da cena diz a fala (voz no próprio personagem).</summary>
        protected IEnumerator DizerPor(Figurante quem, string id) => ctx.Dizer(roteiro.Obter(id), quem);

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
            toqueEsperado = alvo;
            var simulacao = ctx.QA ? StartCoroutine(SimularToque(alvo)) : null;

            yield return EsperarCom(() => feito, dica);

            if (simulacao != null) StopCoroutine(simulacao);
            HandPoints.PontoSimulado = null;
            alvo.Tocado -= marcar;
            alvo.Desativar();
            toqueEsperado = null;
        }

        /// <summary>
        /// QA: um dedo virtual sai da altura do peito e vai até o alvo, como um
        /// visitante faria. Testa a detecção do PokeTrigger, não um atalho.
        /// </summary>
        IEnumerator SimularToque(PokeTrigger alvo)
        {
            yield return Esperar(1.5f);
            var cabeca = Camera.main.transform;
            var inicio = cabeca.position + Vector3.down * 0.45f + Plano(cabeca.forward) * 0.2f;
            var colisor = alvo.GetComponent<Collider>();
            var fim = colisor.bounds.center;
            ctx.Enquadrar(fim);
            for (var t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                HandPoints.PontoSimulado = Vector3.Lerp(inicio, fim, Mathf.SmoothStep(0f, 1f, t / 1.2f));
                yield return null;
            }

            HandPoints.PontoSimulado = fim;
        }

        static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v.normalized;
        }

        protected IEnumerator EsperarProximidade(ProximityTrigger alvo, string dica)
        {
            var feito = false;
            Action marcar = () => feito = true;
            alvo.Alcancado += marcar;
            alvo.Ativar();
            proximidadeEsperada = alvo;
            if (ctx.QA) StartCoroutine(SimularPasso(alvo));

            yield return EsperarCom(() => feito, dica);

            alvo.Alcancado -= marcar;
            alvo.Desativar();
            proximidadeEsperada = null;
        }

        /// <summary>QA: o visitante dá os passos até o gatilho de proximidade.</summary>
        IEnumerator SimularPasso(ProximityTrigger alvo)
        {
            yield return Esperar(1.5f);
            var cabeca = Camera.main.transform.position;
            var direcao = alvo.transform.position - cabeca;
            direcao.y = 0f;
            var destino = alvo.transform.position - direcao.normalized * (alvo.Raio * 0.5f);
            yield return ctx.Desktop.Caminhar(destino, 0.6f);
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
                    QARelatorio.Instancia?.Gatilho("auto");
                    yield break;
                }

                yield return null;
            }

            ctx.Registro.Gatilho("visitante");
            QARelatorio.Instancia?.Gatilho($"visitante ({t:0.0} s)");
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
