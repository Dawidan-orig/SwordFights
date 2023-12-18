using UnityEngine;
using UnityEngine.AI;

public class AI_LongReposition : UtilityAI_BaseState
// �� ��������� � �����-�� ����� � ������� NavMesh, ��� ��� �� ����� ����� ������
{
    private NavMeshPath path;
    public AI_LongReposition(TargetingUtilityAI currentContext, UtilityAI_Factory factory) : base(currentContext, factory)
    {

    }

    public override bool CheckSwitchStates()
    {
        if (_ctx.DecidingStateRequired())
        {
            SwitchStates(_factory.Deciding());
            return true;
        }

        /*if (path.status == NavMeshPathStatus.PathInvalid)
        {
            SwitchStates(_factory.Deciding());
            return true;
        }*/

        if (_ctx.CurrentActivity.actWith is BaseShooting)
        {
            if (((BaseShooting)_ctx.CurrentActivity.actWith).AvilableToShoot(_ctx.CurrentActivity.target, out _))
            {
                SwitchStates(_factory.Attack());
                return true;
            }

            return false;
        }

        if (_ctx.MeleeReachable())
        {
            SwitchStates(_ctx.CurrentActivity.whatDoWhenClose); // ��� ����� - ��������� ��������� ��������
            return true;
        }

        return false;
    }

    public override void EnterState()
    {
        Rigidbody body = _ctx.GetComponent<Rigidbody>();

        if (_ctx.NMAgent)
        {
            body.isKinematic = true;
            _ctx.NMAgent.enabled = true;
        }
        Repath();
    }

    public override void ExitState()
    {
        if (_ctx.NMAgent)
        {
            _ctx.NMAgent.enabled = false;
            _ctx.GetComponent<Rigidbody>().isKinematic = false;
        }
    }

    public override void FixedUpdateState()
    {

    }

    public override void InitializeSubState()
    {

    }

    public override void UpdateState()
    {
        Debug.DrawRay(_ctx.transform.position, Vector3.up * 2, Color.blue);

        if (CheckSwitchStates())
            return;

        if (_ctx.CurrentActivity.target.hasChanged)
        {
            Repath();
        }
    }

    private void Repath()
    {
        //TODO : ���-�� ��� �� ���. Repath �� ������ ���������� ���������...
        Vector3 target = _ctx.CurrentActivity.target.position;

        if (_ctx.CurrentActivity.actWith is BaseShooting)
        {
            target = ((BaseShooting)_ctx.CurrentActivity.actWith).NavMeshClosestAviableToShoot(_ctx.CurrentActivity.target);
        }

        path = new NavMeshPath();
        NavMesh.CalculatePath(_ctx.navMeshCalcFrom.position, target, NavMesh.AllAreas, path);

        //��� ����? ��������� ����� ��-�������:
        if (path.status == NavMeshPathStatus.PathInvalid)
        {
            // ���� ����� ���� ������ � ����
            if (Physics.Raycast(target, Vector3.down, out var hit, 100))
                NavMesh.CalculatePath(_ctx.navMeshCalcFrom.position, hit.point, NavMesh.AllAreas, path);
        }

        //TODO? : ����������� ������ ��� ������������ ���������� � _ctx
        // ����� ����� ����� ����, ��� ������ � ���� ������-�� ������.
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
                _ctx.MovingAgent.MoveIteration(closest.Center());
            }
        }
    }

    public override string ToString()
    {
        return "Moving";
    }
}
