using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AblitySystem : MonoBehaviour
{
    public GameObject slashPrefab; // TODO : ����������� � ScriptableObject, ������� ��� � ProceedingSlash. ��� ����� ��� ���� ������, ��� ������ � �� �������� � �������� - ������ � Proceeding Slash
    public Ability windSlashAbility;

    private void Awake()
    {
        windSlashAbility = new ProceedingSlash(transform, GetComponent<SwordControl>(), slashPrefab);

        windSlashAbility.Activated = true;
    }
}
