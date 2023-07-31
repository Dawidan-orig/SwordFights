using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable_UtilityAI : MonoBehaviour
    // ������������� ��������� ��� GameObject'�, ����� ��� ����� �� ��������� ����� UtilityAI
    // ��������� Debug-���������: 
    // - �������� ����
    // - ���������� ���� ������������ ��������� �� ����������� UAI.
    // - ����������� ���� ��� ������������ ������� ������������ ����������� UAI
    // - ���������� ����, ������� �� UtilityAI Manager, ���� �� ���� GameObject ��� ���� ��, ��� ��������������� � ���. �������� ����� ���� ������ ������� ���� - ��� ������ ��������� ���������.
{
    public int weight = 1;
    public bool sampoSide = true; //TODO : ������, ������� �� enum-�������.

    private void Start()
    {
        UtilityAI_Manager.instance.AddNewInteractable(gameObject, weight);
    }
}
