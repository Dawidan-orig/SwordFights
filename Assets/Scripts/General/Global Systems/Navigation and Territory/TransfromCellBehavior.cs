using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��� ��������� ���������� � ������� transform'�. ����� ��� ������ ������ ������
/// </summary>
public class TransfromCellBehavior : MonoBehaviour
{
    [SerializeField]
    private NavMeshCalculations.Cell aligned;

    public NavMeshCalculations.Cell Aligned { get => aligned; set => aligned = value; }
}
