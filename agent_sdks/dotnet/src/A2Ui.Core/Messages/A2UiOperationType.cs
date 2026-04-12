namespace A2Ui.Core.Messages;

/// <summary>Discriminator for the operation type in an A2UiMessage.</summary>
public enum A2UiOperationType
{
    /// <summary>The message creates a new surface.</summary>
    CreateSurface,

    /// <summary>The message deletes an existing surface.</summary>
    DeleteSurface,

    /// <summary>The message updates the component tree on a surface.</summary>
    UpdateComponents,

    /// <summary>The message updates the data model on a surface.</summary>
    UpdateDataModel,
}
