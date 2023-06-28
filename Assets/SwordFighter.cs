using System;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(AttackCatcher))]
public class SwordFighter : MonoBehaviour
// ��������� �����, ������� �������� � ������� ����� ��������.
{
    [Header("constraints")]
    public float actionSpeed = 10; // �������� �������� ���� � ����
    public float angluarSpeed = 10; // �������� �������� ����
    public float swingDistanceMultiplier = 2; // ��������� ������ ���� ���
    public float criticalImpulse = 200; // ����� ����������, ��� ������ ������ � ��������� ������ �����!
    public float bladeMaxDistance = 2;

    [Header("init-s")]
    public Blade blade;
    public Transform bladeHandle;
    public Collider vital;
    public Transform desireBlade;

    [Header("lookonly")]
    [SerializeField]
    Vector3 formalBladeCenter;
    [SerializeField]
    GameObject bladeObject;
    [SerializeField]
    float moveProgress = 0;
    [SerializeField]
    bool isSwinging = false;
    [SerializeField]
    bool isRepositioning = false;

    //TODO : ���������. ����� ���: Idle � Busy. Busy ��������, ��� ��� ������ ������������.

    void Start()
    {
        AttackCatcher catcher = gameObject.GetComponent<AttackCatcher>();
        catcher.OnIncomingAttack += Incoming;
        catcher.ignored.Add(blade.body);
        bladeObject = blade.gameObject;

        blade.SetHost(gameObject);

        desireBlade.position = bladeHandle.position;
        desireBlade.up = Vector3.up;
        desireBlade.forward = Vector3.forward;
    }

    void Update()
    {
        formalBladeCenter = blade.downerPoint.position + (blade.upperPoint.position - blade.downerPoint.position)/2;

        if (desireBlade.hasChanged)
            SetDesires(desireBlade.position, desireBlade.up);
    }

    private void FixedUpdate()
    {
        if (!isSwinging)
            Control_MoveSword();
        else
            Control_SwingSword();
    }

    private void Incoming(object sender, AttackCatcher.AttackEventArgs e)
    {
        // ����, � ��� ����� ��������� ���.
        if (e.free)
        {
            // ��� ����������������� ������, ������� ������ ����� � ���� �������. ���� ������, ���� ����������!
            if (e.impulse < criticalImpulse)
                Swing(e.start);
            else
            {// Evade() -- ������ ���� � ������ �������, ��� - ������ �����
            }    
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

            GameObject start = new();
            start.transform.position = e.start;
            start.transform.parent = bladePrediction.transform;

            GameObject end = new();
            end.transform.position = e.end;
            end.transform.parent = bladePrediction.transform;

            bladePrediction.transform.Rotate(e.direction, 90);

            Vector3 bladeDown = start.transform.position;
            Vector3 bladeUp = end.transform.position;

            Destroy(bladePrediction);
            int ignored = blade.gameObject.layer; // ���������� ��� �������� �� ������������ ��� ����.
            ignored = ~ignored;

            if (Physics.Raycast(bladeDown, bladeUp - bladeDown, (bladeDown - bladeUp).magnitude, ignored) // ����� �����
                ||
                Physics.Raycast(bladeUp, bladeDown - bladeUp, (bladeDown - bladeUp).magnitude, ignored) // ������ ����
                )
            {
                // ��� ��������� ����-��, ����������.
                //Debug.Log("returning, because precition in: " + hit.collider.transform.name + ", Layermask: " + ignored, hit.collider.gameObject);
                return;
            }            
            
            Block(bladeDown, bladeUp);
        }
    }

    // ���������� �������� �� 0 �� until � ������������ raise � ����� x. ��� ������ x - ��� ����� ��������� � until.
    private float Influence_Func(float x, float until, float raise)
    {
        if (x < 0)
            return 0;
        return Mathf.Pow(raise, -1 / x) * until;
    }

    // ����� ������� �� �����-�� ����� �� ������� �������.
    private void Swing(Vector3 toPoint)
    {
        isSwinging = true;
        
        Vector3 moveTo = toPoint + (toPoint - bladeHandle.position).normalized * swingDistanceMultiplier;    

        Vector3 pointDir = (moveTo - vital.bounds.center).normalized;

        // ����������� ����� � vital
        float distance = (toPoint - vital.ClosestPointOnBounds(toPoint)).magnitude;
        moveTo = vital.ClosestPointOnBounds(moveTo) + (moveTo - vital.ClosestPointOnBounds(moveTo)).normalized * distance;
        
        SetDesires(moveTo, pointDir);
    }

    // ����� ����� �� ����� � �����
    private void Swing(Vector3 from, Vector3 to) 
    {
        // ��������� ����� ����� �� ��� ���, ���� �� ��������� ����.
        isRepositioning = true;
        isSwinging = true;

        SetDesires(from, (from - vital.bounds.center).normalized); // �� ����� from, � ����������� �� vital.

        //TODO : ��� ����� �������� ��� ������, ������� ����� ����������� � ������� ���.
        // ������ ������� Nintendo Wii Sport Resort: ������� ��� ����������� ���������� ��������� �� ���������� �����,
        // � ��� ������ ������������.
    }

    // ׸���� ��������� ���� �� ���� ������. Start - �������, end - ���������� ����������� ����.
    private void Block(Vector3 start, Vector3 end)
    {
        // ������� ������ - ��� ��, ��� ���� �������������.

        SetDesires(start, (end - start).normalized);
    }

    private void Control_MoveSword()
    {
        //TODO : ����� ��� "�������������" ����� �� ����� �����������. ��� � �������� ������������, � �������!
        //TODO : ����������� ������� �� ��� � Lerp, � ������������ Distance(current, desire) < CLOSE_ENOUGH. ��-���� ��� � Control_SwingSword()
        if(moveProgress < 1)
            moveProgress += actionSpeed * Time.fixedDeltaTime;

        float heightFrom = bladeHandle.position.y;
        float heightTo = desireBlade.position.y;

        Vector3 from = new Vector3(bladeHandle.position.x, 0, bladeHandle.position.z);
        Vector3 to = new Vector3(desireBlade.position.x, 0, desireBlade.position.z);

        bladeHandle.position = Vector3.Slerp(from, to, moveProgress) + new Vector3(0, Mathf.Lerp(heightFrom, heightTo, moveProgress), 0);
        
        #region rotationControl;
        //TODO : ������������ ������� ��� moveProgress!
        GameObject go = new();
        Transform probe = go.transform;
        probe.position = bladeHandle.position;
        probe.rotation = bladeHandle.rotation;
        probe.parent = null;

        probe.LookAt(bladeHandle.position + desireBlade.up, Vector3.up);
        probe.Rotate(Vector3.right, 90);        

        // ���� ��� ������� � ������� vital
        if (Vector3.Dot(probe.up, vital.bounds.center - probe.position) < 0)
        {            
            //probe.Rotate(probe.up,180);
        }

        Debug.DrawRay(probe.position, probe.forward, Color.green);
        Debug.DrawRay(probe.position, probe.up, Color.yellow);

        bladeHandle.rotation = Quaternion.Lerp(bladeHandle.rotation, transform.rotation * probe.rotation, actionSpeed * Time.fixedDeltaTime);

        Destroy(go);
        #endregion
        
        // ���� ��� �������� ������������ ����. TODO : �������.
        /*
        Vector3 closestPos = vital.bounds.center;
        if (Vector3.Distance(desireBlade.position, closestPos) > bladeMaxDistance)        
            desireBlade.position = closestPos + (desireBlade.position - closestPos).normalized * bladeMaxDistance;     
        */

        // �� �������:
        //TODO : �������� ������� �� ������ �������� ����, ��������� � ��������� � ����� ��������.
    }

    private void Control_SwingSword() 
    {
        const float CLOSE_ENOUGH = 0.1f;

        if (Vector3.Distance(bladeHandle.position, desireBlade.position) < CLOSE_ENOUGH)
            isSwinging = false;

        if (isRepositioning)
        {
            Control_MoveSword();
            return;
        }

        // ������ ��� ��� ����� ������ ������� ��� �� ������� ������� �� �������, ����������� -- ������ �� vital.
        // �������� - impulse.
        // �������������� ������ ����������� ���� Mathf.Lerp'�, ��������� ��� ����� ������ �������� ������� ��������, � �� ����������� ������� �������.

        if (moveProgress < 1)
            moveProgress += actionSpeed * Time.fixedDeltaTime;

        float heightFrom = bladeHandle.position.y;
        float heightTo = desireBlade.position.y;

        Vector3 from = new Vector3(bladeHandle.position.x, 0, bladeHandle.position.z);
        Vector3 to = new Vector3(desireBlade.position.x, 0, desireBlade.position.z);

        bladeHandle.position = Vector3.Slerp(from, to, moveProgress) + new Vector3(0, Mathf.Lerp(heightFrom, heightTo, moveProgress), 0);

        bladeHandle.up = (bladeHandle.position-vital.bounds.center).normalized * swingDistanceMultiplier;

        Vector3 closestPos = vital.bounds.center;
        if (Vector3.Distance(desireBlade.position, closestPos) > bladeMaxDistance)
            desireBlade.position = closestPos + (desireBlade.position - closestPos).normalized * bladeMaxDistance;        
    }

    private void SetDesires(Vector3 pos, Vector3 dir) 
    {
        desireBlade.position = pos;
        desireBlade.up = dir;
        moveProgress = 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(bladeHandle.position, 0.05f);
        Gizmos.color = new Color(0.9f, 0.9f, 0.9f);
        Gizmos.DrawSphere(desireBlade.position, 0.05f);
        Gizmos.DrawRay(desireBlade.position, desireBlade.up.normalized * (blade.upperPoint.position - blade.downerPoint.position).magnitude);
    }
}