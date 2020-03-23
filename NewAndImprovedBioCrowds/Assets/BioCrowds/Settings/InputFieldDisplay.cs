using UnityEngine;
using UnityEngine.UI;

public class InputFieldDisplay : MonoBehaviour
{
    public Text t1;
    public InputField I1;
    // Use this for initialization
    void Start()
    {
        t1 = GetComponent<UnityEngine.UI.Text>();
        t1.text = I1.text;
    }

    public void ShowText()
    {
        t1.text = I1.text;
    }
}