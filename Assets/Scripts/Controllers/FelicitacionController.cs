using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using TMPro;

using microbytkonamic.proxy;
using static UnityEngine.Networking.UnityWebRequest;

namespace microbytkonamic.navidad
{
    public class FelicitacionController : MonoBehaviourSingleton<FelicitacionController>
    {
        public bool inicio;
        public bool terminado;
        public float intervaloEntreFelicitaciones = 5;  // segundos
        public float intervaloEntreLetras = 30f / 60f;  // segundos
        public TextMeshProUGUI nickText;
        public TextMeshProUGUI fechaText;
        public TextMeshProUGUI textoText;

        [SerializeField, ReadOnly]
        EstadosFelicitacion estado;
        GetFelicitacionIn input;

        public void SetFelicitacion(FelicitacionDto felicitacionDto, IntegerIntervals intervals)
        {
            nickText.text = felicitacionDto.nick;
            //Enviada 30 de Diciembre de 2023
            fechaText.text = $"Enviada el {(System.DateTime)felicitacionDto?.fecha:F}";
            textoText.text = felicitacionDto.texto;
            input.Intervals = intervals;
            estado = EstadosFelicitacion.Felicitacion;
        }

        public Coroutine StartFelicitacion() => StartCoroutine(PonerFelicitacion());

        public Coroutine StartFelicitacion(FelicitacionDto felicitacionDto, IntegerIntervals intervals)
        {
            SetFelicitacion(felicitacionDto, intervals);

            return StartFelicitacion();
        }

        protected override void Awake()
        {
            base.Awake();
            estado = EstadosFelicitacion.GetFelicitacion;
            input = new GetFelicitacionIn
            {
                Intervals = new IntegerIntervals
                {
                    intervals = new IntegerInterval[0]
                }
            };
            nickText.text = fechaText.text = textoText.text = string.Empty;
        }

        // Update is called once per frame
        void Update()
        {
            if (inicio && !terminado)
                switch (estado)
                {
                    case EstadosFelicitacion.GetFelicitacion:
                        StartCoroutine(GetFelicitacion());
                        break;
                }
        }

        IEnumerator GetFelicitacion()
        {
            estado = EstadosFelicitacion.WaitFelicitacion;
            input.Anyo = PostalesController.Instance.anyo;

            yield return StartCoroutine(MicrobytKonamicProxy.Instance.GetFelicitacion(input, GetFelicitacion_Callback));
        }

        IEnumerator GetFelicitacion_Callback(System.Exception ex, GetFelicitacionResult result)
        {
            if (terminado)
                yield break;

            if (result == null)
            {
                estado = EstadosFelicitacion.GetFelicitacion;
                yield break;
            }

            yield return StartFelicitacion(result.felicitacionDto, result.intervals);
        }

        IEnumerator RevelarTextoPocoAPoco()
        {
            textoText.ForceMeshUpdate();

            var longitudTexto = textoText.textInfo.characterCount;

            for (var i = 1; i <= longitudTexto; i++)
            {
                textoText.maxVisibleCharacters = i;

                yield return new WaitForSeconds(intervaloEntreLetras);
            }
        }

        IEnumerator PonerFelicitacion()
        {
            yield return StartCoroutine(RevelarTextoPocoAPoco());

            yield return new WaitForSeconds(intervaloEntreFelicitaciones);

            if (terminado)
                yield break;

            yield return StartCoroutine(GetFelicitacion());
        }
    }
}
