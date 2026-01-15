using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotMachineController : MonoBehaviour
{
    [SerializeField] GameObject slotObject;
    // Start is called before the first frame update
    void Start()
    {
        slotObject.SetActive(false);
        LastHitManager.ScoreBroadcast += HandleScoreExample;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void HandleScoreExample(int score)
    {
        if (score == 3)
        {
            SetWinning();
        }
    }
    private void SetWinning()
    {
        RewardPlayer();
        StartCoroutine(PlaySlotVFX());
    }
    private IEnumerator PlaySlotVFX()
    {
        slotObject.SetActive(true);
        yield return new WaitForSeconds(3);
        slotObject.SetActive(false);

    }
    private void RewardPlayer()
    {
        //give player gold ++
    }
}
