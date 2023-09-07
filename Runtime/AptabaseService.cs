using UnityEngine;

namespace AptabaseSDK
{
    public class AptabaseService : MonoBehaviour
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