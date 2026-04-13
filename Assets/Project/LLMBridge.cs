using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Project;
using System;
using System.Collections.Generic;

public class LLMBridge : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private string apiKey;
    private string model = "gemini-2.5-flash";
    
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
        // Trimming inputs to prevent accidental spaces
        string currentModel = model.Trim();
        string currentKey = apiKey.Trim();
        
        // Endpoint URL 
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{currentModel}:generateContent?key={currentKey}";
        
        string combinedPrompt = systemPrompt + userInput;
        
        // Formatting JSON payload as the API expects it
        // Escaping backslashes, quotes, and newlines to prevent JSON errors
        string escapedPrompt = combinedPrompt
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
            
        string jsonPayload = "{\"contents\": [{\"parts\":[{\"text\":\"" + escapedPrompt + "\"}]}]}";
        
        // UnityWebRequest is Unity's built in tool for handling HTTP 
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest(); // pause function here, render next frame and come back when server replies

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Pass the JSON string to the parser
                ProcessResponse(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("API Error: " + request.error);
                Debug.LogError("Response Code: " + request.responseCode);
                Debug.LogError("Response Body: " + request.downloadHandler.text);
                Debug.LogError("Requested URL: " + url);
            }
        }
    }
    
    // The parser
    void ProcessResponse(string rawResponse)
    {
        // Convert JSON to C# objects
        GeminiResponse responseData = JsonUtility.FromJson<GeminiResponse>(rawResponse);
        
        // safety check to prevent null reference errors
        if (responseData != null && responseData.candidates != null && responseData.candidates.Count > 0)
        {
            // Extract just the string
            string aiText = responseData.candidates[0].content.parts[0].text;
        
            Debug.Log("AI Says: " + aiText);
        }
        else
        {
            Debug.LogError("Failed to parse Gemini response or no candidates returned.");
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