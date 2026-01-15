using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;


namespace RPG.Stats
{
    public class GoldDisplay : MonoBehaviour
    {
        [SerializeField] Text goldDisplay = null;
        [SerializeField] TextMeshProUGUI goldDisplayTMP = null;
        private int currentGold;

        // Start is called before the first frame update
        void Awake()
        {
            LastHitManager.GoldBroadcast += UpdateGold;
        }

        // Update is called once per frame
        void Update()
        {
            if (goldDisplayTMP != null)
            {
                goldDisplayTMP.text = currentGold.ToString();
            }
            else
            {
                goldDisplay.text = currentGold.ToString();
            }
        }

        private void UpdateGold(int gold)
        {
            currentGold = gold;
        }
    }
}