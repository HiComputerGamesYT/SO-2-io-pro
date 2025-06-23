using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Об'єкт, за яким буде слідувати камера.
    // Перетягніть сюди ваш об'єкт Player з Hierarchy в інспекторі Unity.
    [SerializeField]
    private Transform target;

    // Відстань від цілі, на якій буде знаходитися камера по осі Z.
    // Зазвичай для 2D-ігор це негативне значення, щоб камера була "позаду" об'єктів.
    [SerializeField]
    private float offsetZ = -10f;

    // Швидкість згладжування руху камери. Чим більше значення, тим швидше камера "наздоганяє" ціль.
    // Якщо хочете жорстке слідування без затримки, встановіть 1.
    [SerializeField]
    private float smoothSpeed = 0.125f;

    void LateUpdate()
    {
        // Перевіряємо, чи встановлена ціль для камери.
        if (target == null)
        {
            Debug.LogError("Камера не має цілі (Target) для слідування. Призначте об'єкт Player до поля Target в інспекторі.", this);
            return;
        }

        // Обчислюємо бажану позицію камери.
        // Беремо позицію цілі, але зберігаємо offsetZ для глибини.
        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, offsetZ);

        // Використовуємо Lerp для плавного переміщення камери до бажаної позиції.
        // Це створює ефект згладжування руху.
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }

    // Методи для динамічного налаштування камери під час гри (якщо потрібно буде пізніше).
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