using System.Collections.Generic;
using UnityEngine;
using AptabaseSDK;

public class HelloWorld : MonoBehaviour
{
    private void Start()
    {
        Aptabase.TrackEvent("app_started", new Dictionary<string, object>
        {
            {"hello", "world"}
        });
    }
}