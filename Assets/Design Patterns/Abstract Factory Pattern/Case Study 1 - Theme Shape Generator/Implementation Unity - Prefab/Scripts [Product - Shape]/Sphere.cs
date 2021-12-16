using UnityEngine;

namespace AbstractFactoryPattern.Case1.Unity.Prefab {
  /// <summary>
  /// * [A 'Abstract Product']
  /// </summary>
  public abstract class Sphere : MonoBehaviour {
    protected float _radius;
    public void SetPos(Vector3 pos) => transform.position = pos;
    public float GetDiameter() => _radius * 2;
    private void Start() => transform.SetScale(_radius);
  }
}
