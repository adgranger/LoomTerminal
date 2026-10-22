// Ported from xtermjs/xterm.js: addons/addon-webgl/src/CustomGlyphs.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.
using System.Globalization;

namespace LoomTerminal.Presentation.Rendering;

internal static partial class CustomGlyphs
{
    private static string I(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);

    private static Func<double, double, string> S(string path) => (_, _) => path;

    private static class Shapes
    {
        /// <summary>│</summary>
        public const string TopToBottom = "M.5,0 L.5,1";
        /// <summary>─</summary>
        public const string LeftToRight = "M0,.5 L1,.5";
        /// <summary>└</summary>
        public const string TopToRight = "M.5,0 L.5,.5 L1,.5";
        /// <summary>┘</summary>
        public const string TopToLeft = "M.5,0 L.5,.5 L0,.5";
        /// <summary>┐</summary>
        public const string LeftToBottom = "M0,.5 L.5,.5 L.5,1";
        /// <summary>┌</summary>
        public const string RightToBottom = "M0.5,1 L.5,.5 L1,.5";
        /// <summary>╵</summary>
        public const string MiddleToTop = "M.5,.5 L.5,0";
        /// <summary>╴</summary>
        public const string MiddleToLeft = "M.5,.5 L0,.5";
        /// <summary>╶</summary>
        public const string MiddleToRight = "M.5,.5 L1,.5";
        /// <summary>╷</summary>
        public const string MiddleToBottom = "M.5,.5 L.5,1";
        /// <summary>┴</summary>
        public const string TTop = "M0,.5 L1,.5 M.5,.5 L.5,0";
        /// <summary>┤</summary>
        public const string TLeft = "M.5,0 L.5,1 M.5,.5 L0,.5";
        /// <summary>├</summary>
        public const string TRight = "M.5,0 L.5,1 M.5,.5 L1,.5";
        /// <summary>┬</summary>
        public const string TBottom = "M0,.5 L1,.5 M.5,.5 L.5,1";
        /// <summary>┼</summary>
        public const string Cross = "M0,.5 L1,.5 M.5,0 L.5,1";
        /// <summary>╌</summary>
        public const string TwoDashesHorizontal = "M.1,.5 L.4,.5 M.6,.5 L.9,.5";
        /// <summary>┄</summary>
        public const string ThreeDashesHorizontal = "M.0667,.5 L.2667,.5 M.4,.5 L.6,.5 M.7333,.5 L.9333,.5";
        /// <summary>┉</summary>
        public const string FourDashesHorizontal = "M.05,.5 L.2,.5 M.3,.5 L.45,.5 M.55,.5 L.7,.5 M.8,.5 L.95,.5";
        /// <summary>╎</summary>
        public const string TwoDashesVertical = "M.5,.1 L.5,.4 M.5,.6 L.5,.9";
        /// <summary>┆</summary>
        public const string ThreeDashesVertical = "M.5,.0667 L.5,.2667 M.5,.4 L.5,.6 M.5,.7333 L.5,.9333";
        /// <summary>┊</summary>
        public const string FourDashesVertical = "M.5,.05 L.5,.2 M.5,.3 L.5,.45 L.5,.55 M.5,.7 L.5,.95";
    }

    // Normal (1 DIP) and bold (3 DIP) variants of each box-drawing character; the functions get the 15 % offsets.
    private static readonly Dictionary<int, BoxShape> BoxDrawingDefinitions = new()
    {
        [0x2500] = new(S(Shapes.LeftToRight), null), // ─
        [0x2501] = new(null, S(Shapes.LeftToRight)), // ━
        [0x2502] = new(S(Shapes.TopToBottom), null), // │
        [0x2503] = new(null, S(Shapes.TopToBottom)), // ┃
        [0x250C] = new(S(Shapes.RightToBottom), null), // ┌
        [0x250F] = new(null, S(Shapes.RightToBottom)), // ┏
        [0x2510] = new(S(Shapes.LeftToBottom), null), // ┐
        [0x2513] = new(null, S(Shapes.LeftToBottom)), // ┓
        [0x2514] = new(S(Shapes.TopToRight), null), // └
        [0x2517] = new(null, S(Shapes.TopToRight)), // ┗
        [0x2518] = new(S(Shapes.TopToLeft), null), // ┘
        [0x251B] = new(null, S(Shapes.TopToLeft)), // ┛
        [0x251C] = new(S(Shapes.TRight), null), // ├
        [0x2523] = new(null, S(Shapes.TRight)), // ┣
        [0x2524] = new(S(Shapes.TLeft), null), // ┤
        [0x252B] = new(null, S(Shapes.TLeft)), // ┫
        [0x252C] = new(S(Shapes.TBottom), null), // ┬
        [0x2533] = new(null, S(Shapes.TBottom)), // ┳
        [0x2534] = new(S(Shapes.TTop), null), // ┴
        [0x253B] = new(null, S(Shapes.TTop)), // ┻
        [0x253C] = new(S(Shapes.Cross), null), // ┼
        [0x254B] = new(null, S(Shapes.Cross)), // ╋
        [0x2574] = new(S(Shapes.MiddleToLeft), null), // ╴
        [0x2578] = new(null, S(Shapes.MiddleToLeft)), // ╸
        [0x2575] = new(S(Shapes.MiddleToTop), null), // ╵
        [0x2579] = new(null, S(Shapes.MiddleToTop)), // ╹
        [0x2576] = new(S(Shapes.MiddleToRight), null), // ╶
        [0x257A] = new(null, S(Shapes.MiddleToRight)), // ╺
        [0x2577] = new(S(Shapes.MiddleToBottom), null), // ╷
        [0x257B] = new(null, S(Shapes.MiddleToBottom)), // ╻
        [0x2550] = new((xp, yp) => I($"M0,{.5 - yp} L1,{.5 - yp} M0,{.5 + yp} L1,{.5 + yp}"), null), // ═
        [0x2551] = new((xp, yp) => I($"M{.5 - xp},0 L{.5 - xp},1 M{.5 + xp},0 L{.5 + xp},1"), null), // ║
        [0x2552] = new((xp, yp) => I($"M.5,1 L.5,{.5 - yp} L1,{.5 - yp} M.5,{.5 + yp} L1,{.5 + yp}"), null), // ╒
        [0x2553] = new((xp, yp) => I($"M{.5 - xp},1 L{.5 - xp},.5 L1,.5 M{.5 + xp},.5 L{.5 + xp},1"), null), // ╓
        [0x2554] = new((xp, yp) => I($"M1,{.5 - yp} L{.5 - xp},{.5 - yp} L{.5 - xp},1 M1,{.5 + yp} L{.5 + xp},{.5 + yp} L{.5 + xp},1"), null), // ╔
        [0x2555] = new((xp, yp) => I($"M0,{.5 - yp} L.5,{.5 - yp} L.5,1 M0,{.5 + yp} L.5,{.5 + yp}"), null), // ╕
        [0x2556] = new((xp, yp) => I($"M{.5 + xp},1 L{.5 + xp},.5 L0,.5 M{.5 - xp},.5 L{.5 - xp},1"), null), // ╖
        [0x2557] = new((xp, yp) => I($"M0,{.5 + yp} L{.5 - xp},{.5 + yp} L{.5 - xp},1 M0,{.5 - yp} L{.5 + xp},{.5 - yp} L{.5 + xp},1"), null), // ╗
        [0x2558] = new((xp, yp) => I($"M.5,0 L.5,{.5 + yp} L1,{.5 + yp} M.5,{.5 - yp} L1,{.5 - yp}"), null), // ╘
        [0x2559] = new((xp, yp) => I($"M1,.5 L{.5 - xp},.5 L{.5 - xp},0 M{.5 + xp},.5 L{.5 + xp},0"), null), // ╙
        [0x255A] = new((xp, yp) => I($"M1,{.5 - yp} L{.5 + xp},{.5 - yp} L{.5 + xp},0 M1,{.5 + yp} L{.5 - xp},{.5 + yp} L{.5 - xp},0"), null), // ╚
        [0x255B] = new((xp, yp) => I($"M0,{.5 + yp} L.5,{.5 + yp} L.5,0 M0,{.5 - yp} L.5,{.5 - yp}"), null), // ╛
        [0x255C] = new((xp, yp) => I($"M0,.5 L{.5 + xp},.5 L{.5 + xp},0 M{.5 - xp},.5 L{.5 - xp},0"), null), // ╜
        [0x255D] = new((xp, yp) => I($"M0,{.5 - yp} L{.5 - xp},{.5 - yp} L{.5 - xp},0 M0,{.5 + yp} L{.5 + xp},{.5 + yp} L{.5 + xp},0"), null), // ╝
        [0x255E] = new((xp, yp) => I($"{Shapes.TopToBottom} M.5,{.5 - yp} L1,{.5 - yp} M.5,{.5 + yp} L1,{.5 + yp}"), null), // ╞
        [0x255F] = new((xp, yp) => I($"M{.5 - xp},0 L{.5 - xp},1 M{.5 + xp},0 L{.5 + xp},1 M{.5 + xp},.5 L1,.5"), null), // ╟
        [0x2560] = new((xp, yp) => I($"M{.5 - xp},0 L{.5 - xp},1 M1,{.5 + yp} L{.5 + xp},{.5 + yp} L{.5 + xp},1 M1,{.5 - yp} L{.5 + xp},{.5 - yp} L{.5 + xp},0"), null), // ╠
        [0x2561] = new((xp, yp) => I($"{Shapes.TopToBottom} M0,{.5 - yp} L.5,{.5 - yp} M0,{.5 + yp} L.5,{.5 + yp}"), null), // ╡
        [0x2562] = new((xp, yp) => I($"M0,.5 L{.5 - xp},.5 M{.5 - xp},0 L{.5 - xp},1 M{.5 + xp},0 L{.5 + xp},1"), null), // ╢
        [0x2563] = new((xp, yp) => I($"M{.5 + xp},0 L{.5 + xp},1 M0,{.5 + yp} L{.5 - xp},{.5 + yp} L{.5 - xp},1 M0,{.5 - yp} L{.5 - xp},{.5 - yp} L{.5 - xp},0"), null), // ╣
        [0x2564] = new((xp, yp) => I($"M0,{.5 - yp} L1,{.5 - yp} M0,{.5 + yp} L1,{.5 + yp} M.5,{.5 + yp} L.5,1"), null), // ╤
        [0x2565] = new((xp, yp) => I($"{Shapes.LeftToRight} M{.5 - xp},.5 L{.5 - xp},1 M{.5 + xp},.5 L{.5 + xp},1"), null), // ╥
        [0x2566] = new((xp, yp) => I($"M0,{.5 - yp} L1,{.5 - yp} M0,{.5 + yp} L{.5 - xp},{.5 + yp} L{.5 - xp},1 M1,{.5 + yp} L{.5 + xp},{.5 + yp} L{.5 + xp},1"), null), // ╦
        [0x2567] = new((xp, yp) => I($"M.5,0 L.5,{.5 - yp} M0,{.5 - yp} L1,{.5 - yp} M0,{.5 + yp} L1,{.5 + yp}"), null), // ╧
        [0x2568] = new((xp, yp) => I($"{Shapes.LeftToRight} M{.5 - xp},.5 L{.5 - xp},0 M{.5 + xp},.5 L{.5 + xp},0"), null), // ╨
        [0x2569] = new((xp, yp) => I($"M0,{.5 + yp} L1,{.5 + yp} M0,{.5 - yp} L{.5 - xp},{.5 - yp} L{.5 - xp},0 M1,{.5 - yp} L{.5 + xp},{.5 - yp} L{.5 + xp},0"), null), // ╩
        [0x256A] = new((xp, yp) => I($"{Shapes.TopToBottom} M0,{.5 - yp} L1,{.5 - yp} M0,{.5 + yp} L1,{.5 + yp}"), null), // ╪
        [0x256B] = new((xp, yp) => I($"{Shapes.LeftToRight} M{.5 - xp},0 L{.5 - xp},1 M{.5 + xp},0 L{.5 + xp},1"), null), // ╫
        [0x256C] = new((xp, yp) => I($"M0,{.5 + yp} L{.5 - xp},{.5 + yp} L{.5 - xp},1 M1,{.5 + yp} L{.5 + xp},{.5 + yp} L{.5 + xp},1 M0,{.5 - yp} L{.5 - xp},{.5 - yp} L{.5 - xp},0 M1,{.5 - yp} L{.5 + xp},{.5 - yp} L{.5 + xp},0"), null), // ╬
        [0x2571] = new(S("M1,0 L0,1"), null), // ╱
        [0x2572] = new(S("M0,0 L1,1"), null), // ╲
        [0x2573] = new(S("M1,0 L0,1 M0,0 L1,1"), null), // ╳
        [0x257C] = new(S(Shapes.MiddleToLeft), S(Shapes.MiddleToRight)), // ╼
        [0x257D] = new(S(Shapes.MiddleToTop), S(Shapes.MiddleToBottom)), // ╽
        [0x257E] = new(S(Shapes.MiddleToRight), S(Shapes.MiddleToLeft)), // ╾
        [0x257F] = new(S(Shapes.MiddleToBottom), S(Shapes.MiddleToTop)), // ╿
        [0x250D] = new(S(Shapes.MiddleToBottom), S(Shapes.MiddleToRight)), // ┍
        [0x250E] = new(S(Shapes.MiddleToRight), S(Shapes.MiddleToBottom)), // ┎
        [0x2511] = new(S(Shapes.MiddleToBottom), S(Shapes.MiddleToLeft)), // ┑
        [0x2512] = new(S(Shapes.MiddleToLeft), S(Shapes.MiddleToBottom)), // ┒
        [0x2515] = new(S(Shapes.MiddleToTop), S(Shapes.MiddleToRight)), // ┕
        [0x2516] = new(S(Shapes.MiddleToRight), S(Shapes.MiddleToTop)), // ┖
        [0x2519] = new(S(Shapes.MiddleToTop), S(Shapes.MiddleToLeft)), // ┙
        [0x251A] = new(S(Shapes.MiddleToLeft), S(Shapes.MiddleToTop)), // ┚
        [0x251D] = new(S(Shapes.TopToBottom), S(Shapes.MiddleToRight)), // ┝
        [0x251E] = new(S(Shapes.RightToBottom), S(Shapes.MiddleToTop)), // ┞
        [0x251F] = new(S(Shapes.TopToRight), S(Shapes.MiddleToBottom)), // ┟
        [0x2520] = new(S(Shapes.MiddleToRight), S(Shapes.TopToBottom)), // ┠
        [0x2521] = new(S(Shapes.MiddleToBottom), S(Shapes.TopToRight)), // ┡
        [0x2522] = new(S(Shapes.MiddleToTop), S(Shapes.RightToBottom)), // ┢
        [0x2525] = new(S(Shapes.TopToBottom), S(Shapes.MiddleToLeft)), // ┥
        [0x2526] = new(S(Shapes.LeftToBottom), S(Shapes.MiddleToTop)), // ┦
        [0x2527] = new(S(Shapes.TopToLeft), S(Shapes.MiddleToBottom)), // ┧
        [0x2528] = new(S(Shapes.MiddleToLeft), S(Shapes.TopToBottom)), // ┨
        [0x2529] = new(S(Shapes.MiddleToBottom), S(Shapes.TopToLeft)), // ┩
        [0x252A] = new(S(Shapes.MiddleToTop), S(Shapes.LeftToBottom)), // ┪
        [0x252D] = new(S(Shapes.RightToBottom), S(Shapes.MiddleToLeft)), // ┭
        [0x252E] = new(S(Shapes.LeftToBottom), S(Shapes.MiddleToRight)), // ┮
        [0x252F] = new(S(Shapes.MiddleToBottom), S(Shapes.LeftToRight)), // ┯
        [0x2530] = new(S(Shapes.LeftToRight), S(Shapes.MiddleToBottom)), // ┰
        [0x2531] = new(S(Shapes.MiddleToRight), S(Shapes.LeftToBottom)), // ┱
        [0x2532] = new(S(Shapes.MiddleToLeft), S(Shapes.RightToBottom)), // ┲
        [0x2535] = new(S(Shapes.TopToRight), S(Shapes.MiddleToLeft)), // ┵
        [0x2536] = new(S(Shapes.TopToLeft), S(Shapes.MiddleToRight)), // ┶
        [0x2537] = new(S(Shapes.MiddleToTop), S(Shapes.LeftToRight)), // ┷
        [0x2538] = new(S(Shapes.LeftToRight), S(Shapes.MiddleToTop)), // ┸
        [0x2539] = new(S(Shapes.MiddleToRight), S(Shapes.TopToLeft)), // ┹
        [0x253A] = new(S(Shapes.MiddleToLeft), S(Shapes.TopToRight)), // ┺
        [0x253D] = new(S($"{Shapes.TopToBottom} {Shapes.MiddleToRight}"), S(Shapes.MiddleToLeft)), // ┽
        [0x253E] = new(S($"{Shapes.TopToBottom} {Shapes.MiddleToLeft}"), S(Shapes.MiddleToRight)), // ┾
        [0x253F] = new(S(Shapes.TopToBottom), S(Shapes.LeftToRight)), // ┿
        [0x2540] = new(S($"{Shapes.LeftToRight} {Shapes.MiddleToBottom}"), S(Shapes.MiddleToTop)), // ╀
        [0x2541] = new(S($"{Shapes.MiddleToTop} {Shapes.LeftToRight}"), S(Shapes.MiddleToBottom)), // ╁
        [0x2542] = new(S(Shapes.LeftToRight), S(Shapes.TopToBottom)), // ╂
        [0x2543] = new(S(Shapes.RightToBottom), S(Shapes.TopToLeft)), // ╃
        [0x2544] = new(S(Shapes.LeftToBottom), S(Shapes.TopToRight)), // ╄
        [0x2545] = new(S(Shapes.TopToRight), S(Shapes.LeftToBottom)), // ╅
        [0x2546] = new(S(Shapes.TopToLeft), S(Shapes.RightToBottom)), // ╆
        [0x2547] = new(S(Shapes.MiddleToBottom), S($"{Shapes.MiddleToTop} {Shapes.LeftToRight}")), // ╇
        [0x2548] = new(S(Shapes.MiddleToTop), S($"{Shapes.LeftToRight} {Shapes.MiddleToBottom}")), // ╈
        [0x2549] = new(S(Shapes.MiddleToRight), S($"{Shapes.TopToBottom} {Shapes.MiddleToLeft}")), // ╉
        [0x254A] = new(S(Shapes.MiddleToLeft), S($"{Shapes.TopToBottom} {Shapes.MiddleToRight}")), // ╊
        [0x254C] = new(S(Shapes.TwoDashesHorizontal), null), // ╌
        [0x254D] = new(null, S(Shapes.TwoDashesHorizontal)), // ╍
        [0x2504] = new(S(Shapes.ThreeDashesHorizontal), null), // ┄
        [0x2505] = new(null, S(Shapes.ThreeDashesHorizontal)), // ┅
        [0x2508] = new(S(Shapes.FourDashesHorizontal), null), // ┈
        [0x2509] = new(null, S(Shapes.FourDashesHorizontal)), // ┉
        [0x254E] = new(S(Shapes.TwoDashesVertical), null), // ╎
        [0x254F] = new(null, S(Shapes.TwoDashesVertical)), // ╏
        [0x2506] = new(S(Shapes.ThreeDashesVertical), null), // ┆
        [0x2507] = new(null, S(Shapes.ThreeDashesVertical)), // ┇
        [0x250A] = new(S(Shapes.FourDashesVertical), null), // ┊
        [0x250B] = new(null, S(Shapes.FourDashesVertical)), // ┋
        [0x256D] = new((xp, yp) => I($"M.5,1 L.5,{.5 + (yp / .15 * .5)} C.5,{.5 + (yp / .15 * .5)},.5,.5,1,.5"), null), // ╭
        [0x256E] = new((xp, yp) => I($"M.5,1 L.5,{.5 + (yp / .15 * .5)} C.5,{.5 + (yp / .15 * .5)},.5,.5,0,.5"), null), // ╮
        [0x256F] = new((xp, yp) => I($"M.5,0 L.5,{.5 - (yp / .15 * .5)} C.5,{.5 - (yp / .15 * .5)},.5,.5,0,.5"), null), // ╯
        [0x2570] = new((xp, yp) => I($"M.5,0 L.5,{.5 - (yp / .15 * .5)} C.5,{.5 - (yp / .15 * .5)},.5,.5,1,.5"), null), // ╰
    };
}
