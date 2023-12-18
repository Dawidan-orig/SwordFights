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

    private void Start()
    {
        var visuals = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in visuals)
        switch (f_type)
        {
            case FType.sampo: renderer.material = Variable_Provider.Instance.sampo; break;
            case FType.enemy: renderer.material = Variable_Provider.Instance.enemy; break;
            case FType.aggressive: renderer.material = Variable_Provider.Instance.agro; break;
        }
    }

    public FType f_type = FType.neutral;

    public bool IsWillingToAttack(FType type)
    {
        bool comparedFactions = this.f_type != type; // �� �������, ���� ����� ������ �����-������ �������.

        return (comparedFactions || this.f_type == FType.aggressive) && this.f_type != FType.neutral;
    }
}
