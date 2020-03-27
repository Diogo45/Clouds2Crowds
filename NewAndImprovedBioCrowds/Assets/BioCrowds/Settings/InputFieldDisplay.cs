using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldDisplay : MonoBehaviour
{
    public TextMeshProUGUI t1;
    public TMP_InputField I1;
    // Use this for initialization
    void Start()
    {
        t1 = GetComponent<TextMeshProUGUI>();
        t1.text = I1.text;
    }

    public void ShowText()
    {
        t1.text = I1.text;
    }
}