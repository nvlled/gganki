
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

/*
{
    "action": "findCards",
    "version": 6,
    "params": {
        "query": "deck:current"
    }

    {
	"action": "deckNamesAndIds",
	"version": 6
     }

    (is:new OR is:review OR is:learn)
}
*/


public class AnkiConfig
{

    public const string Hostname = "localhost";
    public const uint Port = 8765;

    public const int Version = 6;

}

public class AnkiConnectRequest
{

    public string action { get; set; }
    public int version { get; set; } = AnkiConfig.Version;
    public object @params { get; set; } = new { };

    public AnkiConnectRequest(string action, object? parameters = null)
    {
        this.action = action;
        this.version = version;
        if (parameters != null)
        {
            @params = parameters;
        }
    }
}

public class AnkiConnectResponse<T>
{
    public string? error { get; set; }

    [JsonPropertyName("result")]
    public T? value { get; set; }

    public T Unwrap()
    {
        if (!string.IsNullOrEmpty(error))
        {
            throw new Exception(error);
        }
        return value;
    }

    public static AnkiConnectResponse<T> Ok(T result)
    {
        var resp = new AnkiConnectResponse<T>();
        resp.value = result;
        return resp;
    }
    public static AnkiConnectResponse<T> Error(string err)
    {
        var resp = new AnkiConnectResponse<T>();
        resp.error = err;
        return resp;
    }
}


public record class CardInfoFieldEntry
{
    public int order { get; set; }
    public string? value { get; set; }
}

public record class CardInfoFields
{

}


public class DeckNames : Dictionary<string, ulong> { }

public record class CardInfo
{

    public bool IsPlayed { get; set; }
    public bool IsNew { get; set; }


    // ---------------

    public enum ContentType { Vocab, Example }

    public string? answer { get; set; }
    public ulong cardId { get; set; }
    public string? css { get; set; }
    public string? deckName { get; set; }
    public long due { get; set; }
    public int factor { get; set; }
    public int fieldOrder { get; set; }

    public int interval { get; set; }
    public int lapses { get; set; }
    public int left { get; set; }
    public ulong mod { get; set; }
    public string? modelName { get; set; }
    public ulong note { get; set; }
    public int ord { get; set; }
    public string? question { get; set; }
    public int queue { get; set; }
    public int reps { get; set; }
    public int type { get; set; }

    public Dictionary<string, CardInfoFieldEntry>? fields { get; set; }


    public bool HasExample()
    {
        return fields != null && fields.ContainsKey("SentKanji");
    }

    public bool HasField(string name)
    {
        return fields != null && fields.ContainsKey(name);
    }

    public string? GetExample()
    {
        return GetField("SentKanji");
    }
    public string? GetVocab() { return GetField("VocabKanji"); }
    public string? GetVocabDef() { return GetField("VocabDef"); }

    public string? GetField(string name, string? defaultValue = null)
    {
        if (fields != null && fields.ContainsKey(name))
        {
            return fields[name].value ?? defaultValue;
        }
        return defaultValue;
    }

    public (string?, string?) GetContents(ContentType type)
    {
        var text = type == ContentType.Example ? GetExample() : GetVocab();
        var audio = type == ContentType.Example ? GetField("SentAudio") : GetField("VocabAudio");
        return (text, audio);
    }

    public override string ToString()
    {
        var self = (CardInfo)this.MemberwiseClone();
        self.answer = self.answer?[0..25];
        self.question = self.question?[0..25];
        self.css = self.css?[0..25];
        return DumpVar(self);
    }

    public bool IsDue()
    {
        var dueTime = DateTimeOffset.FromUnixTimeSeconds(due);
        return DateTimeOffset.UtcNow >= dueTime;
    }
}

namespace DeckType
{
    using System.Text.Json.Serialization;

    public partial class DeckConfig
    {
        [JsonPropertyName("lapse")]
        public Lapse Lapse { get; set; }

        [JsonPropertyName("dyn")]
        public bool Dyn { get; set; }

        [JsonPropertyName("autoplay")]
        public bool Autoplay { get; set; }

        [JsonPropertyName("mod")]
        public long Mod { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("maxTaken")]
        public long MaxTaken { get; set; }

        [JsonPropertyName("new")]
        public New New { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("rev")]
        public Rev Rev { get; set; }

        [JsonPropertyName("timer")]
        public long Timer { get; set; }

        [JsonPropertyName("replayq")]
        public bool Replayq { get; set; }

        [JsonPropertyName("usn")]
        public long Usn { get; set; }

        [JsonPropertyName("newMix")]
        public long? NewMix { get; set; }

        [JsonPropertyName("newPerDayMinimum")]
        public long? NewPerDayMinimum { get; set; }

        [JsonPropertyName("interdayLearningMix")]
        public long? InterdayLearningMix { get; set; }

        [JsonPropertyName("reviewOrder")]
        public long? ReviewOrder { get; set; }

        [JsonPropertyName("newSortOrder")]
        public long? NewSortOrder { get; set; }

        [JsonPropertyName("newGatherPriority")]
        public long? NewGatherPriority { get; set; }
    }

    public partial class Lapse
    {
        [JsonPropertyName("leechFails")]
        public long LeechFails { get; set; }

        [JsonPropertyName("delays")]
        public double[] Delays { get; set; }

        [JsonPropertyName("minInt")]
        public long MinInt { get; set; }

        [JsonPropertyName("leechAction")]
        public long LeechAction { get; set; }

        [JsonPropertyName("mult")]
        public double Mult { get; set; }
    }

    public partial class New
    {
        [JsonPropertyName("bury")]
        public bool Bury { get; set; }

        [JsonPropertyName("order")]
        public long Order { get; set; }

        [JsonPropertyName("initialFactor")]
        public long InitialFactor { get; set; }

        [JsonPropertyName("perDay")]
        public long PerDay { get; set; }

        [JsonPropertyName("delays")]
        public double[] Delays { get; set; }

        [JsonPropertyName("separate")]
        public bool Separate { get; set; }

        [JsonPropertyName("ints")]
        public long[] Ints { get; set; }
    }

    public partial class Rev
    {
        [JsonPropertyName("bury")]
        public bool Bury { get; set; }

        [JsonPropertyName("ivlFct")]
        public double IvlFct { get; set; }

        [JsonPropertyName("ease4")]
        public double Ease4 { get; set; }

        [JsonPropertyName("maxIvl")]
        public long MaxIvl { get; set; }

        [JsonPropertyName("perDay")]
        public long PerDay { get; set; }

        [JsonPropertyName("minSpace")]
        public long MinSpace { get; set; }

        [JsonPropertyName("fuzz")]
        public double Fuzz { get; set; }

        [JsonPropertyName("hardFactor")]
        public double? HardFactor { get; set; }
    }
}



public class AnkiConnect
{
    public static HttpClient client = new HttpClient();


    static string GetURL()
    {

        return string.Format("http://{0}:{1}", AnkiConfig.Hostname, AnkiConfig.Port);
    }

    static AnkiConnectResponse<T> WrapNullError<T>(AnkiConnectResponse<T>? resp)
    {
        return resp != null ? resp : AnkiConnectResponse<T>.Error("received a null");
    }

    public static async Task<HttpResponseMessage> Post(AnkiConnectRequest reqBody)
    {
        var reqJson = JsonSerializer.Serialize(reqBody);
        var content = new StringContent(reqJson, Encoding.UTF8, "application/json");
        return await client.PostAsync(GetURL(), content);
    }
    public static async Task<string> FetchJSON(string action, object? args = null)
    {
        var resp = await Post(new AnkiConnectRequest(action, args));
        var json = await resp.Content.ReadAsStringAsync();
        return json;
    }

    public static async Task<AnkiConnectResponse<DeckType.DeckConfig>> FetchDeckConfig(string deckName)
    {
        var resp = await Post(new AnkiConnectRequest("getDeckConfig", new
        {
            deck = deckName
        }));

        var json = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<AnkiConnectResponse<DeckType.DeckConfig>>(json);
        return WrapNullError(data);
    }

    public static async Task<AnkiConnectResponse<DeckNames>> FetchDecks()
    {
        var resp = await Post(new AnkiConnectRequest("deckNamesAndIds"));
        var json = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<AnkiConnectResponse<DeckNames>>(json);
        return WrapNullError(data);
    }
    public static async Task<AnkiConnectResponse<ulong[]>> SearchCardIds(string deckName, string query)
    {
        var queryParam = string.Format("\"deck:{0}\" {1}", deckName, query.Length > 0 ? $"({query})" : "");
        var resp = await Post(new AnkiConnectRequest("findCards", new
        {
            query = queryParam
        }));
        var json = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<AnkiConnectResponse<ulong[]>>(json);

        return WrapNullError(data);
    }
    public static async Task<AnkiConnectResponse<CardInfo[]>> FetchAvailableCards(string deckName, int limit)
    {
        return await FetchCards(deckName, "is:due", limit);
    }
    public static async Task<AnkiConnectResponse<CardInfo[]>> FetchNewCards(string deckName, int limit)
    {
        return await FetchCards(deckName, "is:new -is:learn", limit);
    }

    public static async Task<AnkiConnectResponse<CardInfo[]>> FetchCards(string deckName, string filter, int limit)
    {
        var idResp = await SearchCardIds(deckName, filter);
        if (idResp.error != null)
        {
            return AnkiConnectResponse<CardInfo[]>.Error(idResp.error);
        }
        var ids = idResp.value ?? new ulong[0];
        ids = ids[0..limit];

        var cardResp = await AnkiConnect.FetchCardInfo(ids);
        return cardResp;
    }

    public static async Task<AnkiConnectResponse<ulong[]>> FetchAvailableCardIds(string deckName)
    {
        return await SearchCardIds(deckName, "is:due");
    }

    public static async Task<AnkiConnectResponse<ulong[]>> FetchAllCardIds(string deckName)
    {
        return await SearchCardIds(deckName, "");
    }

    public static async Task<AnkiConnectResponse<CardInfo[]>> FetchAllCards(string deckName)
    {
        var idResp = await FetchAllCardIds(deckName);
        if (idResp.error != null)
        {
            return AnkiConnectResponse<CardInfo[]>.Error(idResp.error);
        }
        var cardResp = await AnkiConnect.FetchCardInfo(idResp.value ?? new ulong[0]);
        return cardResp;
    }

    public static async Task<AnkiConnectResponse<CardInfo[]>> FetchCardInfo(params ulong[] ids)
    {
        var reqBody = new AnkiConnectRequest("cardsInfo", new
        {
            cards = ids
        });
        var resp = await Post(reqBody);
        var json = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<AnkiConnectResponse<CardInfo[]>>(json);

        return WrapNullError(data);
    }

    public static async Task<AnkiConnectResponse<bool[]>> AnswerCard(ulong cardId, AnkiButton button)
    {
        var reqBody = new AnkiConnectRequest("answerCards", new
        {
            cards = new ulong[] { cardId },
            buttons = new int[] { (int)button },
        });
        var resp = await Post(reqBody);
        var json = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<AnkiConnectResponse<bool[]>>(json);

        return WrapNullError(data);
    }
    public static async Task<AnkiConnectResponse<bool[]>> AnswerCards((ulong cardId, AnkiButton button)[] args)
    {
        var reqBody = new AnkiConnectRequest("answerCards", new
        {
            cards = args.Select(x => x.cardId),
            buttons = args.Select(x => (int)x.button),
        });
        var resp = await Post(reqBody);
        var json = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<AnkiConnectResponse<bool[]>>(json);

        return WrapNullError(data);
    }

    public static async Task<AnkiConnectResponse<string>> GetMedia(string ankiFile)
    {
        var reqBody = new AnkiConnectRequest("retrieveMediaFile", new
        {
            filename = ankiFile
        });
        var resp = await Post(reqBody);
        var json = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<AnkiConnectResponse<string>>(json);

        return WrapNullError(data);
    }
}



public enum AnkiButton
{
    Again = 1,
    Hard = 2,
    Good = 3,
    Easy = 4,
}