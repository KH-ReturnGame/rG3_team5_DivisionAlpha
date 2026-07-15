using UnityEngine;

public class IndicatorTarget : MonoBehaviour
{
    void Start()
    {
        if (SpawnIndicatorManager.instance != null)
        {
            SpawnIndicatorManager.instance.RegisterTarget(gameObject);
        }
    }

    void OnDestroy()
    {
        if (SpawnIndicatorManager.instance != null)
        {
            SpawnIndicatorManager.instance.UnregisterTarget(gameObject);
        }
    }
}