// * Usage: mostly for moving objects (projectiles) & controller to restrain/destroy/disable when is out of range
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System;

// TODO: integrate serializable Area
namespace Enginoobz {
  public class Boundary : MonoBehaviour {
    enum ActionOutOfBounds { Destroy, Disable, Constrain, Custom }
    [EnumToggleButtons] [SerializeField] private ActionOutOfBounds actionOutOfBounds = ActionOutOfBounds.Destroy;

    [InfoBox("Scale relative to movement speed to avoid jiggering effect")]
    [ShowIf(nameof(actionOutOfBounds), ActionOutOfBounds.Constrain)]
    [SerializeField] private Vector3 constrainForce = Vector3.one * 30;

    // CONSIDER: remove World-Space Mode, which is a particular/sub case of Target Mode (w/ pos = 0, 0, 0)
    #region WORLD-SPACE
    [TabGroup("World-Space")]
    [SerializeField] public bool enableWorldSpaceBoundary = true;
    [TabGroup("World-Space")]
    [MinMaxSlider(nameof(dynamicMinMax), true)]
    [SerializeField] public Vector2 xBoundary = Vector2.zero;
    [TabGroup("World-Space")]
    [MinMaxSlider(nameof(dynamicMinMax), true)]
    [SerializeField] public Vector2 yBoundary = Vector2.zero;
    [TabGroup("World-Space")]
    [MinMaxSlider(nameof(dynamicMinMax), true)]
    [SerializeField] public Vector2 zBoundary = Vector2.zero;
    [TabGroup("World-Space")]
    [SerializeField] public Vector2 dynamicMinMax = new Vector2(-100, 100);
    #endregion

    #region TARGET
    [TabGroup("Target")]
    [SerializeField] bool enableTargetBoundary = false;
    [TabGroup("Target")]
    [InlineButton(nameof(SetCameraTagTarget), "Camera")]
    [InlineButton(nameof(SetPlayerTagTarget), "Player")]
    [SerializeField] private string targetBoundaryTag = "Player";
    private void SetPlayerTagTarget() {
      targetBoundaryTag = "Player";
    }
    private void SetCameraTagTarget() {
      targetBoundaryTag = "MainCamera";
    }

    [TabGroup("Target")]
    [InlineButton(nameof(SetSelfAsTarget), "Self")]
    [ShowInInspector] private Transform targetBoundary;
    private void SetSelfAsTarget() {
      targetBoundary = transform;
    }
    [TabGroup("Target")]
    [MinMaxSlider(nameof(dynamicMinMaxTarget), true)]
    [SerializeField] private Vector2 xBoundaryTarget = Vector2.zero;
    [TabGroup("Target")]
    [MinMaxSlider(nameof(dynamicMinMaxTarget), true)]
    [SerializeField] private Vector2 yBoundaryTarget = Vector2.zero;
    [TabGroup("Target")]
    [MinMaxSlider(nameof(dynamicMinMaxTarget), true)]
    [SerializeField] private Vector2 zBoundaryTarget = Vector2.zero;
    [TabGroup("Target")]
    [SerializeField] private Vector2 dynamicMinMaxTarget = new Vector2(-100, 100);

    #endregion
    // [Header("Target Boundary")]
    // [Header("Self Boundary")]

    // TODO: emit OutOfRange event

    private void Start() {
      if (enableTargetBoundary) targetBoundary = GameObject.FindGameObjectWithTag(targetBoundaryTag).transform;
    }
    void Update() {
      if (OutOfRange()) HandleOutOfRange();
    }

    private void HandleOutOfRange() {
      switch (actionOutOfBounds) {
        case ActionOutOfBounds.Destroy:
          Destroy(gameObject);
          break;
        case ActionOutOfBounds.Constrain:
          ConstrainGameObject();
          break;
      }
    }

    // FIX: Constrain method causes jiggering/not effect on moving object by setting transform.position directly (e.g., Follower)
    private void ConstrainGameObject() {
      Vector3 pos = transform.position;
      if (!xBoundary.ContainsIgnoreZero(pos.x))
        this.MoveXWorld((xBoundary.Clamp(pos.x) - pos.x) * constrainForce.x);
      if (!yBoundary.ContainsIgnoreZero(pos.y))
        this.MoveYWorld((yBoundary.Clamp(pos.y) - pos.y) * constrainForce.y);
      if (!zBoundary.ContainsIgnoreZero(pos.z))
        this.MoveZWorld((zBoundary.Clamp(pos.z) - pos.z) * constrainForce.z);
    }

    private bool OutOfRange() {
      if (enableWorldSpaceBoundary && OutOfRangeWorldSpace()) return true;
      if (enableTargetBoundary && targetBoundary && OutOfRangeTarget()) return true;
      return false;
    }

    private bool OutOfRangeWorldSpace() {
      Vector3 pos = transform.position;
      return false // just for code formatting
      || !xBoundary.ContainsIgnoreZero(pos.x)
      || !yBoundary.ContainsIgnoreZero(pos.y)
      || !zBoundary.ContainsIgnoreZero(pos.z)
      ;
    }

    private bool OutOfRangeTarget() {
      Vector3 pos = transform.position;
      Vector3 targetPos = targetBoundary.transform.position;
      Vector3 diffPos = pos - targetPos;
      // return targetPos.x > pos.x + 25 - xBoundaryTarget.x; // width
      // Do not count width of this gameobject
      return false
      || !xBoundaryTarget.ContainsIgnoreZero(diffPos.x)
      || !yBoundaryTarget.ContainsIgnoreZero(diffPos.y)
      || !zBoundaryTarget.ContainsIgnoreZero(diffPos.z)
      ;
    }

    // UTIL
    private bool OutOfRangeOneAxis(Vector2 range, float currentValue) {
      return range != Vector2.zero && (currentValue < range.x || range.y < currentValue);
    }

    void OnDrawGizmosSelected() {
      if (enableWorldSpaceBoundary) DrawWorldSpaceBoundary();
      if (enableTargetBoundary) DrawTargetBoundary();
    }
    private void DrawWorldSpaceBoundary() {
      Vector3 pos = transform.position;
      Gizmos.color = Color.yellow;
      Vector3 boundaryCenter = new Vector3(xBoundary.Average(), yBoundary.Average(), zBoundary.Average());
      if (xBoundary == Vector2.zero) boundaryCenter.x = pos.x;
      if (yBoundary == Vector2.zero) boundaryCenter.y = pos.y;
      if (zBoundary == Vector2.zero) boundaryCenter.z = pos.z;
      Vector3 boundarySize = new Vector3(xBoundary.Length(), yBoundary.Length(), zBoundary.Length());
      Gizmos.DrawWireCube(boundaryCenter, boundarySize);
    }

    private void DrawTargetBoundary() {
      if (!targetBoundary) return;
      Vector3 targetPos = targetBoundary.transform.position;
      Gizmos.color = Color.magenta;

      Gizmos.DrawSphere(transform.position, 1f);
      Vector3 boundaryCenter = targetPos + new Vector3(xBoundaryTarget.Average(), yBoundaryTarget.Average(), zBoundaryTarget.Average());
      if (xBoundaryTarget == Vector2.zero) boundaryCenter.x = targetPos.x;
      if (yBoundaryTarget == Vector2.zero) boundaryCenter.y = targetPos.y;
      if (zBoundaryTarget == Vector2.zero) boundaryCenter.z = targetPos.z;
      Vector3 boundarySize = new Vector3(xBoundaryTarget.Length(), yBoundaryTarget.Length(), zBoundaryTarget.Length());
      Gizmos.DrawWireCube(boundaryCenter, boundarySize);
    }
  }
}