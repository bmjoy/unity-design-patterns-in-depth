using UnityEngine;

public abstract class Attacker : MonoBehaviour {
  public abstract void Attack(Attackable target);

  public void Attack(Reference targetRef) {
    if (targetRef.GameObject.TryGetComponent<Attackable>(out Attackable target)) {
      Attack(target);
    }
  }
}