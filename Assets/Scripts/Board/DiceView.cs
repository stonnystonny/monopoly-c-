using System.Collections;
using Monopoly.Core;
using UnityEngine;

namespace Monopoly.Board
{
    /// <summary>
    /// Два кубика, которые падают на доску и останавливаются нужной гранью вверх.
    /// Значения приходят из игровой логики, анимация лишь показывает уже выпавший бросок —
    /// физического движка в проекте нет, поэтому падение и кувыркание считаются вручную.
    /// </summary>
    public class DiceView : MonoBehaviour
    {
        private const float DieSize = 0.55f;
        private const float DropHeight = 3.2f;
        private const float ThrowDuration = 1.15f;
        private const float TravelPhase = 0.62f;
        private const float SettlePhase = 0.72f;
        private const float SpinSpeed = 720f;

        private readonly Transform[] dice = new Transform[2];
        private readonly Vector3[] restPositions = new Vector3[2];

        public void Build(Transform parent)
        {
            for (int i = 0; i < dice.Length; i++)
            {
                restPositions[i] = BoardLayout.DiceRestPosition(i) + Vector3.up * (DieSize * 0.5f);
                dice[i] = CreateDie(parent, i);
                dice[i].position = restPositions[i];
                dice[i].rotation = FaceUpRotation(i + 1);
            }
        }

        /// <summary>Бросок: кубики падают на доску и замирают гранями с выпавшими числами.</summary>
        public IEnumerator Throw(int valueOne, int valueTwo)
        {
            int[] values = { valueOne, valueTwo };
            var start = new Vector3[2];
            var spinAxis = new Vector3[2];
            var target = new Quaternion[2];
            var initial = new Quaternion[2];
            var beforeSettle = new Quaternion[2];
            var snapshotTaken = new bool[2];

            for (int i = 0; i < dice.Length; i++)
            {
                start[i] = restPositions[i] + new Vector3(
                    Random.Range(-0.5f, 0.5f), DropHeight, Random.Range(-1.1f, -0.4f));
                spinAxis[i] = Random.onUnitSphere;
                initial[i] = Random.rotation;
                target[i] = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * FaceUpRotation(values[i]);
                dice[i].position = start[i];
                dice[i].rotation = initial[i];
            }

            float elapsed = 0f;
            while (elapsed < ThrowDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / ThrowDuration);

                for (int i = 0; i < dice.Length; i++)
                {
                    float travel = Mathf.Clamp01(t / TravelPhase);
                    travel = 1f - (1f - travel) * (1f - travel);
                    Vector3 position = Vector3.Lerp(start[i], restPositions[i], travel);
                    position.y = restPositions[i].y + HeightOffset(t);
                    dice[i].position = position;

                    if (t < SettlePhase)
                    {
                        dice[i].rotation = Quaternion.AngleAxis(SpinSpeed * elapsed, spinAxis[i]) * initial[i];
                    }
                    else
                    {
                        if (!snapshotTaken[i])
                        {
                            beforeSettle[i] = dice[i].rotation;
                            snapshotTaken[i] = true;
                        }
                        float k = (t - SettlePhase) / (1f - SettlePhase);
                        dice[i].rotation = Quaternion.Slerp(beforeSettle[i], target[i], Mathf.SmoothStep(0f, 1f, k));
                    }
                }

                yield return null;
            }

            for (int i = 0; i < dice.Length; i++)
            {
                dice[i].position = restPositions[i];
                dice[i].rotation = target[i];
            }
        }

        /// <summary>Высота над точкой покоя: падение, отскок и мягкая посадка.</summary>
        private static float HeightOffset(float t)
        {
            const float hover = 0.22f;
            if (t < TravelPhase)
            {
                float k = t / TravelPhase;
                return DropHeight * (1f - k * k) + hover;
            }
            if (t < 0.9f)
            {
                float k = (t - TravelPhase) / (0.9f - TravelPhase);
                return DropHeight * 0.14f * (4f * k * (1f - k)) + hover;
            }
            return Mathf.Lerp(hover, 0f, (t - 0.9f) / 0.1f);
        }

        /// <summary>Поворот, при котором нужная грань смотрит вверх. Противоположные грани дают в сумме 7.</summary>
        private static Quaternion FaceUpRotation(int value)
        {
            switch (value)
            {
                case 1: return Quaternion.identity;
                case 2: return Quaternion.Euler(90f, 0f, 0f);
                case 3: return Quaternion.Euler(0f, 0f, 90f);
                case 4: return Quaternion.Euler(0f, 0f, -90f);
                case 5: return Quaternion.Euler(-90f, 0f, 0f);
                default: return Quaternion.Euler(180f, 0f, 0f);
            }
        }

        // ---------------------------------------------------------------- сборка кубика

        private static Transform CreateDie(Transform parent, int index)
        {
            var root = new GameObject("Die" + (index + 1));
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * DieSize;

            var body = Primitives.Create(PrimitiveType.Cube, "Body", root.transform,
                SceneEnvironment.LitMaterial(new Color(0.96f, 0.95f, 0.91f)));
            body.transform.localScale = Vector3.one;

            var pipMaterial = SceneEnvironment.LitMaterial(new Color(0.10f, 0.11f, 0.13f));
            BuildPips(root.transform, pipMaterial);
            return root.transform;
        }

        private static void BuildPips(Transform die, Material material)
        {
            Vector3[] normals = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            int[] faceValues = { 1, 6, 3, 4, 5, 2 };

            for (int face = 0; face < normals.Length; face++)
            {
                Vector3 normal = normals[face];
                Vector3 u = Mathf.Abs(normal.y) > 0.5f ? Vector3.forward : Vector3.up;
                Vector3 v = Vector3.Cross(normal, u);

                foreach (Vector2 spot in PipPattern(faceValues[face]))
                {
                    var pip = Primitives.Create(PrimitiveType.Sphere, "Pip", die, material);
                    pip.transform.localScale = Vector3.one * 0.15f;
                    pip.transform.localPosition = normal * 0.48f + u * (spot.y * 0.24f) + v * (spot.x * 0.24f);
                }
            }
        }

        /// <summary>Расположение точек на грани в клетках 3x3.</summary>
        private static Vector2[] PipPattern(int value)
        {
            switch (value)
            {
                case 1: return new[] { Vector2.zero };
                case 2: return new[] { new Vector2(-1f, -1f), new Vector2(1f, 1f) };
                case 3: return new[] { new Vector2(-1f, -1f), Vector2.zero, new Vector2(1f, 1f) };
                case 4: return new[] { new Vector2(-1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(1f, 1f) };
                case 5: return new[] { new Vector2(-1f, -1f), new Vector2(-1f, 1f), Vector2.zero, new Vector2(1f, -1f), new Vector2(1f, 1f) };
                default: return new[]
                {
                    new Vector2(-1f, -1f), new Vector2(-1f, 0f), new Vector2(-1f, 1f),
                    new Vector2(1f, -1f), new Vector2(1f, 0f), new Vector2(1f, 1f)
                };
            }
        }
    }
}
