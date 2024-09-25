using Godot;
using Godot.Collections;
using System.Linq;

namespace TCGHandLayoutPlugin.Scripts{

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
	public delegate void CardDraggingEndedEventHandler(Card card, int index);
	
	
	public IdleLayout IdleLayout = new();
	public HoverLayout HoverLayout = new();
	public HandLayout HandLayout = new();
	public LayoutService LayoutService = new();
	
	public Callable OnChildMouseEnteredCallable;
	
	// GENERAL VARIABLES
	public Tween ResetPositionTween;
	private bool _mouseIn;
	private Control _draggingCard = null;
	public int _draggingCardIndex = -1;
	public Vector2 _draggingMousePosition;
	
	// IDLE LAYOUT
	public bool _dynamicRadius = true;
	public float _dynamicRadiusFactor = 100f;
	public int _cardsNumber = 0;
	public float _cardRadius = 1000f;
	public float _circlePercentage = 0.05f;
	
	// HOVER LAYOUT
	public bool _enableHover = true;
	public int _hoveredIndex = -1;
	public float _hoverPadding = 40f;
	public Vector2 _hoverScale = new(1.1f, 1.1f);
	public Vector2 _hoverRelativePosition = new(0f, -20f);
	
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

	
	[ExportGroup("Hover")]
	[Export] public bool EnableHover{
		get => _enableHover;
		set => HoverLayout.SetEnableHover(this, value);
	}
	[Export] public int HoveredIndex{
		get => _hoveredIndex;
		set => HoverLayout.SetHoveredIndex(this, value);
	}
	[Export] public float HoverPadding{
		get => _hoverPadding;
		set => HoverLayout.SetHoverPadding(this, value);
	}
	[Export] public Vector2 HoverScale{
		get => _hoverScale;
		set => HoverLayout.SetHoverScale(this, value);
	}
	[Export] public Vector2 HoverRelativePosition{
		get => _hoverRelativePosition;
		set => HoverLayout.SetHoverRelativePosition(this, value);
	}
	
	
	[ExportGroup("Drag")]
	[Export] public bool EnableDrag{
		get => _enableDrag;
		set{
			SetEnableDrag();

			void SetEnableDrag(){ // Local because it's the only setter and notifier for Dragging properties
				_enableDrag = value;
				NotifyPropertyListChanged();
			}
		}
	}

	[Export] public Vector2 DragScale{
		get => _dragScale;
		set => _dragScale = value;
	}
	
	
	[ExportGroup("Animation")]
	[Export] public Tween.EaseType AnimationEase = Tween.EaseType.In;
	[Export] public Tween.TransitionType AnimationTransition = Tween.TransitionType.Quad;
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
	
	
	public Layout(){
		OnChildMouseEnteredCallable = new Callable(this, MethodName.OnChildMouseEntered);
	}

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
			property["usage"] =  (int) PropertyUsageFlags.NoEditor;
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
		DraggingCardIndex = -100;
		ChildOrderChanged += OnChildOrderChanged;
		LayoutService.SetupCards(this);
		if (GetChildCount() > 0){
			LayoutService.RecalculateCurve = false;
			LayoutService.Animated = false;
			LayoutService.ResetPositionsIfInTree(this,false);
		}
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
	public void OnChildGuiInput(InputEvent @event, Control card){
		if (!_enableDrag){
			return;
		}
		if (@event is InputEventMouseMotion motion){
			if (DraggingCard != null){
				DraggingCard.GlobalPosition = GetGlobalMousePosition() - _draggingMousePosition;
			}
		}
		if (@event is InputEventMouseButton button){
			if (card.GetParent() != this){
				DraggingCardIndex = -1;
			}
			else{
				DraggingCardIndex = GetChildren().IndexOf(card);
			}
			if (button.Pressed && button.ButtonIndex == MouseButton.Left)
			{
				DraggingCard = card;
				_draggingMousePosition = card.GetLocalMousePosition();
				DraggingCard.ZIndex = 1;
				HoveredIndex = -1;
				EmitSignal(SignalName.CardDraggingStarted, _draggingCard, DraggingCardIndex);
				LayoutService.ResetPositionsIfInTree(this, true);
			}
			else if (!button.Pressed && button.ButtonIndex == MouseButton.Left){
				DraggingCard = null;
				card.ZIndex = 0;
				EmitSignal(SignalName.CardDraggingEnded, _draggingCard, DraggingCardIndex);
				_draggingCardIndex = -100;
				LayoutService.ResetPositionsIfInTree(this, true);
			}
		}
	}
}
}


