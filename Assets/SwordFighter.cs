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

            Vector3 bladeUp = start.transform.position;
            Vector3 bladeDown = end.transform.position;

            Destroy(bladePrediction);

            if (Physics.Raycast(bladeUp, bladeDown, out RaycastHit hit, 10)) //TODO : ���� ����� ����.
            {
                // ��� ��������� ����-��. ��������, � ���� ������. �� ���.
                Debug.DrawLine(bladeUp, bladeDown, Color.red, 10);
                return;
            }

            if ((formalCenter - bladeDown).magnitude < (formalCenter - bladeUp).magnitude)
                Block(bladeDown, bladeUp);
            else
                Block(bladeUp, bladeDown);
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
        probe.parent = transform;

        probe.LookAt(bladeHandle.position + desireDirection, Vector3.up);
        probe.Rotate(Vector3.right, 90);

        bladeHandle.rotation = Quaternion.Lerp(bladeHandle.rotation, probe.rotation, progress);

        Destroy(go);
        #endregion
        
        
        const float push = 0.1f;
        while(true) //TODO : �� ��������, ��������!
        {
            Vector3 up = blade.upperPoint.position;
            Vector3 down = blade.downerPoint.position;
            float length = (up - down).magnitude;

            var hitsDown = Physics.RaycastAll(down, (up-down).normalized, length);
            var hitsUp = Physics.RaycastAll(up, (down - up).normalized, length);
            var hitsAll = new RaycastHit[hitsDown.Length + hitsUp.Length];
            hitsDown.CopyTo(hitsAll, 0);
            hitsUp.CopyTo(hitsAll, hitsDown.Length);

            bool vitalFound = false;
            foreach(RaycastHit hit in hitsAll) 
            {
                vitalFound = hit.transform == vital.transform;
                if(vitalFound)
                {
                    break;
                }
            }

            if (!vitalFound)
                break;

            Vector3 pushDirection = (desirePosition - formalCenter).normalized;
            desirePosition += pushDirection * push;
            Debug.DrawRay(formalCenter, pushDirection, Color.cyan);
        }
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(bladeHandle.position, 0.05f);
        Gizmos.DrawSphere(desirePosition, 0.05f);
    }
}