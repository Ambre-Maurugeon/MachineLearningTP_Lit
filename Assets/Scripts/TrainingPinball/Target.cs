using System;
using UnityEngine;


public class Target : MonoBehaviour {

    public enum TargetType {
        Trigger = 0, // Ball will fly through target. Bounce will not penalize episode duration for OneShot configuration
        Bumper = 1 // Ball will bounce on target. Harder for AI. Used for LongPlay configuration.
    }

    [NonSerialized] public Action onBallHit;
    public TargetType Type => collider.isTrigger ? TargetType.Trigger : TargetType.Bumper;

    new Collider2D collider;

    private void Awake() {
        collider = GetComponent<Collider2D>();
    }

    void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.tag != PinballConfiguration.BallTag) { return; }
        onBallHit?.Invoke();
    }

    void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.tag != PinballConfiguration.BallTag) { return; }
        onBallHit?.Invoke();
    }
}
