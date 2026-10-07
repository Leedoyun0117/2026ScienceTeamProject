using System;

public interface IReactionResolver
{
    ReactionType Current { get; }
    event Action<ReactionType> OnReaction;
    void Complete(ReactionType finished);
}
