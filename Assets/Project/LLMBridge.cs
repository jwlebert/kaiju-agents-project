using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Project;
using System;
using System.Collections.Generic;
using System.Text;

public class LLMBridge : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private string apiKey;
    private string mainModel = "gemini-3-flash-preview"; 
    private string fallbackModel = "gemini-2.5-flash";
    
    void Awake()
    {
        apiKey = APIKeys.GeminiKey;
    }
    
    // Prompt that forces the AI to behave as a bridge
    private string systemPrompt = "You are a helpful assistant. Keep your response to exactly one sentence. User says: ";
        
    // Start() runs automatically when you hit Play in Unity
    void Start()
    {
        Debug.Log("🚀 Sending test request to Gemini...");
        // Hardcode a test question here
        RequestAction("What is the best thing about C#?"); 
    }
        
    // This is the function the UI Button calls
    public void RequestAction(string userInput)
    {
        StartCoroutine(CallGemini(userInput));
    }

    // Network request
    IEnumerator CallGemini(string userInput)
    {
        // 1. Try the Main Model
        Debug.Log($"[Attempt 1] Trying {mainModel}...");
        yield return SendRequest(userInput, mainModel, (success, response) => {
            if (success) {
                ProcessResponse(response);
            } else {
                // 2. If Main fails with 503, try Fallback
                Debug.LogWarning($"[503] {mainModel} busy. Switching to {fallbackModel}...");
                StartCoroutine(SendRequest(userInput, fallbackModel, (fallbackSuccess, fallbackResponse) => {
                    if (fallbackSuccess) {
                        ProcessResponse(fallbackResponse);
                    } else {
                        Debug.LogError("FATAL: Both Gemini models are currently unavailable.");
                    }
                }));
            }
        });
    }
    IEnumerator SendRequest(string userInput, string targetModel, Action<bool, string> callback)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{targetModel}:generateContent?key={apiKey.Trim()}";
        string combinedPrompt = systemPrompt + userInput;
        
        string escapedPrompt = combinedPrompt
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
            
        string jsonPayload = "{\"contents\": [{\"parts\":[{\"text\":\"" + escapedPrompt + "\"}]}]}";
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                callback(true, request.downloadHandler.text);
            }
            else if (request.responseCode == 503)
            {
                // Return false so the fallback logic triggers
                callback(false, null);
            }
            else
            {
                Debug.LogError($"Hard Error on {targetModel}: {request.error}");
                callback(false, null);
            }
        }
    }
    
    // The parser
    void ProcessResponse(string rawResponse)
    {
        GeminiResponse responseData = JsonUtility.FromJson<GeminiResponse>(rawResponse);
        
        if (responseData?.candidates != null && responseData.candidates.Count > 0)
        {
            string aiText = responseData.candidates[0].content.parts[0].text;
            Debug.Log("<color=green>AI Says:</color> " + aiText);
        }
        else
        {
            Debug.LogError("Failed to parse Gemini response.");
        }
    }
    
    // The game logic
}

// Data structures for JSONUtility
[Serializable]
public class GeminiResponse
{
    public List<Candidate> candidates;
}

[Serializable]
public class Candidate
{
    public Content content;
}

[Serializable]
public class Content
{
    public List<Part> parts;
}

[Serializable]
public class Part
{
    public string text;
}