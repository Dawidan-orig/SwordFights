using UnityEngine;

public class Faction : MonoBehaviour
{
    public enum FType
    {
        neutral,

        sampo,
        enemy,
        
        aggressive // �������� ������ �� ���� ��� �������
    }

    //IDEA : ��������� ������� ������ ��������� � ���������.
    // ������ �������: ��������� ��������� ������ ��������� ������ �������� AI. ��� ������� ������ �������� �� ������ ���������.
    // ������� ��� ������� get-set; ���� ������� private
    // ���� �� �������, ����� �������� ����������� �������� ������ ������� ����� ������ ������ ���������� � ���� ��������

    private void Start()
    {
        var visuals = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in visuals)
            switch (_ftype)
            {
                case FType.sampo: renderer.material = Variable_Provider.Instance.sampo; break;
                case FType.enemy: renderer.material = Variable_Provider.Instance.enemy; break;
                case FType.aggressive: renderer.material = Variable_Provider.Instance.agro; break;
            }
    }

    [SerializeField]
    private FType _ftype = FType.neutral;

    public FType FactionType { get => _ftype;}

    /// <summary>
    /// ����������� ����� �������, ��-�� ������� ������ ������� �������� ��������� � ��� ����������� ��������������.
    /// </summary>
    public void ChangeFactionCompletely(FType newFactionType) 
    {        
        _ftype = newFactionType;
    }

    public bool IsWillingToAttack(FType type)
    {
        bool comparedFactions = _ftype != type; // �� �������, ���� ����� ������ �����-������ �������.

        return (comparedFactions || _ftype == FType.aggressive) && _ftype != FType.neutral;
    }
}
