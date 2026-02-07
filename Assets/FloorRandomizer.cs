using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FloorRandomizer : MonoBehaviour
{
    [SerializeField] List<Material> floorMat = null;
    [SerializeField] MeshRenderer meshR = null;

    private void Awake()
    {
        if (floorMat == null || floorMat.Count == 0) return;
        var index = UnityEngine.Random.Range(0, floorMat.Count);
        SetMaterial(index);
    }
    // Update is called once per frame
    private void SetMaterial(int index)
    {
        switch (index)
        {
            case 0:
                meshR.material = floorMat[0];
                break;
            case 1:
                meshR.material = floorMat[1];
                break;
            case 2:
                meshR.material = floorMat[2];
                break;
            case 3:
                meshR.material = floorMat[3];
                break;
            default:
                break;
        }
    }
}
