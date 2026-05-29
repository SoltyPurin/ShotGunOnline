[System.Serializable]

public class ALLEnemyJson
{
    public int SlowHP = 950;

    public int FatHP = 3000;

    public int BombHP = 2000;

    public int CockRoachHP = 200;

    public int ArmorHP = 6000;

    public int LockOnHP = 1500;
    public float TimeUntilNextShot = 1.5f;

    public int TurretHP = 1000;
    //どれだけ鈍く動かすか
    public float BurretSmooth = 0.8f;

    public int BossHP = 10000;
    //敵が湧くまでの時間
    public float EnemyPopTime = 5;
    //パンチ減衰度
    public float AttenuationRate = 0.93f;
    //パンチの速度
    public float PunchSpeed = 70;


}
