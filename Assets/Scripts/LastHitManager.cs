using UnityEngine;
using RPG.Core;

public class LastHitManager : MonoBehaviour
{

    //Delagates
    public delegate void ScoreEventHandler(int something);
    public static event ScoreEventHandler ScoreBroadcast;

    public delegate void GoldEventHandler(int anything);
    public static event GoldEventHandler GoldBroadcast;



    [SerializeField] GoldSpawner goldSpawner;
    private GameObject[] enemies;
    private Spawner[] spawners;
    public int score = 0;
    public int currency = 0;
    public int lastHit = 0;

    public void CheckLastWhoLastHit(GameObject killedUnit, GameObject instigator)
    {
        if (instigator.CompareTag("Player"))
        {
            Score(killedUnit);
        }
        else if (instigator.CompareTag("Ally"))
        {
            Miss();
        }
    }

    public void Score(GameObject killedUnit)
    {
        score++;
        lastHit++;
        currency += score;
        goldSpawner.Spawn(killedUnit.transform, score);
        ScoreBroadcast?.Invoke(score);
        GoldBroadcast?.Invoke(currency);
    }

    public void Miss()
    {
        score = 0;
        ScoreBroadcast?.Invoke(score);
    }
}
