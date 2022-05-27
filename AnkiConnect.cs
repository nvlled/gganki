
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
	public enum ContentType { Vocab, Example }

	public string? answer { get; set; }
	public ulong cardId { get; set; }
	public string? css { get; set; }
	public string? deckName { get; set; }
	public int due { get; set; }
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
	public string? GetVocab()
	{
		return GetField("VocabKanji");
	}

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

	public static async Task<AnkiConnectResponse<DeckNames>> FetchDecks()
	{
		var resp = await Post(new AnkiConnectRequest("deckNamesAndIds"));
		var json = await resp.Content.ReadAsStringAsync();
		var data = JsonSerializer.Deserialize<AnkiConnectResponse<DeckNames>>(json);
		return WrapNullError(data);
	}
	public static async Task<AnkiConnectResponse<ulong[]>> SearchCards(string deckName, string query)
	{
		var queryParam = string.Format("\"deck:{0}\" ({1})", deckName, query);
		var resp = await Post(new AnkiConnectRequest("findCards", new
		{
			query = queryParam
		}));
		var json = await resp.Content.ReadAsStringAsync();
		var data = JsonSerializer.Deserialize<AnkiConnectResponse<ulong[]>>(json);

		return WrapNullError(data);
	}
	public static async Task<AnkiConnectResponse<CardInfo[]>> FetchAvailableCards(string deckName)
	{
		var idResp = await FetchAvailableCardIds(deckName);
		if (idResp.error != null)
		{
			return AnkiConnectResponse<CardInfo[]>.Error(idResp.error);
		}
		var cardResp = await AnkiConnect.FetchCardInfo(idResp.value ?? new ulong[0]);
		return cardResp;
	}

	public static async Task<AnkiConnectResponse<ulong[]>> FetchAvailableCardIds(string deckName)
	{
		return await SearchCards(deckName, "is:review OR is:learn");
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