using System;
using Gum.Graphics.Animation;
using RenderingLibrary.Math;

#nullable enable

namespace RenderingLibrary.Graphics.Animation;

/// <summary>
/// Platform-agnostic AnimationChain playback state + tick logic. Sprites compose one of
/// these and subscribe to <see cref="ApplyFrame"/> to translate the current
/// <see cref="AnimationFrame"/> into platform-specific texture / source rect / flip state.
/// </summary>
public class SpriteAnimationLogic
{
    // Matches the original Sprite behavior: index 0 is active by default so a caller
    // can just assign AnimationChains + Animate and playback works without also
    // setting CurrentChainName. Guards in AnimateSelf / CurrentChain tolerate a
    // null or empty AnimationChains list with this default.
    int _currentChainIndex;
    int _currentFrameIndex;
    double _timeIntoAnimation;
    float _animationSpeed = 1;
    bool _isLooping = true;
    bool _animate;
    bool _justCycled;
    string? _desiredChainName;
    AnimationChainList? _chains;

    public AnimationChainList? AnimationChains
    {
        get => _chains;
        set => _chains = value;
    }

    public AnimationChain? CurrentChain =>
        (_currentChainIndex != -1 && _chains != null && _chains.Count > 0 && _currentChainIndex < _chains.Count)
            ? _chains[_currentChainIndex]
            : null;

    public string? CurrentChainName
    {
        get => CurrentChain?.Name;
        set
        {
            _desiredChainName = value;
            _currentChainIndex = -1;
            if (_chains != null && _chains.Count > 0)
            {
                RefreshCurrentChainToDesiredName();
                UpdateToCurrentAnimationFrame();
            }
        }
    }

    public int CurrentFrameIndex
    {
        get => _currentFrameIndex;
        set
        {
            _currentFrameIndex = value;
            if (CurrentChain != null && CurrentChain.Count > 0)
            {
                double time = 0;
                int clampedIndex = System.Math.Clamp(value, 0, CurrentChain.Count - 1);
                for (int i = 0; i < clampedIndex; i++)
                {
                    time += CurrentChain[i].FrameLength;
                }
                _timeIntoAnimation = time;
            }
        }
    }

    public float AnimationSpeed { get => _animationSpeed; set => _animationSpeed = value; }
    public double TimeIntoAnimation { get => _timeIntoAnimation; set => _timeIntoAnimation = value; }
    public bool Animate { get => _animate; set => _animate = value; }
    public bool IsAnimationChainLooping { get => _isLooping; set => _isLooping = value; }

    public event Action? AnimationChainCycled;

    /// <summary>
    /// Invoked whenever the current frame changes. Subscribers should copy Texture,
    /// source rectangle, and flip flags out of the frame onto their Sprite.
    /// </summary>
    public Action<AnimationFrame>? ApplyFrame;

    public bool AnimateSelf(double secondDifference)
    {
        if (!_animate || _currentChainIndex == -1 || _chains == null ||
            _chains.Count == 0 || _chains[_currentChainIndex].Count == 0)
        {
            return false;
        }

        int frameBefore = _currentFrameIndex;
        _timeIntoAnimation += secondDifference * _animationSpeed;

        AnimationChain animationChain = _chains[_currentChainIndex];

        if (_isLooping)
        {
            _timeIntoAnimation = MathFunctions.Loop(_timeIntoAnimation, animationChain.TotalLength, out _justCycled);
        }
        else if (_timeIntoAnimation >= animationChain.TotalLength)
        {
            _timeIntoAnimation = animationChain.TotalLength;
            _currentFrameIndex = animationChain.Count - 1;
            _animate = false;
            _justCycled = true;
        }
        else
        {
            _justCycled = false;
        }

        if (_justCycled)
        {
            AnimationChainCycled?.Invoke();
        }

        UpdateFrameBasedOffOfTimeIntoAnimation();

        if (_currentFrameIndex != frameBefore)
        {
            return UpdateToCurrentAnimationFrame();
        }
        return false;
    }

    public bool UpdateToCurrentAnimationFrame()
    {
        if (_chains == null || _chains.Count <= _currentChainIndex || _currentChainIndex == -1 ||
            _currentFrameIndex <= -1 || _chains[_currentChainIndex].Count == 0)
        {
            return false;
        }

        var index = _currentFrameIndex;
        if (index >= _chains[_currentChainIndex].Count) index = 0;
        var frame = _chains[_currentChainIndex][index];
        ApplyFrame?.Invoke(frame);
        return true;
    }

    void UpdateFrameBasedOffOfTimeIntoAnimation()
    {
        double timeIntoAnimation = _timeIntoAnimation;
        if (timeIntoAnimation < 0)
        {
            throw new ArgumentException("The timeIntoAnimation argument must be 0 or positive");
        }
        if (CurrentChain == null || CurrentChain.Count == 0 || CurrentChain.TotalLength == 0)
        {
            return;
        }

        int frameIndex = 0;
        while (timeIntoAnimation >= 0)
        {
            double frameTime = CurrentChain[frameIndex].FrameLength;
            if (timeIntoAnimation < frameTime)
            {
                _currentFrameIndex = frameIndex;
                break;
            }
            timeIntoAnimation -= frameTime;
            frameIndex = (frameIndex + 1) % CurrentChain.Count;
        }
    }

    public void RefreshCurrentChainToDesiredName()
    {
        if (_chains == null)
        {
            _currentChainIndex = -1;
            return;
        }
        for (int i = 0; i < _chains.Count; i++)
        {
            if (_chains[i].Name == _desiredChainName)
            {
                _currentChainIndex = i;
                break;
            }
        }
    }
}
