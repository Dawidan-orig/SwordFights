using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class WaveHandler : MonoBehaviour
{
    private static WaveHandler _instance;
    [HideInInspector]
    public static WaveHandler Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<WaveHandler>();
            if (_instance == null)
            {
                GameObject go = new("Wave Controlling Singleton");
                _instance = go.AddComponent<WaveHandler>();
            }

            /*
            if (EditorApplication.isPlaying)
            {
                _instance.transform.parent = null;
                DontDestroyOnLoad(_instance.gameObject);
            }*/

            return _instance;
        }
        private set { }
    }

    [Header("Setup")]
    public Transform container;
    [Tooltip("�����, ��� ������������ ������������� � Editor � ������ ����� ������� ��������� �������")]
    public List<Pallete> prefabPalletes = new List<Pallete>();
    [Tooltip("������� ���� ������, ������� ����� ���������� ���������� � �����")]
    public List<GameObject> allUnits = new List<GameObject>();
    //TODO : ������������� ���������� prefab'�� � ���� ������. ���� ������������� Resources.
    [Tooltip("������������� ����� ��� �������� ������.")]
    public AnimationCurve wavePointDistribution;

    [Header("Constraints")]
    public const int NORMAL_POINTS = 100; // ����� ����� �� ������ ������������� �����, ������� �������� ������ � ������ ���� ������������.
    public float wave_power = 10000;
    public int units_amount = 100;

    [Header("Lookonly")]
    [SerializeField]
    private List<GameObject> unitPrefabsToSpawn = new List<GameObject>();
    // TODO : ���� ����� ���������� � ����������: �� ����� ��������� ����� ����� ������ ����� ���� � ������ ���������, � �� ����� ��� ����� ��������� ���� � ������.
    // ���� ����� ��� ���� ����� ����������, �� ��� ���� ������. ������, �������� ����� ���������.
    // �� ����� �� ���� ����� ������� ���� ������ �������.
    public GameObject GetSpawnedUnit(Vector3 onPosition, Quaternion withRotation = default) 
    {
        if (unitPrefabsToSpawn.Count == 0)
            return null;

        GameObject unit = Instantiate(unitPrefabsToSpawn[0], onPosition, withRotation, container);
        unitPrefabsToSpawn.RemoveAt(0);
        return unit;
    }
    public int GetAmountOfUnitsToSpawn() 
    {
        return unitPrefabsToSpawn.Count;
    }

    private void FormFromPallete(Pallete givenPallete) 
    {
        float remainedPower = wave_power;
        int toSpawn = units_amount;

        float middleValue_PointsForUnit = wave_power / units_amount;

        while (toSpawn > 0 && remainedPower > 0)
        {
            float generationValue = Random.value;

            int usedPoints = Mathf.RoundToInt(wavePointDistribution.Evaluate((units_amount - toSpawn) / units_amount) * middleValue_PointsForUnit);

            //Debug.Log(givenPallete.Pass(generationValue).GetType());
            GameObject newUnitPrefab = (GameObject)givenPallete.Pass(generationValue);
            newUnitPrefab.GetComponent<IPointsDistribution>().GivePoints(usedPoints);
            unitPrefabsToSpawn.Add(newUnitPrefab);

            toSpawn--;
            remainedPower -= usedPoints;
        }
    }

    public void UsePrefabPallete() // ���������� ������� ��������� ������� ������
    {
        unitPrefabsToSpawn.Clear();
        int chosenPalleteIndex = Random.Range(0, prefabPalletes.Count);

        Pallete former = prefabPalletes[chosenPalleteIndex];

        FormFromPallete(former);
    }

    public void FormProceduralPalette() // ������ ���������������� ������� ������ ����������
    {
        unitPrefabsToSpawn.Clear();

        //TODO 
    }

    public void FormPaletteRandomly() // ������ ������� ����� �� ���������� �������� ��������� ������
    {
        unitPrefabsToSpawn.Clear();

        foreach (GameObject unit in allUnits)
        {

        }
    }
}
