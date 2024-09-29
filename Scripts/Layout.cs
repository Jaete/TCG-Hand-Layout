using Godot;
using Godot.Collections;
using System.Linq;

namespace TCGHandLayoutPlugin.Scripts;
[Tool]
public partial class Layout : Control 
{
	[Signal]
	public delegate void CardHoveredEventHandler(Card card, int index);
	[Signal]
	public delegate void CardsUnhoveredEventHandler();
	[Signal]
	public delegate void CardDraggingStartedEventHandler(Card card, int index);
	[Signal]
	public delegate void CardDraggingEndedEventHandler(Card card);
	
	public IdleLayout IdleLayout = new();
	public HoverLayout HoverLayout = new();
	public HandLayout HandLayout = new();
	public DragLayout DragLayout = new();
	public LayoutService LayoutService = new();
	
	public Callable OnChildMouseEnteredCallable;
	public Callable OnChildMouseExitedCallable;
	public Callable OnChildGuiInputCallable;
	
	// GENERAL VARIABLES
	public Tween ResetPositionTween;
	private bool _mouseIn;
	private Control _draggingCard;
	public int _draggingCardIndex = -1;
	public Vector2 _draggingMousePosition;
	public Control PlayableArea;
	public bool _activateOnClick;
	private bool _drawingCard;
	private Card _cardDrawed;
	
	// IDLE LAYOUT
	public bool _dynamicRadius = true;
	public float _dynamicRadiusFactor = 100f;
	public int _cardsNumber = 0;
	public float _cardRadius = 1000f;
	public float _circlePercentage = 0.05f;
	
	// HOVER LAYOUT
	public bool _enableHover = true;
	public string _hoverMode;
	public int _hoveredIndex = -1;
	public float _hoverPadding = 15f;
	public Vector2 _hoverScale = new(1.1f, 1.1f);
	public Vector2 _hoverRelativePosition = new(0f, -20f);
	private bool _alwaysOnTopWhenHovered;
	
	// DRAGGING LAYOUT
	public bool _enableDrag = true;
	public Vector2 _dragScale = new(1.1f, 1.1f);
	
	
	// ANIMATION LAYOUT
	public float _animationTime = 0.1f;
	
	[ExportGroup("IdleLayout")]
	[Export] public bool DynamicRadius{
		get => _dynamicRadius;
		set => IdleLayout.SetDynamicRadius(this, DynamicRadius, value);
	}
	[Export] public float DynamicRadiusFactor{
		get => _dynamicRadiusFactor;
		set => IdleLayout.SetDynamicRadiusFactor(this, value);
	}
	public float CardRadius{
		get => _cardRadius;
		set => IdleLayout.SetRadius(this, value);
	}
	[Export] public float CirclePercentage{
		get => _circlePercentage;
		set => IdleLayout.SetCirclePercentage(this, value);
	}
	[Export] public bool ActivateOnClick{
		get => _activateOnClick;
		set => _activateOnClick = value;
	}

	
	[ExportGroup("Hover")]
	[Export] public bool EnableHover{
		get => _enableHover;
		set => HoverLayout.SetEnableHover(this, value);
	}
	// Hover modes: Standard or Rise Only
	[Export(PropertyHint.Enum, HoverLayout.HoverModeList)] public string HoverMode{
		get => _hoverMode;
		set => HoverLayout.SetHoverMode(this, value);
	}
	// Identifier for which card is hovered
	[Export] public int HoveredIndex{
		get => _hoveredIndex;
		set => HoverLayout.SetHoveredIndex(this, value);
	}
	// How separated the other cards should be when a card is hovered
	[Export] public float HoverPadding{
		get => _hoverPadding;
		set => HoverLayout.SetHoverPadding(this, value);
	}
	// The size of the card when hovered.
	// If HoverMode is Rise Only, this attribute is ignored.
	[Export] public Vector2 HoverScale{
		get => _hoverScale;
		set => HoverLayout.SetHoverScale(this, value);
	}
	// Offset position when the card is hovered. Recommended value: The
	// negative value of the horizontal size of your card divided by 2
	[Export] public Vector2 HoverRelativePosition{
		get => _hoverRelativePosition;
		set => HoverLayout.SetHoverRelativePosition(this, value);
	}
	// Controls if the Z-index of the card is always on top when hovered.
	[Export] public bool AlwaysOnTopWhenHovered{
		get => _alwaysOnTopWhenHovered;
		set => _alwaysOnTopWhenHovered = value;
	}
	//Set this to true if you want the plugin to calculate the hover offset based on the card size
	[Export] public bool CalculateOffsetWhenHovered{ get; set; }
	
	
	[ExportGroup("Drag")]
	// If the cards on layout should be draggable at all
	[Export] public bool EnableDrag{
		get => _enableDrag;
		set => DragLayout.SetEnableDrag(this, value);
	}
	// How much should the card scale when dragged
	[Export] public Vector2 DragScale{
		get => _dragScale;
		set => _dragScale = value;
	}
	
	// Animation-related properties
	[ExportGroup("Animation")]
	// Easing for animations
	[Export] public Tween.EaseType AnimationEase = Tween.EaseType.In;
	// Transition for animations
	[Export] public Tween.TransitionType AnimationTransition = Tween.TransitionType.Quad;
	// Duration of the animations
	[Export] public float AnimationTime{
		get => _animationTime;
		set{
			SetAnimationTime(); // Local because it's the only setter and notifier for Animation properties

			void SetAnimationTime(){
				_animationTime = value;
				NotifyPropertyListChanged();
			}
		}
	}

	[ExportGroup("Sound")]
	[Export] public AudioStreamPlayer2D HoverSound;

	public Control DraggingCard{
		get => _draggingCard;
		set => _draggingCard = value;
	}

	public int CardsNumber{
		get => _cardsNumber;
		set => _cardsNumber = value;
	}

	public int DraggingCardIndex{
		get => _draggingCardIndex;
		set => _draggingCardIndex = value;
	}

	public bool DrawingCard{
		get => _drawingCard;
		set => _drawingCard = value;
	}

	public Card CardDrawed{ 
		get => _cardDrawed;
		set => _cardDrawed = value;}

	// Validation for properties on editor
	public override void _ValidateProperty(Dictionary property){
		string[] radius = { nameof(CardRadius) };
		string[] dynamicRadius = { nameof(DynamicRadiusFactor) };
		string[] hover = {
			nameof(HoveredIndex),
			nameof(HoverPadding),
			nameof(HoverScale),
			nameof(HoverRelativePosition)
		};
		string[] dragging = { nameof(DragScale) };
		string[] animation = { nameof(AnimationEase), nameof(AnimationTransition) };
		if (radius.Contains((string) property["name"]) && DynamicRadius){
			property["usage"] = (int) PropertyUsageFlags.NoEditor;
		}
		if (dynamicRadius.Contains((string) property["name"]) && !DynamicRadius){
			property["usage"] = (int) PropertyUsageFlags.NoEditor;
		}
		if (hover.Contains((string) property["name"]) && !EnableHover){
			property["usage"] = (int) PropertyUsageFlags.NoEditor;
		}
		if (dragging.Contains((string) property["name"]) && !EnableDrag){
			property["usage"] = (int) PropertyUsageFlags.NoEditor;
		}
		if (animation.Contains((string) property["name"]) && _animationTime <= 0f){
			property["usage"] = (int) PropertyUsageFlags.NoEditor;
		}
	}

	public override void _EnterTree(){
		if (IsNodeReady() && GetChildCount() > 0){
			LayoutService.ResetPositionsIfInTree(this,false);
		}
	}
	public override void _Ready(){
		PlayableArea = GetNode<Control>("../PlayableArea");
		DraggingCardIndex = -100;
		ChildOrderChanged += OnChildOrderChanged;
		CardDraggingStarted += OnCardPicked;
		CardDraggingEnded += OnCardReleased;
		LayoutService.SetupCards(this);
		if (GetChildCount() > 0){
			LayoutService.RecalculateCurve = false;
			LayoutService.Animated = false;
			LayoutService.ResetPositionsIfInTree(this,false);
		}
	}
	private void OnCardPicked(Card card, int index){
		DraggingCard = card;
		DraggingCardIndex = GetChildren().IndexOf(card);
		DraggingCard.ZIndex = 1;
		_draggingMousePosition = card.GetLocalMousePosition();
		HoveredIndex = -1;
		LayoutService.ResetPositionsIfInTree(this, true);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint() || !_enableHover && _draggingCard != null){
			return;
		}
		if (_draggingCard != null){
			return;
		}
		if (_mouseIn){
			var mousePosition = GetGlobalMousePosition();
			var newHoverIndex = LayoutService.FindCardWithPoint(this, mousePosition);
			if (HoveredIndex != newHoverIndex){
				HoveredIndex = newHoverIndex;
			}
		}
		else if(HoveredIndex != -1){
			HoveredIndex = -1;
		}
	}
	
	public void OnChildOrderChanged(){
		LayoutService.SetupCards(this);
		LayoutService.ResetPositionsIfInTree(this);
	}
	public void OnChildMouseEntered(){
		if (_draggingCard != null){
			return;
		}
		_mouseIn = true;
	}
	public void OnChildMouseExited(){
		if (_draggingCard != null){
			return;
		}
		_mouseIn = false;
	}
	public async void OnChildGuiInput(InputEvent @event, Control card){
		if (!_enableDrag){
			return;
		}
		switch (@event){
			case InputEventMouseMotion motion:{
				if (DraggingCard != null){
					DraggingCard.GlobalPosition = new(
						GetGlobalMousePosition().X - DraggingCard.Size.X / 2,
						GetGlobalMousePosition().Y - DraggingCard.Size.Y / 2);
				}
				break;
			}
			case InputEventMouseButton button:{
				if (card.GetParent() != this){
					DraggingCardIndex = -1;
				}
				switch (button.Pressed){
					case true when button.ButtonIndex == MouseButton.Left:{
						/* Timer to check if it was a click or a hold */
						var dragTimer = GetTree().CreateTimer(DragLayout.TIME_TO_DRAG);
						await ToSignal(dragTimer, Timer.SignalName.Timeout);
						/* If the button is still pressed after the delay, emits the signal for dragging a card */
						if(Input.IsActionPressed("LMB")){
							EmitSignal(SignalName.CardDraggingStarted, card, DraggingCardIndex);
						}
						else{
							/* Otherwise, activate directly. Behaviour can be modified on the editor. */
							if (ActivateOnClick){
								card.EmitSignal(Card.SignalName.Activated);
							}
						}
						break;
					}
					case false when button.ButtonIndex == MouseButton.Left:
						/* Emits the signal for releasing a card when the button is released. */
						EmitSignal(SignalName.CardDraggingEnded, _draggingCard);
						break;
				}
				break;
			}
		}
	}
	
	public void OnCardReleased(Card card){
		if (card == null){
			return;
		}
		if (LayoutService.IsInPlayableArea(this, card)){
			card.EmitSignal(Card.SignalName.Activated);
			return;
		}
		DraggingCard = null;
		card.ZIndex = 0;
		_draggingCardIndex = -100;
		LayoutService.ReorderCards(this, card);
	}
}