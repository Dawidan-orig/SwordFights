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

    public FType type = FType.neutral;
}
