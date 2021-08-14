using Godot;
using System;
using System.Collections.Generic;

public class Simon : Node2D
{
    public enum STATES { IDLE, MEMORIZE, INPUT }

    public STATES state = 0;
    
    private List<string> ChallengeSequence = new List<string>();
    private List<string> Colors = new List<string> { "Red", "Blue", "Green", "Yellow" };
    private string NextColor;
    private int CurrentStep = 0;
    private RandomNumberGenerator RNG = new RandomNumberGenerator();
    private Label GameLabel;
    private int Score = 0;
    private int HighScore;
    private float SpeedMultiplier = 1;

    [Signal]
    public delegate void Failed();

    [Signal]
    public delegate void Passed();

    [Signal]
    public delegate void ScoreChanged(Label label, int score);

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        RNG.Randomize();
        Connect("Failed", GetNode("Base"), "_OnSequenceFailed");
        Connect("Passed", GetNode("Base"), "_OnSequencePassed");
        GetNode("Base/Center").Connect("input_event", this, nameof(_OnStartGameClicked));
        Connect("ScoreChanged", this, nameof(_ChangeScore));
        GameLabel = (Label)GetNode("Base/Center/Label");
        GameLabel.Text = "Click Here\nTo Play";
        state = STATES.IDLE;
    }
    
    public void NewGame()
    {
        CurrentStep = 0;
        SpeedMultiplier = 1;
        Score = 0;
        EmitSignal(nameof(ScoreChanged), (Label)GetNode("ScoreLabel/Score"), Score);
        ChallengeSequence.Clear();
        ChallengeSequence.Add(Colors[RNG.RandiRange(0, 3)]);
        NextColor = ChallengeSequence[0];
        PlayNewSequence();
    }

    async public void PlayNewSequence()
    {
        // Play the sequence at the start of a new round
        state = STATES.MEMORIZE;
        GameLabel.Text = "Memorize!";
        await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
        if ((ChallengeSequence.Count) == 5) SpeedMultiplier = 0.8f;
        else if ((ChallengeSequence.Count) == 9) SpeedMultiplier = 0.6f;
        else if ((ChallengeSequence.Count) == 13) SpeedMultiplier = 0.5f;
        foreach (string color in ChallengeSequence)
        {
            Base b = (Base)GetNode("Base");
            Area2D button =  (Area2D)GetNode("Base/" + color);
            b.ButtonOn(button);
            await ToSignal(GetTree().CreateTimer(0.5f * SpeedMultiplier), "timeout");
            b.ButtonOff(button);
            await ToSignal(GetTree().CreateTimer(0.2f * SpeedMultiplier), "timeout");
        }
        state = STATES.INPUT;
        GameLabel.Text = "Your turn!";
    }

    public void SequencePassed()
    {
        // To run when the player completes a sequence succesfully
        state = STATES.IDLE;
        CurrentStep = 0;
        GameLabel.Text = "Well done.";
        Score += 1;
        EmitSignal(nameof(Passed));
        EmitSignal(nameof(ScoreChanged), (Label)GetNode("ScoreLabel/Score"), Score);
        ChallengeSequence.Add(Colors[RNG.RandiRange(0, 3)]);
        PlayNewSequence();
    }

    async public void SequenceFailed()
    {
        // To run when the player fails a sequence
        EmitSignal(nameof(Failed));
        state = STATES.IDLE;
        GameLabel.Text = "Game Over";
        if (Score > HighScore)
        {
            HighScore = Score;
            EmitSignal(nameof(ScoreChanged), (Label)GetNode("BestScoreLabel/Score"), HighScore);
        }
        await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
        GameLabel.Text = "Click Here\nTo Play";
    }

    public void _OnStartGameClicked(Node n, InputEvent @event, int idx)
    {
        if (state == STATES.IDLE && @event.IsActionPressed("left_click")) NewGame();
    }

    async public void _OnButtonPressed(string color)
    {
        if (state != STATES.INPUT) return;
        await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
        if (color == ChallengeSequence[CurrentStep])
        {
            CurrentStep += 1;
            if (CurrentStep != ChallengeSequence.Count) NextColor = ChallengeSequence[CurrentStep];
            else SequencePassed();
        }
        else SequenceFailed();
    }

    public void _ChangeScore(Label label, int score)
    {
        label.Text = score.ToString();
    }
}

