using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshCalculations : MonoBehaviour
{
    private static NavMeshCalculations _instance;

    [Min(0)]
    public float MINIMUM_AREA = 99999; //TODO : �������� �� 30, ���� ��� - debug-��������
    [Min(0)]
    public float MAXMIMUM_AREA = 20;
    [Range(0, 100)]
    public float MAX_VERTS_IN_COMPLEX = 50;

    public static NavMeshCalculations Instance
    {
        get
        {
            if (!_instance)
            {
                _instance = new GameObject().AddComponent<NavMeshCalculations>();
                // name it for easy recognition
                _instance.name = _instance.GetType().ToString();
                // mark root as DontDestroyOnLoad();
                DontDestroyOnLoad(_instance.gameObject);

                _instance.Initialize();
            }
            return _instance;
        }
    }
    public class Cell // ��� ���� �� �� ������ A* �� �����, ��������� ���������.
    {
        protected List<Cell> _neighbors = new List<Cell>();
        protected Vector3[] _vectorFormers;

        public void AddNeighbor(Cell neighbor)
        {
            if (neighbor == this)
                return;

            _neighbors.Add(neighbor);
        }

        public void RemoveNeighbor(Cell neighbor)
        {
            if (neighbor == this)
                return;

            _neighbors.Remove(neighbor);
        }
        public void AddNeighbors(List<Cell> neighbors)
        {
            List<Cell> temp = new List<Cell>(neighbors);

            if (neighbors.Contains(this))
                temp.Remove(this);

            _neighbors.AddRange(temp);
        }

        public Vector3 Center()
        {
            Vector3 sum = Vector3.zero;

            foreach (Vector3 former in _vectorFormers)
                sum += former;

            return sum / _vectorFormers.Length;
        }

        public Vector3 NavMeshCenter()
        {
            Physics.Raycast(Center(), Vector3.down, out var hit, 100);

            return hit.point;
        }

        public Vector3[] Formers()
        {
            return _vectorFormers;
        }

        public virtual void DrawGizmo()
        {

        }

        public List<Cell> Neighbors { get => _neighbors; set => _neighbors = value; }
    }

    //������� �������: ����������� Cell'�, � ������. � ������ ������� ��������� ��� ����� �����������, � ������� ��� �� �����������.
    private class TriangleCell : Cell
    {
        public bool draw = true;
        public TriangleCell(Vector3[] formers)
        {
            _vectorFormers = new Vector3[3];
            _vectorFormers = formers;
        }

        public override void DrawGizmo()
        {
            if (!draw)
                return;

            Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            for (int i = 0; i < 3; i++)
            {
                Gizmos.DrawLine(_vectorFormers[i], _vectorFormers[(i + 1) % 3]);
            }

            Gizmos.DrawRay(Center(), Vector3.up);
        }

        public void Draw(Color color, float duration = 0)
        {
            for (int i = 0; i < 3; i++)
            {
                Debug.DrawLine(_vectorFormers[i], _vectorFormers[(i + 1) % 3], color, duration);
            }
        }
    }
    private class ComplexCell : Cell
    {
        private List<TriangleCell> _trianglesFormers = new();
        public void Consume(TriangleCell cell)
        {
            cell.draw = false;

            if (_trianglesFormers.Count == 0) // ����������� ��������� �������
            {
                _vectorFormers = new Vector3[3];

                for (int i = 0; i < 3; i++)
                    _vectorFormers[i] = cell.Formers()[i];
            }
            else
            {
                //���� �� �������, ������� ����������� � ������ ����������� ������

                Vector3 res = Vector3.zero; // ���� ������
                int first = -1; // ������������ ����� ������� � �������, ����� ������� ��������� �����
                int second = -1;

                #region assignes
                int iteration = 0;
                foreach ( Vector3 complexFormer in _vectorFormers)
                {
                    Vector3 possible = Vector3.zero;                    
                    bool match = false;
                    foreach (Vector3 triangleFormer in cell.Formers())
                    {
                        possible = triangleFormer;
                        if (triangleFormer == complexFormer) //_vectorFormers.Contains(triangleFormer):
                        {
                            if (first == -1)
                                first = iteration;
                            else
                                second = iteration;

                            match = true;
                            break;
                        }                        
                    }

                    iteration++;
                    if (!match)
                        res = possible;
                }

                #endregion

                // ���� ������ ����� ����� � ������ ���������

                #region checks

                if (first == -1 && second == -1)
                {
                    // ������ ����������� ��� ����� ���� ������� ������� � ComplexCell
                    // ������ �� ���� �� �������� �������
                    // � ����� ������, ����� ������������� ��� �����������, � ������
                    cell.Draw(Color.yellow, 2);
                    Draw(Color.yellow, 2);
                    return;

                    //throw new Exception("����� ������� ����� �� �������");
                }
                if (res == Vector3.zero)
                {
                    Debug.Log(first + " " + second);

                    int i = 0;
                    foreach (Vector3 vector in _vectorFormers)
                        Utilities.CreateFlowText(i++.ToString(), 1, vector);

                    cell.Draw(Color.red, 100);
                    Draw(Color.red, 100);
                    Debug.LogError("��� ���������� ������������� ������������ �� ������� ������� ������� (Error Pause, ����� �������)");
                    return;
                }

                #endregion

                #region arrayUpdate

                int newArrayLen = _vectorFormers.Length + 1;
                Vector3[] newFormers = new Vector3[newArrayLen];
                
                int offset = 0;
                for (int i = 0; i < _vectorFormers.Length; i++)
                {
                    if(first == 0 && second == _vectorFormers.Length-1) 
                    {
                        Utilities.CreateFlowText($"{first}<->{second}", 1, res + Vector3.up * _vectorFormers.Length, Color.green);

                        newFormers[_vectorFormers.Length] = res;
                        break;
                    }

                    newFormers[i + offset] = _vectorFormers[i];
                    if (i == first)
                    {
                        Utilities.CreateFlowText($"{first}<->{second}", 1, res + Vector3.up * _vectorFormers.Length, Color.cyan);

                        newFormers[i + 1] = res;
                        offset = 1;
                    }
                }

                #endregion

                _vectorFormers = newFormers;
            }

            //TODO : �������
            Draw(_vectorFormers.Length * Vector3.up, _vectorFormers.Length % 2 == 0 ? new Color(0, 0.3f, 0, 0.5f) : new Color(0, 0, 1, 0.5f), 1);

            _trianglesFormers.Add(cell);
        }

        public override void DrawGizmo()
        {
            Gizmos.color = Mathf.CorrelatedColorTemperatureToRGB(CellCount() * 1000);
            Vector3 prev = Vector3.zero;
            foreach (var former in _vectorFormers)
            {
                if (prev == Vector3.zero) { prev = former; continue; }

                Gizmos.DrawLine(prev, former);
                prev = former;
            }

            Gizmos.DrawLine(_vectorFormers[_vectorFormers.Length - 1], _vectorFormers[0]);

            Gizmos.DrawRay(Center(), Vector3.up);
        }

        public void Draw(Color color, float duration = 0)
        {
            Draw(Vector3.zero, color, duration);
        }
        public void Draw(Vector3 offset, Color color, float duration)
        {
            int i = 0;
            Vector3 prev = Vector3.zero;
            foreach (var former in _vectorFormers)
            {
                Utilities.CreateFlowText(i++.ToString(), 1, former + offset, color);

                if (prev == Vector3.zero) { prev = former; continue; }

                Debug.DrawLine(offset + prev, offset + former, color, duration);
                prev = former;
            }
            Debug.DrawLine(offset + prev, offset + _vectorFormers[0], color, duration);
        }
        public float GetArea()
        {
            float res = 0;
            foreach (TriangleCell triangle in _trianglesFormers)
                res += TriangleArea(triangle.Formers());
            return res;
        }
    }

    private struct Edge
    {
        Vector3 former1;
        Vector3 former2;

        public Edge(Vector3 former1, Vector3 former2)
        {
            this.former1 = former1;
            this.former2 = former2;
        }

        public void Draw(Color color, float duration = 0)
        {
            Debug.DrawLine(former2, former1, color, duration);
        }


        public override bool Equals(object obj)
        {
            if (!(obj is Edge)) return false;

            Edge other = (Edge)obj;

            return other.former1 == former1 && other.former2 == former2
            || other.former1 == former2 && other.former2 == former1;
        }

        public override int GetHashCode()
        {
            return former1.GetHashCode() ^ former2.GetHashCode();
        }
    }

    private Cell[] _cells;

    private void OnValidate()
    {
        if (_instance)
            _instance.Initialize();
        MAXMIMUM_AREA = Mathf.Clamp(MAXMIMUM_AREA, MINIMUM_AREA, int.MaxValue);
    }

    public void Initialize()
    {
        //TODO? : ������� ������� ��� �����������, ���� ��������� ��� � ��� ������������� - �� �������� ��� �������.

        List<Cell> _cellsList = new();
        List<Cell> trianglesToCombine = new();
        Dictionary<Edge, List<Cell>> links = new(); //������� ����� ������������� �����-�� Cell'�

        List<Edge> edges = new();

        var triangulation = NavMesh.CalculateTriangulation();

        //������������ �������������
        for (int i = 0; i < triangulation.indices.Length; i += 3)
        {
            Vector3[] triangle = new Vector3[3];

            triangle[0] = triangulation.vertices[triangulation.indices[i]];
            triangle[1] = triangulation.vertices[triangulation.indices[i + 1]];
            triangle[2] = triangulation.vertices[triangulation.indices[i + 2]];

            Edge[] triEdges = new Edge[3];
            for (int j = 0; j < 3; j++)
            {
                triEdges[j] = new Edge(triangle[j], triangle[(j + 1) % 3]);
                //triEdges[j].Draw(new Color(1, 1, 1, 0.3f), 20);
                if (!links.ContainsKey(triEdges[j]))
                    links.Add(triEdges[j], new List<Cell>());
            }

            Cell cell = new TriangleCell(triangle);

            for (int j = 0; j < 3; j++)
                links[triEdges[j]].Add(cell);

            if (TriangleArea(triangle) < MINIMUM_AREA)
            {
                trianglesToCombine.Add(cell);
                continue;
            }

            _cellsList.Add(cell);
        }
        //���������� �������
        foreach (KeyValuePair<Edge, List<Cell>> kvp in links)
        {
            foreach (Cell c in kvp.Value)
            {
                List<Cell> neighbors = kvp.Value;
                c.AddNeighbors(neighbors);
            }
        }
        //������������� �������������
        while (trianglesToCombine.Count > 0)
        {
            //���� ��� �����-�� ��������� �����������. ���� ��� �� ������.
            // ������� �� ��� ������ ComplexCell, ������� ������ �������� ����������� �� ���� �������.
            // ��� ������ ������ ���������, � ��������� ������ - ����� �������� � ����� ComplexCell
            ComplexCell consumer = new ComplexCell();
            LinkedList<TriangleCell> toConsume = new();
            _cellsList.Add(consumer);

            TriangleCell first = (TriangleCell)trianglesToCombine[0];

            foreach (Cell neighbor in first.Neighbors)
            {
                neighbor.RemoveNeighbor(first);
                neighbor.AddNeighbor(consumer);
                if (neighbor is TriangleCell && trianglesToCombine.Contains(neighbor) && !toConsume.Contains((TriangleCell)neighbor))
                    toConsume.AddFirst((TriangleCell)neighbor);
            }

            consumer.Consume(first);
            trianglesToCombine.Remove(first);

            while (toConsume.Count > 0)
            {
                if (consumer.GetArea() > MINIMUM_AREA)
                    break;

                if (consumer.Formers().Length > MAX_VERTS_IN_COMPLEX)
                    break;

                TriangleCell cell = toConsume.First.Value;
                toConsume.RemoveFirst();

                foreach (Cell neighbor in cell.Neighbors)
                {
                    neighbor.RemoveNeighbor(cell);
                    neighbor.AddNeighbor(consumer);
                    if (neighbor is TriangleCell && trianglesToCombine.Contains(neighbor) && !toConsume.Contains((TriangleCell)neighbor))
                        toConsume.AddFirst((TriangleCell)neighbor);
                }

                consumer.Consume(cell);
                trianglesToCombine.Remove(cell);
            }
        }

        _cells = _cellsList.ToArray();
    }

    public Cell GetCell(int index)
    {
        return _cells[index];
    }

    public Cell GetCell(Vector3 pointNear)
    {
        //TODO: ��� ������ ���������� ��������������, �������� ����� ������� ������.
        Cell res = _cells[0];
        float bestDistance = 100000;
        foreach (Cell cell in _cells)
        {
            float distance = Vector3.Distance(cell.Center(), pointNear);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                res = cell;
            }
        }

        return res;
    }

    public void DrawCells()
    {
        foreach (Cell cell in _cells)
        {
            cell.DrawGizmo();
        }
    }

    public static int CellCount() => _instance._cells.Length;

    private static float TriangleArea(Vector3[] triangle)
    {
        Vector3 line1 = triangle[0] - triangle[1];
        Vector3 line2 = triangle[0] - triangle[2];

        return (Vector3.Cross(line2, line1).magnitude) / 2;
    }

    private void OnDrawGizmos()
    {
        DrawCells();
    }


}
