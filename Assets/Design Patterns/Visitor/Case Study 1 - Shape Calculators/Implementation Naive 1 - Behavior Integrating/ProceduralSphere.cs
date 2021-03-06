using Sirenix.OdinInspector;
using UnityEngine;
using static UnityEngine.Mathf;

namespace VisitorPattern.Case1.Naive1 {
  public class ProceduralSphere : ProceduralShape {
    #region CALCULATION-RELATED =======================================================================================================================================================================
    public override double CalculateSurfaceArea() => 4 * PI * Pow(Radius, 2);

    public override double CalculateVolume() => 4 / 3 * PI * Pow(Radius, 3);
    #endregion CALCULATION-RELATED ====================================================================================================================================================================

    #region PROCEDURAL-RELATED =======================================================================================================================================================================
    [SerializeField, OnValueChanged(nameof(CreateMesh)), Range(1f, 5f)]
    private float _radius;

    public float Radius => _radius;

    private int _horizontalSegments = 16;
    private int _verticalSegments = 16;

    protected override void CreateMeshData() {
      float horizontalSegmentAngle = 360f / _horizontalSegments;
      float verticalSegmentAngle = 180f / _verticalSegments;
      float currentVerticalAngle = -90;

      for (int y = 0; y <= _verticalSegments; y++) {
        float currentHorizontalAngle = 0f;
        for (int x = 0; x <= _horizontalSegments; x++) {
          Vector3 point = PointOnSpheroid(_radius, _radius, currentHorizontalAngle, currentVerticalAngle);
          vertices.Add(point);
          normals.Add(point.normalized);
          uv.Add(new Vector2((float)x / _horizontalSegments, (float)y / _verticalSegments));
          currentHorizontalAngle -= horizontalSegmentAngle;
        }
        currentVerticalAngle += verticalSegmentAngle;
      }

      // Extra vertices due to the uvmap seam
      int horizontalCount = _horizontalSegments + 1;
      for (int ring = 0; ring < _verticalSegments; ring++) {
        for (int i = 0; i < horizontalCount - 1; i++) {
          int i0 = ring * horizontalCount + i;
          int i1 = (ring + 1) * horizontalCount + i;
          int i2 = ring * horizontalCount + i + 1;
          int i3 = (ring + 1) * horizontalCount + i + 1;

          triangles.Add(i0);
          triangles.Add(i1);
          triangles.Add(i2);
          triangles.Add(i2);
          triangles.Add(i1);
          triangles.Add(i3);
        }
      }
    }

    private static Vector3 PointOnSpheroid(float radius, float height, float horizontalAngle, float verticalAngle) {
      float horizontalRadians = horizontalAngle * Mathf.Deg2Rad;
      float verticalRadians = verticalAngle * Mathf.Deg2Rad;
      float cosVertical = Mathf.Cos(verticalRadians);

      return new Vector3(
          x: radius * Mathf.Sin(horizontalRadians) * cosVertical,
          y: height * Mathf.Sin(verticalRadians),
          z: radius * Mathf.Cos(horizontalRadians) * cosVertical);
    }
    #endregion PROCEDURAL-RELATED ====================================================================================================================================================================
  }
}
