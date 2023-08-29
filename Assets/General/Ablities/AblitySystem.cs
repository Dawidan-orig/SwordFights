using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AblitySystem : MonoBehaviour
{
    public GameObject slashPrefab; // TODO : ����������� � ScriptableObject, ������� ��� � ProceedingSlash. ��� ����� ��� ���� ������, ��� ������ � �� �������� � �������� - ������ � Proceeding Slash
    public Ability[] abilities;
    public ProceedingSlash slash;
    public Blow blow;
    public WindSlide windSlide;

    private void Awake()
    {
        abilities = new Ability[3];

        abilities[0] = new ProceedingSlash(transform, slashPrefab);
        abilities[1] = new Blow(transform);
        abilities[2] = new WindSlide(transform);

        slash = (ProceedingSlash)abilities[0];
        blow = (Blow)abilities[1];
        windSlide = (WindSlide)abilities[2];
    }

    private void Start()
    {
        foreach (var ability in abilities) { ability.Enable(); }
    }

    private void Update()
    {
        foreach(var ability in abilities) { ability.Update(); }

        if (Input.GetKeyDown(KeyCode.Alpha1))
            abilities[0].Activate();
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            abilities[1].Activate();
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            abilities[2].Activate();
    }

    private void FixedUpdate()
    {
        foreach (var ability in abilities) { ability.FixedUpdate(); }
    }
}
