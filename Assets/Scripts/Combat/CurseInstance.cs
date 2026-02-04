public class CurseInstance
{
    public CurseData data;
    public int remainingDuration; // -1 = hasta usar, 0 = expirado, >0 = combates/turnos restantes
    public bool isActivated; // Para cartas que requieren activación
    
    public CurseInstance(CurseData data)
    {
        this.data = data;
        this.remainingDuration = data.duration;
        this.isActivated = false;
    }
}
