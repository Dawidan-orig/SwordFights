using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(PalleteObject))]
public class PalleteComponent_PropertyDrawer : PropertyDrawer
{
    private SerializedProperty _leftP;
    private SerializedProperty _rightP;

    /*
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement container = new();

        var leftPointer = new PropertyField(property.FindPropertyRelative("left"));
        var rightPointer = new PropertyField(property.FindPropertyRelative("right"));
        var objectUsed = new PropertyField(property.FindPropertyRelative("obj"));

        container.Add(leftPointer);
        container.Add(rightPointer);
        container.Add(objectUsed);

        return container;
    }*/

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        #region Planning
        //fieldInfo.GetValue() ��������� ������� �������� ��������� � ���� ����.
        // ������ ������������ property, ���������� � �������. �� �������� � ���� ��� ���� ��� ���.
        // SerializedProperty - ��� ������ �����, ��� �� � ���� ��������.
        //property.serializedObject.targetObject ��������� ����� �� �������� ��� ����������� ���������������� �������, ��� ��� �� ��������.
        // �����: ���� ������� �� property Pallete, ������� ����� � �� ��������. ����� �� �� ������ ��� � ��� ������� �������.

        //property.propertyPath �������� � ���� ���� � �������� �������, ������� � ���� �������.
        //property.serializedObject.FindProperty() ��������� ������ �� ����... ������.
        //Debug.Log(property.propertyPath);
        //Debug.Log(property.serializedObject.FindProperty(property.propertyPath).displayName); - ��� ���������� Element 0. ��-���� ��� ����� ������. �����
        //  SerializedProperty listSerializedElement = property.serializedObject.FindProperty(property.propertyPath);
        // ��� ���� �������.
        //Debug.Log(listSerializedElement.type); -> PalleteObject, ��, ��� ����.

        //������ �������� ���������� � ���������, � � ����� ��� �������. ��� ���� �� ���:
        //  PalleteObject used = Utilities.Editor.SerializedPropertyToObject<PalleteObject>(listSerializedElement);
        // �� ����, ��� ������� ������ ��, ��� � ������ ����, �� � ����������������� ����. �� �� ������, ��� � �� �����.
        // ��� ������ ��� �� ��������, GetFieldOrPropertyValue ��������� �� NullRef � ������ �� �������.

        // ���������, ��� �� ����� �� ����� �������� �� ����������� (������ � �������, ��� IList'�). � ������� �����-�� �������. ��������.
        //  used.OnValidate();
        // ��������!
        #endregion

        EditorGUI.BeginProperty(position, label, property);

        var identLevel = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        var leftPointerProp = property.FindPropertyRelative("left");
        var rightPointerProp = property.FindPropertyRelative("right");
        var content = property.FindPropertyRelative("obj");

        const float POINTER_WIDTH = 18 * 4;

        Rect leftPos = new Rect(position.x, position.y, POINTER_WIDTH, position.height);
        Rect contentPos = new Rect(position.x + position.width / 2 - 50, position.y, 100, position.height);
        Rect rightPos = new Rect(position.x + position.width - POINTER_WIDTH, position.y, POINTER_WIDTH, position.height);

        SerializedProperty listSerializedElement = property.serializedObject.FindProperty(property.propertyPath);
        PalleteObject used = Utilities.Editor.SerializedPropertyToObject<PalleteObject>(listSerializedElement);

        leftPointerProp.floatValue = FloatVal(leftPos, leftPointerProp, used.left);

        if (content == null)
            EditorGUI.LabelField(contentPos, "Empty");
        else
            EditorGUI.PropertyField(contentPos, content, GUIContent.none);

        rightPointerProp.floatValue = FloatVal(rightPos, rightPointerProp, used.right);

        EditorGUI.indentLevel = identLevel;

        EditorGUI.EndProperty();

        property.serializedObject.ApplyModifiedProperties();
    }

    private float FloatVal(Rect pos, SerializedProperty pointerProp, float passedValue)
    { 
        //TODO : ����� ������������� Add New ������ � Pallete_PropertyDrawer 
        // �������� ��� ��������������. � ��� ��� ������ ������! (��������� ������� ������� > 6 ����� �� �������. ������.)
        float val = EditorGUI.FloatField(pos, passedValue);
        Debug.Log("redacted value : " + val + " real: " + passedValue + " when serProp is " + pointerProp.floatValue);
        pointerProp.floatValue = val;

        //used.OnValidate();

        return val;
    }
}