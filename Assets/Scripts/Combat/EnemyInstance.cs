public class EnemyInstance
{
    public EnemyData enemyData; // Datos del enemigo

    public EnemyTierData enemyTierData; // Datos del tier del enemigo

    public int attemptsRemaining; // Intentos restantes para derrotar al enemigo

    public EnemyInstance(EnemyData data, EnemyTierData tierData)
    {
        enemyData = data;
        enemyTierData = tierData;
        attemptsRemaining = tierData.maximunDiceThrow;
    }
}