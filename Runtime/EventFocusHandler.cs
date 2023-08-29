using UnityEngine;

namespace AptabaseSDK
{
    public class EventFocusHandler : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            Aptabase.OnApplicationFocus(hasFocus);
        }
    }
}