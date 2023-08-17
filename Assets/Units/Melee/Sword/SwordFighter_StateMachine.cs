using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AttackCatcher))]
public class SwordFighter_StateMachine : MeleeFighter
{
    [Header("constraints")]
    public float actionSpeed = 1; // �������� �������� ���� � ����
    public float block_minDistance = 0.3f; // ����������� ���������� ��� �����, ������������ ��� ���� � �����������, � �� ���������.
    public float swing_EndDistanceMultiplier = 1.5f; // ��������� ������ ������ ��������� ��� ����� ���������.
    public float swing_startDistance = 1.5f; // ��������� ������ ������ ��������� ��� �� �����.
    public float criticalImpulse = 400; // ����� ����������, ��� ������ ������ � ��������� ������ �����!
    public float toBladeHandle_MaxDistance = 2; // ������������ ���������� �� vital �� ������� ����. �� ����, ����� ����.
    public float toBladeHandle_MinDistance = 0.1f; // ����������� ���������� �� vital.
    public float blockVelocity = 5;
    public float close_enough = 0.1f; // ���������� �� ����, ��� ������� ����� ������ ���������.
    public float angle_enough = 10; // ����������� ����, ����� ������� ��� handle ������ � desire

    [Header("timers")]
    public float toInitialAwait = 2; // ������� ������� ������� �� ��������� ���� � ������� �������?

    [Header("init-s")]
    [SerializeField]
    private Blade _blade;
    [SerializeField]
    private Transform _bladeContainer;
    [SerializeField]
    private Transform _bladeHandle;
    [SerializeField]
    private Collider _vital;

    [Header("lookonly")]
    [SerializeField]
    Transform _initialBlade;
    [SerializeField]
    Transform _moveFrom;
    [SerializeField]
    Transform _desireBlade;
    [SerializeField]
    float _moveProgress;
    [SerializeField]
    float _currentToInitialAwait;
    [SerializeField]
    AttackCatcher _catcher;
    [SerializeField]
    bool _attackReposition = false; //TODO : �������, �������� �� ������� �����.
    [SerializeField]
    Stack<ActionJoint> _combo = new Stack<ActionJoint>(); //TODO : �������� � ���� ����� �������, ������������ ����� ���������, � ��� �� ��������� ��� ��������� ��������.
    // ��� ����� �������� ��������� ����� ������ �����.

    enum ActionType
    {
        Swing,
        Reposition
    }

    public struct ActionJoint
    {
        public Transform currentDesire;
        public Transform nextDesire;
        ActionType nextActionType;
    }

    SwordFighter_BaseState _currentSwordState;
    SwordFighter_StateFactory _fighter_states;

    //Getters and setters
    public SwordFighter_BaseState CurrentSwordState { get { return _currentSwordState; } set { _currentSwordState = value; } }
    public Transform BladeHandle { get { return _bladeHandle; } }
    public Transform DesireBlade { get { return _desireBlade; } }
    public Transform MoveFrom { get { return _moveFrom; } }
    public float MoveProgress { get { return _moveProgress; } }
    public Blade Blade { get { return _blade; } }
    public Transform InitialBlade { get => _initialBlade; set => _initialBlade = value; }
    public float CurrentToInitialAwait { get => _currentToInitialAwait; set => _currentToInitialAwait = value; }
    public Collider Vital { get => _vital; set => _vital = value; }
    public AttackCatcher AttackCatcher { get => _catcher; set => _catcher = value; }
    public bool AttackReposition { get => _attackReposition; set => _attackReposition = value; } //TODO : �������� �� ��������� ���������� ������!

    public EventHandler<IncomingReposEventArgs> OnRepositionIncoming;
    public EventHandler<IncomingSwingEventArgs> OnSwingIncoming;

    public class IncomingReposEventArgs : EventArgs
    {
        public Vector3 bladeDown;
        public Vector3 bladeUp;
        public Vector3 bladeDir;
    }

    public class IncomingSwingEventArgs : EventArgs
    {
        public Vector3 toPoint;
    }


    [Header("Debug")]
    [SerializeField]
    private bool isSwordFixing = true;
    [SerializeField]
    private string currentState;

    protected override void Awake()
    {
        base.Awake();

        _catcher = gameObject.GetComponent<AttackCatcher>();
        _catcher.ignored.Add(_blade.body);

        _currentToInitialAwait = toInitialAwait;

        _fighter_states = new SwordFighter_StateFactory(this);
        _currentSwordState = _fighter_states.Idle();
        _currentSwordState.EnterState();

        _blade.GetComponent<Tool>().SetHost(transform);

        if (_bladeContainer == null)
            _bladeContainer = transform;

        GameObject desireGO = new("DesireBlade");
        _desireBlade = desireGO.transform;
        _desireBlade.parent = _bladeContainer;
        _desireBlade.gameObject.SetActive(true);
        _desireBlade.position = BladeHandle.position;
        _desireBlade.rotation = BladeHandle.rotation;

        GameObject initialBladeGO = new("InititalBladePosition");
        _initialBlade = initialBladeGO.transform;
        _initialBlade.position = BladeHandle.position;
        _initialBlade.rotation = BladeHandle.rotation;
        _initialBlade.parent = _bladeContainer;

        SetDesires(_initialBlade.position, _initialBlade.up, _initialBlade.forward);
        NullifyProgress();
        _moveProgress = 1;
    }

    protected override void Start()
    {
        base.Start();

        AttackCatcher.OnIncomingAttack += Incoming;
    }

    protected override void Update()
    {
        base.Update();
        _currentSwordState.UpdateState();

        currentState = _currentSwordState.ToString();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        _currentSwordState.FixedUpdateState();

        if (_moveProgress < 1)
            _moveProgress += actionSpeed * Time.fixedDeltaTime / Vector3.Distance(_moveFrom.position, _desireBlade.position);
    }

    private void Incoming(object sender, AttackCatcher.AttackEventArgs e)
    {
        Rigidbody currentIncoming = e.body;
        CurrentToInitialAwait = 0;

        if (e.free && e.body.velocity.magnitude < blockVelocity)
        {
            if (e.impulse < criticalImpulse)
            {
                //IDEA: ������� ������� �����: � ������� Curve.

                Vector3 toPoint = e.start;

                Vector3 bladeCenter = Vector3.Lerp(Blade.upperPoint.position, Blade.downerPoint.position, 0.5f);
                float bladeCenterLen = Vector3.Distance(bladeCenter, Blade.downerPoint.position);
                float swingDistance = bladeCenterLen + toBladeHandle_MaxDistance;

                if (Vector3.Distance(distanceFrom.position, toPoint) < swingDistance)
                {
                    OnSwingIncoming?.Invoke(this, new IncomingSwingEventArgs { toPoint = toPoint });
                }
            }
        }
        else if (e.free && e.body.velocity.magnitude >= blockVelocity)
        {
            Vector3 blockPoint = Vector3.Lerp(e.start, e.end, 0.5f);

            GameObject bladePrediction = new("NotDeletedPrediction");
            bladePrediction.transform.position = _blade.transform.position;

            GameObject start = new();
            start.transform.position = _blade.downerPoint.position;
            start.transform.parent = bladePrediction.transform;

            GameObject end = new();
            end.transform.position = _blade.upperPoint.position;
            end.transform.parent = bladePrediction.transform;

            bladePrediction.transform.position = blockPoint;

            Vector3 toEnemyBlade_Dir = (bladePrediction.transform.position - Vital.bounds.center).normalized;

            bladePrediction.transform.LookAt(start.transform.position +
                Vector3.ProjectOnPlane((end.transform.position - start.transform.position).normalized, e.body.velocity), start.transform.position + Vector3.up);

            //Vector3 closest = _vital.ClosestPointOnBounds(bladePrediction.transform.position);
            //bladePrediction.transform.position = closest
            //    + (bladePrediction.transform.position - closest).normalized * block_minDistance;

            Vector3 bladeDown = start.transform.position;
            Vector3 bladeUp = end.transform.position;

            Destroy(bladePrediction);

            int ignored = Blade.gameObject.layer; // ��� ������ ������ ��� ��������.
            ignored = ~ignored;

            BoxCollider bladeCollider = Blade.GetComponent<BoxCollider>();
            Vector3 bladeHalfWidthLength = new Vector3((bladeCollider.size.x * bladeCollider.transform.lossyScale.x) / 2, 0.1f, (bladeCollider.size.z * bladeCollider.transform.lossyScale.z) / 2);

            //IDEA : ����������, ������� ������� �����.
            // ������ ����� ����� ������������ ������������ ��-�� ��������. ���� �������������� �������: ��������� ��� �������� ��������� ����� �� ���� �� ���������� �����,
            // ��� ��� ��������� ��� ��� - � ��� ����� ������������.

            OnRepositionIncoming?.Invoke(this, new IncomingReposEventArgs { bladeDown = bladeDown, bladeUp = bladeUp, bladeDir = toEnemyBlade_Dir });
        }
        else
        {
            Vector3 blockPoint = Vector3.Lerp(e.start, e.end, 0.5f);

            GameObject bladePrediction = new("NotDeletedPrediction");
            bladePrediction.transform.position = blockPoint;

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

            Vector3 toEnemyBlade_Dir = (bladePrediction.transform.position - Vital.bounds.center).normalized;
            bladePrediction.transform.Rotate(toEnemyBlade_Dir, 90); // ������ ���������������

            // ����������� ��� ����������� ������ � ����.
            if (e.body.GetComponent<Tool>().host != null)
            {
                bladePrediction.transform.position = distanceFrom.position
                    + (bladePrediction.transform.position - distanceFrom.position).normalized * block_minDistance;
            }

            Vector3 bladeDown = start.transform.position;
            Vector3 bladeUp = end.transform.position;

            Destroy(bladePrediction);

            int ignored = Blade.gameObject.layer; // ��� ������ ������ ��� ��������.
            ignored = ~ignored;

            BoxCollider bladeCollider = Blade.GetComponent<BoxCollider>();
            Vector3 bladeHalfWidthLength = new Vector3((bladeCollider.size.x * bladeCollider.transform.lossyScale.x) / 2, 0.1f, (bladeCollider.size.z * bladeCollider.transform.lossyScale.z) / 2);

            /*
            if (Utilities.VisualisedBoxCast(bladeDown,
                bladeHalfWidthLength,
                (bladeUp - bladeDown).normalized,
                out _,
                Quaternion.FromToRotation(Vector3.up, (bladeUp - bladeDown).normalized),
                (bladeDown - bladeUp).magnitude,
                ignored,
                true,
                new Color(0.5f, 0.5f, 1f, 0.6f))
                ||
                Utilities.VisualisedBoxCast(bladeUp,
                bladeHalfWidthLength,
                (bladeDown - bladeUp).normalized,
                out _,
                Quaternion.FromToRotation(Vector3.up, (bladeDown - bladeUp).normalized),
                (bladeDown - bladeUp).magnitude,
                ignored,
                true,
                new Color(0.5f, 0.5f, 1f, 0.6f)))
            {
                return;
            }*/

            //IDEA : ����������, ������� ������� �����.
            // ������ ����� ����� ������������ ������������ ��-�� ��������. ���� �������������� �������: ��������� ��� �������� ��������� ����� �� ���� �� ���������� �����,
            // ��� ��� ��������� ��� ��� - � ��� ����� ������������.

            OnRepositionIncoming?.Invoke(this, new IncomingReposEventArgs { bladeDown = bladeDown, bladeUp = bladeUp, bladeDir = toEnemyBlade_Dir });
        }
    }

    // ��������� ���� �� ���� ��������� ����������
    public override void Block(Vector3 start, Vector3 end, Vector3 SlashingDir)
    {
        base.Block(start, end, SlashingDir);

        if(Vector3.Distance(distanceFrom.position, start) > Vector3.Distance(distanceFrom.position, end))        
            (end, start) = (start, end);
        

        SetDesires(start, (end - start).normalized, SlashingDir);
    }

    // ����� ������� �� �����-�� ����� �� ������� �������.
    public override void Swing(Vector3 toPoint)
    {
        base.Swing(toPoint);

        Vector3 moveTo = toPoint + (toPoint - BladeHandle.position).normalized * swing_EndDistanceMultiplier;

        Vector3 pointDir = (moveTo - _vital.bounds.center).normalized;

        Vector3 closest = _vital.ClosestPointOnBounds(moveTo);
        float distance = (toPoint - closest).magnitude;
        moveTo = closest + (moveTo - closest).normalized * distance;
        SetDesires(moveTo, pointDir, (moveTo - toPoint).normalized);
    }

    private void FixDesire()
    {
        if (!isSwordFixing)
            return;

        Vector3 countFrom = distanceFrom.position;
        Vector3 closest = _vital.ClosestPointOnBounds(_desireBlade.position);
        if (Vector3.Distance(_desireBlade.position, countFrom) > toBladeHandle_MaxDistance)
        {
            Vector3 toCloseDir = (closest - _desireBlade.position).normalized;
            Vector3 exceededHand = _desireBlade.position - countFrom;
            float toCloseLen = -1;

            // ������� ��������� + ������� ����������� ���������
            float angle = Vector3.Angle(toCloseDir, -exceededHand);

            Debug.DrawRay(_desireBlade.position, toCloseDir) ;
            Debug.DrawRay(_desireBlade.position, -exceededHand);

            float b = exceededHand.magnitude * Mathf.Cos(angle);
            float diskr = 4 *
                (Mathf.Pow(toBladeHandle_MaxDistance,2) -
                Mathf.Pow(exceededHand.magnitude, 2) *
                Mathf.Pow(Mathf.Sin(angle * Mathf.Deg2Rad), 2));
            float s1 = b + Mathf.Sqrt(diskr);
            float s2 = b - Mathf.Sqrt(diskr);
            toCloseLen = (s1 > s2 ? s1 : s2);

            Debug.Log(diskr);
            if (diskr > 0)
            {
                Debug.DrawLine(countFrom, _desireBlade.position + toCloseDir * toCloseLen, Color.black);

                _desireBlade.position += toCloseDir * toCloseLen;
            }
            else 
            {
                // ��������, ��� ������� ���. � ��� ��� �� ��� �������, ��� ����� ����� ����� ��� � �������� ������������ ����,
                // � ������ ��� ������ ������� ��� �����.
            }
        }

        if (Vector3.Distance(_desireBlade.position, countFrom) < toBladeHandle_MinDistance)
        {
            /*
            Vector3 fromCloseDir = (_desireBlade.position - closest).normalized;
            Vector3 exceededHand = _desireBlade.position - countFrom;
            // ������� ���������
            float a = 1;
            float b = -2 * exceededHand.magnitude * Mathf.Cos(Vector3.Angle(fromCloseDir, -exceededHand));
            float c = Mathf.Pow(exceededHand.magnitude, 2) - Mathf.Pow(toBladeHandle_MaxDistance, 2);
            float diskr = Mathf.Pow(b, 2) - 4 * a * c;
            float s1 = (-b - Mathf.Sqrt(diskr)) / (2 * a);
            float s2 = (-b + Mathf.Sqrt(diskr)) / (2 * a);
            float fromCloseLen = (s1 > s2 ? s1 : s2);

            _desireBlade.position += fromCloseDir * fromCloseLen;
            */
        }
    }

    public void SetDesires(Vector3 pos, Vector3 up, Vector3 forward)
    {
        _desireBlade.position = pos;
        _desireBlade.LookAt(pos + up, pos + forward);
        _desireBlade.RotateAround(_desireBlade.position, _desireBlade.right, 90);

        FixDesire();

        if (MoveProgress >= 1)
        {
            NullifyProgress();
        }
    }
    public bool CloseToDesire()
    {
        return Vector3.Distance(_bladeHandle.position, _desireBlade.position) < close_enough;
    }

    public bool AlmostDesire()
    {
        return Vector3.Distance(_bladeHandle.position, _desireBlade.position) < close_enough
            && Quaternion.Angle(_bladeHandle.rotation, _desireBlade.rotation) < angle_enough;
    }

    public void NullifyProgress()
    {
        if (_moveFrom != null)
            Destroy(_moveFrom.gameObject);
        GameObject moveFromGO = new("BladeMoveStart");
        _moveFrom = moveFromGO.transform;
        _moveFrom.position = BladeHandle.position;
        _moveFrom.rotation = BladeHandle.rotation;
        _moveFrom.parent = _bladeContainer;
        _moveProgress = 0;
    }

    protected override void DistributeActivityFromManager(object sender, UtilityAI_Manager.UAIData e)
    {
        _currentActivity = _noAction;
        _possibleActions.Clear();

        var activities = e.interactables;
        foreach (KeyValuePair<GameObject, int> activity in activities)
        {
            GameObject target = activity.Key;
            int weight = activity.Value;

            // ����� ������ �� ����� ���� ��������� �� ����� � �����������
            if (target.TryGetComponent<Interactable_UtilityAI>(out _))
            {
                AddNewPossibleAction(target.transform, weight, target.transform.name, _blade, _factory.Attack());
            }
        }
    }

    public override Transform GetRightHandTarget()
    {
        return _bladeHandle;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (_desireBlade != null)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawLine(_desireBlade.position, _moveFrom.position);
            Gizmos.color = Color.gray;
            Gizmos.DrawRay(_desireBlade.position, _desireBlade.up);
            Gizmos.DrawRay(_moveFrom.position, _moveFrom.up);
        }
    }
}