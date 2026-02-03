public class EnemyInstance
{
    public EnemyData enemyData; // Datos del enemigo

    public EnemyTierData enemyTierData; // Datos del tier del enemigo

    public int attemptsRemaining; // Intentos restantes para derrotar al enemigo
    public int currentRPGHealth; // Vida actual en modo RPG Tradicional
    public int currentRPGDiceCount; // Cantidad de dados actuales en modo RPG Tradicional

    public EnemyInstance(EnemyData data, EnemyTierData tierData)
    {
        enemyData = data;
        enemyTierData = tierData;
        attemptsRemaining = tierData.maximunDiceThrow;
        currentRPGHealth = tierData.RPGLife;
        currentRPGDiceCount = tierData.RPGDiceCount;
    }
}