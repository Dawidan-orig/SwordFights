using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextFlow : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.up * 0.4f * Time.deltaTime);
    }
}
