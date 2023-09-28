using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Faction : MonoBehaviour
{
    public enum FType
    {
        sampo,
        enemy,
       
        neutral,
        aggressive // �������� ������ �� ���� ��� �������
    }

    //TODO : ��������� ������� ������ ��������� � ���������.
    // ������ �������: ��������� ��������� ������ ��������� ������ �������� AI. ��� ������� ������ �������� �� ������ ���������.
    // ������� ��� ������� get-set; ���� ������� private

    public FType f_type = FType.neutral;

    public bool IsWillingToAttack(FType type) 
    {
        bool comparedFactions = this.f_type != type; // �� �������, ���� ����� ������ �����-������ �������.

        return (comparedFactions || this.f_type == FType.aggressive) && this.f_type != FType.neutral;
    }
}
