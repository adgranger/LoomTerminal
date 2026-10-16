// Ported from xtermjs/xterm.js: src/common/parser/Params.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Parser;

/// <summary>
/// Params storage class. Used by the parser to accumulate sequence parameters and sub-parameters and transmit them
/// to the input handler actions.
/// </summary>
/// <remarks>
/// <para>Threading: not thread-safe; used from the parser thread only.</para>
/// <para>
/// Hardcoded limitations: the maximum value of a single (sub-)parameter is 2^31 - 1 (greater values are clamped), at
/// most 256 sub-parameters, and no negative value besides -1 (the placeholder for the default value).
/// </para>
/// <para>
/// ZDM (Zero Default Mode) is not orchestrated by this class: in ZDM the parser adds 0 for empty parameters,
/// otherwise -1. Empty sub-parameters are always added with -1.
/// </para>
/// </remarks>
internal class Params : IParams
{
    // max value supported for a single param/subparam (clamped to positive int32 range)
    private const int MaxValue = 0x7FFFFFFF;

    // max allowed subparams for a single sequence (hardcoded limitation)
    private const int MaxSubParams = 256;

    // sub params offsets from param: param idx --> [start, end] offset
    private readonly ushort[] _subParamsIdx;
    private bool _rejectDigits;
    private bool _rejectSubDigits;
    private bool _digitIsSub;

    /// <summary>Creates an empty storage.</summary>
    /// <param name="maxLength">The maximum number of storable parameters.</param>
    /// <param name="maxSubParamsLength">The maximum number of storable sub-parameters.</param>
    public Params(int maxLength = 32, int maxSubParamsLength = 32)
    {
        if (maxSubParamsLength > MaxSubParams)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSubParamsLength), "maxSubParamsLength must not be greater than 256");
        }

        MaxLength = maxLength;
        MaxSubParamsLength = maxSubParamsLength;
        ParamValues = new int[maxLength];
        Length = 0;
        SubParams = new int[maxSubParamsLength];
        SubParamsLength = 0;
        _subParamsIdx = new ushort[maxLength];
    }

    /// <inheritdoc />
    public int MaxLength { get; }

    /// <inheritdoc />
    public int MaxSubParamsLength { get; }

    /// <inheritdoc />
    public int[] ParamValues { get; }

    /// <inheritdoc />
    public int Length { get; private set; }

    /// <summary>The sub-parameter store.</summary>
    protected int[] SubParams { get; }

    /// <summary>The number of stored sub-parameters.</summary>
    protected int SubParamsLength { get; private set; }

    /// <summary>
    /// Creates a <see cref="Params"/> from its array representation (see <see cref="ToArray"/>): items are <c>int</c>
    /// or <c>int[]</c>. Leading sub-parameters are skipped.
    /// </summary>
    /// <param name="values">The parameters and sub-parameters.</param>
    public static Params FromArray(object[] values)
    {
        Params @params = new();
        if (values.Length == 0)
        {
            return @params;
        }

        // skip leading sub params
        for (int i = values[0] is int[] ? 1 : 0; i < values.Length; ++i)
        {
            if (values[i] is int[] subParams)
            {
                foreach (int subParam in subParams)
                {
                    @params.AddSubParam(subParam);
                }
            }
            else
            {
                @params.AddParam((int)values[i]);
            }
        }

        return @params;
    }

    /// <inheritdoc />
    IParams IParams.Clone() => Clone();

    /// <summary>Clones the object.</summary>
    public Params Clone()
    {
        Params newParams = new(MaxLength, MaxSubParamsLength);
        ParamValues.CopyTo(newParams.ParamValues, 0);
        newParams.Length = Length;
        SubParams.CopyTo(newParams.SubParams, 0);
        newParams.SubParamsLength = SubParamsLength;
        _subParamsIdx.CopyTo(newParams._subParamsIdx, 0);
        newParams._rejectDigits = _rejectDigits;
        newParams._rejectSubDigits = _rejectSubDigits;
        newParams._digitIsSub = _digitIsSub;
        return newParams;
    }

    /// <inheritdoc />
    public object[] ToArray()
    {
        List<object> res = [];
        for (int i = 0; i < Length; ++i)
        {
            res.Add(ParamValues[i]);
            int start = _subParamsIdx[i] >> 8;
            int end = _subParamsIdx[i] & 0xFF;
            if (end - start > 0)
            {
                res.Add(SubParams[start..end]);
            }
        }

        return [.. res];
    }

    /// <inheritdoc />
    public void Reset()
    {
        Length = 0;
        SubParamsLength = 0;
        _rejectDigits = false;
        _rejectSubDigits = false;
        _digitIsSub = false;
    }

    /// <summary>
    /// Adds a parameter value. Only up to <see cref="MaxLength"/> parameters are stored, any later parameter is
    /// ignored. (VT devices stored up to 16 values, xterm up to 30.)
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is lesser than -1.</exception>
    public void AddParam(int value)
    {
        _digitIsSub = false;
        if (Length >= MaxLength)
        {
            _rejectDigits = true;
            return;
        }

        if (value < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "values lesser than -1 are not allowed");
        }

        _subParamsIdx[Length] = (ushort)((SubParamsLength << 8) | SubParamsLength);
        ParamValues[Length++] = value;
    }

    /// <summary>
    /// Adds a sub-parameter value, associated with the last parameter (it is ignored when there is none). Only up to
    /// <see cref="MaxSubParamsLength"/> sub-parameters are stored, any later one is ignored.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is lesser than -1.</exception>
    public void AddSubParam(int value)
    {
        _digitIsSub = true;
        if (Length == 0)
        {
            return;
        }

        if (_rejectDigits || SubParamsLength >= MaxSubParamsLength)
        {
            _rejectSubDigits = true;
            return;
        }

        if (value < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "values lesser than -1 are not allowed");
        }

        SubParams[SubParamsLength++] = value;
        _subParamsIdx[Length - 1]++;
    }

    /// <inheritdoc />
    public bool HasSubParams(int idx) => (_subParamsIdx[idx] & 0xFF) - (_subParamsIdx[idx] >> 8) > 0;

    /// <inheritdoc />
    public int[]? GetSubParams(int idx)
    {
        int start = _subParamsIdx[idx] >> 8;
        int end = _subParamsIdx[idx] & 0xFF;
        if (end - start > 0)
        {
            return SubParams[start..end];
        }

        return null;
    }

    /// <inheritdoc />
    public Dictionary<int, int[]> GetSubParamsAll()
    {
        Dictionary<int, int[]> result = [];
        for (int i = 0; i < Length; ++i)
        {
            int start = _subParamsIdx[i] >> 8;
            int end = _subParamsIdx[i] & 0xFF;
            if (end - start > 0)
            {
                result[i] = SubParams[start..end];
            }
        }

        return result;
    }

    /// <summary>Adds a single digit to the current parameter. Used by the parser to account digits char by char.</summary>
    /// <param name="value">The digit (0-9).</param>
    public void AddDigit(int value)
    {
        int length = _digitIsSub ? SubParamsLength : Length;
        if (_rejectDigits || length == 0 || (_digitIsSub && _rejectSubDigits))
        {
            return;
        }

        int[] store = _digitIsSub ? SubParams : ParamValues;
        int cur = store[length - 1];
        store[length - 1] = cur != -1 ? (int)Math.Min((long)cur * 10 + value, MaxValue) : value;
    }
}
