using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    //TODO : �������� GFX
    private void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }
}
