using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Faction : MonoBehaviour
{
    public enum Type
    {
        sampo,
        enemy,
       
        neutral,
        aggressive // �������� ������ �� ���� ��� �������
    }

    public Type type = Type.neutral;
}
