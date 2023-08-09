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

    protected virtual void OnEnable()
    {
        UtilityAI_Manager.Instance.AddNewInteractable(gameObject, weight);
    }

    protected virtual void OnDisable()
    {
        UtilityAI_Manager.Instance.RemoveInteractable(gameObject);
    }
}
