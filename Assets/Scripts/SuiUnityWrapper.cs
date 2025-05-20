using Jint;
using System.IO;
using UnityEngine;

public class SuiUnityWrapper : MonoBehaviour
{
    private Engine _engine;

    void Start()
    {
        _engine = new Engine();

        string path = Path.Combine(Application.streamingAssetsPath, "sui-sdk.bundle.js");
        string jsCode;

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android requires UnityWebRequest for StreamingAssets
        StartCoroutine(LoadJSAndroid(path));
#else
        jsCode = File.ReadAllText(path);
        _engine.Execute(jsCode);
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator LoadJSAndroid(string path)
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(path))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load JS file: " + www.error);
            }
            else
            {
                _engine.Execute(www.downloadHandler.text);
            }
        }
    }
#endif

    public string Invoke(string functionName, params object[] args)
    {
        try
        {
            var result = _engine.Invoke(functionName, args);
            return result.ToString();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error invoking function {functionName}: {ex.Message}");
            return null;
        }
    }
}
