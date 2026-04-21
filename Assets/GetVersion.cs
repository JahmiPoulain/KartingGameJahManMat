using TMPro;
using UnityEngine;

public class GetVersion : MonoBehaviour
{
    [SerializeField] private TMP_Text TMP_Text;

    void Start()
    {
        string version = Application.version;
        TMP_Text.text = "v" + version;

    }
}
