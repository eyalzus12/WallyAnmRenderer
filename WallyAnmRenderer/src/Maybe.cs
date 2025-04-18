using System;
using System.Diagnostics.CodeAnalysis;

namespace WallyAnmRenderer;

// simple optional type
public readonly struct Maybe<T>
{
    private readonly bool _hasValue = false;
    private readonly T _value = default!;

    public Maybe()
    {
        _hasValue = false;
        _value = default!;
    }

    public Maybe(T value)
    {
        _hasValue = true;
        _value = value;
    }

    public bool HasValue => _hasValue;

    public T Value => _hasValue ? _value : throw new InvalidOperationException("Attempt to get value of none type");
    public T ValueOrThrow(Exception e) => _hasValue ? _value : throw e;
    public T ValueUnsafe => _value;

    public bool TryGetValue([MaybeNullWhen(false)] out T t)
    {
        t = _hasValue ? _value : default;
        return _hasValue;
    }

    public T ValueOr(T t) => _hasValue ? _value : t;
    public T ValueOr(Func<T> func) => _hasValue ? _value : func();

    public Maybe<U> Map<U>(Func<T, U> operation) =>
    _hasValue
        ? operation(_value)
        : Maybe<U>.None;

    public Maybe<U> Bind<U>(Func<T, Maybe<U>> operation) =>
    _hasValue
        ? operation(_value)
        : Maybe<U>.None;

    public Maybe<R> Combine<U, R>(Maybe<U> other, Func<T, U, R> operation) =>
    _hasValue && other._hasValue
        ? operation(_value, other._value)
        : Maybe<R>.None;

    public Maybe<R> CombineBind<U, R>(Maybe<U> other, Func<T, U, Maybe<R>> operation) =>
    _hasValue && other._hasValue
        ? operation(_value, other._value)
        : Maybe<R>.None;

    public Maybe<U> Cast<U>() =>
    _hasValue
        ? (U)(object?)_value!
        : Maybe<U>.None;

    public Maybe<T> Do(Action<T> ifSome, Action ifNone)
    {
        if (_hasValue)
            ifSome(_value);
        else
            ifNone();
        return this;
    }

    public Maybe<T> DoIfSome(Action<T> ifSome)
    {
        if (_hasValue)
            ifSome(_value);
        return this;
    }

    public Maybe<T> DoIfNone(Action ifNone)
    {
        if (!_hasValue)
            ifNone();
        return this;
    }

    public Maybe<T> NoneIf(Predicate<T> predicate) => _hasValue && !predicate(_value) ? _value : None;
    public Maybe<T> SomeIf(Predicate<T> predicate) => _hasValue && predicate(_value) ? _value : None;

    public static Maybe<T> Some(T value) => new(value);
    public static Maybe<T> None => new();
    public static implicit operator Maybe<T>(T value) => new(value);
}

public static class Maybe
{
    public static Maybe<T> Some<T>(T value) => Maybe<T>.Some(value);
    public static Maybe<T> None<T>() => Maybe<T>.None;

    public static Maybe<T> FromNullable<T>(T? value)
    {
        if (value is null) return Maybe<T>.None;
        return value;
    }

    public static Maybe<T> Unwrap<T>(this Maybe<Maybe<T>> maybe)
    {
        if (maybe.TryGetValue(out Maybe<T> inside))
            return inside;
        return Maybe<T>.None;
    }
}