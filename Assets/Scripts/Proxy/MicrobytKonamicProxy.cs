using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using microbytkonamic.proxy;
using System.Linq;
using microbytkonamic.navidad;
using static UnityEngine.Networking.UnityWebRequest;

namespace microbytkonamic.proxy
{
    public class MicrobytKonamicProxy : MonoBehaviourSingleton<MicrobytKonamicProxy>
    {
        public string urlLocal = "https://localhost:7076";
        // Cuando cambiemos a https hay que cambiar roject settings -->Player --> Other settings --> Allow downloads over HTTP
        public string urlServidor = "http://www.microbykonamic.es";
        public bool applyUrlLocalInEditor = true;

        //private IEnumerator PostCoroutine<TData, TResult>(string controller, string method, TData postData, System.Func<System.Exception, TResult, IEnumerator> callBack)
        public IEnumerator GetFelicitacion(GetFelicitacionIn input, System.Func<System.Exception, GetFelicitacionResult, IEnumerator> callBack)
            => PostCoroutine("postales", "getfelicitacion", input, callBack);

        public IEnumerator AltaFelicitacion(AltaFelicitacionIn input, System.Func<System.Exception, IntegerIntervals, IEnumerator> callBack)
            => PostCoroutine("postales", "altafelicitacion", input, callBack);

        public IEnumerator MusicaNavidadMP3(System.Func<System.Exception, AudioClip, IEnumerator> callBack) => GetAudioClipCoroutine("PostalNavidenya", "MusicaNavidadMP3", callBack);

        private string GetUrlBase()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                return "";

            bool local = Application.isPlaying ? applyUrlLocalInEditor : false;
            string url = local ? urlLocal : urlServidor;

            if (url.LastOrDefault() == '/')
                url = url.Substring(0, url.Length - 1);

            return url;
        }

        private string GetApiUrl(string controller) => $"{GetUrlBase()}/api/{controller}";
        private string GetApiUrl(string controller, string method) => $"{GetApiUrl(controller)}/{method}";
        private string GetActionUrl(string controller, string action) => $"{GetUrlBase()}/{controller}/{action}";
        private UnityWebRequest Post(string controller, string method, string postData, string contentType) => UnityWebRequest.Post(GetApiUrl(controller, method), postData, contentType);
        private UnityWebRequest Post<T>(string controller, string method, T postData) //where T:class
        {
            var _postData = JsonUtility.ToJson(postData);
            var result = Post(controller, method, _postData, "application/json");

            return result;
        }

        private UnityWebRequest GetAudioClip(string controller, string action) => UnityWebRequestMultimedia.GetAudioClip(GetActionUrl(controller, action), AudioType.MPEG);

        private IEnumerator PostCoroutine<TData, TResult>(string controller, string method, TData postData, System.Func<System.Exception, TResult, IEnumerator> callBack)
        {
            string msg;
            TResult result;
            System.Exception ex;

            using (var webRequest = Post(controller, method, postData))
            {
                yield return webRequest.SendWebRequest();

                string text = webRequest.downloadHandler.text;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError:
                        msg = $"{GetApiUrl(controller, method)} postData: {JsonUtility.ToJson(postData)} Error: {webRequest.error}";
                        Debug.LogError(msg);

                        if (!string.IsNullOrWhiteSpace(text) && WebApiProblemDetails.TryParseFromJson(text, out var problemDetails))
                        {
                            ex = new WebApiProblemDetailsExceptions(problemDetails);
                            Debug.LogError(ex);
                        }
                        else
                            ex = new WebApiProblemDetailsExceptions(!string.IsNullOrWhiteSpace(text) ? text : webRequest.error);

                        yield return StartCoroutine(callBack.Invoke(ex, default(TResult)));
                        break;
                    case UnityWebRequest.Result.Success:
                        msg = $"{GetApiUrl(controller, method)} postData: {JsonUtility.ToJson(postData)} responseCode: {webRequest.responseCode} Received: {text}";
                        result = JsonUtility.FromJson<TResult>(text);
                        Debug.Log(msg);
                        yield return StartCoroutine(callBack.Invoke(null, result));
                        break;
                    default:
                        msg = $"{GetApiUrl(controller, method)} postData: {JsonUtility.ToJson(postData)} Result no controller: {webRequest.result}";
                        Debug.LogError(msg);
                        yield return StartCoroutine(callBack.Invoke(null, default(TResult)));
                        break;
                }
            }
        }

        private IEnumerator GetAudioClipCoroutine(string controller, string action, System.Func<System.Exception, AudioClip, IEnumerator> callBack)
        {
            string msg;

            using (var webRequest = GetAudioClip(controller, action))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == Result.Success)
                {
                    msg = $"{GetActionUrl(controller, action)} responseCode: {webRequest.responseCode}";
                    Debug.Log(msg);
                    yield return StartCoroutine(callBack.Invoke(null, DownloadHandlerAudioClip.GetContent(webRequest)));
                }
                else
                {
                    msg = $"{GetActionUrl(controller, action)} Error: {webRequest.error}";
                    Debug.LogError(msg);
                    yield return StartCoroutine(callBack.Invoke(new WebApiProblemDetailsExceptions(webRequest.error), null));
                }
            }
        }
    }
}
