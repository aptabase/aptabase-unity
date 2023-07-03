using UnityEngine;

namespace AptabaseSDK
{
    [CreateAssetMenu(menuName = "aptabase settings")]
    public class AptabaseSettings : ScriptableObject
    {
        public string AppKey;
        public string SelfHostURL;
        public string BuildNumber;
    }
}