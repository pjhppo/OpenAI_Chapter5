using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using System.IO;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DalleImageSaver : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private string apiKey = "sk-your-api-key-here";
    [SerializeField] private string size = "1024x1024";
    [SerializeField] private string defaultPrompt = "a white siamese cat";
    [SerializeField] private string model = "dall-e-3";

    [Header("Output")]
    [SerializeField] private RawImage outputImage;
    [SerializeField] private string saveLocation = "GeneratedImages";

    [Header("UI")]
    [SerializeField] private Button buttonSave;   // Save 버튼
    [SerializeField] private Button buttonBuild; // Build 버튼
    [SerializeField] private InputField inputField; // 입력 필드

    private Texture2D currentTexture; // 현재 출력된 텍스처

    private const string API_URL = "https://api.openai.com/v1/images/generations";

    [System.Serializable]
    private class DalleRequestData
    {
        public string model;
        public string prompt;
        public int n = 1;
        public string size;
    }

    [System.Serializable]
    private class DalleResponse
    {
        public DalleImageData[] data;
    }

    [System.Serializable]
    private class DalleImageData
    {
        public string url;
    }

    private void Start()
    {
        // 버튼 및 입력 필드 이벤트 등록
        buttonSave.onClick.AddListener(() => SaveTextureToPNG(currentTexture));
        buttonBuild.onClick.AddListener(() => GenerateImage(defaultPrompt));
        inputField.onEndEdit.AddListener(OnInputFieldSubmit);
    }

    public void GenerateImage(string prompt) => StartCoroutine(GenerateImageRoutine(prompt));

    private IEnumerator GenerateImageRoutine(string prompt)
    {
        var requestData = new DalleRequestData
        {
            model = model,
            prompt = prompt,
            size = size
        };

        using var request = CreateApiRequest(JsonUtility.ToJson(requestData));
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            HandleRequestError(request);
            yield break;
        }

        HandleApiResponse(request.downloadHandler.text);
    }

    private UnityWebRequest CreateApiRequest(string jsonPayload)
    {
        var request = new UnityWebRequest(API_URL, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonPayload)),
            downloadHandler = new DownloadHandlerBuffer()
        };

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        return request;
    }

    private void HandleApiResponse(string jsonResponse)
    {
        try
        {
            var response = JsonUtility.FromJson<DalleResponse>(jsonResponse);
            if (response.data?.Length > 0)
            {
                StartCoroutine(DownloadImageRoutine(response.data[0].url));
                return;
            }
            Debug.LogError("No image data in response");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Response parsing failed: {e.Message}");
        }
    }

    private IEnumerator DownloadImageRoutine(string imageUrl)
    {
        using var request = UnityWebRequestTexture.GetTexture(imageUrl, false);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            currentTexture = DownloadHandlerTexture.GetContent(request); // 텍스처 저장
            outputImage.texture = currentTexture;
            Debug.Log("Image loaded successfully");
        }
        else
        {
            Debug.LogError($"Image download failed: {request.error}");
        }
    }

    private void SaveTextureToPNG(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogWarning("No texture to save.");
            return;
        }

        if (string.IsNullOrEmpty(saveLocation))
        {
            Debug.LogWarning("Save location is not set. Image not saved.");
            return;
        }

        try
        {
            string fullPath = Path.Combine(Application.dataPath, saveLocation);
            Directory.CreateDirectory(fullPath);

            string fileName = $"DalleImage_{System.DateTime.Now:yyyyMMddHHmmssfff}.png";
            string filePath = Path.Combine(fullPath, fileName);

            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif

            Debug.Log($"Image saved to: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save image: {e.Message}");
        }
    }

    private void HandleRequestError(UnityWebRequest request)
    {
        Debug.LogError($"API Request Failed: {request.error}");
        Debug.LogError($"Response: {request.downloadHandler.text}");
    }

    private void OnInputFieldSubmit(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            defaultPrompt = text; // prompt 업데이트
            Debug.Log($"Updated prompt: {defaultPrompt}");
        }
    }
}
