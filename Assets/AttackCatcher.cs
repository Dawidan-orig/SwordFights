using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AttackCatcher : MonoBehaviour
{

    [Header("init-s")]
    [Range(0, 15)]
    public int predictions = 10;
    public bool draw = true;
    [Min(0.75f)]
    public float ignoredDistance = 10; // ����������� �� CriticalDistance. ��� ���� - ��� ������ ������������ ����� ������.
    public float ignoredSpeed = 5; // ��� ����� ������� ���������, ����� � ��� ����������.
    public List<Rigidbody> ignored = new List<Rigidbody>();

    [Header("Readonly")]
    public Collider checker; // ���� �������� - ������ ������� �����.
    public Collider vital; // ����� ��������� ������� ��������� �� ������!
    public List<GameObject> controlled = new(); // ��� �� �����, �� �������� ���� ������� � ������� ������� �����.

    public class AttackEventArgs : EventArgs
    {
        public Rigidbody body;
        public bool free; // ����������, ��� ����������� �� ����� ��������.
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

            // � thing �������������� ���� Rigidbody. ��� ������� ���������� � ������.
            Rigidbody rb = thing.GetComponent<Rigidbody>();
            if (ignored.Contains(rb))
                continue;

            if (rb.velocity.magnitude < ignoredSpeed)
                continue;

            if (thing.TryGetComponent(out Blade blade))
                SwordIncoming(blade);
            else
                StuffIncoming(rb);
        }
    }

    private void SwordIncoming(Blade blade)
    {
        // ��� ��������� ��� �����: ���� �������� ���?
        Vector3 center = vital.bounds.center;
        List<Blade.border> preditionList = blade.FixedPredict(predictions);
        Blade.border closest = preditionList[0];
        foreach (Blade.border border in preditionList)
        {
            Vector3 borderCenter = Vector3.Lerp(border.posDown, border.posUp, 0.5f);
            if (Vector3.Dot(border.direction, vital.bounds.center - borderCenter) < 0)
                continue;
            // � ���� ������������ ��� ����� ��������� � ������� vital.

            if ((center - border.posUp).magnitude > ignoredDistance)
            {
                continue;
            }
            // � ��� ������������ ������� ���� ���� ��������� ���������� ������

            RaycastHit hit;
            if (Physics.Raycast(border.posDown, (border.posUp - border.posDown).normalized, out hit, (border.posUp - border.posDown).magnitude, 64))
            {
                continue;
            }
            // ���������� ��, ��� ��������� �� �������� ��������� - ��� ������.
            // � ��� �� ��, ��� ��������� �� ���������� - ��� ��� ����������� ������.

            if (MathF.Abs((center - borderCenter).magnitude) <
                MathF.Abs((center - Vector3.Lerp(closest.posDown, closest.posUp, 0.5f)).magnitude))
            {
                closest = border;
            }
        }

        // ��� ������ ������ ���-�� � ����� ����� - � ��������� ����� �� ��� ����� � vital ����������.
        // � struc'�� ��� "==", �� �������� ���������� � �� ������ ��������, ����� �������� � ������ ���������.
        if (closest.posUp == preditionList[0].posUp)
            return;

        if (draw)
        {
            Debug.DrawLine(closest.posUp, closest.posDown, Color.yellow);
            Debug.DrawRay(closest.posUp, closest.direction * 0.1f, Color.green);
        }
        OnIncomingAttack?.Invoke(this, new AttackEventArgs { body = blade.body, free = false, start = closest.posUp, end = closest.posDown, direction = closest.direction, impulse = blade.body.mass * blade.body.velocity.magnitude });
    }

    private void StuffIncoming(Rigidbody rb)
    {
        if (!Physics.Raycast(rb.position, rb.velocity.normalized))
            return;
        // ����� ����� � ������� ����� transform

        if ((transform.position - rb.position).magnitude >= ignoredDistance)
            return;
        // ����� ��� ������!

        //TODO: ��� ��������� ��� �����, ����� ������� ���� �������, ����� ������ ��������� ������.


        if(draw)
            Debug.DrawLine(transform.position, transform.position + Vector3.up * 5, Color.red);
        OnIncomingAttack?.Invoke(this, new AttackEventArgs { body = rb, free = true, impulse = rb.mass * rb.velocity.magnitude });
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.TryGetComponent(out Blade blade))
        {
            if (blade == GetComponent<SwordFighter>().blade)
                Debug.Log("Selfslash", collision.transform);
            else
                Debug.Log("Skipped slash", collision.transform);

            Debug.DrawLine(blade.downerPoint.position, blade.upperPoint.position, new Color(0.5f, 0, 0), 3); 
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
    }

    private void OnTriggerExit(Collider other)
    {
        // ������ ����� �� ����, �� ��� ������ �� ����� ��������� ���������.
        controlled.Remove(other.gameObject);
    }
}
