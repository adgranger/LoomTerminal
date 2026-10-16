// Ported from xtermjs/xterm.js: src/common/input/TextDecoder.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Text;
using LoomTerminal.Core.Input;

namespace LoomTerminal.Core.Tests.Input;

public class TextDecoderTests
{
    private static readonly string[] TestStrings =
    [
        "Лорем ипсум долор сит амет, ех сеа аццусам диссентиет. Ан еос стет еирмод витуперата. Иус дицерет урбанитас ет. Ан при алтера долорес сплендиде, цу яуо интегре денияуе, игнота волуптариа инструцтиор цу вим.",
        "ლორემ იფსუმ დოლორ სით ამეთ, ფაცერ მუციუს ცონსეთეთურ ყუო იდ, ფერ ვივენდუმ ყუაერენდუმ ეა, ესთ ამეთ მოვეთ სუავითათე ცუ. ვითაე სენსიბუს ან ვიხ. ეხერცი დეთერრუისსეთ უთ ყუი. ვოცენთ დებითის ადიფისცი ეთ ფერ. ნეც ან ფეუგაით ფორენსიბუს ინთერესსეთ. იდ დიცო რიდენს იუს. დისსენთიეთ ცონსეყუუნთურ სედ ნე, ნოვუმ მუნერე ეუმ ათ, ნე ეუმ ნიჰილ ირაცუნდია ურბანითას.",
        "अधिकांश अमितकुमार प्रोत्साहित मुख्य जाने प्रसारन विश्लेषण विश्व दारी अनुवादक अधिकांश नवंबर विषय गटकउसि गोपनीयता विकास जनित परस्पर गटकउसि अन्तरराष्ट्रीयकरन होसके मानव पुर्णता कम्प्युटर यन्त्रालय प्रति साधन",
        "覧六子当聞社計文護行情投身斗来。増落世的況上席備界先関権能万。本物挙歯乳全事携供板栃果以。頭月患端撤競見界記引去法条公泊候。決海備駆取品目芸方用朝示上用報。講申務紙約週堂出応理田流団幸稿。起保帯吉対阜庭支肯豪彰属本躍。量抑熊事府募動極都掲仮読岸。自続工就断庫指北速配鳴約事新住米信中験。婚浜袋著金市生交保他取情距。",
        "八メル務問へふらく博辞説いわょ読全タヨムケ東校どっ知壁テケ禁去フミ人過を装5階がねぜ法逆はじ端40落ミ予竹マヘナセ任1悪た。省ぜりせ製暇ょへそけ風井イ劣手はぼまず郵富法く作断タオイ取座ゅょが出作ホシ月給26島ツチ皇面ユトクイ暮犯リワナヤ断連こうでつ蔭柔薄とレにの。演めけふぱ損田転10得観びトげぎ王物鉄夜がまけ理惜くち牡提づ車惑参ヘカユモ長臓超漫ぼドかわ。",
        "모든 국민은 행위시의 법률에 의하여 범죄를 구성하지 아니하는 행위로 소추되지 아니하며. 전직대통령의 신분과 예우에 관하여는 법률로 정한다, 국회는 헌법 또는 법률에 특별한 규정이 없는 한 재적의원 과반수의 출석과 출석의원 과반수의 찬성으로 의결한다. 군인·군무원·경찰공무원 기타 법률이 정하는 자가 전투·훈련등 직무집행과 관련하여 받은 손해에 대하여는 법률이 정하는 보상외에 국가 또는 공공단체에 공무원의 직무상 불법행위로 인한 배상은 청구할 수 없다.",
        "كان فشكّل الشرقي مع, واحدة للمجهود تزامناً بعض بل. وتم جنوب للصين غينيا لم, ان وبدون وكسبت الأمور ذلك, أسر الخاسر الانجليزية هو. نفس لغزو مواقعها هو. الجو علاقة الصعداء انه أي, كما مع بمباركة للإتحاد الوزراء. ترتيب الأولى أن حدى, الشتوية باستحداث مدن بل, كان قد أوسع عملية. الأوضاع بالمطالبة كل قام, دون إذ شمال الربيع،. هُزم الخاصّة ٣٠ أما, مايو الصينية مع قبل.",
        "או סדר החול מיזמי קרימינולוגיה. קהילה בגרסה לויקיפדים אל היא, של צעד ציור ואלקטרוניקה. מדע מה ברית המזנון ארכיאולוגיה, אל טבלאות מבוקשים כלל. מאמרשיחהצפה העריכהגירסאות שכל אל, כתב עיצוב מושגי של. קבלו קלאסיים ב מתן. נבחרים אווירונאוטיקה אם מלא, לוח למנוע ארכיאולוגיה מה. ארץ לערוך בקרבת מונחונים או, עזרה רקטות לויקיפדים אחר גם.",
        "Лорем ლორემ अधिकांश 覧六子 八メル 모든 בקרבת 💮 😂 äggg 123€ 𝄞.",
    ];

    // Converts UTF-32 code points to a string (String.fromCodePoint in the xterm.js test).
    private static string ToText(uint[] data, int length)
    {
        StringBuilder result = new();
        for (int i = 0; i < length; ++i)
        {
            result.Append(char.ConvertFromUtf32((int)data[i]));
        }

        return result.ToString();
    }

    private static byte[] ToUtf8(string text) => Encoding.UTF8.GetBytes(text);

    // Decodes the data in chunks of the given size and returns the decoded text.
    private static string DecodeInChunks(byte[] utf8Data, int chunkSize)
    {
        Utf8ToUtf32 decoder = new();
        uint[] target = new uint[5];
        StringBuilder decoded = new();
        for (int i = 0; i < utf8Data.Length; i += chunkSize)
        {
            int written = decoder.Decode(utf8Data.AsSpan(i, Math.Min(chunkSize, utf8Data.Length - i)), target);
            decoded.Append(ToText(target, written));
        }

        return decoded.ToString();
    }

    private static byte[] ByteString(string s) => [.. s.Select(c => (byte)c)];

    // xterm.js: 'stringFromCodePoint/utf32ToString'
    [Fact]
    public void StringFromCodePoint_and_Utf32ToString_convert_code_points()
    {
        const string s = "abcdefg";
        uint[] data = new uint[s.Length];
        for (int i = 0; i < s.Length; ++i)
        {
            data[i] = s[i];
            Assert.Equal(s[i].ToString(), TextDecoder.StringFromCodePoint(data[i]));
        }

        Assert.Equal(s, TextDecoder.Utf32ToString(data));
    }

    // xterm.js: 'StringToUtf32 decoder' › 'full codepoint test' › 'min..max' (BMP)
    [Fact]
    public void StringToUtf32_decodes_every_BMP_code_point()
    {
        StringToUtf32 decoder = new();
        uint[] target = new uint[5];
        for (int i = 0; i < 65536; ++i)
        {
            // skip surrogate pairs and a BOM
            if ((i >= 0xD800 && i <= 0xDFFF) || i == 0xFEFF)
            {
                continue;
            }

            int length = decoder.Decode(((char)i).ToString(), target);
            Assert.Equal(1, length);
            Assert.Equal((uint)i, target[0]);
            Assert.Equal(((char)i).ToString(), TextDecoder.Utf32ToString(target, 0, length));
            decoder.Clear();
        }
    }

    // xterm.js: 'StringToUtf32 decoder' › 'full codepoint test' › 'min..max (surrogates)'
    [Fact]
    public void StringToUtf32_decodes_every_surrogate_pair()
    {
        StringToUtf32 decoder = new();
        uint[] target = new uint[5];
        for (int i = 65536; i < 0x10FFFF; ++i)
        {
            int codePoint = i - 0x10000;
            string s = new([(char)((codePoint >> 10) + 0xD800), (char)((codePoint % 0x400) + 0xDC00)]);
            int length = decoder.Decode(s, target);
            Assert.Equal(1, length);
            Assert.Equal((uint)i, target[0]);
            Assert.Equal(s, TextDecoder.Utf32ToString(target, 0, length));
            decoder.Clear();
        }
    }

    // xterm.js: 'StringToUtf32 decoder' › 'full codepoint test' › '0xFEFF(BOM)'
    [Fact]
    public void StringToUtf32_drops_the_BOM()
    {
        StringToUtf32 decoder = new();
        uint[] target = new uint[5];
        int length = decoder.Decode(((char)0xFEFF).ToString(), target);
        Assert.Equal(0, length);
        decoder.Clear();
    }

    // xterm.js: 'StringToUtf32 decoder' › 'test strings'
    [Fact]
    public void StringToUtf32_decodes_the_test_strings()
    {
        StringToUtf32 decoder = new();
        uint[] target = new uint[500];
        foreach (string testString in TestStrings)
        {
            int length = decoder.Decode(testString, target);
            Assert.Equal(testString, ToText(target, length));
            decoder.Clear();
        }
    }

    // xterm.js: 'StringToUtf32 decoder' › 'stream handling' › 'surrogates mixed advance by 1'
    // (the upstream test uses assert(decoded, ...), which only checks truthiness; this one checks the value)
    [Fact]
    public void StringToUtf32_handles_surrogates_mixed_advance_by_1()
    {
        StringToUtf32 decoder = new();
        uint[] target = new uint[5];
        const string input = "Ä€𝄞Ö𝄞€Ü𝄞€";
        StringBuilder decoded = new();
        foreach (char c in input)
        {
            int written = decoder.Decode(c.ToString(), target);
            decoded.Append(ToText(target, written));
        }

        Assert.Equal(input, decoded.ToString());
    }

    // xterm.js: 'Utf8ToUtf32 decoder' › 'full codepoint test' › 'min..max (1/2/3 byte sequences)'
    [Fact]
    public void Utf8ToUtf32_decodes_every_1_2_3_byte_sequence()
    {
        Utf8ToUtf32 decoder = new();
        uint[] target = new uint[5];
        for (int i = 0; i < 65536; ++i)
        {
            // skip surrogate pairs and a BOM
            if ((i >= 0xD800 && i <= 0xDFFF) || i == 0xFEFF)
            {
                continue;
            }

            int length = decoder.Decode(ToUtf8(((char)i).ToString()), target);
            Assert.Equal(1, length);
            Assert.Equal(((char)i).ToString(), ToText(target, length));
            decoder.Clear();
        }
    }

    // xterm.js: 'Utf8ToUtf32 decoder' › 'full codepoint test' › 'min..max (4 byte sequences)'
    [Fact]
    public void Utf8ToUtf32_decodes_every_4_byte_sequence()
    {
        Utf8ToUtf32 decoder = new();
        uint[] target = new uint[5];
        for (int i = 65536; i < 0x10FFFF; ++i)
        {
            int length = decoder.Decode(ToUtf8(char.ConvertFromUtf32(i)), target);
            Assert.Equal(1, length);
            Assert.Equal((uint)i, target[0]);
            decoder.Clear();
        }
    }

    // xterm.js: 'Utf8ToUtf32 decoder' › 'full codepoint test' › '0xFEFF(BOM)'
    [Fact]
    public void Utf8ToUtf32_drops_the_BOM()
    {
        Utf8ToUtf32 decoder = new();
        uint[] target = new uint[5];
        int length = decoder.Decode(ToUtf8(((char)0xFEFF).ToString()), target);
        Assert.Equal(0, length);
        decoder.Clear();
    }

    // xterm.js: 'Utf8ToUtf32 decoder' › 'test strings'
    [Fact]
    public void Utf8ToUtf32_decodes_the_test_strings()
    {
        Utf8ToUtf32 decoder = new();
        uint[] target = new uint[500];
        foreach (string testString in TestStrings)
        {
            // The longest test string is shorter than 500 code points, but its UTF-8 is not: decode in one chunk.
            int length = decoder.Decode(ToUtf8(testString), target);
            Assert.Equal(testString, ToText(target, length));
            decoder.Clear();
        }
    }

    // xterm.js: '2 byte sequences - advance by 1'
    [Fact]
    public void Utf8ToUtf32_handles_2_byte_sequences_advance_by_1()
    {
        byte[] utf8Data = ByteString("Ã\u0084Ã\u0096Ã\u009cÃ\u009fÃ¶Ã¤Ã¼");
        Assert.Equal("ÄÖÜßöäü", DecodeInChunks(utf8Data, 1));
    }

    // xterm.js: '2/3 byte sequences - advance by 1'
    [Fact]
    public void Utf8ToUtf32_handles_2_3_byte_sequences_advance_by_1()
    {
        byte[] utf8Data = ToUtf8("Ä€Ö€Ü€ß€ö€ä€ü");
        Assert.Equal("Ä€Ö€Ü€ß€ö€ä€ü", DecodeInChunks(utf8Data, 1));
    }

    // xterm.js: '2/3/4 byte sequences - advance by 1'
    [Fact]
    public void Utf8ToUtf32_handles_2_3_4_byte_sequences_advance_by_1()
    {
        byte[] utf8Data = ToUtf8("Ä€𝄞Ö𝄞€Ü𝄞€");
        Assert.Equal("Ä€𝄞Ö𝄞€Ü𝄞€", DecodeInChunks(utf8Data, 1));
    }

    // xterm.js: '2/3/4 byte sequences - advance by 2'
    [Fact]
    public void Utf8ToUtf32_handles_2_3_4_byte_sequences_advance_by_2()
    {
        byte[] utf8Data = ToUtf8("Ä€𝄞Ö𝄞€Ü𝄞€");
        Assert.Equal("Ä€𝄞Ö𝄞€Ü𝄞€", DecodeInChunks(utf8Data, 2));
    }

    // xterm.js: '2/3/4 byte sequences - advance by 3'
    [Fact]
    public void Utf8ToUtf32_handles_2_3_4_byte_sequences_advance_by_3()
    {
        byte[] utf8Data = ToUtf8("Ä€𝄞Ö𝄞€Ü𝄞€");
        Assert.Equal("Ä€𝄞Ö𝄞€Ü𝄞€", DecodeInChunks(utf8Data, 3));
    }

    // xterm.js: 'BOMs (3 byte sequences) - advance by 2'
    [Fact]
    public void Utf8ToUtf32_drops_BOMs_advance_by_2()
    {
        byte[] utf8Data = [0xEF, 0xBB, 0xBF, 0xEF, 0xBB, 0xBF];
        Assert.Equal(string.Empty, DecodeInChunks(utf8Data, 2));
    }

    // xterm.js: 'test break after 3 bytes - issue #2495'
    [Fact]
    public void Utf8ToUtf32_handles_a_break_after_3_bytes()
    {
        Utf8ToUtf32 decoder = new();
        uint[] target = new uint[5];
        byte[] utf8Data = [0xF0, 0xA0, 0x9C, 0x8E];
        int written = decoder.Decode(utf8Data.AsSpan(0, 3), target);
        Assert.Equal(0, written);
        written = decoder.Decode(utf8Data.AsSpan(3), target);
        Assert.Equal(1, written);
        Assert.Equal("𠜎", ToText(target, written));
    }
}
