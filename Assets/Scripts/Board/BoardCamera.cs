using Monopoly.Core;
using UnityEngine;

namespace Monopoly.Board
{
    /// <summary>
    /// Держит камеру так, чтобы лежащая доска целиком помещалась в кадр и стояла ровно по центру экрана.
    /// Боковые поля зарезервированы под HUD телефона в горизонтальной ориентации.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BoardCamera : MonoBehaviour
    {
        [SerializeField] private float tilt = 56f;
        [SerializeField] private float yaw = 0f;
        [SerializeField] private float fieldOfView = 52f;

        /// <summary>Поля по краям экрана, чтобы доска не упиралась в границы кадра.</summary>
        [SerializeField] private float horizontalMargin = 0.04f;
        [SerializeField] private float verticalMargin = 0.04f;

        private Camera view;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            view = GetComponent<Camera>();
            view.orthographic = false;
            view.fieldOfView = fieldOfView;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Palette.Sky;
            view.nearClipPlane = 0.3f;
            view.farClipPlane = 300f;
            RestoreViewport();
        }

        /// <summary>Камера должна занимать весь экран: при нулевом viewport 3D-сцена не рисуется вовсе.</summary>
        private void RestoreViewport()
        {
            var full = new Rect(0f, 0f, 1f, 1f);
            if (view.rect != full) view.rect = full;
            if (view.targetTexture != null) view.targetTexture = null;
        }

        private void Start()
        {
            Frame();
        }

        private void LateUpdate()
        {
            if (Screen.width == lastWidth && Screen.height == lastHeight) return;
            Frame();
        }

        /// <summary>Подбирает дистанцию так, чтобы углы доски попали в рабочую область экрана.</summary>
        public void Frame()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            RestoreViewport();

            var rotation = Quaternion.Euler(tilt, yaw, 0f);
            Vector3 direction = rotation * Vector3.forward;
            transform.rotation = rotation;

            float halfX = Mathf.Max(0.05f, 0.5f - horizontalMargin);
            float halfY = Mathf.Max(0.05f, 0.5f - verticalMargin);
            float distance = BoardLayout.Side * 1.6f;

            for (int iteration = 0; iteration < 12; iteration++)
            {
                transform.position = -direction * distance;

                float worst = 0f;
                foreach (Vector3 corner in BoardCorners())
                {
                    Vector3 viewport = view.WorldToViewportPoint(corner);
                    if (viewport.z <= 0.05f)
                    {
                        worst = 3f;
                        break;
                    }
                    worst = Mathf.Max(worst, Mathf.Abs(viewport.x - 0.5f) / halfX);
                    worst = Mathf.Max(worst, Mathf.Abs(viewport.y - 0.5f) / halfY);
                }

                if (float.IsNaN(worst) || float.IsInfinity(worst))
                {
                    distance = BoardLayout.Side * 1.6f;
                    break;
                }
                if (Mathf.Abs(worst - 1f) < 0.004f) break;
                distance = Mathf.Clamp(distance * Mathf.Max(worst, 0.05f), BoardLayout.Side * 0.6f, BoardLayout.Side * 12f);
            }

            transform.position = -direction * distance;
        }

        private static Vector3[] BoardCorners()
        {
            float half = BoardLayout.Side * 0.5f;
            const float top = BoardLayout.TileSurface + 0.75f;
            return new[]
            {
                new Vector3(-half, 0f, -half), new Vector3(half, 0f, -half),
                new Vector3(-half, 0f, half), new Vector3(half, 0f, half),
                new Vector3(-half, top, -half), new Vector3(half, top, -half),
                new Vector3(-half, top, half), new Vector3(half, top, half)
            };
        }
    }
}
