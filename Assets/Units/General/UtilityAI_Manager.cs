using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UtilityAI_Manager : MonoBehaviour
    // �������� ��� ������� �� �����, � �������� ����� �����������������, � ������������� ���������� ��� ���� UtilityAI
    // Singleton
{
    public static UtilityAI_Manager instance { get; private set; }

    [Header("Setup")]
    // ��� ����� ��� ������������� ��������� ������ ���������, � ��� �� ������� �� ������ � ���� �����.
    // ��������, �� ������� ������ ��������� �����, ��������� ���������� ��������� �������� ����� ������ �������� � �����, � �� � ������ ������.
    [SerializeField]
    private GameObject _player;
    [SerializeField]
    private GameObject _sampo;

    private Dictionary<GameObject, int> interactables = new Dictionary<GameObject, int>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }

        //interactables.Add(_sampo, 0);
        //interactables.Add(_player, 1);
    }

    public void AddNewInteractable(GameObject interactable, int weight) 
    {
        if(interactables.ContainsKey(interactable)) 
        {
            Debug.LogWarning($"{interactable.transform.name} ��� ��� �������� � ������, ������");
            return;
        }

        //TODO : ��������� ������, ����� interactable �����. ��������, ����� ������� ������ ����������.

        interactables.Add(interactable, weight);
    }

    public Dictionary<GameObject, int> GetPossibleActivities() 
    {
        // TODO : �������� �������, ����� ���������� ������ ��, ��� ����� ���� �����:
        // - ���������� �������
        // - ����������� ���� ��
        // � ��� �����
        return interactables;
    }
}
