using Godot;
using Godot.Collections;
using System.Linq;
using TCGHandLayoutPlugin.Scripts;

[Tool]
public partial class Deck : Control
{
	public PackedScene card = GD.Load<PackedScene>("res://Example/Card.tscn");
	public Layout hand;
	public Array<Card> Cards = new();
	public override void _Ready()
	{
		hand = GetNode<Layout>("../HandLayout");
		/* 40 card sample deck. */
		for (var i = 0; i < 40; i++){
			Cards.Add(card.Instantiate<Card>());
		}
		GuiInput += DrawCard;	
	}
	private void DrawCard(InputEvent @event){
		if (@event is InputEventMouseButton mouseButton){
			if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left){
				var topCard = Cards.Last();
				hand.DrawingCard = true;
				hand.CardDrawed = topCard;
				hand.AddChild(topCard);
				topCard.GlobalPosition = GlobalPosition;
				topCard.ZIndex = 1;
				Cards.Remove(topCard);
			}
		}
	}
}
