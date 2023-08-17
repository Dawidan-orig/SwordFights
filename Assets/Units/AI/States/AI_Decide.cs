using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

public class AI_Decide : UtilityAI_BaseState
// ���� �� ����� � ������� ��������, ���������� � �����-�� ������� ��� ��� �� �����-�� ���������������� �������� �� �������� ������ -
// �� �������� � ��� ���������.
{
    public AI_Decide(TargetingUtilityAI currentContext, UtilityAI_Factory factory) : base(currentContext, factory)
    {
    }

    public override bool CheckSwitchStates()
    {
        UtilityAI_BaseState newAcitivty = _ctx.SelectBestActivity();

        if(newAcitivty == null)
        {
            // ����� ��� ������.
            return false;
        }

        switch(newAcitivty) 
        {
            case AI_LongReposition:
                SwitchStates(_factory.Reposition());
                return true;
            default:
                
                if (!_ctx.MeleeReachable()) {
                    SwitchStates(_factory.Reposition());
                    return true;
                }
                // ���� �� ������: �������� �� ��������� ���������.
                SwitchStates(newAcitivty);
                return true;
        }
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
        if (!_ctx.AIActive)
            return;

        Debug.DrawRay(_ctx.transform.position, Vector3.up * 2, Color.black);

        CheckSwitchStates();
    }
    public override void FixedUpdateState()
    {

    }

    public override string ToString()
    {
        return "Thinking";
    }
}
