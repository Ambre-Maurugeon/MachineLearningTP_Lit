using UnityEngine;

public class BallSpawner : Spawner {
    [SerializeField] Vector2 randomVelocityMin = Vector2.zero;
    [SerializeField] Vector2 randomVelocityMax = Vector2.one;

    public Vector2 RandomVelocity => new Vector2(Random.Range(randomVelocityMin.x, randomVelocityMax.x), Random.Range(randomVelocityMin.y, randomVelocityMax.y));
}
