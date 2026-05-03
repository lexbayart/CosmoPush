
using Unity.Mathematics;

/// <summary>
/// Унифицированный интерфейс для трекинга тела.
/// Позволяет игре работать одинаково с Sentis, ARKit или заглушками.
/// </summary>
public interface IBodyTracker
{
    bool IsTracking { get; }
    
    // Возвращает нормализованные координаты (0..1)
    float2 GetNose();
    float2 GetShoulderMidpoint();
    float GetKeypointZ(int index);
    float GetKeypointVisibility(int index);
    
    // Для отладки
    float GetKeypointY(int index);
}
