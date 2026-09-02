namespace ProjectService.Domain.Enums;

/// <summary>
/// Статус проекта.
/// </summary>
public enum ProjectStatus
{
    /// <summary>
    /// Проект активен.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Проект архивирован.
    /// </summary>
    Archived = 2,

    /// <summary>
    /// Проект удалён.
    /// </summary>
    Deleted = 3
}
