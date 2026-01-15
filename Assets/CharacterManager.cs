using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG.Combat;

public class CharacterManager : MonoBehaviour
{
    [SerializeField] Transform rightHandTransform = null;
    [SerializeField] Transform leftHandTransform = null;
    [SerializeField] Avatar avatar = null;
    [SerializeField] WeaponConfig weaponcfg = null;
    public Transform RightHandTransform
    {
        get
        {
            return rightHandTransform;
        }
    }
    public Transform LeftHandTransform
    {
        get
        {
            return leftHandTransform;
        }
    }

    public Avatar Avatar
    {
        get
        {
            return avatar;
        }
    }
    public WeaponConfig WeaponCFG
    {
        get
        {
            return weaponcfg;
        }
    }


    // Start is called before the first frame update
    void Awake()
    {

    }
}
