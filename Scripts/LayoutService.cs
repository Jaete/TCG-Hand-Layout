using Godot;
using Godot.Collections;
using System.Xml.Serialization;

namespace TCGHandLayoutPlugin.Scripts;
public partial class LayoutService : Control
{
    private bool _recalculateCurve;
    private bool _animated;

    public bool RecalculateCurve{
        get => _recalculateCurve;
        set => _recalculateCurve = value;
    }
        
    public bool Animated{
        get => _animated;
        set => _animated = value;
    }
    public void ResetPositionsIfInTree(Layout layout, bool animated = true){
        if (layout.IsInsideTree()){
            ResetPositions(layout, animated);
        }
    }
    private void ResetPositions(Layout layout, bool animated = true){
        layout.CardsNumber = layout.GetChildCount();
        if (layout.DraggingCard != null && layout.DraggingCard.GetParent() == layout){
            layout.CardsNumber -= 1;
        }
        layout.HandLayout.SetData(
            layout.CardsNumber,
            layout.DynamicRadius,
            layout.DynamicRadiusFactor,
            layout.CardRadius,
            layout.CirclePercentage,
            layout.HoverPadding,
            layout.HoveredIndex,
            layout.HoverRelativePosition
        );
        var shouldAnimate = layout._animationTime > 0.0 && animated && layout.CardsNumber > 0 && layout.IsInsideTree();
        var layoutInfos = layout.HandLayout.GetCardLayouts(layout);
        var positionIndex = 0;
        if (layout.ResetPositionTween != null && layout.ResetPositionTween.IsRunning()){
            layout.ResetPositionTween.Stop();
        }
        if (shouldAnimate){
            layout.ResetPositionTween = layout.CreateTween();
            layout.ResetPositionTween.SetParallel();
        }
        for(var i = 0; i < layout.GetChildCount(); i++){
            var card = (Control) layout.GetChild(i);
            var targetScale = Vector2.One;
            var targetPosition = new Vector2();
            var targetRotation = 0f;
            var targetZIndex = 0;
            if (i == layout.DraggingCardIndex){
                targetScale = layout.DragScale;
            }
            else
            {
                var layoutInfo = layoutInfos[positionIndex];
                targetPosition = layoutInfo.Position;
                targetRotation = layoutInfo.Rotation;
                if (i == layout._hoveredIndex){
                    targetRotation = 0;
                    targetScale = layout.HoverMode == HoverLayout.HoverMode.RiseOnly ? Vector2.One : layout.HoverScale;
                    targetZIndex = 1;
                }
            }
            if (!shouldAnimate){
                if (i != layout.DraggingCardIndex){
                    card.Position = targetPosition;
                }
                if (layout.AlwaysOnTopWhenHovered){
                    card.ZIndex = targetZIndex;
                }
                card.Rotation = targetRotation;
                card.Scale = targetScale;
                    
            }
            else{
                if (i != layout.DraggingCardIndex){
                    layout.ResetPositionTween
                        .TweenProperty(card, "position", targetPosition, layout.AnimationTime)
                        .SetEase(layout.AnimationEase)
                        .SetTrans(layout.AnimationTransition);
                }
                if (layout.DrawingCard && layout.GetChild<Card>(i) == layout.CardDrawed){
                    layout.ResetPositionTween
                        .TweenProperty(card, "position", targetPosition, layout.AnimationTime * 3f)
                        .SetEase(layout.AnimationEase)
                        .SetTrans(layout.AnimationTransition);
                }
                if (layout.AlwaysOnTopWhenHovered){
                    layout.ResetPositionTween
                        .TweenProperty(card, "z_index", targetZIndex, layout._animationTime)
                        .SetEase(layout.AnimationEase)
                        .SetTrans(layout.AnimationTransition);
                }
                layout.ResetPositionTween
                    .TweenProperty(card, "rotation", targetRotation, layout._animationTime)
                    .SetEase(layout.AnimationEase)
                    .SetTrans(layout.AnimationTransition);
                layout.ResetPositionTween
                    .TweenProperty(card, "scale", targetScale, layout._animationTime)
                    .SetEase(layout.AnimationEase)
                    .SetTrans(layout.AnimationTransition);
            }
            if (i != layout.DraggingCardIndex){
                positionIndex++;
            }
        }
        if (shouldAnimate){
            layout.ResetPositionTween.Play();
        }
        layout.DrawingCard = false;
    }   
        
    public void SetupCards(Layout layout){
        foreach (var child in layout.GetChildren()){
            if (child is not Card card){
                continue;
            }
            card.PivotOffset = new(card.Size.X / 2, 0f);
            if (layout.CalculateOffsetWhenHovered){
                layout.HoverRelativePosition = new(0, -(card.Size.X / 2.5f));
            }
            if (card.AllSignalsConected){
                continue;
            }
            card.MouseEntered += layout.OnChildMouseEntered;
            card.MouseExited += layout.OnChildMouseExited;
            card.GuiInput += BindCard(layout, card);
            card.AllSignalsConected = true;
        }
    }

    public int FindCardWithPoint(Layout layout, Vector2 globalPoint){
        for (int i = 0; i < layout.GetChildCount(); i++){
            var card = (Control) layout.GetChildren()[i];
            if (card.GetGlobalRect().HasPoint(globalPoint)){
                return i;
            }
        }
        return -1;
    }

    private GuiInputEventHandler BindCard(Layout layout, Control card){
        return (inputEvent) => layout.OnChildGuiInput(inputEvent, card);
    }

    private int CountCards(Layout layout){
        var cards = 0;
        foreach (var node in layout.GetChildren()){
            if(node is Card card){
                cards++;
            }
        }
        return cards;
    }

    public void ReorderCards(Layout layout, Card card){
        var newIndex = 0;
        for (var i = 0; i < CountCards(layout); i++){
            if (card.GlobalPosition.X >= layout.GetChild<Card>(i).GlobalPosition.X){
                newIndex++;
            }
        }
        layout.MoveChild(card, newIndex - 1);
    }
    public bool IsInPlayableArea(Layout layout, Card card){
        var playableArea = layout.PlayableArea.GetGlobalRect();
        if (!playableArea.Intersects(card.GetGlobalRect())){
            return false;
        }
        if (playableArea.HasPoint(card.GlobalPosition) && playableArea.HasPoint(card.GlobalPosition + card.Size / 2)){
            return true;
        }
        return false;
    }
}