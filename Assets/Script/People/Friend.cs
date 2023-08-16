using UnityEngine;
using UnityEngine.UI;

public class Friend : MonoBehaviour
{
    public string Name { get; private set; } = "KOKO";

    [SerializeField] private Text nameText = null;

    private void OnChat()
    {

    }
}
