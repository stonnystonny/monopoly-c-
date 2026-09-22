using System.Collections.Generic;
using UnityEngine;

namespace Monopoly.Board
{
    /// <summary>
    /// Создаёт примитивы без коллайдеров: в проекте не подключён модуль Physics,
    /// поэтому GameObject.CreatePrimitive использовать нельзя. Берём встроенные меши напрямую.
    /// </summary>
    public static class Primitives
    {
        private static readonly Dictionary<PrimitiveType, Mesh> Cache = new Dictionary<PrimitiveType, Mesh>();
        private static Mesh fallbackBox;

        public static GameObject Create(PrimitiveType type, string objectName, Transform parent, Material material)
        {
            var obj = new GameObject(objectName);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = MeshFor(type);
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return obj;
        }

        private static Mesh MeshFor(PrimitiveType type)
        {
            if (Cache.TryGetValue(type, out Mesh cached) && cached != null) return cached;

            // Встроенные меши лежат в ресурсах под именами вида "Cube.fbx";
            // запрос по имени без расширения пишет ошибку в консоль, поэтому его не делаем.
            string name = type + ".fbx";
            var mesh = Resources.GetBuiltinResource<Mesh>(name);
            if (mesh == null)
            {
                Debug.LogWarning("Встроенный меш «" + name + "» не найден, используется куб.");
                mesh = FallbackBox();
            }

            Cache[type] = mesh;
            return mesh;
        }

        /// <summary>Запасной куб 1x1x1 на случай, если встроенный меш недоступен.</summary>
        private static Mesh FallbackBox()
        {
            if (fallbackBox != null) return fallbackBox;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            Vector3[] faceNormals = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down };
            foreach (Vector3 normal in faceNormals)
            {
                Vector3 up = Mathf.Abs(normal.y) > 0.5f ? Vector3.forward : Vector3.up;
                Vector3 right = Vector3.Cross(up, normal);
                Vector3 center = normal * 0.5f;
                right *= 0.5f;
                up *= 0.5f;

                int start = vertices.Count;
                vertices.Add(center - right - up);
                vertices.Add(center + right - up);
                vertices.Add(center - right + up);
                vertices.Add(center + right + up);
                for (int i = 0; i < 4; i++) normals.Add(normal);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 1f));

                triangles.AddRange(new[] { start, start + 1, start + 2, start + 2, start + 1, start + 3 });
            }

            fallbackBox = new Mesh { name = "FallbackBox" };
            fallbackBox.SetVertices(vertices);
            fallbackBox.SetNormals(normals);
            fallbackBox.SetUVs(0, uvs);
            fallbackBox.SetTriangles(triangles, 0);
            fallbackBox.RecalculateBounds();
            return fallbackBox;
        }
    }
}
