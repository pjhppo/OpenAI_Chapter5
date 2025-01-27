using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public class DalleImageGenerator : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private string apiKey = "sk-your-api-key-here";
    [SerializeField] private string size = "1024x1024"; // 사이즈 변경
    [SerializeField] private string prompt = "a white siamese cat";
    [SerializeField] private string model = "dall-e-3";

    [Header("Output")]
    [SerializeField] private RawImage outputImage;

    private const string API_URL = "https://api.openai.com/v1/images/generations";

    private void Start()
    {
        StartCoroutine(GenerateImage(prompt));
    }

    public IEnumerator GenerateImage(string prompt)
    {
        // 프롬프트 특수문자 이스케이프 처리
        string sanitizedPrompt = prompt.Replace("\"", "\\\"");

        string jsonPayload = $@"{{
            ""model"": ""{model}"",
            ""prompt"": ""{sanitizedPrompt}"",
            ""n"": 1,
            ""size"": ""{size}""
        }}";

        byte[] payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(payloadBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ExtractAndLoadImage(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"API 요청 실패: {request.error}");
                Debug.LogError($"응답 본문: {request.downloadHandler.text}");
            }
        }
    }

    private void ExtractAndLoadImage(string jsonResponse)
    {
        try
        {
            Match match = Regex.Match(jsonResponse, @"""url"":\s*""([^""]+)""");
            if (match.Success)
            {
                StartCoroutine(DownloadImage(match.Groups[1].Value));
            }
            else
            {
                Debug.LogError("URL 추출 실패. 전체 응답: " + jsonResponse);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"응답 처리 오류: {e.Message}");
        }
    }

    private IEnumerator DownloadImage(string imageUrl)
    {
        using (UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return imageRequest.SendWebRequest();

            if (imageRequest.result == UnityWebRequest.Result.Success)
            {
                outputImage.texture = DownloadHandlerTexture.GetContent(imageRequest);
                Debug.Log("이미지 로드 성공!");
            }
            else
            {
                Debug.LogError($"이미지 다운로드 실패: {imageRequest.error}");
            }
        }
    }
}
