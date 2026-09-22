using Monopoly.Core;
using UnityEngine;

namespace Monopoly.Board
{
    /// <summary>
    /// Фон 3D-сцены: сетчатый пол, на котором лежит доска, свет и цвет неба.
    /// Пол специально крупный — при широком телефонном экране края не видны.
    /// </summary>
    public static class SceneEnvironment
    {
        private const float FloorSize = 120f;

        public static void Build(Transform parent)
        {
            BuildFloor(parent);
            BuildLighting(parent);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.47f, 0.58f);
            RenderSettings.fog = false;
        }

        private static void BuildFloor(Transform parent)
        {
            var material = new Material(FindShader("Unlit/Texture", "Legacy Shaders/Diffuse"));
            material.mainTexture = CreateGridTexture(128, Palette.FloorBase, Palette.FloorLine, 3);
            material.mainTextureScale = Vector2.one * (FloorSize / BoardLayout.CellSize);

            var floor = Primitives.Create(PrimitiveType.Plane, "Floor", parent, material);
            floor.transform.localScale = Vector3.one * (FloorSize / 10f);
            floor.transform.localPosition = new Vector3(0f, -0.02f, 0f);

            // Мягкое свечение под доской, чтобы она читалась как лежащий на столе объект.
            var glowMaterial = new Material(FindShader("Unlit/Color", "Legacy Shaders/Diffuse"));
            glowMaterial.color = new Color(0.10f, 0.16f, 0.24f);

            var glow = Primitives.Create(PrimitiveType.Quad, "BoardGlow", parent, glowMaterial);
            glow.transform.localPosition = new Vector3(0f, 0.005f, 0f);
            glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            glow.transform.localScale = Vector3.one * (BoardLayout.Side * 1.5f);
        }

        private static void BuildLighting(Transform parent)
        {
            var lightObject = new GameObject("KeyLight");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(52f, -34f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.89f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.45f;
        }

        private static Texture2D CreateGridTexture(int size, Color background, Color line, int thickness)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "GridTexture",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };
            var pixels = new Color32[size * size];
            Color32 backgroundPixel = background;
            Color32 linePixel = line;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool onLine = x < thickness || y < thickness;
                    pixels[y * size + x] = onLine ? linePixel : backgroundPixel;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        public static Shader FindShader(params string[] names)
        {
            foreach (string name in names)
            {
                var shader = Shader.Find(name);
                if (shader != null) return shader;
            }
            return Shader.Find("Standard");
        }

        public static Material LitMaterial(Color color)
        {
            var material = new Material(FindShader("Standard", "Legacy Shaders/Diffuse", "Unlit/Color"));
            material.color = color;
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.25f);
            return material;
        }
    }
}
