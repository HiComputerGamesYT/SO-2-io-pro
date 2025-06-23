using UnityEngine;

public class CameraController : MonoBehaviour
{
    // ��'���, �� ���� ���� �������� ������.
    // ���������� ���� ��� ��'��� Player � Hierarchy � ��������� Unity.
    [SerializeField]
    private Transform target;

    // ³������ �� ���, �� ��� ���� ����������� ������ �� �� Z.
    // �������� ��� 2D-���� �� ��������� ��������, ��� ������ ���� "������" ��'����.
    [SerializeField]
    private float offsetZ = -10f;

    // �������� ������������ ���� ������. ��� ����� ��������, ��� ������ ������ "����������" ����.
    // ���� ������ ������� ��������� ��� ��������, ��������� 1.
    [SerializeField]
    private float smoothSpeed = 0.125f;

    void LateUpdate()
    {
        // ����������, �� ����������� ���� ��� ������.
        if (target == null)
        {
            Debug.LogError("������ �� �� ��� (Target) ��� ���������. ��������� ��'��� Player �� ���� Target � ���������.", this);
            return;
        }

        // ���������� ������ ������� ������.
        // ������ ������� ���, ��� �������� offsetZ ��� �������.
        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, offsetZ);

        // ������������� Lerp ��� �������� ���������� ������ �� ������ �������.
        // �� ������� ����� ������������ ����.
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }

    // ������ ��� ���������� ������������ ������ �� ��� ��� (���� ������� ���� �����).
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetOffsetZ(float newOffsetZ)
    {
        offsetZ = newOffsetZ;
    }

    public void SetSmoothSpeed(float newSmoothSpeed)
    {
        smoothSpeed = newSmoothSpeed;
    }
}