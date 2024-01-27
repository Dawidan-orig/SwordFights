using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sampo.AI
{
    [SelectionBase]
    [RequireComponent(typeof(IMovingAgent))]
    [RequireComponent(typeof(Faction))]
    public abstract class TargetingUtilityAI : MonoBehaviour, IAnimationProvider, IPointsDistribution
    // ��, �������� ���������� ���������� ��������
    // ���������� StateMachine � �������� �����������
    {
        public bool _AIActive = true;

        [Header("Setup")]
        [Tooltip("������ �� 0 �� 1, ������������ ��," +
            "� ����� �������������� � ����������� �� ��������� ������ �� ����� ���������� �� ����")]
        public AnimationCurve retreatInfluence;
        [Tooltip("������� ��������� �� ����� ����� ��")]
        public float distanceWeightMultiplier = 1;

        [Header("Ground for animation and movement")]
        public Collider vital;
        public float toGroundDist = 0.3f;
        [Tooltip("����� ������� ��� NavMeshAgent")]
        public Transform navMeshCalcFrom;

        [Header("lookonly")]
        [SerializeField]
        protected AIAction _currentActivity;
        [SerializeField]
        protected List<AIAction> _possibleActions = new();
        [SerializeField]
        [Tooltip(@"��� ���� ������� ���������, �� ������� �� ����������, ��������� ���� ��������� ������.
            ����� ������ ���������� ������������.
            ���������� ��������������� ��� ����������� �������������, �� ����� �������� � ���� ����.")]
        protected int visiblePowerPoints = 100;

        IMovingAgent _movingAgent;
        Rigidbody _body;
        private AIAction _noAction;
        protected UtilityAI_Factory _factory;
        protected UtilityAI_BaseState _currentState;

        public UtilityAI_BaseState CurrentState { get => _currentState; set => _currentState = value; }
        public AIAction CurrentActivity { get => _currentActivity; }        
        public Rigidbody Body { get => _body; set => _body = value; }
        public IMovingAgent MovingAgent { get => _movingAgent; set => _movingAgent = value; }

        [Serializable]
        public class AIAction
        {
            public Transform target;
            public string name;
            private int totalWeight;
            public int baseWeight;
            public int distanceSubstraction;
            public int enemiesAmountSubstraction;
            public Tool actWith;
            public UtilityAI_BaseState whatDoWhenClose;

            public int TotalWeight { get => totalWeight; set => totalWeight = value; }

            public AIAction()
            {
                target = default;
                name = default;
                totalWeight = default;
                baseWeight = default;
                distanceSubstraction = default;
                actWith = default;
                whatDoWhenClose = default;
            }

            public AIAction(Transform target, string name, int weight, Tool actWith, UtilityAI_BaseState alignedState)
            {
                this.target = target;
                this.name = name;
                this.baseWeight = weight;
                this.actWith = actWith;
                whatDoWhenClose = alignedState;
                totalWeight = weight;
                distanceSubstraction = 0;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is AIAction))
                    return false;

                AIAction casted = (AIAction)obj;

                return (target == casted.target) && (whatDoWhenClose == casted.whatDoWhenClose) && (actWith == casted.actWith);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(target, name, baseWeight, whatDoWhenClose);
            }

            public static bool operator ==(AIAction c1, AIAction c2)
            {
                if (c1 is null && c2 is null)
                    return true;
                else if (c1 is null && c2 is not null || c1 is not null && c2 is null)
                    return false;

                return (c1.target == c2.target) && (c1.whatDoWhenClose == c2.whatDoWhenClose) && (c1.actWith == c2.actWith);
            }
            public static bool operator !=(AIAction c1, AIAction c2)
            {
                if (c1 is null && c2 is null)
                    return true;
                else if (c1 is null && c2 is not null || c1 is not null && c2 is null)
                    return false;

                return (c1.target != c2.target) || (c1.name != c2.name) || (c1.actWith != c2.actWith);
            }
        }

        #region Unity

        protected virtual void Awake() 
        {
            _movingAgent = GetComponent<IMovingAgent>();
            _body = GetComponent<Rigidbody>();
            _factory = new UtilityAI_Factory(this);
            _currentState = _factory.Deciding();
            navMeshCalcFrom = navMeshCalcFrom == null ? transform : navMeshCalcFrom;
        }

        protected virtual void OnEnable()
        {
            _AIActive = true;
        }

        protected virtual void Start()
        {            
            UtilityAI_Manager.Instance.NewAdded += FetchNewActivityFromManager;
            UtilityAI_Manager.Instance.NewRemoved += RemoveActivityFromManager;

            _noAction = new AIAction();

            NullifyActivity();

            FetchAndAddAllActivities();
        }

        protected virtual void Update()
        {
            if (!_AIActive)
            {
                return;
            }

            _currentState.UpdateState();
        }

        protected virtual void FixedUpdate()
        {
            if (!_AIActive)
            {
                return;
            }

            _currentState.FixedUpdateState();
        }

        protected virtual void OnDisable()
        {
            UtilityAI_Manager.Instance.NewAdded -= FetchNewActivityFromManager;
            UtilityAI_Manager.Instance.NewRemoved -= RemoveActivityFromManager;
            if (_currentActivity.target)
                UtilityAI_Manager.Instance.ChangeCongestion(_currentActivity.target.GetComponent<Interactable_UtilityAI>(), -visiblePowerPoints);
            NullifyActivity();
            _AIActive = false;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                NormilizeActions();
                foreach (AIAction action in _possibleActions)
                {
                    Utilities.CreateTextInWorld(action.baseWeight.ToString(), action.target, position: action.target.position + Vector3.up * 2);
                    Utilities.CreateTextInWorld(action.distanceSubstraction.ToString(), action.target, position: action.target.position + Vector3.up * 2.5f, color: Color.blue);
                    Utilities.CreateTextInWorld(action.enemiesAmountSubstraction.ToString(), action.target, position: action.target.position + Vector3.up * 3f, color: Color.yellow);
                }
            }
        }

        #endregion

        #region actions
        private void FetchAndAddAllActivities() 
        {
            var dict = UtilityAI_Manager.Instance.GetAllInteractions(GetComponent<Faction>().FactionType);
            foreach(var kvp in dict) 
            {
                Interactable_UtilityAI target = kvp.Key;
                int weight = kvp.Value;

                if (!IsEnemyPassing(target.transform))
                    return;

                Tool toolUsed = ToolChosingCheck(target.transform);

                AddNewPossibleAction(target.transform, weight, target.transform.name, toolUsed, _factory.Attack());
            }
        }
        private void FetchNewActivityFromManager(object sender, UtilityAI_Manager.UAIData e)
        {
            Faction faction = GetComponent<Faction>();
            if (!faction.IsWillingToAttack(e.factionWhereChangeHappened))
                return;

            Interactable_UtilityAI target = e.newInteractable.Key;
            int weight = e.newInteractable.Value;

            if (!IsEnemyPassing(target.transform))
                return;

            Tool toolUsed = ToolChosingCheck(target.transform);

            //TODO DESIGN : _factory.Attack() - �� ����������� ��, ���� �������� �� ������� ������
            AIAction action = new AIAction(target.transform, name, weight, toolUsed, _factory.Attack());
            NormilizeAction(action);
            AddNewPossibleAction(action);
            if (action.TotalWeight > CurrentActivity.TotalWeight)
                ChangeAction(action);
        }
        private void RemoveActivityFromManager(object sender, UtilityAI_Manager.UAIData e) 
        {
            AIAction similar = _possibleActions.Find(item => item.target == e.newInteractable.Key.transform);

            if (_possibleActions.Contains(similar)) 
            {
                _possibleActions.Remove(similar);

                if (CurrentActivity == similar)
                    NullifyActivity();
            }
        }

        private void AddNewPossibleAction(AIAction action)
        {
            if (_possibleActions.Contains(action))
            {
                Debug.LogWarning("��� ��� �������� " + name, transform);
                return;
            }

            _possibleActions.Add(action);
        }
        private void AddNewPossibleAction(Transform target, int weight, string name, Tool actWith, UtilityAI_BaseState treatment)
        {
            AIAction action = new AIAction(target, name, weight, actWith, treatment);

            AddNewPossibleAction(action);

            _possibleActions.Add(action);
        }
        private void ChangeAction(AIAction to)
        {
            if (!IsNoActionCurrently() && _currentActivity.target) //������� ������� ������� ����
                UtilityAI_Manager.Instance.ChangeCongestion(_currentActivity.target.GetComponent<Interactable_UtilityAI>(), -visiblePowerPoints);
            _currentActivity = to;
            UtilityAI_Manager.Instance.ChangeCongestion(_currentActivity.target.GetComponent<Interactable_UtilityAI>(), visiblePowerPoints);
        }
        public UtilityAI_BaseState SelectBestActivity()
        {          
            if (_possibleActions.Count == 0)            
                return null;            

            NormilizeActions();

            if (_possibleActions.Count == 0)
                return null;

            int bestActivityIndex = 0;

            _possibleActions.Sort((i1, i2) => i2.TotalWeight.CompareTo(i1.TotalWeight));

            ChangeAction(_possibleActions[bestActivityIndex]);

            return _currentActivity.whatDoWhenClose;
        }
        private void NormilizeActions()
        {
            for (int i = 0; i < _possibleActions.Count; i++)
            {
                AIAction action = _possibleActions[i];
                if (action.target != null)
                {
                    NormilizeAction(action);
                }
                else 
                {
                    _possibleActions.RemoveAt(i);
                    i--;
                }
            }
        }
        private void NormilizeAction(AIAction action)
        {
            action.distanceSubstraction =
                        Mathf.RoundToInt(Vector3.Distance(transform.position, action.target.position) * distanceWeightMultiplier);
            action.enemiesAmountSubstraction =
                UtilityAI_Manager.Instance.GetCongestion(action.target.GetComponent<Interactable_UtilityAI>());

            action.TotalWeight = action.baseWeight
            - action.distanceSubstraction
            - action.enemiesAmountSubstraction;
        }
        private void NullifyActivity()
        {
            _currentActivity = _noAction; // �����, ����� StateMachine ������������� � ��������� Decide � �� ������ nullReference
        }
        public bool IsNoActionCurrently() => _currentActivity == _noAction;
        #endregion

        public bool DecidingStateRequired()
        {
            return _currentActivity == _noAction;
        }

        /// <summary>
        /// ����������� ������� ����������, ��������� ��������� ����� ��� ������� ��������
        /// </summary>
        /// <param name="ofTarget">�������� ����, ��� ������� ����� ����� ��������� �����</param>
        /// <param name="CalculateFrom">�� ���� ������� ��������� ��������� �������</param>
        /// <returns>��������� �����</returns>
        public Vector3 GetClosestPoint(Transform ofTarget, Vector3 CalculateFrom) 
        {
            Vector3 closestPointToTarget;
            if (ofTarget.TryGetComponent<IDamagable>(out var ab))
                closestPointToTarget = ab.Vital.ClosestPointOnBounds(CalculateFrom);
            else if (ofTarget.TryGetComponent<Collider>(out var c))
                closestPointToTarget = c.ClosestPointOnBounds(CalculateFrom);
            else
                closestPointToTarget = ofTarget.position;

            //closestPointToTarget = ofTarget.position;

            return closestPointToTarget;
        }

        #region virtual functions
        /// <summary>
        /// ������������ ����� � ��������� ����������
        /// </summary>
        /// <param name="points">������������� ����</param>
        public virtual void AssignPoints(int points)
        {
            int remaining = points;
            visiblePowerPoints = points;

            //TODO DESIGN : ����������� ��������� �������� ��������
        }
        /// <summary>
        /// �������� ������� �� ����
        /// </summary>
        /// <param name="target">������������ ���� ����</param>
        /// <returns>true, ���� ���� ��������</returns>
        protected virtual bool IsEnemyPassing(Transform target)
        {
            bool res = true;

            Faction other = target.GetComponent<Faction>();

            if (!other.IsWillingToAttack(GetComponent<Faction>().FactionType) || target == transform)
                res = false;

            if (other.TryGetComponent(out AliveBeing b))
                if (b.mainBody == transform)
                    res = false;

            return res;
        }
        /// <summary>
        /// ����� ������
        /// </summary>
        /// <param name="target">������������ ���� ����</param>
        /// <returns></returns>
        protected abstract Tool ToolChosingCheck(Transform target);

        /*TODO dep AI_Factory : ������� ���, ����� ����������� StateMachine ���� ������������� ����, ���� ������ ������������...
        * ��� ������� �� ������ ���� ��������, ��� - ������ ��� ����������� StateMachine!
        * ������� ����� Event'� ����� ��������� factory.
        */
        /// <summary>
        /// ������� Update, �� ���������� � ���������, ����� ���� �������.
        /// </summary>
        /// <param name="target"></param>
        public abstract void AttackUpdate(Transform target);


        /// <summary>
        /// ������� Update, �� ����� ���� ���������
        /// </summary>
        /// <param name="target"></param>
        public abstract void ActionUpdate(Transform target);
        #endregion

        #region animation
        public Vector3 GetLookTarget()
        {
            return (_currentActivity.target ? _currentActivity.target.position : Vector3.zero);
        }

        public bool IsGrounded()
        {
            return Physics.BoxCast(vital.bounds.center, new Vector3(vital.bounds.size.x / 2, 0.1f, vital.bounds.size.z / 2),
                transform.up * -1, out _, transform.rotation, vital.bounds.size.y / 2 + toGroundDist);
        }

        public bool IsInJump()
        {
            //TODO DESIGN : �� ������� �� ������ � ������. ���, ������-��, ���� �� ���������.
            return false;
        }
        /// <summary>
        /// ����� ��� �������� ����
        /// </summary>
        /// <returns>�����, ���� ����� ���������� ����</returns>
        public virtual Transform GetRightHandTarget() {  return null; }
        #endregion

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.black;
            NavMeshCalculations.Cell cell = NavMeshCalculations.Instance.GetCell(transform.position);
            if (cell == null)
                return;

            cell.DrawGizmo();
            Gizmos.DrawLine(cell.Center(), transform.position);
        }
    }
}