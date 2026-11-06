namespace Duzhe.Interaction
{
    /// Optional behaviour component placed next to an Interactable of kind
    /// Prop. InteractionController calls it when the player presses E on the
    /// prop; the stage authorisation (activeStageIds) has already happened
    /// upstream, so implementations only run their own behaviour.
    public interface IPropInteractable
    {
        /// Returns true if the interaction was accepted and consumed.
        bool OnPropInteract(Interactable self);
    }
}
