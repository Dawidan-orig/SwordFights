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
}
