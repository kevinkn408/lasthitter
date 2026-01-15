using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RPG.Stats
{
    public class ComboDisplay : MonoBehaviour
    {
        [SerializeField] Text comboDisplay = null;
        [SerializeField] TextMeshProUGUI comboDisplayTMP = null;
        int currentScore;
        LastHitManager lastHitManager;

        void Awake()
        {
            LastHitManager.ScoreBroadcast += Score;
        }

        void Update()
        {
            if (comboDisplayTMP != null)
            {
                UI_DisplayCombo();
            }
            else
            {
                comboDisplay.text = currentScore.ToString();
            }
        }

        private void Score(int score)
        {
            currentScore = score;
        }

        private void UI_DisplayCombo()
        {
            comboDisplayTMP.text = currentScore.ToString();
            if (currentScore > 1)
            {
                comboDisplayTMP.enabled = true;
            }
            else
            {
                comboDisplayTMP.enabled = false;
            }
        }
    }
}