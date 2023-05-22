using UnityEngine;

public class DialogOpenClose : MonoBehaviour, IDialog
{
    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
