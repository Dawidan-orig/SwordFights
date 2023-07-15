using System;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(AttackCatcher))]
public class SwordFighter : MonoBehaviour
// ��������� �����, ������� �������� � ������� ����� ��������.
{
    [Header("constraints")]
    public float actionSpeed = 10; // �������� �������� ���� � ����
    public float swing_EndDistanceMultiplier = 2; // ��������� ������ ������ ��������� ��� ����� ���������.
    public float swing_startDistance = 2; // ��������� ������ ������ ��������� ��� �� �����.
    public float criticalImpulse = 200; // ����� ����������, ��� ������ ������ � ��������� ������ �����!
    public float bladeMaxDistance = 2; // ������������ ���������� �� vital �� ������� ����. �� ����, ����� ����.
    public float bladeMinDistance = 0.1f; // ����������� ���������� �� vital.
    public float close_enough = 0.1f; // ���������� �� ����, ��� ������� ����� ������ ���������.
    public float toInitialAwait = 2; // ������� ������� ������� �� ��������� ���� � ������� �������?

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
    float currentToInitialAwait;
    [SerializeField]
    Rigidbody lastIncoming = null;

    [Header("Debug")]
    public bool isSwordFixing = true;

    void Start()
    {
        AttackCatcher catcher = gameObject.GetComponent<AttackCatcher>();
        catcher.OnIncomingAttack += Incoming;
        catcher.ignored.Add(blade.body);

        //currentToInitialAwait = toInitialAwait;

        blade.SetHost(gameObject);

        desireBlade.gameObject.SetActive(true);
        desireBlade.position = bladeHandle.position;
        desireBlade.rotation = bladeHandle.rotation;

        GameObject initialBladeGO = new GameObject("InititalBladePosition");
        initialBlade = initialBladeGO.transform;
        initialBlade.position = bladeHandle.position;
        initialBlade.rotation = bladeHandle.rotation;
        initialBlade.parent = transform;
    }

    private void FixedUpdate()
    {
        if (!isSwinging)
        {
            if (Vector3.Distance(bladeHandle.position, desireBlade.position) < close_enough)
            {   
                if (currentToInitialAwait < toInitialAwait)
                    currentToInitialAwait += Time.fixedDeltaTime;
                else
                {
                    if(initialBlade.position != desireBlade.position)
                        SetDesires(initialBlade.position, initialBlade.up, initialBlade.forward);                    
                }
            }
            
            Control_MoveSword();
        }
        else
            Control_SwingSword();

        if(isSwordFixing)
            Control_FixSword();
    }

    private void Incoming(object sender, AttackCatcher.AttackEventArgs e)
    {
        Rigidbody currentIncoming = e.body;
        currentToInitialAwait = 0;

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
                    Swing(e.start);
                }
                else // ���������� ����
                {
                    //...���� � ���� ���� �����
                    if (Vector3.Distance(e.start, bladeCenter) < swing_startDistance)
                    {
                        SetDesires(e.start + (bladeCenter - e.start).normalized * swing_startDistance,
                            (bladeCenter - vital.bounds.center).normalized,
                            (e.start - bladeHandle.position).normalized,
                            nullifyProgress: true);
                    }
                    //TODO : ������� ����� ���� � ��������������� �������. ��� �� ������ ������ SwordFighter - �� ��������� ������ ����� � �����.
                }
            }
            else
            {
                // Evade() -- ������ ���� � ������ �������, ��� - ������ �����
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

            // ������������� ��� �������������� vital
            //bladePrediction.transform.rotation = Quaternion.FromToRotation((end.transform.position - start.transform.position).normalized, transform.up);
          
            Vector3 toEnemyBlade_Dir = (bladePrediction.transform.position - vital.bounds.center).normalized;
            bladePrediction.transform.Rotate(toEnemyBlade_Dir, 90);

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
                return;
            }

            Block(bladeDown, bladeUp, toEnemyBlade_Dir, currentIncoming != lastIncoming);
        }

        lastIncoming = currentIncoming;
    }

    // ����� ������� �� �����-�� ����� �� ������� �������.
    private void Swing(Vector3 toPoint)
    {
        isSwinging = true;
        
        Vector3 moveTo = toPoint + (toPoint - bladeHandle.position).normalized * swing_EndDistanceMultiplier;    

        Vector3 pointDir = (moveTo - vital.bounds.center).normalized;

        // ����������� ����� � vital
        float distance = (toPoint - vital.ClosestPointOnBounds(toPoint)).magnitude;
        moveTo = vital.ClosestPointOnBounds(moveTo) + (moveTo - vital.ClosestPointOnBounds(moveTo)).normalized * distance;
        
        SetDesires(moveTo, pointDir, (moveTo - toPoint).normalized);
    }

    // ��������� ���� �� ���� ��������� ����������
    private void Block(Vector3 start, Vector3 end, Vector3 SlashingDir, bool nullifyProgress = false)
    {
        SetDesires(start, (end - start).normalized, SlashingDir, nullifyProgress);
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
        GameObject go = new();
        Transform probe = go.transform;
        probe.position = bladeHandle.position;
        probe.rotation = desireBlade.rotation;
        probe.parent = null;

        bladeHandle.rotation = Quaternion.Lerp(bladeHandle.rotation, transform.rotation * probe.rotation, moveProgress);

        Destroy(go);
        #endregion

        Vector3 closestPos = vital.ClosestPointOnBounds(bladeHandle.position);
        const float TWO_DIVIDE_THREE = 2/3;
        
        if(moveProgress < TWO_DIVIDE_THREE) // ����������� ����������� ������
        {
            desireBlade.position = closestPos + (desireBlade.position - closestPos).normalized * bladeMinDistance;

            GameObject upDirectioner = new();
            Vector3 toNearest = closestPos - desireBlade.position;
            upDirectioner.transform.up = toNearest;
            upDirectioner.transform.Rotate(0,0,90);
            desireBlade.up = upDirectioner.transform.up;
            Destroy(upDirectioner);
        }

        // �� �������:
        //TODO : ����������� desire, ����� �� ���� �� ���� �� ���� ������ �����������.
        //TODO : �������� ������� �� ������ �������� ����, ��������� � ��������� � ����� ��������.
    }

    private void Control_SwingSword() 
    {
        //TODO : ���� "Ƹ�����" �������� ��� swing (���� ��� ���-�� ��� Rigidbody (�����, ��������)) - �������� �����.

        if (Vector3.Distance(bladeHandle.position, desireBlade.position) < close_enough)
            isSwinging = false;

        if (moveProgress < 1)
            moveProgress += actionSpeed * Time.fixedDeltaTime;

        float heightFrom = bladeHandle.position.y;
        float heightTo = desireBlade.position.y;

        Vector3 from = new Vector3(bladeHandle.position.x, 0, bladeHandle.position.z);
        Vector3 to = new Vector3(desireBlade.position.x, 0, desireBlade.position.z);

        bladeHandle.position = Vector3.Slerp(from, to, moveProgress) + new Vector3(0, Mathf.Lerp(heightFrom, heightTo, moveProgress), 0);

        bladeHandle.LookAt((desireBlade.position - bladeHandle.position).normalized, (bladeHandle.position - vital.bounds.center).normalized);    
    }

    private void Control_FixSword()
    {
        // ����������� ��� �����
        Vector3 closestPos = vital.bounds.center;
        if (Vector3.Distance(bladeHandle.position, closestPos) > bladeMaxDistance)
            bladeHandle.position = closestPos + (bladeHandle.position - closestPos).normalized * bladeMaxDistance;

        //� Desire-������� ����
        if (Vector3.Distance(desireBlade.position, closestPos) > bladeMaxDistance)
            desireBlade.position = closestPos + (desireBlade.position - closestPos).normalized * bladeMaxDistance;

        //����������� ������� �����������
        if (Vector3.Distance(bladeHandle.position, closestPos) < bladeMinDistance)
            bladeHandle.position = closestPos + (bladeHandle.position - closestPos).normalized * bladeMinDistance;

        //� Desire-������� ����
        if (Vector3.Distance(desireBlade.position, closestPos) < bladeMinDistance)
            desireBlade.position = closestPos + (desireBlade.position - closestPos).normalized * bladeMinDistance;
    }

    private void SetDesires(Vector3 pos, Vector3 dir, bool nullifyProgress = false) 
    {
        desireBlade.position = pos;
        desireBlade.up = dir;
        if(moveProgress > 1)
            moveProgress = 0;

        if (nullifyProgress)
            moveProgress = 0;
    }

    private void SetDesires(Vector3 pos, Vector3 up, Vector3 forward, bool nullifyProgress = false)
    {
        desireBlade.position = pos;
        desireBlade.LookAt(pos + forward, up);

        if (moveProgress > 1)
            moveProgress = 0;

        if (nullifyProgress)
            moveProgress = 0;
    }
}