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
    public float swingDistanceMultiplier = 2; // ��������� ������ ������ ��������� ��� ����� ���������.
    public float startSwingDistance = 2; // ��������� ������ ������ ��������� ��� �� �����.
    public float criticalImpulse = 200; // ����� ����������, ��� ������ ������ � ��������� ������ �����!
    public float bladeMaxDistance = 2; // ������������ ���������� �� vital �� ������� ����. �� ����, ����� ����.
    public float close_enough = 0.1f; // ���������� �� ����, ��� ������� ����� ������ ���������.    

    [Header("init-s")]
    public Blade blade;
    public Transform bladeHandle;
    public Collider vital;
    public Transform desireBlade;

    [Header("lookonly")]
    [SerializeField]
    Transform initialBlade;
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

        blade.SetHost(gameObject);

        desireBlade.gameObject.SetActive(true);
        desireBlade.position = bladeHandle.position;
        desireBlade.up = Vector3.up;
        desireBlade.forward = Vector3.forward;

        GameObject initialBladeGO = new GameObject();
        initialBlade = initialBladeGO.transform;
        initialBlade.position = desireBlade.position;
        initialBlade.rotation = desireBlade.rotation;
    }

    void Update()
    {
        // ����� ��� ��������� � Editor'� ��������, ������� ���:
        if (desireBlade.hasChanged)
            SetDesires(desireBlade.position, desireBlade.up);
    }

    private void FixedUpdate()
    {
        //TODO : ����� �� �������� ������� �� ������� isSwinging, ���� ����� ������� �� �������� ��� ������ � ������
        if (!isSwinging)
        {            
            if (Vector3.Distance(bladeHandle.position, desireBlade.position) < close_enough)
                SetDesires(initialBlade.position, initialBlade.up);
            
            Control_MoveSword();
        }
        else
            Control_SwingSword();
    }

    private void Incoming(object sender, AttackCatcher.AttackEventArgs e)
    {
        // ����, � ��� ����� ��������� ���.
        if (e.free)
        {
            // ��� ����������������� ������, ������� ������ ����� � ���� �������. ���� ������, ���� ����������!
            if (e.impulse < criticalImpulse && !isSwinging)
            {
                Vector3 bladeCenter = Vector3.Lerp(blade.upperPoint.position, blade.downerPoint.position, 0.5f);

                if (Vector3.Distance(vital.bounds.center, e.body.position) <
                    Vector3.Distance(vital.bounds.center, bladeCenter) + bladeMaxDistance) // ���������� ������, ����� ����.
                {
                    if (Vector3.Distance(e.start, bladeCenter) > startSwingDistance)
                        Swing(e.start);
                    else
                        Swing(e.start + (bladeCenter - e.start).normalized * startSwingDistance,to : e.start);
                }
            }
            else
            {// Evade() -- ������ ���� � ������ �������, ��� - ������ �����
            }    
        }
        else
        {
            Vector3 enemyBladeCenter = Vector3.Lerp(e.start, e.end, 0.5f); // ����� �����, ����� ������ ���� ������ ���� � ���� �� �����.

            GameObject bladePrediction = new();
            bladePrediction.transform.position = enemyBladeCenter;

            GameObject start = new();
            start.transform.position = e.start;
            start.transform.parent = bladePrediction.transform;

            GameObject end = new();
            end.transform.position = e.end;
            end.transform.parent = bladePrediction.transform;

            //TODO : ��� ������ ������, ��-�� ���� ���������� ������ "����������".
            //bladePrediction.transform.Rotate(e.direction, 90);

            bladePrediction.transform.rotation = Quaternion.FromToRotation((end.transform.position - start.transform.position).normalized, transform.up);

            Vector3 closest = vital.ClosestPointOnBounds(enemyBladeCenter);            
            Vector3 toBlade_Dir = (bladePrediction.transform.position - closest).normalized;
            toBlade_Dir.y = 0;
            bladePrediction.transform.Rotate(toBlade_Dir, 90);

            Vector3 bladeDown = start.transform.position;
            Vector3 bladeUp = end.transform.position;

            Destroy(bladePrediction);
            int ignored = blade.gameObject.layer; // ��� ������ ������ ��� ��������.
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
        throw new NotImplementedException();

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
        SetDesires(start, (end - start).normalized);
    }

    private void Control_MoveSword()
    {        
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

        bladeHandle.rotation = Quaternion.Lerp(bladeHandle.rotation, transform.rotation * probe.rotation, actionSpeed * Time.fixedDeltaTime);
        bladeHandle.LookAt(probe);

        Destroy(go);
        #endregion
        
        // ����������� ��� �����
        Vector3 closestPos = vital.bounds.center;
        if (Vector3.Distance(desireBlade.position, closestPos) > bladeMaxDistance)        
            desireBlade.position = closestPos + (desireBlade.position - closestPos).normalized * bladeMaxDistance;

        //TODO (�� 01.07.2023) :
        // - ������� ����������� ���� � idle � initialPosition
        // - ����� ��� �� ����� "�������" ������ ��������������� ��������� ������� � vital. ������ ��������� �����������.
        // - ���:
        //TODO : ����� ��� "�������������" ����� �� ����� �����������. ��� � �������� ������������, � �������!
        //TODO : ����������� ������� �� ��� � Lerp, � ������������ Distance(current, desire) < CLOSE_ENOUGH. ��-���� ��� � Control_SwingSword()
        // ����� ��� ��������: "������������"; ����� ��������; ����� "������������" � ������ ���������.
        // � ������� ������ �� ������ Lerp'�. ��� ����� ������ ��������.

        // �� �������:
        //TODO : �������� ������� �� ������ �������� ����, ��������� � ��������� � ����� ��������.
    }

    private void Control_SwingSword() 
    {
        if (Vector3.Distance(bladeHandle.position, desireBlade.position) < close_enough)
            isSwinging = false;

        if (isRepositioning)
        {
            Control_MoveSword();
            if (Vector3.Distance(bladeHandle.position, desireBlade.position) < close_enough)
                isRepositioning = false;
            return;
        }

        if (moveProgress < 1)
            moveProgress += actionSpeed * Time.fixedDeltaTime;

        float heightFrom = bladeHandle.position.y;
        float heightTo = desireBlade.position.y;

        Vector3 from = new Vector3(bladeHandle.position.x, 0, bladeHandle.position.z);
        Vector3 to = new Vector3(desireBlade.position.x, 0, desireBlade.position.z);

        bladeHandle.position = Vector3.Slerp(from, to, moveProgress) + new Vector3(0, Mathf.Lerp(heightFrom, heightTo, moveProgress), 0);

        bladeHandle.up = (bladeHandle.position-vital.bounds.center).normalized * swingDistanceMultiplier;
        /*
        Vector3 closestPos = vital.bounds.center;
        if (Vector3.Distance(desireBlade.position, closestPos) > bladeMaxDistance)
            desireBlade.position = closestPos + (desireBlade.position - closestPos).normalized * bladeMaxDistance;
        */
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
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f);
        Gizmos.DrawSphere(desireBlade.position, 0.05f);
        Gizmos.DrawRay(desireBlade.position, desireBlade.up.normalized * (blade.upperPoint.position - blade.downerPoint.position).magnitude);
    }
}