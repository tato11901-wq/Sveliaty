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

// Maldiciones

public enum CurseType 
{ 
    Positive, 
    Negative, 
    Gambling 
}

public enum CurseActivationType 
{ 
    Instant,      // Se aplica inmediatamente al obtenerla
    PreCombat,    // Se aplica al inicio del combate
    TurnStart,    // Se aplica al inicio de cada turno
    Activated,    // Requiere activación manual del jugador
    PostCombat    // Se aplica al finalizar el combate
}

public enum CurseEffect 
{ 
    ModifyHealth,           // Modificar HP del jugador
    ModifyCards,            // Dar o quitar cartas
    InvertVictoryCondition, // Invertir condición de victoria (≤ en vez de ≥)
    WeakenEnemy,            // Reducir HP del enemigo
    NegateCards,            // Las cartas restan en vez de sumar
    BlockRewards,           // No dar recompensas de cartas
    EscapeCombat,           // Escapar del combate actual
    NegateDamage,           // No recibir daño este combate
    GamblingDice            // Efecto de dado aleatorio
}