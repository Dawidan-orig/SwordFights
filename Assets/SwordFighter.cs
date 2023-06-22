using System;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(AttackCatcher))]
public class SwordFighter : MonoBehaviour
// ��������� �����, ������� �������� � ������� ����� ��������.
{
    [Header("constraints")]
    public GameObject enemy;
    public float actionSpeed = 10; // �������� �������� ���� � ����
    public float angluarSpeed = 10; // �������� �������� ����
    public float swingImpulse = 20; // ��������� ������ ���� ���
    public float actionDistance = 1; // ��� ����� ���� �� ������ ���� ���������.
    public float criticalImpulse = 200; // ����� ����������, ��� ������ ������ � ��������� ������ �����!

    [Header("init-s")]
    public Blade blade;
    public Transform bladeHandle;
    public Vector3 offset = Vector3.up;
    public Collider vital;
    public bool fixated = true;

    [Header("lookonly")]
    public Vector3 formalCenter;
    public GameObject bladeObject;
    public Vector3 desireDirection = Vector3.up;
    public Vector3 desirePosition;

    // Start is called before the first frame update
    void Start()
    {
        AttackCatcher catcher = gameObject.GetComponent<AttackCatcher>();
        catcher.OnIncomingAttack += Incoming;
        catcher.ignored.Add(blade.body);
        bladeObject = blade.gameObject;

        desirePosition = bladeHandle.position;
    }

    // Update is called once per frame
    void Update()
    {
        // Bridge pattern?
        formalCenter = offset + transform.position;
    }

    private void FixedUpdate()
    {
        Contorl_MoveSword();

        Debug.DrawRay(desirePosition, desireDirection.normalized);
    }

    private void Incoming(object sender, AttackCatcher.AttackEventArgs e)
    {
        // ����, � ��� ����� ��������� ���.
        if (e.free)
        {
            // ��� ����������������� ������, ������� ������ ����� � ���� �������. ���� ������, ���� ����������!
            if (e.impulse < criticalImpulse)
                Swing(e.body.position);
            else
                Evade(e.body.position);
        }
        else
        {
            // � ���� �������� ����� ����������� ��� � ������������ ����.
            // ������ �����, ��� ����� ������.
            // ����� ��������� ������� ����,
            // ������ ����� ������� ������ ���� (����-���������)
            // � ������ ����� ������ ����������, ���� ����� ������ �������.


            // ���� ����� ������������� �� ����

            Vector3 bladeCenter = Vector3.Lerp(e.start, e.end, 0.5f); // ����� �����, ����� ������ ���� ������ ���� � ���� �� �����.

            GameObject bladePrediction = new();
            bladePrediction.transform.position = bladeCenter;
            if (fixated)
            {
                Vector3 closest = vital.ClosestPoint(bladeCenter);
                bladePrediction.transform.position = closest + (bladeCenter - closest).normalized * actionDistance;
            }

            GameObject start = new();
            start.transform.position = e.start;
            start.transform.parent = bladePrediction.transform;

            GameObject end = new();
            end.transform.position = e.end;
            end.transform.parent = bladePrediction.transform;

            bladePrediction.transform.Rotate(e.direction, 90);

            Vector3 bladeStart = start.transform.position;
            Vector3 bladeEnd = end.transform.position;

            Destroy(bladePrediction);
            int ignored = blade.gameObject.layer; // ���������� ��� �������� �� ������������ ��� ����.
            ignored = ~ignored;

            // ��������� ����� �����
            if (Physics.Raycast(bladeStart, bladeEnd, out RaycastHit hit, (bladeStart - bladeEnd).magnitude, ignored))
            {
                // ��� ��������� ����-��. ��������, � ���� ������. �� ���.
                Debug.DrawLine(bladeStart, bladeEnd, new Color(0.9f,0.6f, 0.6f), 10);
                Debug.Log("returning, because precition in: " + hit.collider.transform.name + ", Layermask: " + ignored, hit.collider.gameObject);
                return;
            }

            // ������ ����.
            if (Physics.Raycast(bladeEnd, bladeStart, out hit, (bladeStart - bladeEnd).magnitude, ignored))
            {
                // ��� ��������� ����-��. ��������, � ���� ������. �� ���.
                Debug.DrawLine(bladeStart, bladeEnd, new Color(0.9f, 0.6f, 0.6f), 10);
                Debug.Log("returning, because precition in: " + hit.collider.transform.name, hit.collider.gameObject);
                return;
            }

            if ((formalCenter - bladeEnd).magnitude < (formalCenter - bladeStart).magnitude)
                Block(bladeStart, bladeEnd);
            else
                Block(bladeStart, bladeEnd);
        }
    }

    // ������ �� Rigidbody ����� �������.
    private void Evade(Vector3 fromPoint) { }

    // ����� ������� �� ��� ������� �����.
    private void Swing(Vector3 toPoint) { }

    // ��������� ���� � ������� ���������� �� �����-�� �����.
    private void Block(Vector3 point)
    {
        // ������ �� �������� ��������� ����, ����������� ������ ��������� ���, ����� ������������� �����.
        // ����� - ����� ����, ��� �������
    }
    private void Block(Vector3 start, Vector3 end)
    {
        // ������� ������ - ��� ��, ��� ���� �������������.

        desireDirection = (end - start).normalized;
        desirePosition = start;

        //TODO : ������ ��� ���� ������� ����� ��������� � ������� ����������� ����.

    }

    private void Contorl_MoveSword()
    {
        float progress = actionSpeed * Time.fixedDeltaTime;

        float heightFrom = (bladeHandle.position - formalCenter).y;
        float heightTo = (desirePosition - formalCenter).y;

        Vector3 from = new Vector3((bladeHandle.position - formalCenter).x, 0, (bladeHandle.position - formalCenter).z);
        Vector3 to = new Vector3((desirePosition - formalCenter).x, 0, (desirePosition - formalCenter).z);

        bladeHandle.position = formalCenter + Vector3.Slerp(from,to , progress) + new Vector3(0, Mathf.Lerp(heightFrom, heightTo, progress), 0);

        
        #region rotationControl;
        GameObject go = new();
        Transform probe = go.transform;
        probe.position = bladeHandle.position;
        probe.rotation = bladeHandle.rotation;
        probe.parent = null;

        probe.LookAt(bladeHandle.position + desireDirection, Vector3.up);
        probe.Rotate(Vector3.right, 90);

        //���� ������� ���������, � ���� �����-���� � ���������.
        //����� ������� ������������� ���, ��� ��� �����, ���� ���������� ������� probe � �� �� �����, � ������� �������� ��������
        //������ ������� ����, ��� ��, ���������. ��-���� ����� �, � ������� �� ���������� �� ������ vital.
        //��� ��������� ������ ��� �������, ������� ���� ������������.
        //��������, ���� ������� ������ � ������� � ���������� ��������� - �������� �� ����� ������, � ��-�� position-�������� probe ������� vital.
        //


        bladeHandle.rotation = Quaternion.Lerp(bladeHandle.rotation, transform.rotation * probe.rotation, progress);

        Debug.DrawRay(bladeHandle.position, probe.rotation * Vector3.up);

        Destroy(go);
        #endregion
        
        //TODO : �������� ���� ������� ������������ ���� �� vital ����� ��������������
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(bladeHandle.position, 0.05f);
        Gizmos.DrawSphere(desirePosition, 0.05f);
    }
}