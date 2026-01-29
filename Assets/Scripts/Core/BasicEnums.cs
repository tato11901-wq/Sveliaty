public enum AffinityType
{
    Fuerza,
    Agilidad,
    Destreza
}

public enum AffinityMultiplier
{
    Immune,   // ×0   - El enemigo es completamente inmune
    Strong,   // ×0.5 - El enemigo es resistente
    Neutral,  // ×1.0 - Daño normal
    Weak      // ×1.5 - El enemigo es débil a este tipo
}

public enum EnemyTier
{
    Tier_1,
    Tier_2,
    Tier_3
}

public enum CombatMode
{
    Passive,       // Sistema simple: suma automática
    PlayerChooses,  // Sistema avanzado: con multiplicadores
    TraditionalRPG // Sistema RPG tradicional
}