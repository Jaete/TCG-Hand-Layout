using Godot;

namespace TCGHandLayoutPlugin.Scripts
{
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
                        targetScale = layout._hoverScale;
                        targetZIndex = 1;

                    }
                }
                if (!shouldAnimate){
                    if (i != layout.DraggingCardIndex){
                        card.Position = targetPosition;
                    }
                    card.Rotation = targetRotation;
                    card.Scale = targetScale;
                    card.ZIndex = targetZIndex;
                }
                else{
                    
                    if (i != layout.DraggingCardIndex){
                        layout.ResetPositionTween
                            .TweenProperty(card, "position", targetPosition, layout.AnimationTime)
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
                    layout.ResetPositionTween
                        .TweenProperty(card, "z_index", targetZIndex, layout._animationTime)
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
        }   
        
        public void SetupCards(Layout layout){
            foreach (var child in layout.GetChildren()){
                var card = (Control) child;
                if (card.IsConnected(Control.SignalName.MouseEntered, layout.OnChildMouseEnteredCallable)){
                    continue;
                }
                card.MouseEntered += layout.OnChildMouseEntered;
                card.MouseExited += layout.OnChildMouseExited;
                card.GuiInput += BindCard(layout, card);
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
    }
}

