using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// UnityEvent<string> 외부에 정의
[System.Serializable]
public class StringEvent : UnityEvent<string> { }

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public InputField inputField; // Unity Editor에서 연결할 InputField
    public Text resultText; // 결과를 표시할 Text 컴포넌트

    // 이벤트 정의
    public StringEvent onInputFieldSubmit;  // InputField 텍스트 완료 이벤트

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
        // InputField에 입력 완료 이벤트 등록
        inputField.onEndEdit.AddListener(OnInputFieldEndEdit);

        // if (OpenAITTS.Instance != null)
        // {
        //     OpenAITTS.Instance.onResponseTTS.AddListener(OnResponseOpenAI);
        // }
        // else
        // {
        //     Debug.LogError("UIManager 인스턴스가 없습니다.");
        // }
    }

    private void OnInputFieldEndEdit(string inputText)
    {
        // 입력된 텍스트가 null이 아니고 공백이 아닌지 확인
        if (!string.IsNullOrEmpty(inputText))
        {
            onInputFieldSubmit.Invoke(inputText); // 입력된 텍스트를 이벤트로 전달
            // 입력 필드 초기화 (필요할 경우)
            inputField.text = "";
        }
        else
        {
            Debug.LogWarning("Input field is empty or null.");
        }
    }

    private void OnResponseOpenAI(string message){
        resultText.text = message;
    }
}
