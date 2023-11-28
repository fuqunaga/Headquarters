using MaterialDesignThemes.Wpf.Transitions;
using System;
using System.Windows;
using System.Windows.Media.Animation;


namespace Headquarters;

public class CustomFadeWipe : ITransitionWipe
{
    private readonly SineEase _sineEase = new SineEase();
    private readonly KeyTime zeroKeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero);

    /// <summary>
    /// Duration of the animation
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(0.5);

    public void Wipe(TransitionerSlide fromSlide, TransitionerSlide toSlide, Point origin, IZIndexController zIndexController)
    {
        ArgumentNullException.ThrowIfNull(fromSlide);
        ArgumentNullException.ThrowIfNull(toSlide);
        ArgumentNullException.ThrowIfNull(zIndexController);

        // Set up time points
        var endKeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(Duration.TotalSeconds / 2));

        // From
        var fromAnimation = new DoubleAnimationUsingKeyFrames();
        fromAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(1, zeroKeyTime));
        fromAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0, endKeyTime, _sineEase));

        // To
        var toAnimation = new DoubleAnimationUsingKeyFrames();
        toAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, zeroKeyTime));
        toAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1, endKeyTime, _sineEase));

        // Preset
        fromSlide.Opacity = 1;
        toSlide.Opacity = 0;

        // Set up events
        toAnimation.Completed += (sender, args) =>
        {
            //toSlide.BeginAnimation(UIElement.OpacityProperty, null);
        };
        fromAnimation.Completed += (sender, args) =>
        {
            fromSlide.BeginAnimation(UIElement.OpacityProperty, null);
            toSlide.BeginAnimation(UIElement.OpacityProperty, toAnimation);
        };

        // Animate
        fromSlide.BeginAnimation(UIElement.OpacityProperty, fromAnimation);
        zIndexController.Stack(toSlide, fromSlide);
    }
}
