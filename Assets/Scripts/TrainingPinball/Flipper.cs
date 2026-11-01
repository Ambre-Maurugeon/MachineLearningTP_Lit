using System;
using UnityEngine;



public class Flipper : MonoBehaviour {

    public enum FlipperSide {
        Left = 0,
        Right = 1
    }

    // Owner
    TrainingPinballArea pinball;

    // Collision callbacks
    [NonSerialized] public Action<FlipperSide> onBallEnter;
    [NonSerialized] public Action<FlipperSide> onBallStay;
    [NonSerialized] public Action<FlipperSide> onBallExit;

    // Input and Angle 
    public float InitialAngle { get; private set; }
    public float CurrentAngleOffset { get; private set; }
    public float FinalAngle => CurrentAngleOffset * SideFactor + InitialAngle;
    bool isPressed = false;

    // Usefull accessors
    public float SideFactor => (Side == FlipperSide.Left) ? 1f : -1f;
    public Vector2 WorldToLocalVector(Vector2 vector) => Quaternion.Inverse(Quaternion.Euler(0, 0, InitialAngle)) * vector;
    [field: SerializeField] public FlipperSide Side { get; private set; } = FlipperSide.Left;

    // Physics
    Rigidbody2D rb;

    #region Public methods

    public void Initialize(TrainingPinballArea pinball) {
        this.pinball = pinball;
    }

    public void SetState(bool isPressed) {
        this.isPressed = isPressed;
    }

    public void Clear() {
        isPressed = false;
        CurrentAngleOffset = 0f;
        rb.SetRotation(InitialAngle);
    }

    #endregion // Public methods

    #region Private methods

    void Awake() {
        InitialAngle = transform.rotation.eulerAngles.z;
        rb = GetComponent<Rigidbody2D>();
    }


    void FixedUpdate() {
        if (isPressed) {
            CurrentAngleOffset = Mathf.Min(CurrentAngleOffset + pinball.Configuration.FlipperAngularSpeed * Time.deltaTime, pinball.Configuration.FlipperAngleOffsetMax);
            rb.MoveRotation(FinalAngle);
        } else if (CurrentAngleOffset > 0) {
            CurrentAngleOffset = Mathf.Max(CurrentAngleOffset - pinball.Configuration.FlipperAngularSpeed * Time.deltaTime, 0f);
            rb.MoveRotation(FinalAngle);
        }
    }

    void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.tag != PinballConfiguration.BallTag) { return; }
        onBallEnter?.Invoke(Side);
    }

    void OnCollisionStay2D(Collision2D collision) {
        if (collision.gameObject.tag != PinballConfiguration.BallTag) { return; }
        onBallStay?.Invoke(Side);
    }

    void OnCollisionExit2D(Collision2D collision) {
        if (collision.gameObject.tag != PinballConfiguration.BallTag) { return; }
        onBallExit?.Invoke(Side);
    }

    #endregion // Private methods

}
