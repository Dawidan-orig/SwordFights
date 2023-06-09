using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Blade : MonoBehaviour
{
    [Header("Init-s")]
    public Transform upperPoint;
    public Transform downerPoint;

    public GameObject host { get; private set; }
    [Header("lookonly")]
    public Rigidbody body;
    public Vector3 DEBUG_AngularVelocityEuler;

    [Header("Constraints")]
    public bool visualPrediction = true;
    public bool alwaysDraw = false;
    public int iterations = 1;

    public struct border
    {
        public Vector3 posUp;
        public Vector3 posDown;
        public Vector3 direction;
    }

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        body.centerOfMass = Vector3.up / 3;
    }

    public void SetHost(GameObject newHost) 
    {
        host = newHost;   
    }

    public List<border> FixedPredict(int prediction)
    {
        List<border> res = new List<border>();

        border start = new();

        Vector3 rotatedPosUp = upperPoint.position - transform.position;
        rotatedPosUp = Quaternion.Euler(DEBUG_AngularVelocityEuler * Time.fixedDeltaTime) * rotatedPosUp;
        start.posUp = transform.position + rotatedPosUp + (body.velocity * Time.fixedDeltaTime);

        Vector3 rotatedPosDown = downerPoint.position - transform.position;
        rotatedPosDown = Quaternion.Euler(DEBUG_AngularVelocityEuler * Time.fixedDeltaTime) * rotatedPosDown;
        start.posDown = transform.position + rotatedPosDown + (body.velocity * Time.fixedDeltaTime);

        start.direction = body.velocity.normalized;

        res.Add(start);
        // ������ ������������ - ������ ������.
        CollisionControl(start);

        for (int i = 0; i < prediction; i++)
        {
            border border = new();

            int offset_i = i + 1; // ��� ����� ��-�� ����, ��� ������� ��������� ���������, � ��� ����� - ����������. ��-�� ����� ��� ������ �� ���� ��������

            rotatedPosUp = upperPoint.position - transform.position;
            rotatedPosDown = downerPoint.position - transform.position;
            for (int j = 0; j < offset_i; j++)
            {
                rotatedPosUp = Quaternion.Euler(DEBUG_AngularVelocityEuler * Time.fixedDeltaTime) * rotatedPosUp;
                rotatedPosDown = Quaternion.Euler(DEBUG_AngularVelocityEuler * Time.fixedDeltaTime) * rotatedPosDown;
            }

            border.posUp = transform.position + rotatedPosUp + offset_i * body.velocity * Time.fixedDeltaTime;
            border.posDown = transform.position + rotatedPosDown + offset_i * body.velocity * Time.fixedDeltaTime;

            //������� �� PosUp, ��� ��� �� ����� ���������� ���������
            border.direction = body.velocity.normalized;

            //TODO : ����������� ���� �� ��������� ����� ���� � ������������ � HandlePoint, ����� ��������� ������������� ���� � handlepoint 

            res.Add(border);
        }

        border? prevous = null;
        if (visualPrediction)
            foreach (border border in res)
            {
                if (prevous == null)
                {
                    prevous = border;
                    continue;
                }

                Debug.DrawLine(prevous.Value.posUp, border.posUp);
                Debug.DrawLine(border.posUp, border.posUp + Vector3.up * 0.05f);

                Debug.DrawLine(prevous.Value.posDown, border.posDown);
                Debug.DrawLine(border.posDown, border.posDown + Vector3.up * 0.05f);


                Debug.DrawLine(border.posDown, border.posUp, Color.red);


                Vector3 center = Vector3.Lerp(border.posDown, border.posUp, 0.5f);
                Debug.DrawLine(center, center + border.direction * 0.1f, new Color(1, 0.4f, 0.4f));

                prevous = border;
            }

        return res;
    }

    public void CollisionControl(border border)
    {
        // ��� ������� �������� � Predict ��� �����������. ��� �������� �� �������� ���������� ������ ����� ��� ��������� �� ����.
        // ������ ���� ������� - ��������� ��� � ������ �����.
        Vector3 center = Vector3.Lerp(downerPoint.position, upperPoint.position, 0.5f);
        Vector3 halfExtents = new Vector3(0.1f, (upperPoint.position - downerPoint.position).magnitude, 0.1f);
        if (Physics.BoxCast(
            center,
            halfExtents,
            border.direction,
            out RaycastHit hit,
            transform.rotation,
            (border.posUp - upperPoint.position).magnitude
            ))
        {
            if (hit.transform.TryGetComponent(out Blade _))
            {   
                //TODO : ��������, ���������, �������� �� ������.
                Vector3 closest = gameObject.GetComponent<Collider>().ClosestPointOnBounds(hit.point);
                transform.position = closest;
            }
        }
    }

    private void FixedUpdate()
    {
        //TODO : �������� boxcast ��� �������� �� �������� ��� ������� ��������.
        DEBUG_AngularVelocityEuler = body.angularVelocity * 360 / (2 * Mathf.PI);

        if (alwaysDraw)
            FixedPredict(iterations);
    }

    /*
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.TryGetComponent(out Blade b)) 
        {
            b.body.velocity = b.body.velocity * 0.5f;
        }
    }*/

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(downerPoint.position, upperPoint.position);
    }
}
