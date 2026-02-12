using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class CurseManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private CurseDatabase curseDatabase;

    [Header("Curse Event Settings")]
    [Range(0, 100)] public float baseCurseChance = 5f;
    [Range(0, 100)] public float curseChancePerTurn = 5f;
    [Range(0, 100)] public float spiritCurseChance = 80f;

    private List<CurseInstance> activeCurses = new List<CurseInstance>();
    private int combatsWithoutRewards = 0;

    public event Action<CurseData> OnCurseObtained;
    public event Action<CurseData> OnCurseActivated;
    public event Action<List<CurseData>> OnCurseChoiceEvent;

    void OnEnable()
    {
        if (playerManager != null)
            playerManager.OnPlayerDeath += OnDefeat;
    }

    void OnDisable()
    {
        if (playerManager != null)
            playerManager.OnPlayerDeath -= OnDefeat;
    }

    // =========================
    // PROBABILIDAD DE EVENTO
    // =========================

    public bool ShouldTriggerCurseEvent(int turnsUsed, bool isSpirit)
    {
        if (isSpirit)
            return UnityEngine.Random.Range(0f, 100f) < spiritCurseChance;

        float chance = baseCurseChance + (turnsUsed * curseChancePerTurn);
        return UnityEngine.Random.Range(0f, 100f) < chance;
    }

    public void TriggerCurseChoiceEvent()
    {
        if (curseDatabase == null)
        {
            Debug.LogError("CurseDatabase no asignada.");
            return;
        }

        List<CurseData> options = curseDatabase.GetThreeRandomCurses();

        if (OnCurseChoiceEvent == null)
        {
            Debug.LogWarning("OnCurseChoiceEvent no tiene suscriptores.");
            return;
        }

        OnCurseChoiceEvent.Invoke(options);
    }

    // =========================
    // OBTENER MALDICIÓN
    // =========================

    public void ObtainCurse(CurseData curse)
    {
        if (curse == null) return;

        Debug.Log($"Maldición obtenida: {curse.curseName}");

        if (curse.activationType == CurseActivationType.Instant)
        {
            ApplyInstantEffect(curse);
        }
        else
        {
            activeCurses.Add(new CurseInstance(curse));
        }

        OnCurseObtained?.Invoke(curse);
    }

    void ApplyInstantEffect(CurseData curse)
    {
        if (playerManager == null)
        {
            Debug.LogError("PlayerManager no asignado.");
            return;
        }

        switch (curse.effectType)
        {
            case CurseEffect.ModifyHealth:
                playerManager.ModifyHealth(curse.effectValue);
                break;

            case CurseEffect.ModifyCards:
                if (curse.effectValue > 0)
                {
                    AffinityType randomType = GetRandomAffinityType();
                    playerManager.AddCards(randomType, curse.effectValue);
                }
                else
                {
                    int amountToRemove = Mathf.Abs(curse.effectValue);

                    for (int i = 0; i < amountToRemove; i++)
                    {
                        bool removed = playerManager.RemoveRandomCard();
                        if (!removed) break;
                    }
                }
                break;

            case CurseEffect.GamblingDice:
                int roll = UnityEngine.Random.Range(1, 13);
                int effect = (roll % 2 == 0) ? roll : -roll;

                playerManager.ModifyHealth(effect);

                Debug.Log($"Gambling: {roll} → {(effect > 0 ? "+" : "")}{effect} HP");
                break;
        }
    }

    // =========================
    // FASES DE COMBATE
    // =========================

    public void OnPreCombat(EnemyInstance enemy)
    {
        foreach (var curse in activeCurses.ToList())
        {
            if (curse.data.activationType != CurseActivationType.PreCombat) continue;

            switch (curse.data.effectType)
            {
                case CurseEffect.WeakenEnemy:
                    enemy.currentRPGHealth = Mathf.RoundToInt(
                        enemy.currentRPGHealth * curse.data.enemyHealthMultiplier
                    );
                    break;

                case CurseEffect.InvertVictoryCondition:
                    Debug.Log("Condición de victoria invertida");
                    break;
            }

            ReduceDuration(curse);
        }
    }

    public void OnTurnStart()
    {
        foreach (var curse in activeCurses.ToList())
        {
            if (curse.data.activationType != CurseActivationType.TurnStart) continue;

            switch (curse.data.effectType)
            {
                case CurseEffect.NegateCards:
                    Debug.Log("Cartas negadas este turno");
                    break;
            }

            ReduceDuration(curse);
        }
    }

    public void OnPostCombat(bool victory)
    {
        if (!victory) return;

        foreach (var curse in activeCurses.ToList())
        {
            if (curse.data.activationType != CurseActivationType.PostCombat) continue;

            ReduceDuration(curse);
        }
    }

    void ReduceDuration(CurseInstance curse)
    {
        if (curse.remainingDuration > 0)
        {
            curse.remainingDuration--;

            if (curse.remainingDuration == 0)
                activeCurses.Remove(curse);
        }
    }

    // =========================
    // ACTIVACIÓN MANUAL
    // =========================

    public bool CanActivateCurse(CurseData curse, int currentTurn)
    {
        if (!curse.requiresPlayerActivation) return false;

        var instance = activeCurses.FirstOrDefault(c => c.data.id == curse.id);
        if (instance == null) return false;

        if (curse.mustActivateOnTurnOne && currentTurn != 1) return false;

        return !instance.isActivated;
    }

    public void ActivateCurse(CurseData curse)
    {
        var instance = activeCurses.FirstOrDefault(c => c.data.id == curse.id);
        if (instance == null) return;

        instance.isActivated = true;
        OnCurseActivated?.Invoke(curse);

        switch (curse.effectType)
        {
            case CurseEffect.EscapeCombat:
                activeCurses.Remove(instance);
                break;

            case CurseEffect.NegateDamage:
                break;
        }
    }

    // =========================
    // VERIFICADORES
    // =========================

    public bool HasInvertedVictoryCondition()
    {
        return activeCurses.Any(c =>
            c.data.effectType == CurseEffect.InvertVictoryCondition &&
            c.remainingDuration != 0);
    }

    public bool HasNegatedCards()
    {
        return activeCurses.Any(c =>
            c.data.effectType == CurseEffect.NegateCards &&
            c.remainingDuration != 0);
    }

    public bool HasDamageNegation()
    {
        return activeCurses.Any(c =>
            c.data.effectType == CurseEffect.NegateDamage &&
            c.isActivated);
    }

    public bool HasRewardBlock()
    {
        return combatsWithoutRewards > 0;
    }

    // =========================
    // ESCUDO DE MUERTE
    // =========================

    public void OnDefeat()
    {
        var shield = activeCurses.FirstOrDefault(c =>
            c.data.effectType == CurseEffect.NegateDamage &&
            c.remainingDuration == -1);

        if (shield != null)
        {
            Debug.Log("Escudo de derrota usado");

            activeCurses.Remove(shield);

            playerManager.SetHealth(1);
        }
    }

    // =========================
    // HELPERS
    // =========================

    AffinityType GetRandomAffinityType()
    {
        AffinityType[] allTypes = (AffinityType[])Enum.GetValues(typeof(AffinityType));
        return allTypes[UnityEngine.Random.Range(0, allTypes.Length)];
    }

    public List<CurseInstance> GetActiveCurses() => activeCurses;
}
