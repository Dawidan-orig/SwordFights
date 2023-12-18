using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Blade : MeleeTool
{
    [Header("Init-s")]
    public Transform upperPoint;
    public Transform downerPoint;
    [SerializeField]
    private Transform handle;

    [Header("lookonly")]
    public Rigidbody body;
    public Vector3 AngularVelocityEuler;
    public Faction faction;

    [Header("Constraints")]
    public bool visualPrediction = true;
    public bool alwaysDraw = false;
    public Color predictionColor = Color.red;
    public int iterations = 1;
    public float noDamageTime = 0.5f;

    [Header("Visuals")]
    public ParticleSystem sparkles;

    public Transform Handle { get => handle; private set => handle = value; }

    public event EventHandler<Collision> OnBladeCollision; //���������� ������� �������� � MeleeFighter'a
    public event EventHandler<Collider> OnBladeTrigger;

    public struct border
    {
        public Vector3 posUp;
        public Vector3 posDown;
        public Vector3 direction;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        faction = GetComponent<Faction>();
    }

    private void Start()
    {
        if(host)
            Physics.IgnoreCollision(GetComponent<Collider>(), host.GetComponent<AliveBeing>().vital);

        GameObject massCenterGo = new("MassCenter");
        massCenterGo.transform.parent = transform;        
        body.centerOfMass = handle.localPosition;
        massCenterGo.transform.position = body.worldCenterOfMass;

        additionalMeleeReach = Vector3.Distance(upperPoint.position, handle.position)/2;
    }

    private void Update()
    {
        if (faction)
        {
            if (host)
                faction.f_type = host.GetComponent<Faction>().f_type;
            else
                faction.f_type = Faction.FType.aggressive;
        }
    }

    //TODO : Refactor, ������ �� ������.
    public List<border> FixedPredict(int prediction)
    {
        List<border> res = new List<border>();

        border start = new();

        Quaternion rotationIteration = Quaternion.Euler(AngularVelocityEuler * Time.fixedDeltaTime);

        Vector3 rotatedPosUp = upperPoint.position - transform.position;
        rotatedPosUp = rotationIteration * rotatedPosUp;
        start.posUp = transform.position + rotatedPosUp + (body.velocity * Time.fixedDeltaTime);

        Vector3 rotatedPosDown = downerPoint.position - transform.position;
        rotatedPosDown = rotationIteration * rotatedPosDown;
        start.posDown = transform.position + rotatedPosDown + (body.velocity * Time.fixedDeltaTime);

        start.direction = body.velocity.normalized;

        res.Add(start);
        // ������ ������������ - ������ ������.
        CollisionPredictionControl(start);

        for (int i = 0; i < prediction; i++)
        {
            border border = new();

            int offset_i = i + 1; // ��� ����� ��-�� ����, ��� ������� ��������� ���������, � ��� ����� - ����������. ��-�� ����� ��� ������ �� ���� ��������

            rotatedPosUp = upperPoint.position - transform.position;
            rotatedPosDown = downerPoint.position - transform.position;
            for (int j = 0; j < offset_i; j++)
            {
                rotatedPosUp = rotationIteration * rotatedPosUp;
                rotatedPosDown = rotationIteration * rotatedPosDown;
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


                Debug.DrawLine(border.posDown, border.posUp, predictionColor);


                Vector3 center = Vector3.Lerp(border.posDown, border.posUp, 0.5f);
                Debug.DrawLine(center, center + border.direction * 0.1f, new Color(1, 0.4f, 0.4f));

                prevous = border;
            }

        return res;
    }

    public void CollisionPredictionControl(border border)
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
        AngularVelocityEuler = body.angularVelocity * 360 / (2 * Mathf.PI);

        if (alwaysDraw)
            FixedPredict(iterations);
    }


    private void OnCollisionEnter(Collision collision)
    {
        OnBladeCollision?.Invoke(this, collision);

        if(collision.collider.transform.TryGetComponent<AliveBeing>(out var alive)) 
        {
            Utilities.DrawSphere(collision.GetContact(0).point, color: Color.red, duration: 3);
            alive.Damage(body.velocity.magnitude * body.mass * damageMultiplier, IDamagable.DamageType.sharp);
            GetComponent<Collider>().isTrigger = true;
            Invoke(nameof(ResetCollision), noDamageTime);
        }
        else if(collision.collider.transform.TryGetComponent<Blade>(out _)) 
        {
            GetComponent<Collider>().isTrigger = true;
            Invoke(nameof(ResetCollision), noDamageTime);

            Vector3 sparklesSpread = collision.GetContact(0).point;
            transform.position = sparklesSpread;
            sparkles.Emit((int)sparkles.emission.GetBurst(0).count.constant);
        }
    }

    private void ResetCollision() 
    {
        GetComponent<Collider>().isTrigger = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        OnBladeTrigger?.Invoke(this, other);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(downerPoint.position, upperPoint.position);
    }
}
