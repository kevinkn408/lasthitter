using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using RPG.Core;


namespace RPG.Attributes
{
    public class StateDisplay : MonoBehaviour
    {
        [SerializeField] Text stateDisplay = null;
        ActionScheduler state;
        // Start is called before the first frame update
        void Awake()
        {
            state = FindObjectOfType<Player>().GetComponent<ActionScheduler>();
        }

        // Update is called once per frame
        void Update()
        {
            print(state.GetState.GetType());
            stateDisplay.text = state.GetState.ToString();
        }
    }

}
