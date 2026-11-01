using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TrainingPinballArea : MonoBehaviour {


    public enum EpisodeType {
        OneShot = 0, // An episode ends when ball hits the target or any Bound (Drain, Reset, Other Side), or after configured episode duration.
        LongPlay = 1 // An episode end after configured episode duration. Ball position is reseted to a spawner if ball ends in a drain. 
    }


    [field: SerializeField] public PinballConfiguration Configuration { get; private set; }

    [Header("Episode")]
    public EpisodeType episodeType = EpisodeType.OneShot;
    [SerializeField] float episodeDuration = 10f;

    [Header("Prefabs")]
    [SerializeField] Rigidbody2D ballPrefab;
    [SerializeField] Target targetPrefab;

    // Instanciated prefabs
    public Rigidbody2D Ball { get; private set; }
    public Target Target { get; private set; }

    // Spawners used to relocate ball and target (we never destroy them)
    List<BallSpawner> ballSpawners;
    List<TargetSpawner> targetSpawners;


    // Flippers (using FlipperSide enum for index)
    Flipper[] flippers = new Flipper[2];
    public Flipper LeftFlipper => flippers[0];
    public Flipper RightFlipper => flippers[1];
    public Flipper Flipper(Flipper.FlipperSide side) => flippers[(int)side];

    // Controls (using FlipperSide enum for index)
    bool[] isButtonsPressed = new bool[2];
    public bool IsLeftButtonPressed => isButtonsPressed[0];
    public bool IsRightButtonPressed => isButtonsPressed[1];
    public bool IsButtonPressed(Flipper.FlipperSide side) => isButtonsPressed[(int)side];

    // Bounds
    List<BoundTrigger> boundTriggers;

    // Callbacks
    public Action<BoundTrigger.BoundType> onBallEnterBound;
    public Action onEpisodeEnd;
    public Action onBallHitTarget;

    float episodeBeginTimestamp = 0f;

    // long play episode target hits
    int targetHitsThisEpisode = 0;
    public int TargetHitsThisEpisode => targetHitsThisEpisode;

    #region Public methods

    public void BeginEpisode() {
        episodeBeginTimestamp = Time.fixedTime;
        RelocateBall();
        RelocateTarget();
        foreach (Flipper flipper in flippers) {
            if (flipper == null) { continue; }
            flipper.Clear();
        }
        isButtonsPressed[0] = false;
        isButtonsPressed[1] = false;

        targetHitsThisEpisode = 0;
    }

    public void PlayNextEpisode() {
        onEpisodeEnd?.Invoke();
        BeginEpisode();
    }

    public void RelocateBall() {
        if (ballSpawners.Count <= 0) { return; }

        int spawnerIndex = Random.Range(0, ballSpawners.Count);
        Ball.transform.position = ballSpawners[spawnerIndex].RandomPosition;
        Ball.linearVelocity = ballSpawners[spawnerIndex].RandomVelocity;
        Ball.angularVelocity = 0f;
    }

    public void RelocateTarget() {
        if (targetSpawners.Count <= 0) { return; }

        int spawnerIndex = Random.Range(0, targetSpawners.Count);
        Target.transform.position = targetSpawners[spawnerIndex].RandomPosition;
    }

    public void SetButtonState(bool isPressed, Flipper.FlipperSide side) {
        isButtonsPressed[(int)side] = isPressed;
        flippers[(int)side]?.SetState(isPressed);
    }

    #endregion // Public methods

    #region Private methods

    void Awake() {
        boundTriggers = new List<BoundTrigger>(GetComponentsInChildren<BoundTrigger>());
        ballSpawners = new List<BallSpawner>(GetComponentsInChildren<BallSpawner>());
        targetSpawners = new List<TargetSpawner>(GetComponentsInChildren<TargetSpawner>());
        Flipper[] foundFlippers = GetComponentsInChildren<Flipper>();

        foreach (Flipper flipper in foundFlippers) {
            flippers[(int)flipper.Side] = flipper;
            flipper.Initialize(this);
        }

        foreach (BoundTrigger boundTrigger in boundTriggers) {
            boundTrigger.onBallEnter += HandleOnBallEnterBound;
        }

        Ball = GameObject.Instantiate(ballPrefab);
        Ball.gravityScale = Configuration.GetGravityScale();
        Ball.transform.localScale = Vector3.one * Configuration.GetBallScale();

        Target = GameObject.Instantiate(targetPrefab);

        Target.onBallHit += HandleOnBallHitTarget;
    }

    void OnDestroy() {
        foreach (BoundTrigger boundTrigger in boundTriggers) {
            boundTrigger.onBallEnter -= HandleOnBallEnterBound;
        }
    }

    void Start() {
        BeginEpisode();
    }

    void FixedUpdate() {
        flippers[0]?.SetState(isButtonsPressed[0]);
        flippers[1]?.SetState(isButtonsPressed[1]);

        if (episodeDuration > 0 && Time.fixedTime - episodeBeginTimestamp >= episodeDuration) {
            PlayNextEpisode();
        }
    }

    void HandleOnBallEnterBound(BoundTrigger.BoundType boundType) {
        onBallEnterBound?.Invoke(boundType);
        if (episodeType == EpisodeType.OneShot) {
            PlayNextEpisode();
        } else {
            RelocateBall();
        }
    }

    void HandleOnBallHitTarget() {
        targetHitsThisEpisode++;

        onBallHitTarget?.Invoke();
        if (episodeType == EpisodeType.OneShot) {
            PlayNextEpisode();
        } else {
            RelocateTarget();
        }
    }

    #endregion // Private methods

}
