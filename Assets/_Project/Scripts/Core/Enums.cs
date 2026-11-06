namespace Duzhe.Core
{
    /// Global run mode owned by GameSession. Main story, chapter replay and
    /// presentation replay must never write into each other's progress.
    public enum GameMode
    {
        MainMenu,
        MainRun,
        ChapterReplay,
        PresentationReplay
    }

    /// Stage-level input modes (per TechnicalAndInteractionSpec section 5).
    /// Paused is an overlay flag, NOT a mode: after closing the pause menu the
    /// previous mode must be restored.
    public enum InputMode
    {
        Exploring,
        Dialogue,
        MiniGame,
        Ritual,
        Transition,
        River,
        Gallery,
        Menu
    }

    /// Default event semantics (TechnicalAndInteractionSpec section 4).
    /// New values are appended only: the order is part of the event contract.
    public enum GameEventType
    {
        DialogueFinished,
        ItemCollected,
        ItemsReturned,
        MinigameCompleted,
        RitualCompleted,
        Boarded,
        RiverSucceeded,
        PresentationFinished,
        ChapterCompleted,
        /// C08: the player confirmed the end credits (CH05 only).
        CreditsConfirmed
    }

    /// C09: which page of the start menu to show.
    public enum MenuTab
    {
        Home,
        Chapters,
        Gallery,
        Settings
    }

    public enum InteractableKind
    {
        Item,
        Npc,
        Prop
    }

    /// Four facing directions. Row order matches the asset contract
    /// frameLayout.rowOrder = [Down, Left, Right, Up].
    public enum Direction4
    {
        Down = 0,
        Left = 1,
        Right = 2,
        Up = 3
    }
}
