using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AttackCatcher : MonoBehaviour
{

    [Header("init-s")]
    [Range(0, 30)]
    public int predictions = 10;
    public bool debug_Draw = true;
    public bool sword_as_stuff = false;
    [Min(0.75f)]
    public float ignoredDistance = 10; // ����������� �� CriticalDistance. ��� ���� - ��� ������ ������������ ����� ������.
    public float ignoredSpeed = 5; // ����������� ����������� ��������, ������� � ������� ������ ���� ������. TODO : �������� �� ������� f=mv
    public List<Rigidbody> ignored = new List<Rigidbody>();

    [Header("Readonly")]
    public Collider checker; // ���� �������� - ������ ������� �����.
    public Collider vital; // ����� ��������� ������� ��������� �� ������!
    public List<GameObject> controlled = new(); // ��� �� �����, �� �������� ���� ������� � ������� ������� �����.

    public class AttackEventArgs : EventArgs
    {
        public Rigidbody body;
        public bool free; /// <summary> ����������, ��� ����������� �� ����� ��������. <\summary>
        public Vector3 start;
        public Vector3 end;
        public Vector3 direction;
        public float impulse;
    }

    public event EventHandler<AttackEventArgs> OnIncomingAttack;


    private void FixedUpdate()
    {
        foreach (GameObject thing in controlled)
        {
            if (!thing) // ��������� ���������� �������� � ���� ����� �������
                continue;

            if(thing.TryGetComponent(out Faction f)) 
            {
                if (!f.IsWillingToAttack(GetComponent<Faction>().f_type))
                    continue;
            }

            // � thing �������������� ���� Rigidbody. ��� ������� ���������� � ������.
            Rigidbody rb = thing.GetComponent<Rigidbody>();
            if (ignored.Contains(rb))
                continue;

            if (rb.velocity.magnitude < ignoredSpeed)
                continue;

            if (thing.TryGetComponent(out Blade blade))
                if (blade.host != null || !sword_as_stuff)
                {
                    SwordIncoming(blade);
                    continue;
                }
            
            StuffIncoming(rb);
        }
    }

    private void SwordIncoming(Blade blade)
    {
        // ��� ��������� ��� �����: ���� �������� ���?
        Vector3 center = vital.bounds.center;
        List<Blade.border> preditionList = blade.FixedPredict(predictions);
        //preditionList.Reverse();

        Blade.border closest = preditionList[0];

        foreach (Blade.border border in preditionList) // ���������� �� ���� ������������
        {
            Vector3 borderCenter = Vector3.Lerp(border.posDown, border.posUp, 0.5f);
            if (Vector3.Dot(border.direction, vital.bounds.center - borderCenter) < 0)
                continue;
            // � ���� ������������ ������ �������� ���� ����� ��������� � ������� vital.

            if ((center - border.posUp).magnitude > ignoredDistance)
            {
                continue;
            }
            // � ���� ������������ ������� ���� ���� ��������� ���������� ������

            if (Physics.Raycast(border.posDown, (border.posUp - border.posDown).normalized, (border.posUp - border.posDown).magnitude) //����� �����
                || 
                Physics.Raycast(border.posUp, (border.posDown-border.posUp).normalized, (border.posUp - border.posDown).magnitude) //������ ����
                )
            {
                continue;
            }

            // ���������� ��, ��� ��������� �� �������� ��������� - ��� ������.
            // � ��� �� ��, ��� ��������� �� ���������� - ��� ��� ����������� ������.

            const float TOO_CLOSE = 0.5f; // ���������� ����������� ��������� ������������
            if (Vector3.Distance(border.posDown, vital.ClosestPointOnBounds(border.posDown)) < TOO_CLOSE
                || Vector3.Distance(border.posUp, vital.ClosestPointOnBounds(border.posUp)) < TOO_CLOSE)
                continue;

            closest = border;
        }

        // ��� ������ ������ ���-�� � ����� ����� - � ��������� ����� �� ��� ����� � vital ����������.
        // � struct'�� ��� "==", �� �������� ���������� � �� ������ ��������, ����� �������� � ������ ���������.
        if (closest.posUp == preditionList[0].posUp)
            return;

        if (debug_Draw)
        {
            Debug.DrawLine(closest.posUp, closest.posDown, Color.yellow);
            Debug.DrawRay(closest.posUp, closest.direction * 0.1f, Color.green);
            Debug.DrawLine(vital.bounds.center, closest.posDown, Color.yellow * 0.3f);
            Debug.DrawLine(vital.bounds.center, closest.posUp, Color.yellow * 0.3f);
        }

        OnIncomingAttack?.Invoke(this,
            new AttackEventArgs { body = blade.body, free = false, start = closest.posUp, end = closest.posDown, direction = closest.direction, impulse = blade.body.mass * blade.body.velocity.magnitude });
    }

    private void StuffIncoming(Rigidbody rb)
    {
        if (Vector3.Dot(rb.velocity, vital.bounds.center - rb.position) < 0)
            return;
        // ����� ����� � ������� ����� transform

        Vector3 center = rb.GetComponent<Collider>().bounds.center;

        if ((rb.position - center).magnitude >= ignoredDistance)
            return;
        // ����� ��� ������!

        if (rb.velocity.magnitude < ignoredSpeed)
            return;
        // � ����� ���������� ������� ��������

        Vector3 predictionPoint = rb.position + predictions * Time.fixedDeltaTime * rb.velocity;

        // ���� ����� ��������� �������� - ��� ���� ����� ����������� ������� ������� � ����
        if (Vector3.Dot(rb.velocity, vital.bounds.center - predictionPoint) < 0)
        {
            predictionPoint = Utilities.NearestPointOnLine(rb.position, rb.velocity, vital.ClosestPointOnBounds(rb.position));
            //Debug.DrawLine(vital.bounds.center, predictionPoint, Color.cyan, 2);
        }

        const float BLADE_MIN_DIST = 0.5f;

        // �����������
        if(Vector3.Distance(predictionPoint, vital.ClosestPointOnBounds(predictionPoint)) < BLADE_MIN_DIST)
            predictionPoint = predictionPoint + (rb.position - predictionPoint).normalized * BLADE_MIN_DIST;

        if (debug_Draw)
        {
            Debug.DrawLine(rb.position, predictionPoint, Color.yellow);
        }

        OnIncomingAttack?.Invoke(this, new AttackEventArgs { body = rb,direction = rb.velocity.normalized, start = predictionPoint, end = predictionPoint, free = true, impulse = rb.mass * rb.velocity.magnitude });
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.TryGetComponent<Rigidbody>(out _))
            return;

        if (!debug_Draw)
            return;

        if (collision.transform.TryGetComponent(out Blade blade))
        {
            if (TryGetComponent(out SwordFighter_StateMachine s) && blade == s?.Blade)
            {
                //Debug.Log($"Selfslash at speed {blade.body.velocity.magnitude}", collision.transform);
                Debug.DrawLine(blade.downerPoint.position, blade.upperPoint.position, new Color(0.8f, 0.2f, 0), 3);
            }
            else
            {
                //Debug.Log($"Skipped slash at speed {blade.body.velocity.magnitude}", collision.transform);
                Debug.DrawLine(blade.downerPoint.position, blade.upperPoint.position, new Color(0.5f, 0, 0), 3);
            }
        }
        else
        {
            Utilities.DrawSphere(collision.GetContact(0).point,color : Color.red, duration : 3);
            //Debug.Log($"Blunt damage at speed {collision.rigidbody.velocity.magnitude}", collision.transform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody body = other.GetComponent<Rigidbody>();
        if (body == null)
            return;
        // ��� ����� ����� ����������� �������������� ������������.

        // ����� ��� ����������, ����� ��������� ����������� ������ � ���� ���������.
        controlled.Add(body.gameObject);
        controlled.RemoveAll(item => item == null);
    }

    private void OnTriggerExit(Collider other)
    {
        // ������ ����� �� ����, �� ��� ������ �� ����� ��������� ���������.
        controlled.Remove(other.gameObject);
    }
}
