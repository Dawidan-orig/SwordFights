using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace Sampo.AI
{
    public abstract class UtilityAI_BaseState
    {
        protected TargetingUtilityAI _ctx { get; private set; }
        protected UtilityAI_Factory _factory { get; private set; }
        protected UtilityAI_BaseState _currentSubState;
        protected UtilityAI_BaseState _currentSuperState;

        protected NavMeshPath path;
        protected Vector3 moveTargetPos { get; private set; }
        private Vector3 repathLastTargetPos;
        private const float RECALC_DIFF = 3;

        public UtilityAI_BaseState(TargetingUtilityAI currentContext, UtilityAI_Factory factory)
        {
            _ctx = currentContext;
            _factory = factory;
        }

        public virtual void EnterState() 
        {
            CheckRepath();
        }

        public abstract void UpdateState();

        public abstract void FixedUpdateState();

        public abstract void ExitState();

        public abstract bool CheckSwitchStates();

        public abstract void InitializeSubState();


        protected void SwitchStates(UtilityAI_BaseState newState)
        {
            ExitState();
            newState.EnterState();
            _ctx.CurrentState = newState;
        }

        protected void SetSuperState(UtilityAI_BaseState newSuperState)
        {
            _currentSuperState = newSuperState;
        }

        protected void SetSubState(UtilityAI_BaseState newSubState)
        {
            _currentSubState = newSubState;
            newSubState.SetSuperState(this);
        }

        public void ForceDecideState()
        {
            SwitchStates(_factory.Deciding());
        }

        /// <summary>
        /// ���������, ���� �� ����� ������������� ���� � ����
        /// </summary>
        protected void CheckRepath() 
        {
            if (!Utilities.ValueInArea(repathLastTargetPos, _ctx.CurrentActivity.target.position, RECALC_DIFF))
            {
                Repath();
            }
        }
        /// <summary>
        /// ������������� ���� � ����
        /// </summary>
        private void Repath()
        {
            moveTargetPos = _ctx.CurrentActivity.target.position;
            repathLastTargetPos = moveTargetPos;

            if (_ctx.CurrentActivity.actWith is BaseShooting shooting) // ���� ������ ������� ��� ��������
            {
                moveTargetPos = shooting.NavMeshClosestAviableToShoot(_ctx.CurrentActivity.target);
            }

            path = new NavMeshPath();
            NavMesh.CalculatePath(_ctx.navMeshCalcFrom.position, moveTargetPos, NavMesh.AllAreas, path);

            if (_ctx.NMAgent)
            {
                _ctx.NMAgent.SetPath(path);
            }
            else if (_ctx.MovingAgent)
            {
                if (path.status != NavMeshPathStatus.PathInvalid && path.corners.Length > 1)
                {
                    _ctx.MovingAgent.MoveIteration(path.corners[1]);
                }
                else
                {
                    var closest = NavMeshCalculations.Instance.GetCell(_ctx.navMeshCalcFrom.position);
                    moveTargetPos = closest.Center();
                    _ctx.MovingAgent.MoveIteration(moveTargetPos);
                }
            }
        }
    }
}