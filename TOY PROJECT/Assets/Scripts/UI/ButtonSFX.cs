using UnityEngine;
using UnityEngine.UI;

public class ButtonSFX : MonoBehaviour
{
    
    [SerializeField] private string sfxKey = "button_click";
private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClickSFX);
    }

    void OnDestroy()
    {
        button.onClick.RemoveListener(PlayClickSFX);
    }

void PlayClickSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(sfxKey);
        }
    }
}