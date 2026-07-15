using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ZombieSequence
{
    public string sequenceName;
    public GameObject zombiePrefab;
    public int count; 
}

[System.Serializable]
public class ZombieGroup
{
    public string groupName;
    public Vector3 spawnPosition;
    public List<ZombieSequence> sequence;
}

[System.Serializable]
public class WaveData
{
    public string waveName;
    public float startTime;
    public float spawnInterval; // 반드시 0.1 이상 권장
    public bool isBigWave;
    public List<ZombieGroup> zombieGroups; 
}

public class WaveManager : MonoBehaviour
{
    public List<WaveData> waves;
    public GameObject sirenObject;

    void Start()
    {
        foreach (WaveData wave in waves)
        {
            StartCoroutine(SpawnWave(wave));
        }
    }

    IEnumerator SpawnWave(WaveData wave)
    {
        yield return new WaitForSeconds(wave.startTime);
        if (wave.isBigWave && sirenObject != null) StartCoroutine(BlinkSiren());

        foreach (ZombieGroup group in wave.zombieGroups)
        {
            StartCoroutine(SpawnSequence(group, wave.spawnInterval));
        }
    }

    IEnumerator SpawnSequence(ZombieGroup group, float interval)
    {
        foreach (ZombieSequence seq in group.sequence)
        {
            if (seq.zombiePrefab == null) continue;

            for (int i = 0; i < seq.count; i++)
            {
                GameObject newZombie = Instantiate(seq.zombiePrefab, group.spawnPosition, Quaternion.identity);
                if (newZombie.GetComponent<IndicatorTarget>() == null)
                    newZombie.AddComponent<IndicatorTarget>();
                
                // 생성 간격이 0이면 시스템이 멈춥니다. 최소 0.1 이상을 보장합니다.
                float waitTime = Mathf.Max(0.1f, interval);
                yield return new WaitForSeconds(waitTime);
            }
        }
    }

    IEnumerator BlinkSiren()
    {
        for (int i = 0; i < 10; i++)
        {
            if (sirenObject != null) sirenObject.SetActive(!sirenObject.activeSelf);
            yield return new WaitForSeconds(0.5f);
        }
        if (sirenObject != null) sirenObject.SetActive(false);
    }
}