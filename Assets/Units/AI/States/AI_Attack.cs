using UnityEngine;

public class AI_Attack : UtilityAI_BaseState
// �� ��������� � ������� � ���� ���������.
{
    public AI_Attack(TargetingUtilityAI currentContext, UtilityAI_Factory factory) : base(currentContext, factory)
    {
    }

    public override bool CheckSwitchStates()
    {
        if (_ctx.DecidingStateRequired())
        {
            SwitchStates(_factory.Deciding());
            return true;
        }

        Tool weapon = _ctx.CurrentActivity.actWith;

        if(_ctx.MovingAgent)
        _ctx.MovingAgent.MoveIteration(_ctx.transform.position);

        //TODO : ����� �� ������� ��� ��� ���������� �� �����������, �������� ��� � ������ - ����� ������� ������� ����� � _ctx.
        // ��� ����� � �������� ��� �������, � ������� ������ �� ����������������.
        if (weapon is SimplestShooting)
        {
            if (!((SimplestShooting)weapon).AvilableToShoot(_ctx.CurrentActivity.target, out RaycastHit hit))
            {
                SwitchStates(_factory.Deciding());
                return true;
            }

            // ������� �����
            if(_ctx.MovingAgent) 
            {
                //TODO : ����������� ��� � LocalReposition
                float progress = 1 - (Vector3.Distance(_ctx.CurrentActivity.target.position, _ctx.transform.position) / (((SimplestShooting)weapon).range));
                _ctx.MovingAgent.MoveIteration(
                    _ctx.transform.position + _ctx.retreatInfluence.Evaluate(progress)
                    * (_ctx.CurrentActivity.target.position - _ctx.transform.position),
                    _ctx.CurrentActivity.target.position);
            }

            return false;
        }

        if (!_ctx.MeleeReachable())
        {
            SwitchStates(_factory.Deciding());
            return true;
        }

        // ������� �����
        if (_ctx.MovingAgent)
        {
            //TODO : ����������� ��� � LocalReposition
            float progress = 1 - (Vector3.Distance(_ctx.CurrentActivity.target.position, _ctx.transform.position)
                / (_ctx.CurrentActivity.actWith.additionalMeleeReach + _ctx.baseReachDistance));
            _ctx.MovingAgent.MoveIteration(
                _ctx.transform.position + _ctx.retreatInfluence.Evaluate(progress)
                * (_ctx.CurrentActivity.target.position - _ctx.transform.position),
                _ctx.CurrentActivity.target.position);
        }

        return false;
    }

    public override void EnterState()
    {

    }

    public override void ExitState()
    {

    }

    public override void InitializeSubState()
    {

    }

    public override void UpdateState()
    {
        Debug.DrawRay(_ctx.transform.position, Vector3.up * 2, Color.red);

        if (CheckSwitchStates())
            return;

        _ctx.AttackUpdate(_ctx.CurrentActivity.target);
    }
    public override void FixedUpdateState()
    {

    }

    public override string ToString()
    {
        return "Attacking";
    }
}
