using System.Collections.Immutable;

namespace ProjectService.Domain.ValueObjects;

/// <summary>
/// Базовый класс для реализации паттерна Value Object.
/// Value Object — это объект, который определяется своими свойствами, а не идентификатором.
/// Они неизменяемы и должны корректно реализовывать Equals и GetHashCode.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Получает компоненты равенства для этого Value Object.
    /// Каждый наследник должен возвращать коллекцию свойств, используемых для сравнения.
    /// </summary>
    protected abstract IEnumerable<object> GetEqualityComponents();

    /// <summary>
    /// Определяет, равен ли указанный объект этому Value Object.
    /// Равенство определяется по значениям всех компонентов.
    /// </summary>
    /// <param name="obj">Объект для сравнения</param>
    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (GetType() != obj.GetType())
            return false;

        return Equals((ValueObject)obj);
    }

    /// <summary>
    /// Возвращает hash code на основе компонентов равенства.
    /// </summary>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(
                default(int),
                (hash, component) => HashCode.Combine(hash, component.GetHashCode()));
    }

    /// <summary>
    /// Определяет, равен ли указанный Value Object этому объекту.
    /// </summary>
    /// <param name="other">Value Object для сравнения</param>
    public bool Equals(ValueObject other)
    {
        if (other == null)
            return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Оператор равенства.
    /// </summary>
    public static bool operator ==(ValueObject a, ValueObject b)
    {
        if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
            return true;

        if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            return false;

        return a.Equals(b);
    }

    /// <summary>
    /// Оператор неравенства.
    /// </summary>
    public static bool operator !=(ValueObject a, ValueObject b)
    {
        return !(a == b);
    }
}

/// <summary>
/// Обобщённая версия ValueObject для удобства использования.
/// </summary>
/// <typeparam name="T">Тип наследника</typeparam>
public abstract class ValueObject<T> : ValueObject where T : ValueObject<T>
{
    /// <summary>
    /// Определяет, равен ли указанный объект типа T этому объекту.
    /// </summary>
    public bool Equals(T other)
    {
        if (other == null)
            return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj)
    {
        T? t = obj as T;
        return Equals(t);
    }

    public static bool operator ==(ValueObject<T> a, ValueObject<T> b)
    {
        if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
            return true;

        if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            return false;

        return a.Equals(b);
    }

    public static bool operator !=(ValueObject<T> a, ValueObject<T> b)
    {
        return !(a == b);
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
