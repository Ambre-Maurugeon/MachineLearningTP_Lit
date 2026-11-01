using System;
using UnityEngine;

public class BoundTrigger : MonoBehaviour {
    public enum BoundType {
        Drain = 0,
        ResetZone = 1,
        OtherSide = 2, // Other side of the flipper. Not necessarly a bad place to land.
    }

    [field: SerializeField] public BoundType Type { get; private set; }

    [NonSerialized] public Action<BoundType> onBallEnter;

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.tag != PinballConfiguration.BallTag) { return; }

        onBallEnter?.Invoke(Type);
    }
}
