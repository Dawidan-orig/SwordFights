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

    public FType type = FType.neutral;

    public bool IsWillingToAttack(FType type) 
    {
        bool comparedFactions = this.type != type; // �� �������, ���� ����� ������ �����-������ �������.

        return (comparedFactions || this.type == FType.aggressive) && this.type != FType.neutral;
    }
}
