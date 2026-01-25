using System.Collections.Generic;

public static class AffinityDiscoveryTracker 
{
    // Diccionario: ID del Enemigo -> Lista de Afinidades ya probadas
    private static Dictionary<int, HashSet<AffinityType>> discoveredAffinities = new Dictionary<int, HashSet<AffinityType>>();

    public static void RegisterDiscovery(int enemyId, AffinityType type)
    {
        if (!discoveredAffinities.ContainsKey(enemyId))
            discoveredAffinities[enemyId] = new HashSet<AffinityType>();
        
        discoveredAffinities[enemyId].Add(type);
    }

    public static bool IsDiscovered(int enemyId, AffinityType type)
    {
        if (discoveredAffinities.TryGetValue(enemyId, out var types))
            return types.Contains(type);
        return false;
    }
}