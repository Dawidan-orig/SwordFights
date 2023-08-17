using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AliveBeing : Interactable_UtilityAI, IDamagable
{
    public float health = 100;
    public Collider vital;
    public GameObject parent;

    private void Awake()
    {
        if (GetComponents<Collider>().Length == 1)
            vital = GetComponent<Collider>();
    }

    public void Damage(float harm, IDamagable.DamageType type)
    {
        if (type == IDamagable.DamageType.sharp)
            health -= harm * 0.5f;
        else if (type == IDamagable.DamageType.blunt)
            health -= harm * 0.2f;
        else if (type == IDamagable.DamageType.thermal)
            health -= harm;

        Utilities.CreateFlowText(Mathf.RoundToInt(harm).ToString(), 5, transform.position, Color.red);

        if(health < 0) 
        {
            if (parent == null)
                Destroy(gameObject);
            else
                Destroy(parent);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if(EditorApplication.isPlaying && !EditorApplication.isPaused)
            Utilities.CreateTextInWorld(health.ToString(), transform, position: transform.position + GetComponent<Collider>().bounds.size.y/2 * Vector3.up, color : Color.green);
    }
}
