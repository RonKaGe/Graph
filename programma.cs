using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphEditor.Core
{
    public partial class MyGraphModel : GraphModelBase
    {
        public override double[,] BuildAdjacencyMatrix(bool useWeights = true)
        {
            int vertexCount = _verticesStorage.Count;

            // Если граф пустой, возвращаем пустую матрицу
            if (vertexCount == 0)
            {
                return new double[0, 0];
            }

            // Сортируем вершины по ID для детерминированного порядка
            var sortedVertices = _verticesStorage.Keys.OrderBy(id => id).ToList();

            // Создаем словарь для быстрого доступа к индексам вершин
            var vertexIndexMap = new Dictionary<string, int>();
            for (int i = 0; i < sortedVertices.Count; i++)
            {
                vertexIndexMap[sortedVertices[i]] = i;
            }

            // Создаем матрицу смежности
            double[,] adjacencyMatrix = new double[vertexCount, vertexCount];

            // Инициализируем матрицу
            for (int i = 0; i < vertexCount; i++)
            {
                for (int j = 0; j < vertexCount; j++)
                {
                    // Для взвешенного графа используем PositiveInfinity как аналог "отсутствия ребра"
                    // Это стандартный подход в алгоритмах на графах (например, алгоритм Флойда-Уоршелла)
                    adjacencyMatrix[i, j] = useWeights ? double.PositiveInfinity : 0.0;
                }
            }

            // Заполняем матрицу на основе ребер
            foreach (var edge in _edgesStorage.Values)
            {
                int sourceIndex = vertexIndexMap[edge.Source];
                int targetIndex = vertexIndexMap[edge.Target];

                double value;
                if (useWeights)
                {
                    // Если ребро имеет вес, используем его, иначе 1.0
                    value = edge.Weight ?? 1.0;
                }
                else
                {
                    // Бинарная матрица смежности: 1 - есть ребро, 0 - нет ребра
                    value = 1.0;
                }

                // Записываем значение в матрицу
                adjacencyMatrix[sourceIndex, targetIndex] = value;

                // Если граф ненаправленный, заполняем и обратное направление
                if (!IsDirected && sourceIndex != targetIndex) // Не заполняем петли дважды
                {
                    adjacencyMatrix[targetIndex, sourceIndex] = value;
                }
            }

            // Диагональ матрицы (петли)
            if (AllowSelfLoops)
            {
                // Ищем петли
                foreach (var edge in _edgesStorage.Values.Where(e => e.Source == e.Target))
                {
                    int vertexIndex = vertexIndexMap[edge.Source];

                    double value;
                    if (useWeights)
                    {
                        value = edge.Weight ?? 1.0;
                    }
                    else
                    {
                        value = 1.0;
                    }

                    adjacencyMatrix[vertexIndex, vertexIndex] = value;
                }
            }

            return adjacencyMatrix;
        }

        // Альтернативная версия с настройкой значения для отсутствующих ребер
        public double[,] BuildAdjacencyMatrix(bool useWeights = true, double missingEdgeValue = double.PositiveInfinity)
        {
            int vertexCount = _verticesStorage.Count;

            if (vertexCount == 0)
            {
                return new double[0, 0];
            }

            var sortedVertices = _verticesStorage.Keys.OrderBy(id => id).ToList();
            var vertexIndexMap = new Dictionary<string, int>();

            for (int i = 0; i < sortedVertices.Count; i++)
            {
                vertexIndexMap[sortedVertices[i]] = i;
            }

            double[,] adjacencyMatrix = new double[vertexCount, vertexCount];

            // Инициализация с пользовательским значением для отсутствующих ребер
            for (int i = 0; i < vertexCount; i++)
            {
                for (int j = 0; j < vertexCount; j++)
                {
                    adjacencyMatrix[i, j] = missingEdgeValue;
                }
            }

            // Заполнение матрицы ребрами
            foreach (var edge in _edgesStorage.Values)
            {
                int sourceIndex = vertexIndexMap[edge.Source];
                int targetIndex = vertexIndexMap[edge.Target];

                double value = useWeights ? (edge.Weight ?? 1.0) : 1.0;

                adjacencyMatrix[sourceIndex, targetIndex] = value;

                if (!IsDirected && sourceIndex != targetIndex)
                {
                    adjacencyMatrix[targetIndex, sourceIndex] = value;
                }
            }

            // Обработка петель
            if (AllowSelfLoops)
            {
                foreach (var edge in _edgesStorage.Values.Where(e => e.Source == e.Target))
                {
                    int vertexIndex = vertexIndexMap[edge.Source];
                    double value = useWeights ? (edge.Weight ?? 1.0) : 1.0;
                    adjacencyMatrix[vertexIndex, vertexIndex] = value;
                }
            }

            return adjacencyMatrix;
        }

        // Метод для получения отладочного представления матрицы
        public string GetAdjacencyMatrixString(bool useWeights = true)
        {
            var matrix = BuildAdjacencyMatrix(useWeights);
            int size = matrix.GetLength(0);

            if (size == 0)
            {
                return "Empty graph";
            }

            // Получаем отсортированные ID вершин для заголовков
            var sortedVertices = _verticesStorage.Keys.OrderBy(id => id).ToList();

            var result = new System.Text.StringBuilder();
            result.AppendLine("Adjacency Matrix:");
            result.Append("      ");

            // Заголовки столбцов
            foreach (var vertex in sortedVertices)
            {
                result.Append($"{vertex,-8}");
            }
            result.AppendLine();
            result.AppendLine(new string('-', 8 * (size + 1)));

            // Данные матрицы
            for (int i = 0; i < size; i++)
            {
                result.Append($"{sortedVertices[i],-6}|");

                for (int j = 0; j < size; j++)
                {
                    if (useWeights && matrix[i, j] == double.PositiveInfinity)
                    {
                        result.Append("Inf     ");
                    }
                    else if (!useWeights && matrix[i, j] == 0)
                    {
                        result.Append("0       ");
                    }
                    else
                    {
                        result.Append($"{matrix[i, j]:F2}    ");
                    }
                }
                result.AppendLine();
            }

            return result.ToString();
        }

        // Метод для построения списка вершин в порядке матрицы
        public IReadOnlyList<string> GetVertexOrderForMatrix()
        {
            return _verticesStorage.Keys.OrderBy(id => id).ToList().AsReadOnly();
        }

        // Метод для получения значения из матрицы по ID вершин
        public double? GetMatrixValue(string sourceId, string targetId, bool useWeights = true)
        {
            if (!_verticesStorage.ContainsKey(sourceId) || !_verticesStorage.ContainsKey(targetId))
            {
                return null;
            }

            var sortedVertices = _verticesStorage.Keys.OrderBy(id => id).ToList();
            var vertexIndexMap = new Dictionary<string, int>();

            for (int i = 0; i < sortedVertices.Count; i++)
            {
                vertexIndexMap[sortedVertices[i]] = i;
            }

            int sourceIndex = vertexIndexMap[sourceId];
            int targetIndex = vertexIndexMap[targetId];

            // Проверяем наличие ребра
            bool hasEdge = _edgesStorage.Values.Any(e =>
                e.Source == sourceId && e.Target == targetId ||
                (!IsDirected && e.Source == targetId && e.Target == sourceId));

            if (!hasEdge)
            {
                return useWeights ? double.PositiveInfinity : 0.0;
            }

            // Находим ребро
            var edge = _edgesStorage.Values.FirstOrDefault(e =>
                e.Source == sourceId && e.Target == targetId ||
                (!IsDirected && e.Source == targetId && e.Target == sourceId));

            if (edge == null)
            {
                return useWeights ? double.PositiveInfinity : 0.0;
            }

            return useWeights ? (edge.Weight ?? 1.0) : 1.0;
        }
    }
}