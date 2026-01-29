using System.Collections.Generic;

public class PlayerCombatData
{
    // Estático porque es inventario global del jugador
    public static Dictionary<AffinityType, int> cards =
        new Dictionary<AffinityType, int>()
        {
            { AffinityType.Fuerza, 0 },
            { AffinityType.Agilidad, 0 },
            { AffinityType.Destreza, 0 }
        };

    public AffinityType selectedAttackType;

    public int playerLife = 100;
    public int playerMaxLife = 100;
    public int score = 0; //Puntuacion del jugador 
}

