using System.Collections.Generic;
using System.Linq;

namespace ImageRecognition.Domain;

/// <summary>
/// Реализация k-мерного дерева (KD-Tree) для ускорения поиска ближайших соседей.
/// </summary>
public sealed class KDTree
{
    /// <summary>
    /// Корневой узел дерева.
    /// </summary>
    public KDNode? Root { get; private set; }

    /// <summary>
    /// Размерность пространства (число признаков).
    /// </summary>
    public int Dimension { get; }

    public KDTree(int dimension)
    {
        if (dimension <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dimension), "Dimension must be positive.");
        }

        Dimension = dimension;
    }

    /// <summary>
    /// Строит сбалансированное KD-дерево по обучающей выборке.
    /// </summary>
    /// <param name="points">Коллекция векторов признаков.</param>
    /// <param name="labels">Коллекция меток классов.</param>
    public void BuildTree(IReadOnlyList<double[]> points, IReadOnlyList<int> labels)
    {
        if (points is null) throw new ArgumentNullException(nameof(points));
        if (labels is null) throw new ArgumentNullException(nameof(labels));
        if (points.Count != labels.Count)
        {
            throw new ArgumentException("Points and labels collections must have the same length.");
        }

        if (points.Count == 0)
        {
            Root = null;
            return;
        }

        Root = BuildRecursive(points, labels, depth: 0);
    }

    private KDNode? BuildRecursive(IReadOnlyList<double[]> points, IReadOnlyList<int> labels, int depth)
    {
        if (points.Count == 0)
        {
            return null;
        }

        int axis = depth % Dimension;

        var sortedIndices = Enumerable.Range(0, points.Count)
            .OrderBy(i => points[i][axis])
            .ToArray();

        int medianIndex = sortedIndices.Length / 2;
        int medianPointIndex = sortedIndices[medianIndex];

        double[] medianPoint = points[medianPointIndex];
        int medianLabel = labels[medianPointIndex];

        var leftPoints = new List<double[]>(medianIndex);
        var leftLabels = new List<int>(medianIndex);
        var rightPoints = new List<double[]>(sortedIndices.Length - medianIndex - 1);
        var rightLabels = new List<int>(sortedIndices.Length - medianIndex - 1);

        for (int i = 0; i < sortedIndices.Length; i++)
        {
            if (i == medianIndex) continue;

            int originalIndex = sortedIndices[i];
            if (i < medianIndex)
            {
                leftPoints.Add(points[originalIndex]);
                leftLabels.Add(labels[originalIndex]);
            }
            else
            {
                rightPoints.Add(points[originalIndex]);
                rightLabels.Add(labels[originalIndex]);
            }
        }

        var node = new KDNode(medianPoint, medianLabel, axis)
        {
            Left = BuildRecursive(leftPoints, leftLabels, depth + 1),
            Right = BuildRecursive(rightPoints, rightLabels, depth + 1)
        };

        return node;
    }

    /// <summary>
    /// Вставляет новую точку в уже построенное KD-дерево.
    /// </summary>
    public void Insert(double[] point, int label)
    {
        if (point is null) throw new ArgumentNullException(nameof(point));
        if (point.Length != Dimension)
        {
            throw new ArgumentException("Point has incorrect dimension.", nameof(point));
        }

        Root = InsertRecursive(Root, point, label, depth: 0);
    }

    private KDNode InsertRecursive(KDNode? node, double[] point, int label, int depth)
    {
        if (node is null)
        {
            int newNodeAxis = depth % Dimension;
            return new KDNode(point, label, newNodeAxis);
        }

        int axis = node.Axis;
        if (point[axis] < node.Point[axis])
        {
            node.Left = InsertRecursive(node.Left, point, label, depth + 1);
        }
        else
        {
            node.Right = InsertRecursive(node.Right, point, label, depth + 1);
        }

        return node;
    }

    /// <summary>
    /// Находит ближайшего соседа к заданной точке.
    /// </summary>
    public KDNode? NearestNeighbor(double[] target)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (target.Length != Dimension)
        {
            throw new ArgumentException("Target has incorrect dimension.", nameof(target));
        }

        if (Root is null) return null;

        return NearestNeighborRecursive(Root, target, bestNode: Root, bestDistance: DistanceCalculator.EuclideanDistance(Root.Point, target));
    }

    private KDNode NearestNeighborRecursive(KDNode node, double[] target, KDNode bestNode, double bestDistance)
    {
        double distance = DistanceCalculator.EuclideanDistance(node.Point, target);
        if (distance < bestDistance)
        {
            bestDistance = distance;
            bestNode = node;
        }

        KDNode? nextBranch;
        KDNode? oppositeBranch;

        if (target[node.Axis] < node.Point[node.Axis])
        {
            nextBranch = node.Left;
            oppositeBranch = node.Right;
        }
        else
        {
            nextBranch = node.Right;
            oppositeBranch = node.Left;
        }

        if (nextBranch is not null)
        {
            bestNode = NearestNeighborRecursive(nextBranch, target, bestNode, bestDistance);
            bestDistance = DistanceCalculator.EuclideanDistance(bestNode.Point, target);
        }

        double axisDistance = target[node.Axis] - node.Point[node.Axis];
        if (oppositeBranch is not null && axisDistance * axisDistance < bestDistance * bestDistance)
        {
            bestNode = NearestNeighborRecursive(oppositeBranch, target, bestNode, bestDistance);
        }

        return bestNode;
    }

    /// <summary>
    /// Находит k ближайших соседей к заданной точке.
    /// </summary>
    public IReadOnlyList<(KDNode Node, double Distance)> KNearestNeighbors(double[] target, int k)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (target.Length != Dimension)
        {
            throw new ArgumentException("Target has incorrect dimension.", nameof(target));
        }

        if (k <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(k), "k must be positive.");
        }

        var best = new List<(KDNode Node, double Distance)>(capacity: k);

        if (Root is null)
        {
            return best;
        }

        SearchKNearest(Root, target, k, best);
        best.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        return best;
    }

    private void SearchKNearest(KDNode node, double[] target, int k, List<(KDNode Node, double Distance)> best)
    {
        double distance = DistanceCalculator.EuclideanDistance(node.Point, target);
        InsertCandidate(best, (node, distance), k);

        KDNode? nextBranch;
        KDNode? oppositeBranch;

        if (target[node.Axis] < node.Point[node.Axis])
        {
            nextBranch = node.Left;
            oppositeBranch = node.Right;
        }
        else
        {
            nextBranch = node.Right;
            oppositeBranch = node.Left;
        }

        if (nextBranch is not null)
        {
            SearchKNearest(nextBranch, target, k, best);
        }

        double furthestDistance = best.Count == 0 ? double.PositiveInfinity : best.Max(p => p.Distance);
        double axisDistance = target[node.Axis] - node.Point[node.Axis];

        if (oppositeBranch is not null && (best.Count < k || axisDistance * axisDistance < furthestDistance * furthestDistance))
        {
            SearchKNearest(oppositeBranch, target, k, best);
        }
    }

    private static void InsertCandidate(List<(KDNode Node, double Distance)> best, (KDNode Node, double Distance) candidate, int k)
    {
        best.Add(candidate);
        best.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        if (best.Count > k)
        {
            best.RemoveAt(best.Count - 1);
        }
    }
}

