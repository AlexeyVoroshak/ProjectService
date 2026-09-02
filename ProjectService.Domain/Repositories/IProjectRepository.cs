using ProjectService.Domain.Entities;

namespace ProjectService.Domain.Repositories;

/// <summary>
/// Интерфейс репозитория для работы с проектами.
/// Определяет контракт для CRUD-операций и сохранения доменных событий.
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    /// Получает проект по идентификатору с возможностью загрузки связанных сущностей.
    /// </summary>
    /// <param name="id">Идентификатор проекта</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Проект или null, если не найден</returns>
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает все проекты с пагинацией.
    /// </summary>
    /// <param name="skip">Количество пропускаемых записей</param>
    /// <param name="take">Количество записей для возврата</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Коллекция проектов</returns>
    Task<IEnumerable<Project>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет новый проект.
    /// </summary>
    /// <param name="project">Проект для добавления</param>
    void Add(Project project);

    /// <summary>
    /// Обновляет существующий проект.
    /// </summary>
    /// <param name="project">Проект для обновления</param>
    void Update(Project project);

    /// <summary>
    /// Помечает проект на удаление.
    /// </summary>
    /// <param name="id">Идентификатор проекта</param>
    void Remove(Guid id);
}
