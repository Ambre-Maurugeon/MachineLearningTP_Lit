using UnityEngine;

[CreateAssetMenu(fileName = "PinballConfiguration", menuName = "Scriptable Objects/PinballConfiguration")]
public class PinballConfiguration : ScriptableObject {
    [Header("General Configuration")]
    public static readonly string BallTag = "Ball";
    [field: SerializeField] public Vector2 AveragePinballDimension { get; private set; } = new Vector2(53f, 110f); // Average pinball dimension in centimeters. Used to normalize the observations of the agent.
    [field: SerializeField] public float CentimeterPerUnit { get; private set; } = 1f; // 1 cm per 1 Unity unit.
    [field: SerializeField, Range(4f, 11f)] public float SurfaceAngle { get; private set; } = 6f; // Around 7f for a standard pinball surface. Used to calculate gravity scale. Lower values to have an enjoyable simulation.

    [Header("Flippers Configuration")]
    [field: SerializeField, Range(700f, 1200f)] public float FlipperAngularSpeed { get; private set; } = 1000f; // Around 1050f for a standard pinball flipper but it feels too fast for Unity. 1000f seems to give a good simulation.
    [field: SerializeField, Range(40f, 70f)] public float FlipperAngleOffsetMax { get; private set; } = 60f; // Around 50 to 60 for a standard pinball flipper. Higher value to have more control over the ball.

    [Header("Ball Configuration")]
    [field: SerializeField, Range(1.5f, 3.5f)] public float BallDiameter { get; private set; } = 2.6f; // Around 2.6 cm diameter for a standard pinball ball

    public float GetGravityScale() {
        return Mathf.Sin(SurfaceAngle * Mathf.Deg2Rad) * 100f / CentimeterPerUnit;
    }

    public float GetBallScale() {
        return BallDiameter / CentimeterPerUnit;
    }
}
