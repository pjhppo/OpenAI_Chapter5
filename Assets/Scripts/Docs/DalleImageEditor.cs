using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.IO;

// JSON 구조에 맞춘 직렬화 클래스들
[System.Serializable]
public class DallEResponse
{
    public long created;
    public DataItem[] data;
}

[System.Serializable]
public class DataItem
{
    public string url;
}

public class DalleImageEditor : MonoBehaviour
{
    [Header("OpenAI Settings")]
    [SerializeField] private string openAIApiKey = "YOUR_API_KEY_HERE"; 
    [SerializeField] private string model = "dall-e-3";
    
    [Header("Image Files (Local Paths)")]
    [SerializeField] private string imagePath = "sunlit_lounge.png";
    [SerializeField] private string maskPath = "mask.png";

    [Header("Edit Prompt")]
    [TextArea]
    [SerializeField] private string prompt = "A sunlit indoor lounge area with a pool containing a flamingo";
    
    [Header("Image Generation Options")]
    [SerializeField] private int n = 1;
    [SerializeField] private string size = "1024x1024";

    [Header("Output Image")]
    [SerializeField] private RawImage outputImage; // 결과 이미지를 표시할 RawImage

    private void Start()
    {
        StartCoroutine(EditImageWithMask());
    }

    private IEnumerator EditImageWithMask()
    {
        // 1) 파일 읽기
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        byte[] maskBytes = File.ReadAllBytes(maskPath);

        // 2) WWWForm (멀티파트 폼 데이터)
        WWWForm form = new WWWForm();
        form.AddField("model", model); 
        form.AddBinaryData("image", imageBytes, Path.GetFileName(imagePath), "image/png");
        form.AddBinaryData("mask", maskBytes, Path.GetFileName(maskPath), "image/png");
        form.AddField("prompt", prompt);
        form.AddField("n", n.ToString());
        form.AddField("size", size);

        // 3) UnityWebRequest POST
        using (UnityWebRequest request = UnityWebRequest.Post("https://api.openai.com/v1/images/edits", form))
        {
            request.SetRequestHeader("Authorization", "Bearer " + openAIApiKey);

            yield return request.SendWebRequest();

            // 4) 결과 확인
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("DALL·E Mask Edit 성공");
                Debug.Log("응답 데이터: " + request.downloadHandler.text);

                // JSON -> 객체 변환
                var dallEResponse = JsonUtility.FromJson<DallEResponse>(request.downloadHandler.text);

                if (dallEResponse != null && dallEResponse.data != null && dallEResponse.data.Length > 0)
                {
                    string imageUrl = dallEResponse.data[0].url;
                    Debug.Log("이미지 URL: " + imageUrl);

                    // URL로부터 이미지 다운로드 및 표시
                    yield return StartCoroutine(DownloadImage(imageUrl));
                }
                else
                {
                    Debug.LogError("이미지 URL을 찾을 수 없습니다. (data 배열이 비었거나 null)");
                }
            }
            else
            {
                Debug.LogError("DALL·E Mask Edit 실패: " + request.error);
                Debug.LogError("응답 내용: " + request.downloadHandler.text);
            }
        }
    }

    private IEnumerator DownloadImage(string imageUrl)
    {
        using (UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return imageRequest.SendWebRequest();

            if (imageRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("이미지 다운로드 성공: " + imageUrl);
                Texture2D texture = DownloadHandlerTexture.GetContent(imageRequest);
                outputImage.texture = texture;
                outputImage.color = Color.white; // 혹시 모를 투명 상태 방지
            }
            else
            {
                Debug.LogError("이미지 다운로드 실패: " + imageRequest.error);
            }
        }
    }
}
