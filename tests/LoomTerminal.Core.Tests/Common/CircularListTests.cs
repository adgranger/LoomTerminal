// Ported from xtermjs/xterm.js: src/common/CircularList.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Tests.Common;

public class CircularListTests
{
    private static CircularList<string> Make(int maxLength, params string[] values)
    {
        CircularList<string> list = new(maxLength);
        foreach (string value in values)
        {
            list.Push(value);
        }

        return list;
    }

    // xterm.js: 'should push values onto the array'
    [Fact]
    public void Push_pushes_values_onto_the_array()
    {
        CircularList<string> list = Make(5, "1", "2", "3", "4", "5");
        Assert.Equal("1", list.Get(0));
        Assert.Equal("2", list.Get(1));
        Assert.Equal("3", list.Get(2));
        Assert.Equal("4", list.Get(3));
        Assert.Equal("5", list.Get(4));
    }

    // xterm.js: 'should push old values from the start out of the array when max length is reached'
    [Fact]
    public void Push_pushes_old_values_out_when_max_length_is_reached()
    {
        CircularList<string> list = Make(2, "1", "2");
        Assert.Equal("1", list.Get(0));
        Assert.Equal("2", list.Get(1));
        list.Push("3");
        Assert.Equal("2", list.Get(0));
        Assert.Equal("3", list.Get(1));
        list.Push("4");
        Assert.Equal("3", list.Get(0));
        Assert.Equal("4", list.Get(1));
    }

    // xterm.js: 'should increase the size of the list'
    [Fact]
    public void MaxLength_increases_the_size_of_the_list()
    {
        CircularList<string> list = Make(2, "1", "2");
        Assert.Equal("1", list.Get(0));
        Assert.Equal("2", list.Get(1));
        list.MaxLength = 4;
        list.Push("3");
        list.Push("4");
        Assert.Equal("1", list.Get(0));
        Assert.Equal("2", list.Get(1));
        Assert.Equal("3", list.Get(2));
        Assert.Equal("4", list.Get(3));
        list.Push("wrapped");
        Assert.Equal("2", list.Get(0));
        Assert.Equal("3", list.Get(1));
        Assert.Equal("4", list.Get(2));
        Assert.Equal("wrapped", list.Get(3));
    }

    // xterm.js: 'should return the maximum length of the list'
    [Fact]
    public void MaxLength_returns_the_maximum_length_of_the_list()
    {
        CircularList<string> list = new(2);
        Assert.Equal(2, list.MaxLength);
        list.Push("1");
        list.Push("2");
        Assert.Equal(2, list.MaxLength);
        list.Push("3");
        Assert.Equal(2, list.MaxLength);
        list.MaxLength = 4;
        Assert.Equal(4, list.MaxLength);
    }

    // xterm.js: 'should return the current length of the list, capped at the maximum length'
    [Fact]
    public void Length_is_the_current_length_capped_at_the_maximum_length()
    {
        CircularList<string> list = new(2);
        Assert.Equal(0, list.Length);
        list.Push("1");
        Assert.Equal(1, list.Length);
        list.Push("2");
        Assert.Equal(2, list.Length);
        list.Push("3");
        Assert.Equal(2, list.Length);
    }

    // xterm.js: 'should delete items'
    [Fact]
    public void Splice_deletes_items()
    {
        CircularList<string> list = Make(2, "1", "2");
        list.Splice(0, 1);
        Assert.Equal(1, list.Length);
        Assert.Equal("2", list.Get(0));
        list.Push("3");
        list.Splice(1, 1);
        Assert.Equal(1, list.Length);
        Assert.Equal("2", list.Get(0));
    }

    // xterm.js: 'should insert items'
    [Fact]
    public void Splice_inserts_items()
    {
        CircularList<string> list = Make(2, "1");
        list.Splice(0, 0, "2");
        Assert.Equal(2, list.Length);
        Assert.Equal("2", list.Get(0));
        Assert.Equal("1", list.Get(1));
        list.Splice(1, 0, "3");
        Assert.Equal(2, list.Length);
        Assert.Equal("3", list.Get(0));
        Assert.Equal("1", list.Get(1));
    }

    // xterm.js: 'should delete items then insert items'
    [Fact]
    public void Splice_deletes_items_then_inserts_items()
    {
        CircularList<string> list = Make(3, "1", "2");
        list.Splice(0, 1, "3", "4");
        Assert.Equal(3, list.Length);
        Assert.Equal("3", list.Get(0));
        Assert.Equal("4", list.Get(1));
        Assert.Equal("2", list.Get(2));
    }

    // xterm.js: 'should wrap the array correctly when more items are inserted than deleted'
    [Fact]
    public void Splice_wraps_the_array_when_more_items_are_inserted_than_deleted()
    {
        CircularList<string> list = Make(3, "1", "2");
        list.Splice(1, 0, "3", "4");
        Assert.Equal(3, list.Length);
        Assert.Equal("3", list.Get(0));
        Assert.Equal("4", list.Get(1));
        Assert.Equal("2", list.Get(2));
    }

    // xterm.js: 'should remove items from the beginning of the list'
    [Fact]
    public void TrimStart_removes_items_from_the_beginning_of_the_list()
    {
        CircularList<string> list = Make(5, "1", "2", "3", "4", "5");
        list.TrimStart(1);
        Assert.Equal(4, list.Length);
        Assert.Equal("2", list.Get(0));
        Assert.Equal("3", list.Get(1));
        Assert.Equal("4", list.Get(2));
        Assert.Equal("5", list.Get(3));
        list.TrimStart(2);
        Assert.Equal(2, list.Length);
        Assert.Equal("4", list.Get(0));
        Assert.Equal("5", list.Get(1));
    }

    // xterm.js: 'should remove all items if the requested trim amount is larger than the list's length'
    [Fact]
    public void TrimStart_removes_all_items_if_the_amount_is_larger_than_the_length()
    {
        CircularList<string> list = Make(5, "1");
        list.TrimStart(2);
        Assert.Equal(0, list.Length);
    }

    // xterm.js: 'should not mutate the list when count is 0'
    [Fact]
    public void ShiftElements_does_not_mutate_the_list_when_count_is_0()
    {
        CircularList<string> list = Make(5, "1", "2");
        list.ShiftElements(0, 0, 1);
        Assert.Equal(2, list.Length);
        Assert.Equal("1", list.Get(0));
        Assert.Equal("2", list.Get(1));
    }

    // xterm.js: 'should throw for invalid args'
    [Fact]
    public void ShiftElements_throws_for_invalid_args()
    {
        CircularList<string> list = Make(5, "1");
        Assert.Contains("start argument out of range", Assert.Throws<ArgumentOutOfRangeException>(() => list.ShiftElements(-1, 1, 1)).Message);
        Assert.Contains("start argument out of range", Assert.Throws<ArgumentOutOfRangeException>(() => list.ShiftElements(1, 1, 1)).Message);
        Assert.Contains("Cannot shift elements in list beyond index 0", Assert.Throws<ArgumentOutOfRangeException>(() => list.ShiftElements(0, 1, -1)).Message);
    }

    // xterm.js: 'should shift an element forward'
    [Fact]
    public void ShiftElements_shifts_an_element_forward()
    {
        CircularList<string> list = Make(5, "1", "2");
        list.ShiftElements(0, 1, 1);
        Assert.Equal(2, list.Length);
        Assert.Equal("1", list.Get(0));
        Assert.Equal("1", list.Get(1));
    }

    // xterm.js: 'should shift elements forward'
    [Fact]
    public void ShiftElements_shifts_elements_forward()
    {
        CircularList<string> list = Make(5, "1", "2", "3", "4");
        list.ShiftElements(0, 2, 2);
        Assert.Equal(4, list.Length);
        Assert.Equal("1", list.Get(0));
        Assert.Equal("2", list.Get(1));
        Assert.Equal("1", list.Get(2));
        Assert.Equal("2", list.Get(3));
    }

    // xterm.js: 'should shift elements forward, expanding the list if needed'
    [Fact]
    public void ShiftElements_shifts_elements_forward_expanding_the_list_if_needed()
    {
        CircularList<string> list = Make(5, "1", "2");
        list.ShiftElements(0, 2, 2);
        Assert.Equal(4, list.Length);
        Assert.Equal("1", list.Get(0));
        Assert.Equal("2", list.Get(1));
        Assert.Equal("1", list.Get(2));
        Assert.Equal("2", list.Get(3));
    }

    // xterm.js: 'should shift elements forward, wrapping the list if needed'
    [Fact]
    public void ShiftElements_shifts_elements_forward_wrapping_the_list_if_needed()
    {
        CircularList<string> list = Make(5, "1", "2", "3", "4", "5");
        list.ShiftElements(2, 2, 3);
        Assert.Equal(5, list.Length);
        Assert.Equal("3", list.Get(0));
        Assert.Equal("4", list.Get(1));
        Assert.Equal("5", list.Get(2));
        Assert.Equal("3", list.Get(3));
        Assert.Equal("4", list.Get(4));
    }

    // xterm.js: 'should shift an element backwards'
    [Fact]
    public void ShiftElements_shifts_an_element_backwards()
    {
        CircularList<string> list = Make(5, "1", "2");
        list.ShiftElements(1, 1, -1);
        Assert.Equal(2, list.Length);
        Assert.Equal("2", list.Get(0));
        Assert.Equal("2", list.Get(1));
    }

    // xterm.js: 'should shift elements backwards'
    [Fact]
    public void ShiftElements_shifts_elements_backwards()
    {
        CircularList<string> list = Make(5, "1", "2", "3", "4");
        list.ShiftElements(2, 2, -2);
        Assert.Equal(4, list.Length);
        Assert.Equal("3", list.Get(0));
        Assert.Equal("4", list.Get(1));
        Assert.Equal("3", list.Get(2));
        Assert.Equal("4", list.Get(3));
    }
}
