// Ported from xtermjs/xterm.js: src/common/data/EscapeSequences.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Data;

/// <summary>C0 control codes (https://en.wikipedia.org/wiki/C0_and_C1_control_codes) as strings.</summary>
/// <remarks>Threading: constants only.</remarks>
internal static class C0
{
    /// <summary>Null (Caret = ^@, C = \0)</summary>
    public const string Nul = "\u0000";

    /// <summary>Start of Heading (Caret = ^A)</summary>
    public const string Soh = "\u0001";

    /// <summary>Start of Text (Caret = ^B)</summary>
    public const string Stx = "\u0002";

    /// <summary>End of Text (Caret = ^C)</summary>
    public const string Etx = "\u0003";

    /// <summary>End of Transmission (Caret = ^D)</summary>
    public const string Eot = "\u0004";

    /// <summary>Enquiry (Caret = ^E)</summary>
    public const string Enq = "\u0005";

    /// <summary>Acknowledge (Caret = ^F)</summary>
    public const string Ack = "\u0006";

    /// <summary>Bell (Caret = ^G, C = \a)</summary>
    public const string Bel = "\u0007";

    /// <summary>Backspace (Caret = ^H, C = \b)</summary>
    public const string Bs = "\u0008";

    /// <summary>Character Tabulation, Horizontal Tabulation (Caret = ^I, C = \t)</summary>
    public const string Ht = "\u0009";

    /// <summary>Line Feed (Caret = ^J, C = \n)</summary>
    public const string Lf = "\u000a";

    /// <summary>Line Tabulation, Vertical Tabulation (Caret = ^K, C = \v)</summary>
    public const string Vt = "\u000b";

    /// <summary>Form Feed (Caret = ^L, C = \f)</summary>
    public const string Ff = "\u000c";

    /// <summary>Carriage Return (Caret = ^M, C = \r)</summary>
    public const string Cr = "\u000d";

    /// <summary>Shift Out (Caret = ^N)</summary>
    public const string So = "\u000e";

    /// <summary>Shift In (Caret = ^O)</summary>
    public const string Si = "\u000f";

    /// <summary>Data Link Escape (Caret = ^P)</summary>
    public const string Dle = "\u0010";

    /// <summary>Device Control One (XON) (Caret = ^Q)</summary>
    public const string Dc1 = "\u0011";

    /// <summary>Device Control Two (Caret = ^R)</summary>
    public const string Dc2 = "\u0012";

    /// <summary>Device Control Three (XOFF) (Caret = ^S)</summary>
    public const string Dc3 = "\u0013";

    /// <summary>Device Control Four (Caret = ^T)</summary>
    public const string Dc4 = "\u0014";

    /// <summary>Negative Acknowledge (Caret = ^U)</summary>
    public const string Nak = "\u0015";

    /// <summary>Synchronous Idle (Caret = ^V)</summary>
    public const string Syn = "\u0016";

    /// <summary>End of Transmission Block (Caret = ^W)</summary>
    public const string Etb = "\u0017";

    /// <summary>Cancel (Caret = ^X)</summary>
    public const string Can = "\u0018";

    /// <summary>End of Medium (Caret = ^Y)</summary>
    public const string Em = "\u0019";

    /// <summary>Substitute (Caret = ^Z)</summary>
    public const string Sub = "\u001a";

    /// <summary>Escape (Caret = ^[, C = \e)</summary>
    public const string Esc = "\u001b";

    /// <summary>File Separator (Caret = ^\)</summary>
    public const string Fs = "\u001c";

    /// <summary>Group Separator (Caret = ^])</summary>
    public const string Gs = "\u001d";

    /// <summary>Record Separator (Caret = ^^)</summary>
    public const string Rs = "\u001e";

    /// <summary>Unit Separator (Caret = ^_)</summary>
    public const string Us = "\u001f";

    /// <summary>Space</summary>
    public const string Sp = "\u0020";

    /// <summary>Delete (Caret = ^?)</summary>
    public const string Del = "\u007f";
}

/// <summary>C0 control codes as code points.</summary>
/// <remarks>Threading: constants only.</remarks>
internal static class C0Code
{
    /// <summary>The code of <see cref="C0.Nul"/>.</summary>
    public const int Nul = 0x00;

    /// <summary>The code of <see cref="C0.Soh"/>.</summary>
    public const int Soh = 0x01;

    /// <summary>The code of <see cref="C0.Stx"/>.</summary>
    public const int Stx = 0x02;

    /// <summary>The code of <see cref="C0.Etx"/>.</summary>
    public const int Etx = 0x03;

    /// <summary>The code of <see cref="C0.Eot"/>.</summary>
    public const int Eot = 0x04;

    /// <summary>The code of <see cref="C0.Enq"/>.</summary>
    public const int Enq = 0x05;

    /// <summary>The code of <see cref="C0.Ack"/>.</summary>
    public const int Ack = 0x06;

    /// <summary>The code of <see cref="C0.Bel"/>.</summary>
    public const int Bel = 0x07;

    /// <summary>The code of <see cref="C0.Bs"/>.</summary>
    public const int Bs = 0x08;

    /// <summary>The code of <see cref="C0.Ht"/>.</summary>
    public const int Ht = 0x09;

    /// <summary>The code of <see cref="C0.Lf"/>.</summary>
    public const int Lf = 0x0a;

    /// <summary>The code of <see cref="C0.Vt"/>.</summary>
    public const int Vt = 0x0b;

    /// <summary>The code of <see cref="C0.Ff"/>.</summary>
    public const int Ff = 0x0c;

    /// <summary>The code of <see cref="C0.Cr"/>.</summary>
    public const int Cr = 0x0d;

    /// <summary>The code of <see cref="C0.So"/>.</summary>
    public const int So = 0x0e;

    /// <summary>The code of <see cref="C0.Si"/>.</summary>
    public const int Si = 0x0f;

    /// <summary>The code of <see cref="C0.Dle"/>.</summary>
    public const int Dle = 0x10;

    /// <summary>The code of <see cref="C0.Dc1"/>.</summary>
    public const int Dc1 = 0x11;

    /// <summary>The code of <see cref="C0.Dc2"/>.</summary>
    public const int Dc2 = 0x12;

    /// <summary>The code of <see cref="C0.Dc3"/>.</summary>
    public const int Dc3 = 0x13;

    /// <summary>The code of <see cref="C0.Dc4"/>.</summary>
    public const int Dc4 = 0x14;

    /// <summary>The code of <see cref="C0.Nak"/>.</summary>
    public const int Nak = 0x15;

    /// <summary>The code of <see cref="C0.Syn"/>.</summary>
    public const int Syn = 0x16;

    /// <summary>The code of <see cref="C0.Etb"/>.</summary>
    public const int Etb = 0x17;

    /// <summary>The code of <see cref="C0.Can"/>.</summary>
    public const int Can = 0x18;

    /// <summary>The code of <see cref="C0.Em"/>.</summary>
    public const int Em = 0x19;

    /// <summary>The code of <see cref="C0.Sub"/>.</summary>
    public const int Sub = 0x1a;

    /// <summary>The code of <see cref="C0.Esc"/>.</summary>
    public const int Esc = 0x1b;

    /// <summary>The code of <see cref="C0.Fs"/>.</summary>
    public const int Fs = 0x1c;

    /// <summary>The code of <see cref="C0.Gs"/>.</summary>
    public const int Gs = 0x1d;

    /// <summary>The code of <see cref="C0.Rs"/>.</summary>
    public const int Rs = 0x1e;

    /// <summary>The code of <see cref="C0.Us"/>.</summary>
    public const int Us = 0x1f;

    /// <summary>The code of <see cref="C0.Sp"/>.</summary>
    public const int Sp = 0x20;

    /// <summary>The code of <see cref="C0.Del"/>.</summary>
    public const int Del = 0x7f;
}

/// <summary>C1 control codes (https://en.wikipedia.org/wiki/C0_and_C1_control_codes) as strings.</summary>
/// <remarks>Threading: constants only.</remarks>
internal static class C1
{
    /// <summary>padding character</summary>
    public const string Pad = "\u0080";

    /// <summary>High Octet Preset</summary>
    public const string Hop = "\u0081";

    /// <summary>Break Permitted Here</summary>
    public const string Bph = "\u0082";

    /// <summary>No Break Here</summary>
    public const string Nbh = "\u0083";

    /// <summary>Index</summary>
    public const string Ind = "\u0084";

    /// <summary>Next Line</summary>
    public const string Nel = "\u0085";

    /// <summary>Start of Selected Area</summary>
    public const string Ssa = "\u0086";

    /// <summary>End of Selected Area</summary>
    public const string Esa = "\u0087";

    /// <summary>Horizontal Tabulation Set</summary>
    public const string Hts = "\u0088";

    /// <summary>Horizontal Tabulation With Justification</summary>
    public const string Htj = "\u0089";

    /// <summary>Vertical Tabulation Set</summary>
    public const string Vts = "\u008a";

    /// <summary>Partial Line Down</summary>
    public const string Pld = "\u008b";

    /// <summary>Partial Line Up</summary>
    public const string Plu = "\u008c";

    /// <summary>Reverse Index</summary>
    public const string Ri = "\u008d";

    /// <summary>Single-Shift 2</summary>
    public const string Ss2 = "\u008e";

    /// <summary>Single-Shift 3</summary>
    public const string Ss3 = "\u008f";

    /// <summary>Device Control String</summary>
    public const string Dcs = "\u0090";

    /// <summary>Private Use 1</summary>
    public const string Pu1 = "\u0091";

    /// <summary>Private Use 2</summary>
    public const string Pu2 = "\u0092";

    /// <summary>Set Transmit State</summary>
    public const string Sts = "\u0093";

    /// <summary>Destructive backspace, intended to eliminate ambiguity about meaning of BS.</summary>
    public const string Cch = "\u0094";

    /// <summary>Message Waiting</summary>
    public const string Mw = "\u0095";

    /// <summary>Start of Protected Area</summary>
    public const string Spa = "\u0096";

    /// <summary>End of Protected Area</summary>
    public const string Epa = "\u0097";

    /// <summary>Start of String</summary>
    public const string Sos = "\u0098";

    /// <summary>Single Graphic Character Introducer</summary>
    public const string Sgci = "\u0099";

    /// <summary>Single Character Introducer</summary>
    public const string Sci = "\u009a";

    /// <summary>Control Sequence Introducer</summary>
    public const string Csi = "\u009b";

    /// <summary>String Terminator</summary>
    public const string St = "\u009c";

    /// <summary>Operating System Command</summary>
    public const string Osc = "\u009d";

    /// <summary>Privacy Message</summary>
    public const string Pm = "\u009e";

    /// <summary>Application Program Command</summary>
    public const string Apc = "\u009f";
}

/// <summary>C1 control codes as code points.</summary>
/// <remarks>Threading: constants only.</remarks>
internal static class C1Code
{
    /// <summary>The code of <see cref="C1.Pad"/>.</summary>
    public const int Pad = 0x80;

    /// <summary>The code of <see cref="C1.Hop"/>.</summary>
    public const int Hop = 0x81;

    /// <summary>The code of <see cref="C1.Bph"/>.</summary>
    public const int Bph = 0x82;

    /// <summary>The code of <see cref="C1.Nbh"/>.</summary>
    public const int Nbh = 0x83;

    /// <summary>The code of <see cref="C1.Ind"/>.</summary>
    public const int Ind = 0x84;

    /// <summary>The code of <see cref="C1.Nel"/>.</summary>
    public const int Nel = 0x85;

    /// <summary>The code of <see cref="C1.Ssa"/>.</summary>
    public const int Ssa = 0x86;

    /// <summary>The code of <see cref="C1.Esa"/>.</summary>
    public const int Esa = 0x87;

    /// <summary>The code of <see cref="C1.Hts"/>.</summary>
    public const int Hts = 0x88;

    /// <summary>The code of <see cref="C1.Htj"/>.</summary>
    public const int Htj = 0x89;

    /// <summary>The code of <see cref="C1.Vts"/>.</summary>
    public const int Vts = 0x8a;

    /// <summary>The code of <see cref="C1.Pld"/>.</summary>
    public const int Pld = 0x8b;

    /// <summary>The code of <see cref="C1.Plu"/>.</summary>
    public const int Plu = 0x8c;

    /// <summary>The code of <see cref="C1.Ri"/>.</summary>
    public const int Ri = 0x8d;

    /// <summary>The code of <see cref="C1.Ss2"/>.</summary>
    public const int Ss2 = 0x8e;

    /// <summary>The code of <see cref="C1.Ss3"/>.</summary>
    public const int Ss3 = 0x8f;

    /// <summary>The code of <see cref="C1.Dcs"/>.</summary>
    public const int Dcs = 0x90;

    /// <summary>The code of <see cref="C1.Pu1"/>.</summary>
    public const int Pu1 = 0x91;

    /// <summary>The code of <see cref="C1.Pu2"/>.</summary>
    public const int Pu2 = 0x92;

    /// <summary>The code of <see cref="C1.Sts"/>.</summary>
    public const int Sts = 0x93;

    /// <summary>The code of <see cref="C1.Cch"/>.</summary>
    public const int Cch = 0x94;

    /// <summary>The code of <see cref="C1.Mw"/>.</summary>
    public const int Mw = 0x95;

    /// <summary>The code of <see cref="C1.Spa"/>.</summary>
    public const int Spa = 0x96;

    /// <summary>The code of <see cref="C1.Epa"/>.</summary>
    public const int Epa = 0x97;

    /// <summary>The code of <see cref="C1.Sos"/>.</summary>
    public const int Sos = 0x98;

    /// <summary>The code of <see cref="C1.Sgci"/>.</summary>
    public const int Sgci = 0x99;

    /// <summary>The code of <see cref="C1.Sci"/>.</summary>
    public const int Sci = 0x9a;

    /// <summary>The code of <see cref="C1.Csi"/>.</summary>
    public const int Csi = 0x9b;

    /// <summary>The code of <see cref="C1.St"/>.</summary>
    public const int St = 0x9c;

    /// <summary>The code of <see cref="C1.Osc"/>.</summary>
    public const int Osc = 0x9d;

    /// <summary>The code of <see cref="C1.Pm"/>.</summary>
    public const int Pm = 0x9e;

    /// <summary>The code of <see cref="C1.Apc"/>.</summary>
    public const int Apc = 0x9f;
}

/// <summary>Escaped C1 control codes (the 7-bit forms).</summary>
/// <remarks>Threading: constants only.</remarks>
internal static class C1Escaped
{
    /// <summary>String Terminator, 7-bit form: ESC followed by a backslash.</summary>
    public const string St = C0.Esc + "\\";
}
