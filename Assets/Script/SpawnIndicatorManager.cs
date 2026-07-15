using System.Collections.Generic;
using UnityEngine;

public class SpawnIndicatorManager : MonoBehaviour
{
    public static SpawnIndicatorManager instance;

    [Header("UI Settings")]
    public GameObject indicatorPrefab; 
    public RectTransform canvasRect;   
    
    [Range(0f, 0.2f)]
    public float edgePadding = 0.05f; 

    private Dictionary<GameObject, GameObject> activeIndicators = new Dictionary<GameObject, GameObject>();
    private Camera mainCamera;

    void Awake()
    {
        instance = this;
        mainCamera = Camera.main;
        if (canvasRect == null) canvasRect = GetComponent<RectTransform>();
    }

    public void RegisterTarget(GameObject zombieTarget)
    {
        if (indicatorPrefab == null || zombieTarget == null) return;
        if (activeIndicators.ContainsKey(zombieTarget)) return;

        GameObject newIndicator = Instantiate(indicatorPrefab, canvasRect);
        newIndicator.SetActive(false); 
        activeIndicators.Add(zombieTarget, newIndicator);
    }

    public void UnregisterTarget(GameObject zombieTarget)
    {
        if (zombieTarget != null && activeIndicators.TryGetValue(zombieTarget, out GameObject indicator))
        {
            if (indicator != null) Destroy(indicator);
            activeIndicators.Remove(zombieTarget);
        }
    }

    void Update()
    {
        if (mainCamera == null || canvasRect == null) return;

        List<GameObject> deadTargets = new List<GameObject>();

        foreach (var entry in activeIndicators)
        {
            GameObject target = entry.Key;
            GameObject indicator = entry.Value;

            if (target == null)
            {
                deadTargets.Add(target);
                continue;
            }

            UpdateIndicatorPosition(target, indicator);
        }

        foreach (var target in deadTargets)
        {
            UnregisterTarget(target);
        }
    }

    private void UpdateIndicatorPosition(GameObject target, GameObject indicator)
    {
        Vector3 screenPos = mainCamera.WorldToScreenPoint(target.transform.position);
        float buffer = 0.05f; 
        bool isBehind = screenPos.z < 0;

        bool isOffScreen = isBehind || 
                           screenPos.x < Screen.width * buffer || screenPos.x > Screen.width * (1f - buffer) || 
                           screenPos.y < Screen.height * buffer || screenPos.y > Screen.height * (1f - buffer);

        if (isOffScreen)
        {
            if (!indicator.activeSelf) indicator.SetActive(true);
            if (isBehind) screenPos *= -1;

            Vector2 center = new Vector2(Screen.width / 2, Screen.height / 2);
            Vector2 pos2D = new Vector2(screenPos.x, screenPos.y);
            Vector2 dir = (pos2D - center).normalized;

            float angle = Mathf.Atan2(dir.y, dir.x);
            Vector2 boundary = center;
            float screenRatio = (float)Screen.width / Screen.height;

            if (Mathf.Abs(dir.x / dir.y) < screenRatio)
            {
                boundary.y = dir.y > 0 ? Screen.height : 0;
                boundary.x = center.x + (center.y / Mathf.Abs(Mathf.Tan(angle))) * (dir.x > 0 ? 1 : -1);
            }
            else
            {
                boundary.x = dir.x > 0 ? Screen.width : 0;
                boundary.y = center.y + (center.x * Mathf.Abs(Mathf.Tan(angle))) * (dir.y > 0 ? 1 : -1);
            }

            float paddingX = Screen.width * edgePadding;
            float paddingY = Screen.height * edgePadding;
            boundary.x = Mathf.Clamp(boundary.x, paddingX, Screen.width - paddingX);
            boundary.y = Mathf.Clamp(boundary.y, paddingY, Screen.height - paddingY);

            indicator.transform.position = boundary;
            indicator.transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
        }
        else
        {
            if (indicator.activeSelf) indicator.SetActive(false);
        }
    }
}