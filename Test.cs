

using Xunit;

public class Test
{
	[Fact]
	public async void TestFetchDecks()
	{
		var deckResp = await AnkiConnect.FetchDecks();
		Assert.NotNull(deckResp);

		if (deckResp?.value?.Count() == 0)
		{
			Console.WriteLine("no decks available");
			return;
		}

		var deckName = deckResp?.value?.Keys.First() ?? "";
		deckName = "AJT Kanji Transition TSC";

		Console.WriteLine("using deck: " + deckName);
		var cardIdResp = await AnkiConnect.FetchAvailableCardIds(deckName);
		Assert.NotNull(cardIdResp);
		Assert.Null(cardIdResp.error);
		Assert.NotEmpty(cardIdResp.value);


		var cardResp = await AnkiConnect.FetchCardInfo(cardIdResp?.value?[1] ?? 0);
		Assert.NotNull(cardResp);
		Assert.NotEmpty(cardResp.value);
		Console.WriteLine(cardResp?.value?[0].cardId);
	}

	[Fact]
	public async void TestFetchCardInfo()
	{
		var resp = await AnkiConnect.FetchCardInfo(1614446068564);
		Assert.NotNull(resp);
		Assert.Null(resp.error);
		Assert.NotEmpty(resp.value);
	}

}
