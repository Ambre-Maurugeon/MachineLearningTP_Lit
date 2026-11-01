using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static Flipper;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

public class FlipperAgent : Agent {

    static int NextAgentId = 0;

    [SerializeField] TrainingPinballArea trainingPinball;
    [SerializeField] Flipper.FlipperSide side = Flipper.FlipperSide.Left;

    int id = 0;
    int episodeId = 0;


    PlayerController playerController;


    float buttonPressedTimestamp = 0f;
    float ballInContactTimestamp = 0f;
    float ballEnterFlipperTimestamp = 0f;
    float ballEnterOtherFlipperTimestamp = 0f;
    float episodeBeginTimestamp = 0f;

    // min target distance for relative reward
    float minTargetDistance = float.MaxValue;


    bool BallInContact => Time.fixedTime <= ballInContactTimestamp + Time.fixedDeltaTime * 3; // Solves jittering at low speed. We allow 3 fixed update of before counting a new collision as an "OnCollisionEnter"    

    void Awake() {
        playerController = new PlayerController();
        playerController.Enable();
        id = NextAgentId++;
    }

    void Start() {
        trainingPinball.Flipper(side).onBallEnter += HandleOnBallEnterFlipper;
        trainingPinball.Flipper(side).onBallStay += HandleOnBallInContactWithFlipper;
        trainingPinball.Flipper(side).onBallExit += HandleOnBallExitFlipper;
        if (trainingPinball.LeftFlipper) {
            trainingPinball.LeftFlipper.onBallEnter += HandleOnBallEnterAnyFlipper;
        }
        if (trainingPinball.RightFlipper) {
            trainingPinball.RightFlipper.onBallEnter += HandleOnBallEnterAnyFlipper;
        }

        trainingPinball.onBallEnterBound += HandleOnBallEnterBound;
        trainingPinball.onBallHitTarget += HandleOnBallHitTarget;
        trainingPinball.onEpisodeEnd += HandleOnEpisodeEnd;
    }

    private void OnDestroy() {
        trainingPinball.Flipper(side).onBallEnter -= HandleOnBallEnterFlipper;
        trainingPinball.Flipper(side).onBallStay -= HandleOnBallInContactWithFlipper;
        trainingPinball.Flipper(side).onBallExit -= HandleOnBallExitFlipper;
        if (trainingPinball.LeftFlipper) {
            trainingPinball.LeftFlipper.onBallEnter -= HandleOnBallEnterAnyFlipper;
        }
        if (trainingPinball.RightFlipper) {
            trainingPinball.RightFlipper.onBallEnter -= HandleOnBallEnterAnyFlipper;
        }

        trainingPinball.onBallEnterBound -= HandleOnBallEnterBound;
        trainingPinball.onBallHitTarget -= HandleOnBallHitTarget;
        trainingPinball.onEpisodeEnd -= HandleOnEpisodeEnd;
    }

    private void FixedUpdate() {

        // Calculate min target distance to give relative reward value
        EvaluateMinTargetDistance();

    }

    public override void OnEpisodeBegin() {
        trainingPinball.BeginEpisode();
        buttonPressedTimestamp = 0f;
        ballInContactTimestamp = 0f;
        ballEnterFlipperTimestamp = 0f;
        ballEnterOtherFlipperTimestamp = 0;
        episodeBeginTimestamp = Time.fixedTime;


        // Evaluate min target distance
        minTargetDistance = float.MaxValue;

    }

    public override void CollectObservations(VectorSensor sensor) {

        // observation where the ball is relative to the flipper
        Vector2 flipperPos = trainingPinball.Flipper(side).transform.position;
        Vector2 v = (trainingPinball.Ball.position - flipperPos);

        v /= trainingPinball.Configuration.AveragePinballDimension; // relative to the pinball scale
        Vector2 localV = trainingPinball.Flipper(side).WorldToLocalVector(v);
        sensor.AddObservation(localV);

        // Velocity of the ball
        Vector2 ballVelocity = trainingPinball.Ball.linearVelocity;
        ballVelocity /= trainingPinball.Configuration.AveragePinballDimension; // relative to the pinball scale

        sensor.AddObservation(ballVelocity.x);
        sensor.AddObservation(ballVelocity.y);

        // Target position relative to the flipper
        Vector2 targetRelativePos = trainingPinball.Target.transform.position - trainingPinball.Flipper(side).transform.position;
        targetRelativePos /= trainingPinball.Configuration.AveragePinballDimension; // relative to the pinball scale
        targetRelativePos = trainingPinball.Flipper(side).WorldToLocalVector(targetRelativePos); 

        sensor.AddObservation(targetRelativePos.x);
        sensor.AddObservation(targetRelativePos.y);


    }

    public override void OnActionReceived(ActionBuffers actions) {
        bool isPressed = actions.DiscreteActions[0] > 0;

        if (!trainingPinball.IsButtonPressed(side) && isPressed) {
            buttonPressedTimestamp = Time.fixedTime;

        }

        trainingPinball.SetButtonState(isPressed, side);
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        var discreteActionsOut = actionsOut.DiscreteActions;
        bool isPressed = side == Flipper.FlipperSide.Left ? playerController.Player.LeftFlipper.IsPressed() : playerController.Player.RightFlipper.IsPressed();
        discreteActionsOut[0] = isPressed ? 1 : 0;
    }

    void HandleOnEpisodeEnd() {
        // add relative distance reward 
        float maxDistance = 20; // max distance that is visually bad
        float normalizedDistance = Mathf.Round(Mathf.Clamp01(minTargetDistance / maxDistance)*100)/100; // .00f

        if (normalizedDistance > 0)
            DiagnosticSystem.AddRelativeReward(this,REWARDS_TYPE.MIN_TARGET_DISTANCE, normalizedDistance);

        
        // -- diagnostic --
        //longplay
        if (trainingPinball.episodeType == TrainingPinballArea.EpisodeType.LongPlay)
        {
            if (side == FlipperSide.Right) // only one flipper is in charge of register score of its pinball area
            {
                DiagnosticSystem.RegisterTargetHits(trainingPinball.TargetHitsThisEpisode); // print diag for pinball 
            }
        }
        // one shot
        else
        {
            DiagnosticSystem.RegisterEpisodeEnd(this, GetCumulativeReward()); // also print diagnostic agent
        }

        // end
        EndEpisode();
        episodeId++;

    }



    void HandleOnBallEnterFlipper(FlipperSide side) {
        // Solves jittering at low speed. We allow 3 fixed update of before counting a new collision as an "OnCollisionEnter"
        if (!BallInContact) {
            ballEnterFlipperTimestamp = Time.fixedTime;
            //Debug.Log("HandleOnBallEnterFlipper");
        }

        HandleOnBallInContactWithFlipper(side);
    }

    void HandleOnBallInContactWithFlipper(FlipperSide side) {
        ballInContactTimestamp = Time.fixedTime;

    }

    void HandleOnBallExitFlipper(FlipperSide side) {
        // Do not use. May be called multiple times due to jittering
    }

    void HandleOnBallHitTarget() {
        // add reward when hits target
        DiagnosticSystem.AddReward(this, REWARDS_TYPE.TARGET);

        // distance reward
        minTargetDistance = 0; // avoid weird distance reward
    }

    void HandleOnBallEnterAnyFlipper(FlipperSide side) {
        if (side != this.side) {
            ballEnterOtherFlipperTimestamp = Time.fixedTime;
        }

    }

    void HandleOnBallEnterBound(BoundTrigger.BoundType type) 
    {
        switch (type)
        {
            case BoundTrigger.BoundType.Drain:
                DiagnosticSystem.AddReward(this, REWARDS_TYPE.DRAIN);
                break;
        }

    }

    // reward distance
    void EvaluateMinTargetDistance()
    {
        // Calculate Min distance from target per episode

        Collider2D ballCollider = trainingPinball.Ball.GetComponent<Collider2D>();
        Collider2D targetCollider = trainingPinball.Target.GetComponent<Collider2D>();

        if (ballCollider == null || targetCollider == null)
            return;

        Vector2 ballPosition = trainingPinball.Ball.position;
        Vector2 closestPoint = targetCollider.ClosestPoint(ballPosition);

        float centerToTargetDistance = Vector2.Distance(ballPosition, closestPoint);

        float ballRadius = Mathf.Max(ballCollider.bounds.extents.x,ballCollider.bounds.extents.y);

        float distance = Mathf.Max(0f, centerToTargetDistance - ballRadius);

        if (distance < minTargetDistance)
            minTargetDistance = distance;
    }

}
