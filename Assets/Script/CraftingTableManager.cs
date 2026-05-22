using System.Collections.Generic;
using UnityEngine;

public class CraftingTableManager : MonoBehaviour
{
    public CraftingTableDB db;
    private List<KeyValuePair<string, CraftingTableStat>> GetCardPool()
    {
        return db.GetMetadataPool();
    }
    private void Start()
    {
        if (db == null)
        {
            db = FindAnyObjectByType<CraftingTableDB>();
        }
    }

    public List<KeyValuePair<string, CraftingTableStat>> RollThreeCards()
    {
        var allCards = GetCardPool();

        for (int i = allCards.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            var temp = allCards[i];
            allCards[i] = allCards[randomIndex];
            allCards[randomIndex] = temp;
        }

        List<KeyValuePair<string, CraftingTableStat>> choices = new List<KeyValuePair<string, CraftingTableStat>>();
        for (int i = 0; i < 3; i++)
        {
            if (i < allCards.Count) 
            {
                choices.Add(allCards[i]);
            }
        }

        return choices;
    }
    //db.UpgradeStat(upgradeID);
}