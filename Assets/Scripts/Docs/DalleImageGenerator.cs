using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;
using UnityEngine.UI;

public class DalleImageGenerator : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private string apiKey = "sk-your-api-key-here"; // 인스펙터에서 API 키 설정
    [SerializeField] private string size = "512x512";
    [SerializeField] private string prompt = "a white siamese cat";
    [SerializeField] private string model = "dall-e-3";

    [Header("Output")]
    [SerializeField] private RawImage outputImage; // 생성된 이미지를 표시할 RawImage

    private const string API_URL = "https://api.openai.com/v1/images/generations";

    private void Start()
    {
        StartCoroutine(GenerateImage(prompt));
    }

    // 이미지 생성 요청 코루틴
    public IEnumerator GenerateImage(string prompt)
    {
        // 요청 본문 생성
        string jsonPayload = $@"{{
            ""model"": ""{model}"",
            ""prompt"": ""{prompt}"",
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
                ProcessResponse(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"API 요청 실패: {request.error}");
            }
        }
    }

    // 응답 처리
    private void ProcessResponse(string jsonResponse)
    {
        try
        {
            // JSON 파싱을 위한 간단한 클래스
            DalleResponse response = JsonUtility.FromJson<DalleResponse>(jsonResponse);
            if (response.data.Length > 0)
            {
                StartCoroutine(DownloadImage(response.data[0].url));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"응답 처리 오류: {e.Message}");
        }
    }

    // 이미지 다운로드 코루틴
    private IEnumerator DownloadImage(string imageUrl)
    {
        using (UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return imageRequest.SendWebRequest();

            if (imageRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(imageRequest);
                outputImage.texture = texture;
                Debug.Log("이미지 생성 및 로드 성공!");
            }
            else
            {
                Debug.LogError($"이미지 다운로드 실패: {imageRequest.error}");
            }
        }
    }

    // 테스트용 메서드
    public void TestImageGeneration()
    {
        StartCoroutine(GenerateImage("a white siamese cat"));
    }

    // JSON 응답 파싱을 위한 헬퍼 클래스
    [System.Serializable]
    private class DalleResponse
    {
        public long created;
        public DalleImageData[] data;
    }

    [System.Serializable]
    private class DalleImageData
    {
        public string url;
    }
}