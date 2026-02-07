using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDeactivate : MonoBehaviour
{
    public float selfdestruct_in = 4f; // 0 = never disable

    private void OnEnable()
    {
        if (selfdestruct_in > 0f)
        {
            Invoke(nameof(DisableSelf), selfdestruct_in);
        }
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void DisableSelf()
    {
        gameObject.SetActive(false);
    }
}
