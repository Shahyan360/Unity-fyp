using TMPro;
using UnityEngine;

public class WelcomeMessageRotator : MonoBehaviour
{
    [Header("Text")]
    public TMP_Text messageText;

    [Header("Messages")]
    public string[] welcomeMessages =
    {
        "Hi, how can I help you today?",
        "Ready to explore knowledge in 3D?",
        "Choose a scenario and let's begin!",
        "I can tutor, guide, or coach you."
    };

    [Header("Rotation")]
    public bool rotateMessages = true;
    public float secondsBetweenMessages = 4f;

    private void Start()
    {
        ShowRandomMessage();

        if (rotateMessages && secondsBetweenMessages > 0f)
        {
            InvokeRepeating(nameof(ShowRandomMessage), secondsBetweenMessages, secondsBetweenMessages);
        }
    }

    public void ShowRandomMessage()
    {
        if (messageText == null || welcomeMessages == null || welcomeMessages.Length == 0)
        {
            return;
        }

        int randomIndex = Random.Range(0, welcomeMessages.Length);
        messageText.text = welcomeMessages[randomIndex];
    }
}

