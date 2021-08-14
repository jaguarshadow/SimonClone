using Godot;
using System;
using System.Collections.Generic;

public class Base : Polygon2D
{
    private Simon Game;
    private Area2D ButtonLit = null;
    private Vector2 LockMousePos = new Vector2();
    private Dictionary<Area2D, Color> ColorNodes = new Dictionary<Area2D, Color>();

    [Signal]
    public delegate void ButtonPressed(string color);

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Game = GetParent<Simon>();
        ColorNodes.Add(GetNode<Area2D>("Red"), new Color(255, 0, 0));
        ColorNodes.Add(GetNode<Area2D>("Blue"), new Color(0, 0, 255));
        ColorNodes.Add(GetNode<Area2D>("Green"), new Color(0, 255, 0));
        ColorNodes.Add(GetNode<Area2D>("Yellow"), new Color(255, 255, 0));

        Connect("ButtonPressed", GetParent(), "_OnButtonPressed");

        foreach (Area2D node in ColorNodes.Keys)
        {
            node.Connect("input_event", this, nameof(_OnBaseButtonInputEvent), new Godot.Collections.Array { node.Name });
            node.Connect("mouse_exited", this, nameof(_OnBaseButtonMouseExit), new Godot.Collections.Array { node });
        }
    }

    public void ButtonOn(Area2D button)
    {
        LockMousePos = GetGlobalMousePosition();
        ButtonLit = button;
        // Light up the respective color and play sound
        button.Modulate = ColorNodes[button];
        button.GetNode<AudioStreamPlayer2D>("Sound").Play();
    }

    public void ButtonOff(Area2D button)
    {
        ButtonLit = null;
        // Darken and stop sound 
        button.Modulate = new Color(1, 1, 1);
        button.GetNode<AudioStreamPlayer2D>("Sound").Stop();
    }
    
    public void _OnBaseButtonInputEvent(Node n, InputEvent @event, int idx, string color)
    {
        if (Game.state != Simon.STATES.INPUT) return;
        if (!@event.IsAction("left_click")) return;
        Area2D button = GetNode<Area2D>(color);
        if (@event.IsActionReleased("left_click") && ButtonLit != null)
        {
            EmitSignal("ButtonPressed", color);
            ButtonOff(button);
        }
        if (@event.IsActionPressed("left_click"))
        {
            ButtonOn(button);
        }
    }

    async public void _OnSequenceFailed()
    {
        for (int i = 0; i < 2; i++)
        {
            foreach (Area2D button in ColorNodes.Keys) ButtonOn(button);
            await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
            foreach (Area2D button in ColorNodes.Keys) ButtonOff(button);
            await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
        }
    }

   public void _OnSequencePassed()
    {
        foreach (Area2D button in ColorNodes.Keys) ButtonOff(button);
    }

    public void _OnBaseButtonMouseExit(Area2D button)
    {
        if (Input.IsActionPressed("left_click"))
        {
            ButtonOff(button);
            EmitSignal("ButtonPressed", button.Name);
        }
    }
}
