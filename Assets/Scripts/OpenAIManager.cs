using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class OpenAIManager : MonoBehaviour
{
    [Header("OpenAI 설정")]
    public string apiKey = "YOUR_API_KEY";
    [SerializeField] private string model = "gpt-4o-mini";

    public static OpenAIManager Instance;

    // 이벤트 정의
    public StringEvent onResponseOpenAI;  // InputField 텍스트 완료 이벤트

    // JSON 응답 핵심 데이터만 추출하는 단순 클래스
    [System.Serializable]
    private class SimpleResponse
    {
        [System.Serializable]
        public class Choice
        {
            [System.Serializable]
            public class Message
            {
                public string content;
            }
            public Message message;
        }
        public Choice[] choices;
    }

    // 싱글톤 선언
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 싱글톤 인스턴스를 통해 이벤트 구독
        if (UIManager.Instance != null)
        {
            UIManager.Instance.onInputFieldSubmit.AddListener(OnInputFieldCompleted);
        }
        else
        {
            Debug.LogError("UIManager 인스턴스가 없습니다.");
        }
    }

    private void OnInputFieldCompleted(string message)
    {
        StartCoroutine(SendRequestToOpenAI(message));
    }

    private IEnumerator SendRequestToOpenAI(string message)
    {
        string url = "https://api.openai.com/v1/chat/completions";

        // 요청 데이터 (직접 JSON 문자열 생성)
        string jsonPayload = @"{
            ""model"": """ + model + @""",
            ""messages"": [
                { ""role"": ""system"", ""content"": ""You are a helpful assistant. Answer questions concisely using only standard alphanumeric characters and basic punctuation (e.g., periods, commas). Avoid symbols, emojis, or markdown formatting to ensure compatibility with text-to-speech APIs."" },
                { ""role"": ""user"", ""content"": """ + message + @""" }
            ],
            ""store"": false
        }";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // JsonUtility로 직접 파싱 (클래스 1개로 계층 구조 처리)
                SimpleResponse response = JsonUtility.FromJson<SimpleResponse>(request.downloadHandler.text);
                string responseMessage = response.choices[0].message.content;
                onResponseOpenAI.Invoke(responseMessage); // 입력된 텍스트를 이벤트로 전달
                Debug.Log("응답: " + responseMessage);
            }
            else
            {
                Debug.LogError("요청 실패: " + request.error);
            }
        }
    }


}
