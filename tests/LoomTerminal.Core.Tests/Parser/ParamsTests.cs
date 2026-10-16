// Ported from xtermjs/xterm.js: src/common/parser/Params.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.Tests.Parser;

public class ParamsTests
{
    private sealed class TestParams(int maxLength = 32, int maxSubParamsLength = 32) : Params(maxLength, maxSubParamsLength)
    {
        public int[] SubParamsStore => SubParams;

        public int SubParamsCount => SubParamsLength;
    }

    // A ParamsArray: items are int (a parameter) or int[] (the sub-parameters of the parameter before it).
    private static object[] A(params object[] items) => items;

    private static int[] S(params ReadOnlySpan<int> items) => items.ToArray();

    // `Params` parser shim
    private static void Parse(Params @params, params string[] chunks)
    {
        @params.Reset();
        @params.AddParam(0);
        foreach (string chunk in chunks)
        {
            // The upstream loop groups consecutive digits and separators in a do/while; handling one char at a time
            // gives the same result because every char goes through the same switch.
            foreach (char c in chunk)
            {
                switch (c)
                {
                    case ';':
                        @params.AddParam(0);
                        break;
                    case ':':
                        @params.AddSubParam(-1);
                        break;
                    default: // 0x30 - 0x39
                        @params.AddDigit(c - '0');
                        break;
                }
            }
        }
    }

    private static int[] Stored(Params @params) => @params.ParamValues[..@params.Length];

    // xterm.js: 'should respect ctor args'
    [Fact]
    public void Constructor_respects_its_arguments()
    {
        TestParams @params = new(12, 23);
        Assert.Equal(12, @params.ParamValues.Length);
        Assert.Equal(23, @params.SubParamsStore.Length);
        Assert.Equal(A(), @params.ToArray());
    }

    // xterm.js: 'addParam'
    [Fact]
    public void AddParam_stores_parameters()
    {
        TestParams @params = new();
        @params.AddParam(1);
        Assert.Equal(1, @params.Length);
        Assert.Equal([1], Stored(@params));
        Assert.Equal(A(1), @params.ToArray());
        @params.AddParam(23);
        Assert.Equal(2, @params.Length);
        Assert.Equal([1, 23], Stored(@params));
        Assert.Equal(A(1, 23), @params.ToArray());
        Assert.Equal(0, @params.SubParamsCount);
    }

    // xterm.js: 'addSubParam'
    [Fact]
    public void AddSubParam_stores_sub_parameters()
    {
        TestParams @params = new();
        @params.AddParam(1);
        @params.AddSubParam(2);
        @params.AddSubParam(3);
        Assert.Equal(1, @params.Length);
        Assert.Equal(2, @params.SubParamsCount);
        Assert.Equal(A(1, S(2, 3)), @params.ToArray());
        @params.AddParam(12345);
        @params.AddSubParam(-1);
        Assert.Equal(2, @params.Length);
        Assert.Equal(3, @params.SubParamsCount);
        Assert.Equal(A(1, S(2, 3), 12345, S(-1)), @params.ToArray());
    }

    // xterm.js: 'should not add sub params without previous param'
    [Fact]
    public void AddSubParam_without_a_previous_param_is_ignored()
    {
        TestParams @params = new();
        @params.AddSubParam(2);
        @params.AddSubParam(3);
        Assert.Equal(0, @params.Length);
        Assert.Equal(0, @params.SubParamsCount);
        Assert.Equal(A(), @params.ToArray());
        @params.AddParam(1);
        @params.AddSubParam(2);
        @params.AddSubParam(3);
        Assert.Equal(1, @params.Length);
        Assert.Equal(2, @params.SubParamsCount);
        Assert.Equal(A(1, S(2, 3)), @params.ToArray());
    }

    // xterm.js: 'reset'
    [Fact]
    public void Reset_empties_the_storage()
    {
        TestParams @params = new();
        @params.AddParam(1);
        @params.AddSubParam(2);
        @params.AddSubParam(3);
        @params.AddParam(12345);
        @params.AddSubParam(-1);
        @params.Reset();
        Assert.Equal(0, @params.Length);
        Assert.Equal(0, @params.SubParamsCount);
        Assert.Equal(A(), @params.ToArray());
        @params.AddParam(1);
        @params.AddSubParam(2);
        @params.AddSubParam(3);
        @params.AddParam(12345);
        @params.AddSubParam(-1);
        Assert.Equal(2, @params.Length);
        Assert.Equal(3, @params.SubParamsCount);
        Assert.Equal(A(1, S(2, 3), 12345, S(-1)), @params.ToArray());
    }

    // xterm.js: 'Params.fromArray --> toArray'
    [Fact]
    public void FromArray_then_ToArray_round_trips()
    {
        object[] data = A();
        Assert.Equal(data, Params.FromArray(data).ToArray());
        data = A(1, S(2, 3), 12345, S(-1));
        Assert.Equal(data, Params.FromArray(data).ToArray());
        data = A(38, 2, 50, 100, 150);
        Assert.Equal(data, Params.FromArray(data).ToArray());
        data = A(38, 2, 50, 100, S(150));
        Assert.Equal(data, Params.FromArray(data).ToArray());
        data = A(38, S(2, 50, 100, 150));
        Assert.Equal(data, Params.FromArray(data).ToArray());
        // strip empty sub params
        data = A(38, S(2, 50, 100, 150), 5, Array.Empty<int>(), 6);
        Assert.Equal(A(38, S(2, 50, 100, 150), 5, 6), Params.FromArray(data).ToArray());
    }

    // xterm.js: 'clone'
    [Fact]
    public void Clone_copies_every_field()
    {
        Params @params = Params.FromArray(A(38, S(2, 50, 100, 150), 5, Array.Empty<int>(), 6, 1, S(2, 3), 12345, S(-1)));
        Params clone = @params.Clone();
        Assert.NotSame(@params, clone);
        Assert.Equal(@params.Length, clone.Length);
        Assert.Equal(@params.MaxLength, clone.MaxLength);
        Assert.Equal(@params.MaxSubParamsLength, clone.MaxSubParamsLength);
        Assert.Equal(@params.ParamValues, clone.ParamValues);
        Assert.Equal(@params.ToArray(), clone.ToArray());
        Assert.Equal(@params.GetSubParamsAll(), clone.GetSubParamsAll());
    }

    // xterm.js: 'hasSubParams / getSubParams'
    [Fact]
    public void HasSubParams_and_GetSubParams_describe_the_sub_parameters()
    {
        Params @params = Params.FromArray(A(38, S(2, 50, 100, 150), 5, Array.Empty<int>(), 6));
        Assert.True(@params.HasSubParams(0));
        Assert.Equal([2, 50, 100, 150], @params.GetSubParams(0)!);
        Assert.False(@params.HasSubParams(1));
        Assert.Null(@params.GetSubParams(1));
        Assert.False(@params.HasSubParams(2));
        Assert.Null(@params.GetSubParams(2));
    }

    // xterm.js: 'getSubParamsAll'
    [Fact]
    public void GetSubParamsAll_returns_the_sub_parameters_by_index()
    {
        Params @params = Params.FromArray(A(1, S(2, 3), 7, 12345, S(-1)));
        Dictionary<int, int[]> all = @params.GetSubParamsAll();
        Assert.Equal(2, all.Count);
        Assert.Equal([2, 3], all[0]);
        Assert.Equal([-1], all[2]);
    }

    // xterm.js: 'param defaults to 0 (ZDM - zero default mode)'
    [Fact]
    public void Parse_param_defaults_to_0()
    {
        Params @params = new();
        Parse(@params, "");
        Assert.Equal(A(0), @params.ToArray());
    }

    // xterm.js: 'sub param defaults to -1'
    [Fact]
    public void Parse_sub_param_defaults_to_minus_1()
    {
        Params @params = new();
        Parse(@params, ":");
        Assert.Equal(A(0, S(-1)), @params.ToArray());
    }

    // xterm.js: 'should correctly reset on new sequence'
    [Fact]
    public void Parse_resets_on_a_new_sequence()
    {
        Params @params = new();
        Parse(@params, "1;2;3");
        Assert.Equal(A(1, 2, 3), @params.ToArray());
        Parse(@params, "4");
        Assert.Equal(A(4), @params.ToArray());
        Parse(@params, "4::123:5;6;7");
        Assert.Equal(A(4, S(-1, 123, 5), 6, 7), @params.ToArray());
        Parse(@params, "");
        Assert.Equal(A(0), @params.ToArray());
    }

    // xterm.js: 'should handle length restrictions correctly'
    [Fact]
    public void Parse_handles_length_restrictions()
    {
        // restrict to 3 params and 3 sub params
        Params @params = new(3, 3);
        Parse(@params, "1;2;3");
        Assert.Equal(A(1, 2, 3), @params.ToArray());
        Parse(@params, "4");
        Assert.Equal(A(4), @params.ToArray());
        Parse(@params, "4::123:5;6;7");
        Assert.Equal(A(4, S(-1, 123, 5), 6, 7), @params.ToArray());
        Parse(@params, "");
        Assert.Equal(A(0), @params.ToArray());
        // overlong params
        Parse(@params, "1;2;3;4;5;6;7");
        Assert.Equal(A(1, 2, 3), @params.ToArray());
        // overlong sub params
        Parse(@params, "4;38:2::50:100:150;48:5:22");
        Assert.Equal(A(4, 38, S(2, -1, 50), 48), @params.ToArray());
    }

    // xterm.js: 'typical sequences'
    [Fact]
    public void Parse_handles_typical_sequences()
    {
        Params @params = new();
        // SGR with semicolon syntax
        Parse(@params, "0;4;38;2;50;100;150;48;5;22");
        Assert.Equal(A(0, 4, 38, 2, 50, 100, 150, 48, 5, 22), @params.ToArray());
        // SGR mixed style (partly wrong)
        Parse(@params, "0;4;38;2;50:100:150;48;5:22");
        Assert.Equal(A(0, 4, 38, 2, 50, S(100, 150), 48, 5, S(22)), @params.ToArray());
        // SGR colon style
        Parse(@params, "0;4;38:2::50:100:150;48:5:22");
        Assert.Equal(A(0, 4, 38, S(2, -1, 50, 100, 150), 48, S(5, 22)), @params.ToArray());
    }

    // xterm.js: 'reject params lesser -1'
    [Fact]
    public void AddParam_rejects_params_lesser_than_minus_1()
    {
        Params @params = new();
        @params.AddParam(-1);
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => @params.AddParam(-2));
        Assert.Contains("values lesser than -1 are not allowed", error.Message);
    }

    // xterm.js: 'reject subparams lesser -1'
    [Fact]
    public void AddSubParam_rejects_sub_params_lesser_than_minus_1()
    {
        Params @params = new();
        @params.AddParam(-1);
        @params.AddSubParam(-1);
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => @params.AddSubParam(-2));
        Assert.Contains("values lesser than -1 are not allowed", error.Message);
        Assert.Equal(A(-1, S(-1)), @params.ToArray());
    }

    // xterm.js: 'clamp parsed params'
    [Fact]
    public void AddDigit_clamps_parsed_params()
    {
        Params @params = new();
        Parse(@params, "2147483648");
        Assert.Equal(A(0x7FFFFFFF), @params.ToArray());
    }

    // xterm.js: 'clamp parsed subparams'
    [Fact]
    public void AddDigit_clamps_parsed_sub_params()
    {
        Params @params = new();
        Parse(@params, ":2147483648");
        Assert.Equal(A(0, S(0x7FFFFFFF)), @params.ToArray());
    }

    // xterm.js: 'should cancel subdigits if beyond params limit'
    [Fact]
    public void Parse_cancels_sub_digits_beyond_the_params_limit()
    {
        Params @params = new();
        Parse(@params, ";;;;;;;;;10;;;;;;;;;;20;;;;;;;;;;30;31;32;33;34;35::::::::");
        Assert.Equal(
            A(
                0, 0, 0, 0, 0, 0, 0, 0, 0, 10,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 20,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 30, 31, 32),
            @params.ToArray());
    }

    // xterm.js: 'should carry forward isSub state'
    [Fact]
    public void Parse_carries_the_sub_state_forward_between_chunks()
    {
        Params @params = new();
        Parse(@params, "1:22:33", "44");
        Assert.Equal(A(1, S(22, 3344)), @params.ToArray());
    }
}
