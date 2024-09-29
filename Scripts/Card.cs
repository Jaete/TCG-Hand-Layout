using Godot;
using System;

namespace TCGHandLayoutPlugin.Scripts;
public partial class Card : Control
{
	public bool AllSignalsConected = false;
	
	[Signal]
	public delegate void ActivatedEventHandler();
	// This is the node to your card class, so it can have its own functions.
	// Called when the node enters the scene tree for the first time.
	public override void _Ready(){
		/* Random color so it can differ from other cards, since it's the same sprite
		 Not necessary for the plugin to work.
		 */
		var rng = new Random();
		Modulate = new Color(
			Color.Color8(
				(byte) rng.Next(0, 255),
				(byte) rng.Next(0, 255),
				(byte) rng.Next(0, 255)
		));
		Activated += HandleEffect;
	}
	private void HandleEffect(){
		GD.Print("Handling your effect.");
		/* Deleting the activated card and resetting the other cards positions */
		var layout = GetParent<Layout>();
		layout.DraggingCard = null;
		layout._draggingCardIndex = -100;
		layout.CardsNumber -= 1;
		layout.LayoutService.ResetPositionsIfInTree(layout, true);
		/* That's the part where you're free to edit at will without breaking anything.
		 Insert animations, VFX, change data somewhere, go wild.
		 */
		QueueFree();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}