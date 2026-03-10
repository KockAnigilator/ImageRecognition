namespace ImageRecognition.Domain;

/// <summary>
/// Узел k-мерного дерева для хранения вектора признаков и метки класса.
/// </summary>
public sealed class KDNode
{
    /// <summary>
    /// Точка в k-мерном пространстве (вектор признаков).
    /// </summary>
    public double[] Point { get; }

    /// <summary>
    /// Метка класса (например, цифра или идентификатор фигуры).
    /// </summary>
    public int Label { get; }

    /// <summary>
    /// Левый потомок (точки, у которых значение по оси меньше разделяющего значения).
    /// </summary>
    public KDNode? Left { get; set; }

    /// <summary>
    /// Правый потомок (точки, у которых значение по оси больше либо равно разделяющему значению).
    /// </summary>
    public KDNode? Right { get; set; }

    /// <summary>
    /// Ось (координата), по которой выполнялось разбиение в этом узле.
    /// </summary>
    public int Axis { get; }

    public KDNode(double[] point, int label, int axis)
    {
        Point = point ?? throw new ArgumentNullException(nameof(point));
        Label = label;
        Axis = axis;
    }
}

