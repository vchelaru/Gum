using System;
using Gum.Graphics.Animation;
using RenderingLibrary.Math;

#nullable enable

namespace RenderingLibrary.Graphics.Animation;

/// <summary>
/// Platform-agnostic AnimationChain playback state + tick logic. Renderables
/// (Sprite, NineSlice, ...) compose one of these and subscribe to
/// <see cref="ApplyFrame"/> to translate the current
/// <see cref="AnimationFrame"/> into platform-specific texture / source rect / flip state.
/// </summary>
public class AnimationChainLogic
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
                if (CurrentChain != null)
                {
                    _isLooping = CurrentChain.Loop;
                }
                UpdateToCurrentAnimationFrame();
            }
        }
    }

    /// <summary>
    /// Index of the current frame in the current chain. Setting it moves
    /// <see cref="TimeIntoAnimation"/> to the start of that frame and applies the frame.
    /// When a chain is set, the index is clamped to the chain's frames, so a value past the end
    /// selects the last frame and a negative value selects the first. With no chain set, the
    /// value is stored as given and clamped once a chain is applied.
    /// </summary>
    public int CurrentFrameIndex
    {
        get => _currentFrameIndex;
        set
        {
            AnimationChain? chain = CurrentChain;
            if (chain != null && chain.Count > 0)
            {
                SeekToFrame(chain, value);
            }
            else
            {
                _currentFrameIndex = value;
            }
            UpdateToCurrentAnimationFrame();
        }
    }

    // Clamps the index to the chain and moves the time to the start of that frame, so the
    // index, the time and the frame applied next all agree.
    void SeekToFrame(AnimationChain chain, int frameIndex)
    {
        _currentFrameIndex = System.Math.Clamp(frameIndex, 0, chain.Count - 1);
        double time = 0;
        for (int i = 0; i < _currentFrameIndex; i++)
        {
            time += chain[i].FrameLength;
        }
        _timeIntoAnimation = time;
    }

    public float AnimationSpeed { get => _animationSpeed; set => _animationSpeed = value; }

    /// <summary>
    /// Seconds into the current chain. Setting it selects and applies the frame at that time,
    /// wrapping past the end when looping and holding the last frame when not. When a chain is
    /// set, a negative value is clamped to 0 and selects the first frame. With no chain set, the
    /// value is stored as given and clamped once a chain is applied.
    /// </summary>
    public double TimeIntoAnimation
    {
        get => _timeIntoAnimation;
        set
        {
            _timeIntoAnimation = value;
            AnimationChain? chain = CurrentChain;
            if (chain == null || chain.Count == 0)
            {
                return;
            }
            if (value < 0)
            {
                SeekToFrame(chain, 0);
            }
            else if (!_isLooping && value >= chain.TotalLength)
            {
                _currentFrameIndex = chain.Count - 1;
            }
            else
            {
                UpdateFrameBasedOffOfTimeIntoAnimation();
            }
            UpdateToCurrentAnimationFrame();
        }
    }

    public bool Animate { get => _animate; set => _animate = value; }
    public bool IsAnimationChainLooping { get => _isLooping; set => _isLooping = value; }

    public event Action? AnimationChainCycled;

    /// <summary>
    /// Invoked whenever the current frame changes. Subscribers should copy Texture,
    /// source rectangle, and flip flags out of the frame onto their renderable.
    /// </summary>
    public Action<AnimationFrame>? ApplyFrame;

    /// <summary>
    /// Returns a copy for a cloned renderable. The copy starts at this playback position (chain,
    /// frame, time, speed, looping, animate) and shares the chain list, but advances on its own.
    /// Its frames go to <paramref name="applyFrame"/>, and <see cref="AnimationChainCycled"/>
    /// subscribers on this instance are not carried over, since they belong to the source.
    /// </summary>
    public AnimationChainLogic Clone(Action<AnimationFrame>? applyFrame)
    {
        AnimationChainLogic clone = (AnimationChainLogic)MemberwiseClone();
        clone.ApplyFrame = applyFrame;
        clone.AnimationChainCycled = null;
        return clone;
    }

    public bool AnimateSelf(double secondDifference)
    {
        AnimationChain? animationChain = CurrentChain;
        if (!_animate || animationChain == null || animationChain.Count == 0)
        {
            return false;
        }

        int frameBefore = _currentFrameIndex;
        _timeIntoAnimation += secondDifference * _animationSpeed;

        if (_isLooping)
        {
            _timeIntoAnimation = MathFunctions.Loop(_timeIntoAnimation, animationChain.TotalLength, out _justCycled);
        }
        else if (_timeIntoAnimation < 0)
        {
            // Played backward (negative speed) past the start, or a negative time stored before
            // a chain was set: hold the first frame.
            _timeIntoAnimation = 0;
            _justCycled = false;
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

    /// <summary>
    /// Applies the current frame. If the index or time is outside the current chain (the chain
    /// changed to a shorter one, or they were set before a chain was), they are first clamped to
    /// it as <see cref="CurrentFrameIndex"/> does.
    /// </summary>
    public bool UpdateToCurrentAnimationFrame()
    {
        AnimationChain? chain = CurrentChain;
        if (chain == null || chain.Count == 0)
        {
            return false;
        }

        if (_currentFrameIndex < 0 || _currentFrameIndex >= chain.Count || _timeIntoAnimation < 0)
        {
            SeekToFrame(chain, _currentFrameIndex);
        }
        ApplyFrame?.Invoke(chain[_currentFrameIndex]);
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
