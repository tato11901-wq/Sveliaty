using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(menuName = "Sveliaty/Data Base/Curse Database")]
public class CurseDatabase : ScriptableObject
{
    public List<CurseData> allCurses;
    
    public CurseData GetRandomCurse()
    {
        return allCurses[Random.Range(0, allCurses.Count)];
    }
    
    public List<CurseData> GetThreeRandomCurses()
    {
        
        List<CurseData> result = new List<CurseData>();
        for (int i = 0; i < 3; i++)
        {
            result.Add(GetRandomCurse());
        }
        return result;
    }
}